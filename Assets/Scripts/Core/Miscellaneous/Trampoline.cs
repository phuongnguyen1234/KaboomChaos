using UnityEngine;
using Core.Interfaces;

namespace Core.Miscellaneous
{
    /// <summary>
    /// Dieu khien doi tuong Trampoline (dem bat nhay).
    /// Khi player hoac vat the dam vao khu vuc trigger/collider, trampoline se bat doi tuong len cao
    /// voi muc luc ngau nhien trong khoang [min, max], dong thoi doi mau tam bat thong qua MaterialEffectController va phat SFX/VFX.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Trampoline : MonoBehaviour
    {
        #region Fields

        [Header("Bounce Force Settings")]
        [Tooltip("Luc bat nhay toi thieu (min) ap dung len player.")]
        [SerializeField] private float _minBounceForce = 15f;

        [Tooltip("Luc bat nhay toi da (max) ap dung len player.")]
        [SerializeField] private float _maxBounceForce = 25f;

        [Tooltip("Thoi gian hoan loai (cooldown) giua 2 lan bat nhay (giay) de tranh bi spam trigger.")]
        [SerializeField] private float _cooldown = 0.2f;

        [Header("Non-Player Objects")]
        [Tooltip("Cho phep bat nhay ca cac doi tuong Rigidbody khac (bom, vat pham) khi cham vao trampoline.")]
        [SerializeField] private bool _affectOtherRigidbodies = true;

        [Header("Material Effect Settings")]
        [Tooltip("MaterialEffectController dieu khien mau sac cua tam bat. Neu de trong se tu dong GetComponent.")]
        [SerializeField] private MaterialEffectController _materialEffectController;

        [Tooltip("Mau sac tam bat nhay den khi co va cham.")]
        [SerializeField] private Color _bounceColor = Color.cyan;

        [Tooltip("Thoi gian dien ra hieu ung doi mau/pulse mau (giay).")]
        [SerializeField] private float _colorFlashDuration = 0.3f;

        [Tooltip("Neu la true se doi mau Decal (_DecalColor), false se doi mau Base (_BaseColor).")]
        [SerializeField] private bool _useDecalColor = false;

        [Header("Audio Settings")]
        [Tooltip("SFX phat khi co doi tuong giam len trampoline.")]
        [SerializeField] private AudioClip _bounceSfx;

        [Tooltip("Am luong SFX bat nhay.")]
        [Range(0f, 1f)]
        [SerializeField] private float _sfxVolume = 1f;

        [Tooltip("Pitch toi thieu cho SFX bat nhay.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _sfxPitchMin = 0.9f;

        [Tooltip("Pitch toi da cho SFX bat nhay.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _sfxPitchMax = 1.1f;

        [Header("Visual Effects")]
        [Tooltip("VFX prefab sinh ra tai vi tri trampoline khi bat nhay (tuy chon).")]
        [SerializeField] private GameObject _bounceVfxPrefab;

        private float _lastBounceTime;

        #endregion

        #region Properties

        /// <summary>
        /// Luc bat nhay toi thieu.
        /// </summary>
        public float MinBounceForce
        {
            get => _minBounceForce;
            set => _minBounceForce = value;
        }

        /// <summary>
        /// Luc bat nhay toi da.
        /// </summary>
        public float MaxBounceForce
        {
            get => _maxBounceForce;
            set => _maxBounceForce = value;
        }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_materialEffectController == null)
            {
                _materialEffectController = GetComponent<MaterialEffectController>();
                if (_materialEffectController == null)
                {
                    _materialEffectController = GetComponentInChildren<MaterialEffectController>();
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TryBounce(other.gameObject);
        }

        private void OnCollisionEnter(Collision collision)
        {
            TryBounce(collision.gameObject);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Kich hoat bat nhay thu cong cho mot GameObject.
        /// </summary>
        /// <param name="targetObj">Doi tuong can bat nhay.</param>
        public void ManualBounce(GameObject targetObj)
        {
            TryBounce(targetObj);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kiem tra va thuc hien luc bat nhay len doi tuong targetObj neu hop le.
        /// </summary>
        /// <param name="targetObj">GameObject va cham hoac di vao trigger.</param>
        private void TryBounce(GameObject targetObj)
        {
            if (targetObj == null) return;
            if (Time.time - _lastBounceTime < _cooldown) return;

            // Kiem tra player qua IPlayer interface
            IPlayer player = targetObj.GetComponentInParent<IPlayer>();
            if (player == null && targetObj.CompareTag("Player"))
            {
                player = targetObj.GetComponent<IPlayer>();
            }

            // Neu la Player
            if (player != null && player.GameObject != null)
            {
                ExecuteBounceOnPlayer(player);
                return;
            }

            // Neu la Rigidbody khac
            if (_affectOtherRigidbodies)
            {
                Rigidbody rb = targetObj.GetComponentInParent<Rigidbody>();
                if (rb != null && !rb.isKinematic)
                {
                    ExecuteBounceOnRigidbody(rb);
                }
            }
        }

        /// <summary>
        /// Thuc hien luc bat nhay len Player.
        /// </summary>
        /// <param name="player">Doi tuong IPlayer.</param>
        private void ExecuteBounceOnPlayer(IPlayer player)
        {
            _lastBounceTime = Time.time;

            float force = Random.Range(_minBounceForce, _maxBounceForce);

            // Triet tieu van toc roi xuong theo truc Y neu player dang falling de dam bao do cao bat duy tri on dinh
            if (player.VerticalVelocity < 0f)
            {
                player.AddMomentum(Vector3.up * (-player.VerticalVelocity));
            }

            // Ap dung luc bat nhay len tren
            player.AddMomentum(Vector3.up * force);

            TriggerEffects();
        }

        /// <summary>
        /// Thuc hien luc bat nhay len Rigidbody thong thuong.
        /// </summary>
        /// <param name="rb">Rigidbody cua doi tuong.</param>
        private void ExecuteBounceOnRigidbody(Rigidbody rb)
        {
            _lastBounceTime = Time.time;

            float force = Random.Range(_minBounceForce, _maxBounceForce);

            Vector3 currentVel = rb.linearVelocity;
            if (currentVel.y < 0f)
            {
                currentVel.y = 0f;
                rb.linearVelocity = currentVel;
            }

            rb.AddForce(Vector3.up * force, ForceMode.Impulse);

            TriggerEffects();
        }

        /// <summary>
        /// Phat am thanh SFX, doi mau tam bat qua MaterialEffectController va tao VFX neu co.
        /// </summary>
        private void TriggerEffects()
        {
            // Doi mau tam bat nhay dung MaterialEffectController
            if (_materialEffectController != null)
            {
                if (_useDecalColor)
                {
                    _materialEffectController.PulseDecalColor(_bounceColor, _colorFlashDuration);
                }
                else
                {
                    _materialEffectController.PulseBaseColor(_bounceColor, _colorFlashDuration);
                }
            }

            // Phat am thanh SFX qua SfxService
            if (_bounceSfx != null && SfxService.Instance != null)
            {
                float pitch = Random.Range(_sfxPitchMin, _sfxPitchMax);
                SfxService.Instance.PlaySfx(_bounceSfx, transform.position, _sfxVolume, pitch);
            }

            // Tao VFX
            if (_bounceVfxPrefab != null)
            {
                GameEvents.TriggerVFXSpawnRequest(_bounceVfxPrefab, transform.position, Quaternion.identity);
            }
        }

        #endregion
    }
}
