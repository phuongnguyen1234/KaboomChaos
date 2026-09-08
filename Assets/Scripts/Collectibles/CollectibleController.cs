using UnityEngine;
using Core.Interfaces;
using Core;
using System;
using System.Collections.Generic;
using Collectibles.Behaviors;
using Collectibles.Data;
using DG.Tweening;

namespace Collectibles
{
    [RequireComponent(typeof(Rigidbody))]
    public class CollectibleController : MonoBehaviour, ICollectibleController, IExplosionReactable
    {
        private ICollectibleData _data;
        private ICollectibleBehavior _behavior;

        private static readonly Dictionary<Type, Func<ICollectibleBehavior>> _behaviorFactory = new()
        {
            { typeof(CoinData), () => new CoinBehavior() },
            { typeof(HealingCollectibleData), () => new HealingBehavior() },
            { typeof(ShieldCollectibleData), () => new ShieldBehavior() },
            { typeof(BatteryCollectibleData), () => new BatteryBehavior() },
        };
        private Coroutine _lifespanCoroutine;

        [Header("Components")]
        [Tooltip("Collider của vật phẩm. Nếu để trống, sẽ tự động lấy từ GameObject này.")]
        [SerializeField] private Collider _collider;

        // Cached components
        private Rigidbody _rigidbody;
        private Camera _mainCamera;
        private Vector3 _initialArrowLocalPosition;
        private bool _isCollected; // Guards against double-collection (double heal/coin) when colliders overlap.

        [Header("Visuals")]
        [Tooltip("Renderer chính của vật phẩm, dùng cho các hiệu ứng hình ảnh như nhấp nháy.")]
        [SerializeField] protected Renderer _mainRenderer;

        [Tooltip("Đối tượng mũi tên chỉ báo, sẽ tự động nảy và xoay về phía camera.")]
        [SerializeField] protected GameObject _indicatorArrow;

        public GameObject GameObject => gameObject;
        public ICollectibleData Data => _data;

        protected virtual void Awake()
        {
            if (_collider == null) _collider = GetComponent<Collider>();
            _rigidbody = GetComponent<Rigidbody>();
            _mainCamera = Camera.main;
            if (_indicatorArrow != null)
            {
                _initialArrowLocalPosition = _indicatorArrow.transform.localPosition;
            }
        }

        protected virtual void OnEnable()
        {
            if (_indicatorArrow != null)
            {
                _indicatorArrow.transform.DOLocalMoveY(_initialArrowLocalPosition.y + 0.25f, 1f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        }

        public virtual void Initialize(ICollectibleData data)
        {
            _data = data;
            _isCollected = false; // Reset guard khi vật phẩm được re-initialize từ pool.
            if (_data.Lifespan > 0)
            {
                if (_lifespanCoroutine != null) StopCoroutine(_lifespanCoroutine);
                _lifespanCoroutine = StartCoroutine(LifespanRoutine(_data.Lifespan));
            }

            // Create behavior from factory
            if (_behaviorFactory.TryGetValue(_data.GetType(), out var factoryFunc))
            {
                _behavior = factoryFunc();
            }
            else
            {
                Debug.LogError($"[CollectibleController] No behavior factory for collectible data type {_data.GetType()}", this);
            }
        }

        private System.Collections.IEnumerator LifespanRoutine(float duration)
        {
            if (_mainRenderer == null)
            {
                // Nếu không có renderer, chỉ chờ và despawn
                yield return new WaitForSeconds(duration);
                GameEvents.TriggerCollectibleDespawnRequest(gameObject);
                yield break;
            }

            // Chờ 75% thời gian sống mà không nhấp nháy
            float initialWaitDuration = duration * 0.75f;
            yield return new WaitForSeconds(initialWaitDuration);

            // Thời gian còn lại sẽ dùng để nhấp nháy
            float remainingDuration = duration - initialWaitDuration;
            float lifeTimer = remainingDuration;
            float flashTimer = 0f;
            bool isVisible = true;

            while (lifeTimer > 0)
            {
                lifeTimer -= Time.deltaTime;

                // Tính toán khoảng thời gian nhấp nháy dựa trên % thời gian còn lại trong giai đoạn nhấp nháy
                float flashingPhasePercentage = Mathf.Clamp01(lifeTimer / remainingDuration);
                float maxInterval = 1f;
                float minInterval = 0.5f;
                float currentFlashInterval = Mathf.Lerp(minInterval, maxInterval, flashingPhasePercentage * flashingPhasePercentage * 0.25f); // Dùng bình phương để hiệu ứng nhanh dần ở cuối

                flashTimer -= Time.deltaTime;
                if (flashTimer <= 0)
                {
                    isVisible = !isVisible;
                    _mainRenderer.enabled = isVisible;
                    flashTimer = currentFlashInterval;
                }

                yield return null;
            }

            // Hết thời gian sống
            _mainRenderer.enabled = false;
            GameEvents.TriggerCollectibleDespawnRequest(gameObject);
        }

        protected void OnTriggerEnter(Collider other)
        {
            // Prevent collection while the collectible is already being collected/despawning.
            // A pooled collectible can have several colliders touching the player in the same
            // frame, causing OnTriggerEnter (and thus heal/coin) to run more than once.
            if (_isCollected) return;

            // Check if the collector is a player
            IPlayer player = other.GetComponentInParent<IPlayer>();
            if (player != null)
            {
                OnCollect(player);
            }
        }

        protected void LateUpdate()
        {
            // Làm cho mũi tên luôn xoay về phía camera (billboarding)
            if (_indicatorArrow != null && _indicatorArrow.activeSelf && _mainCamera != null)
            {
                // SỬA LỖI: Gán trực tiếp rotation của camera sẽ làm mũi tên bị nghiêng và "dãn" (stretch)
                // khi camera nhìn từ trên xuống.
                // Thay vào đó, chúng ta sử dụng LookAt để mũi tên luôn xoay mặt về phía mặt phẳng của camera
                // mà không bị biến dạng. Logic này đảm bảo "mặt trước" của mũi tên (trục Z)
                // luôn song song với "mặt trước" của camera, tương tự như cách FloatingText hoạt động.
                _indicatorArrow.transform.LookAt(
                    _indicatorArrow.transform.position + _mainCamera.transform.rotation * Vector3.forward,
                    _mainCamera.transform.rotation * Vector3.up);
            }
        }

        public void OnCollect(IPlayer player)
        {
            // Ensure the collect logic runs only once per collectible.
            // Prevents duplicated create/credits when the trigger fires multiple times.
            if (_isCollected) return;
            _isCollected = true;

            // 1. Ủy quyền logic thu thập cốt lõi cho "strategy" hành vi.
            // Behavior trả về false nghĩa là item KHÔNG được tiêu thụ (vd: đã có Crystal Shield)
            // → giữ nguyên node lại để sau này nhặt được.
            bool consumed = true;
            if (_behavior != null)
            {
                // Truyền 'this' (controller) và 'player' cho strategy.
                consumed = _behavior.Execute(this, player);
            }
            else
            {
                Debug.LogWarning($"Vật phẩm '{Data?.DisplayName}' được thu thập nhưng không có hành vi nào được gán.", gameObject);
            }

            if (!consumed)
            {
                // Không tiêu thụ: reset cờ để node này vẫn có thể được nhặt trong lần chạm sau.
                _isCollected = false;
                return;
            }

            // 2. Phát âm thanh thu thập.
            // Phát âm thanh thu thập (có thể được quản lý bởi một audio manager toàn cục,
            // nhưng để đơn giản, chúng ta sẽ phát từ người chơi đã thu thập)
            if (_data?.CollectionSFX != null)
            {
                if (player.GameObject.TryGetComponent<AudioSource>(out var playerAudio))
                {
                    playerAudio.PlayOneShot(_data.CollectionSFX);
                }
            }

            // 3. Yêu cầu despawn vật phẩm.
            GameEvents.TriggerCollectibleDespawnRequest(gameObject);
        }

        public void ResetState()
        {
            if (_lifespanCoroutine != null)
            {
                StopCoroutine(_lifespanCoroutine);
                _lifespanCoroutine = null;
            }
            _data = null;
            _behavior = null;
            _isCollected = false;
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }

            // Reset lại trạng thái hình ảnh
            if (_mainRenderer != null)
            {
                _mainRenderer.enabled = true;
            }
            if (_indicatorArrow != null)
            {
                _indicatorArrow.transform.DOKill(); // Dừng các animation DOTween
                _indicatorArrow.transform.localPosition = _initialArrowLocalPosition;
                _indicatorArrow.SetActive(true); // Đảm bảo nó hiển thị cho lần sử dụng tiếp theo
            }
        }

        /// <summary>
        /// Handles being hit by an explosion.
        /// </summary>
        public void OnExplosionHit(Vector3 force, Vector3 point, IBaseBombData bombData)
        {
            // Collectibles are simply pushed by explosions.
            if (_rigidbody != null && !_rigidbody.isKinematic)
            {
                _rigidbody.AddForceAtPosition(force, point, bombData.ForceMode);
            }
        }
    }
}