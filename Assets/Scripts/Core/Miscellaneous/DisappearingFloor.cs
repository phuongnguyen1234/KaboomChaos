using UnityEngine;
using System.Collections;
using DG.Tweening;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Quan ly san bien mat (Disappearing Floor / Hex-a-gone tile).
    /// Khi player giam len, san se tu tu mo dan alpha ve 0 qua MaterialEffectController,
    /// tat collider de player roi xuong, va tu dong tai sinh (fade alpha tu 0 ve 1) sau mot thoi gian.
    /// </summary>
    public class DisappearingFloor : MonoBehaviour
    {
        #region Fields

        [Header("Timing Settings")]
        [Tooltip("Thoi gian (giay) de alpha mo dan tu 1.0 ve 0.0 khi bien mat.")]
        [SerializeField] private float _disappearDuration = 0.5f;

        [Tooltip("Thoi gian (giay) san giu trang thai an truoc khi bat dau tai sinh.")]
        [SerializeField] private float _reappearDelay = 3.0f;

        [Tooltip("Thoi gian (giay) de alpha hien dan tu 0.0 len 1.0 khi tai sinh.")]
        [SerializeField] private float _reappearDuration = 0.5f;

        [Header("Material Effect Controller")]
        [Tooltip("Tham chieu den MaterialEffectController tren object. Neu de trong, script tu dong GetComponent.")]
        [SerializeField] private MaterialEffectController _effectController;

        [Header("Audio & Visual Effects")]
        [Tooltip("SFX phat khi player giam len san.")]
        [SerializeField] private AudioClip _steppedSfx;

        [Tooltip("SFX phat khi san bien mat hoan toan.")]
        [SerializeField] private AudioClip _disappearSfx;

        [Tooltip("SFX phat khi san tai sinh.")]
        [SerializeField] private AudioClip _reappearSfx;

        [Tooltip("VFX prefab tao ra khi san bien mat (tuy chon).")]
        [SerializeField] private GameObject _disappearVfxPrefab;

        [Tooltip("VFX prefab tao ra khi san tai sinh (tuy chon).")]
        [SerializeField] private GameObject _reappearVfxPrefab;

        // Components va trang thai runtime
        private Collider _tileCollider;
        private Renderer _tileRenderer;
        private Coroutine _processRoutine;
        private Tweener _alphaTween;
        private bool _isProcessing;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_effectController == null)
            {
                _effectController = GetComponent<MaterialEffectController>();
                if (_effectController == null)
                {
                    _effectController = GetComponentInChildren<MaterialEffectController>();
                }
            }

            _tileCollider = GetComponent<Collider>();
            if (_tileCollider == null)
            {
                _tileCollider = GetComponentInChildren<Collider>();
            }

            _tileRenderer = GetComponent<Renderer>();
            if (_tileRenderer == null)
            {
                _tileRenderer = GetComponentInChildren<Renderer>();
            }
        }

        private void OnEnable()
        {
            ResetTileState();
        }

        private void OnDisable()
        {
            KillCurrentEffects();
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryTriggerFloor(collision.gameObject);
        }

        private void OnTriggerEnter(Collider other)
        {
            TryTriggerFloor(other.gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kich hoat chu trinh mo dan bien mat va tai sinh thu cong qua code.
        /// </summary>
        public void TriggerDisappear()
        {
            if (_isProcessing) return;
            _processRoutine = StartCoroutine(DisappearAndReappearRoutine());
        }

        /// <summary>
        /// Khoi phuc san ve trang thai ban dau (alpha = 1, bat collider va renderer).
        /// </summary>
        public void ResetTileState()
        {
            KillCurrentEffects();

            if (_effectController != null)
            {
                _effectController.CustomAlpha = 1.0f;
            }

            if (_tileCollider != null) _tileCollider.enabled = true;
            if (_tileRenderer != null) _tileRenderer.enabled = true;

            _isProcessing = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Huy cac tween va coroutine dang chay de tranh loi khi disable hoac reset.
        /// </summary>
        private void KillCurrentEffects()
        {
            _alphaTween?.Kill();
            _alphaTween = null;

            if (_processRoutine != null)
            {
                StopCoroutine(_processRoutine);
                _processRoutine = null;
            }
        }

        /// <summary>
        /// Kiem tra xem doi tuong va cham co phai la than chinh cua Player hay khong.
        /// </summary>
        private void TryTriggerFloor(GameObject obj)
        {
            if (_isProcessing || obj == null) return;

            // Uu tien kiem tra than chinh cua Player qua IPrimaryExplosionTarget
            bool isPrimary = obj.GetComponentInParent<IPrimaryExplosionTarget>() != null;
            IPlayer player = obj.GetComponentInParent<IPlayer>();

            if ((player != null && (isPrimary || player.GameObject == obj)) || obj.CompareTag("Player"))
            {
                TriggerDisappear();
            }
        }

        /// <summary>
        /// Coroutine thuc hien chu trinh: Fade Out -> Tat Collider -> Cho -> Fade In -> Bat Collider.
        /// </summary>
        private IEnumerator DisappearAndReappearRoutine()
        {
            _isProcessing = true;

            // Phat SFX khi giam len san
            PlaySfx(_steppedSfx);

            // 1. Giai doan Fade Out Alpha (tu tu bien mat)
            if (_disappearDuration > 0f)
            {
                float currentAlpha = GetAlpha();
                _alphaTween?.Kill();
                _alphaTween = DOTween.To(() => currentAlpha, x => SetAlpha(x), 0f, _disappearDuration)
                    .SetEase(Ease.InOutQuad);

                yield return new WaitForSeconds(_disappearDuration);
            }
            else
            {
                SetAlpha(0f);
            }

            // Tat collider va renderer sau khi alpha ve 0
            if (_tileCollider != null) _tileCollider.enabled = false;
            if (_tileRenderer != null) _tileRenderer.enabled = false;

            PlaySfx(_disappearSfx);
            SpawnVfx(_disappearVfxPrefab);

            // 2. Giai doan cho tai sinh
            yield return new WaitForSeconds(_reappearDelay);

            // 3. Giai doan Fade In Alpha (tu tu hien lai)
            if (_tileRenderer != null) _tileRenderer.enabled = true;
            PlaySfx(_reappearSfx);
            SpawnVfx(_reappearVfxPrefab);

            if (_reappearDuration > 0f)
            {
                _alphaTween?.Kill();
                _alphaTween = DOTween.To(() => 0f, x => SetAlpha(x), 1.0f, _reappearDuration)
                    .SetEase(Ease.InOutQuad);

                yield return new WaitForSeconds(_reappearDuration);
            }
            else
            {
                SetAlpha(1.0f);
            }

            // Bat lai collider va hoan tat chu ky
            if (_tileCollider != null) _tileCollider.enabled = true;
            _isProcessing = false;
            _processRoutine = null;
        }

        /// <summary>
        /// Lay gia tri alpha hien tai tu MaterialEffectController.
        /// </summary>
        private float GetAlpha()
        {
            if (_effectController != null)
            {
                return _effectController.CustomAlpha;
            }
            return 1.0f;
        }

        /// <summary>
        /// Gan gia tri alpha cho MaterialEffectController.
        /// </summary>
        private void SetAlpha(float value)
        {
            if (_effectController != null)
            {
                _effectController.CustomAlpha = value;
            }
        }

        /// <summary>
        /// Phat SFX am thanh qua SfxService neu co.
        /// </summary>
        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            if (SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(clip, transform.position);
            }
        }

        /// <summary>
        /// Tao VFX tai vi tri cua san.
        /// </summary>
        private void SpawnVfx(GameObject vfxPrefab)
        {
            if (vfxPrefab == null) return;
            GameEvents.TriggerVFXSpawnRequest(vfxPrefab, transform.position, Quaternion.identity);
        }

        #endregion
    }
}

