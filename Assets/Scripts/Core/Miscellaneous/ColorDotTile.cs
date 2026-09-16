using UnityEngine;
using System.Collections;
using DG.Tweening;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Script quan ly dom mau (ColorDot) cho minigame ColorDots trong Lobby.
    /// Khi Player giam/va cham bang than chinh (PrimaryTarget), dom mau se lun xuong,
    /// phat SFX/VFX va tu dong teleport den vi tri ngau nhien moi trong khu vuc bien (Boundary Collider).
    /// </summary>
    public class ColorDotTile : MonoBehaviour
    {
        #region Fields

        [Header("Boundary Area")]
        [Tooltip("Collider dinh nghia khu vuc bien (Bounds) ma dom mau co the teleport den.")]
        [SerializeField] private Collider _boundaryCollider;

        [Tooltip("Tu dong khoi tao dom mau tai vi tri ngau nhien trong bien khi khoi dong game.")]
        [SerializeField] private bool _randomizeOnStart = true;

        [Header("Sink & Teleport Settings")]
        [Tooltip("Do sau lun xuong (don vi Y) khi player giam len.")]
        [SerializeField] private float _sinkDepth = 0.2f;

        [Tooltip("Thoi gian (giay) chay animation lun xuong.")]
        [SerializeField] private float _sinkDuration = 0.15f;

        [Tooltip("Thoi gian (giay) doi sau khi lun xuong roi moi teleport den vi tri moi.")]
        [SerializeField] private float _teleportDelay = 0.1f;

        [Header("Audio & Visual Effects")]
        [Tooltip("SFX phat khi player giam len dom mau.")]
        [SerializeField] private AudioClip _stepSfx;

        [Tooltip("VFX prefab tao tai vi tri cu khi giam (tuy chon).")]
        [SerializeField] private GameObject _stepVfxPrefab;

        [Tooltip("VFX prefab tao tai vi tri moi khi teleport den (tuy chon).")]
        [SerializeField] private GameObject _teleportVfxPrefab;

        // Components va trang thai runtime
        private Vector3 _initialLocalPos;
        private float _initialY;
        private bool _isStepped;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _initialLocalPos = transform.localPosition;
            _initialY = transform.position.y;
        }

        private void Start()
        {
            if (_randomizeOnStart)
            {
                TeleportToRandomPosition();
            }
        }

        private void OnEnable()
        {
            ResetDotState();
        }

        private void OnDisable()
        {
            transform.DOKill();
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryTriggerDot(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryTriggerDot(other.gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Dat lai trang thai dom mau va dua ve vi tri lung ban dau.
        /// </summary>
        public void ResetDotState()
        {
            transform.DOKill();
            transform.localPosition = _initialLocalPos;
            _isStepped = false;
        }

        /// <summary>
        /// Doi dom mau ngay lap tuc toi mot vi tri ngau nhien moi ben trong Boundary.
        /// </summary>
        public void TeleportToRandomPosition()
        {
            transform.DOKill();
            transform.position = GetRandomPositionInBoundary();
            _isStepped = false;
        }

        /// <summary>
        /// Kich hoat hieu ung lun va teleport thu cong qua code.
        /// </summary>
        public void TriggerStep()
        {
            if (_isStepped) return;
            StartCoroutine(StepAndTeleportRoutine());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kiem tra va kich hoat khi doi tuong va cham la than chinh cua Player (co IPrimaryExplosionTarget).
        /// </summary>
        private void TryTriggerDot(GameObject obj)
        {
            if (_isStepped || obj == null) return;

            bool isPrimary = obj.GetComponentInParent<IPrimaryExplosionTarget>() != null;
            IPlayer player = obj.GetComponentInParent<IPlayer>();

            if ((player != null && (isPrimary || player.GameObject == obj)) || obj.CompareTag("Player"))
            {
                TriggerStep();
            }
        }

        /// <summary>
        /// Coroutine thuc hien: Lun xuong -> Phat SFX/VFX -> Teleport den vi tri ngau nhien moi trong bien.
        /// </summary>
        private IEnumerator StepAndTeleportRoutine()
        {
            _isStepped = true;

            // 1. Phat SFX & VFX khi giam
            PlaySfx(_stepSfx);
            SpawnVfx(_stepVfxPrefab, transform.position);

            // 2. Animation lun xuong (Sink Down)
            transform.DOKill();
            Vector3 sinkTargetPos = transform.position + Vector3.down * _sinkDepth;
            transform.DOMove(sinkTargetPos, _sinkDuration).SetEase(Ease.InQuad);

            yield return new WaitForSeconds(_sinkDuration + _teleportDelay);

            // 3. Teleport den vi tri ngau nhien moi trong Boundary
            Vector3 newPosition = GetRandomPositionInBoundary();
            SpawnVfx(_teleportVfxPrefab, newPosition);

            transform.DOKill();
            transform.position = newPosition;

            // Reset lún để đốm nhô lên lại ở vị trí mới
            _isStepped = false;
        }

        /// <summary>
        /// Tinh toan vi tri ngau nhien nam trong khu vuc Bounds cua Boundary Collider.
        /// </summary>
        private Vector3 GetRandomPositionInBoundary()
        {
            if (_boundaryCollider == null)
            {
                return transform.position;
            }

            Bounds bounds = _boundaryCollider.bounds;
            float randomX = Random.Range(bounds.min.x, bounds.max.x);
            float randomZ = Random.Range(bounds.min.z, bounds.max.z);

            return new Vector3(randomX, _initialY, randomZ);
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            if (SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(clip, transform.position);
            }
        }

        private void SpawnVfx(GameObject vfxPrefab, Vector3 pos)
        {
            if (vfxPrefab == null) return;
            GameEvents.TriggerVFXSpawnRequest(vfxPrefab, pos, Quaternion.identity);
        }

        #endregion
    }
}

