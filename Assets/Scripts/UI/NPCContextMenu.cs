using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using DG.Tweening;
using Core.Interfaces;
using Core.Interfaces.UI;

namespace UI
{
    /// <summary>
    /// Component quan ly Context Menu cho NPC (danh cho mo Shop hoac tuong tac).
    /// - Khi Player den gan NPC: hien thi Context Menu dang Billboard huong ve Camera.
    /// - Khi Player di ra xa: an Context Menu.
    /// - Khi o trong tam tuong tac: an phim E (hoac phim tuy chon) de mo Shop.
    /// </summary>
    public class NPCContextMenu : MonoBehaviour
    {
        #region Fields
        [Header("UI & Billboard")]
        [Tooltip("GameObject context menu (billboard) se duoc bat khi player o gan va an khi di xa.")]
        [SerializeField] private GameObject _contextMenuObject;
        [Tooltip("Transform dung de xoay ve huong Camera. Neu de trong se dung transform cua _contextMenuObject.")]
        [SerializeField] private Transform _billboardTransform;
        [Tooltip("Bat/tat tu dong xoay ve phia Camera moi frame (Billboard).")]
        [SerializeField] private bool _enableBillboard = true;
        [Tooltip("Chi xoay theo truc Y (giu thang dung) hay xoay toan bo theo huong Camera.")]
        [SerializeField] private bool _rotateYOnly = false;

        [Header("Detection Settings")]
        [Tooltip("Khoang cach toi da de phat hien Player va hien thi context menu (met).")]
        [SerializeField] private float _interactionDistance = 3.5f;

        [Header("Input & Interaction")]
        [Tooltip("Phim tuong tac mo Shop tren ban phim (mac dinh la phim E).")]
        [SerializeField] private Key _interactKey = Key.E;
        [Tooltip("SFX phat khi nguoi choi bam phim tuong tac (tuy chon).")]
        [SerializeField] private AudioClip _interactSfx;
        [Tooltip("Su kien tuy bien kich hoat khi tuong tac thanh cong (mac dinh da tu dong goi mo Shop).")]
        [SerializeField] private UnityEvent _onInteract;

        [Header("Animation Settings")]
        [Tooltip("Thoi gian hieu ung scale pop up khi hien thi (giay).")]
        [SerializeField] private float _animationDuration = 0.2f;
        [Tooltip("Su dung hieu ung scale khi hien/an context menu.")]
        [SerializeField] private bool _useScaleAnimation = true;

        private Camera _mainCamera;
        private Transform _playerTransform;
        private bool _isPlayerInRange;
        private Vector3 _originalScale = Vector3.one;
        private Tween _scaleTween;
        #endregion

        #region Properties
        /// <summary>
        /// Cho biet Player hien co dang o trong vung tuong tac hay khong.
        /// </summary>
        public bool IsPlayerInRange => _isPlayerInRange;

        /// <summary>
        /// GameObject Context Menu duoc gan.
        /// </summary>
        public GameObject ContextMenuObject => _contextMenuObject;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            EnsureContextMenuReference();

            if (_contextMenuObject != null)
            {
                _originalScale = _contextMenuObject.transform.localScale;
                if (_originalScale == Vector3.zero)
                {
                    _originalScale = Vector3.one;
                }
                _contextMenuObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            _mainCamera = Camera.main;
        }

        private void OnDisable()
        {
            _scaleTween?.Kill();
            _isPlayerInRange = false;

            if (_contextMenuObject != null)
            {
                _contextMenuObject.SetActive(false);
            }
        }

        private void Update()
        {
            CheckPlayerProximity();
            HandleInteractionInput();
        }

        private void LateUpdate()
        {
            UpdateBillboardRotation();
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();
        }

        private void OnDrawGizmosSelected()
        {
            // Ve vong tron pham vi tuong tac trong Scene view
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _interactionDistance);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Kich hoat tuong tac mo Shop (tuong tu nhu khi nhan phim E).
        /// </summary>
        public void Interact()
        {
            if (_interactSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_interactSfx);
            }

            // Mo popup Shop qua IUIManager
            if (IUIManager.Instance != null)
            {
                IUIManager.Instance.OpenShopPopup();
            }

            _onInteract?.Invoke();
        }

        /// <summary>
        /// Hien thi Context Menu voi animation scale (neu bat).
        /// </summary>
        public void ShowContextMenu()
        {
            if (_contextMenuObject == null) return;

            _scaleTween?.Kill();
            _contextMenuObject.SetActive(true);

            UpdateBillboardRotation();

            if (_useScaleAnimation)
            {
                _contextMenuObject.transform.localScale = Vector3.zero;
                _scaleTween = _contextMenuObject.transform.DOScale(_originalScale, _animationDuration)
                    .SetEase(Ease.OutBack)
                    .SetUpdate(true);
            }
            else
            {
                _contextMenuObject.transform.localScale = _originalScale;
            }
        }

        /// <summary>
        /// An Context Menu voi animation scale down (neu bat).
        /// </summary>
        public void HideContextMenu()
        {
            if (_contextMenuObject == null) return;

            _scaleTween?.Kill();

            if (_useScaleAnimation && _contextMenuObject.activeSelf)
            {
                _scaleTween = _contextMenuObject.transform.DOScale(Vector3.zero, _animationDuration)
                    .SetEase(Ease.InQuad)
                    .SetUpdate(true)
                    .OnComplete(() =>
                    {
                        if (!_isPlayerInRange && _contextMenuObject != null)
                        {
                            _contextMenuObject.SetActive(false);
                            _contextMenuObject.transform.localScale = _originalScale;
                        }
                    });
            }
            else
            {
                _contextMenuObject.SetActive(false);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Kiem tra khoang cach giua NPC va Player de bat/tat Context Menu.
        /// </summary>
        private void CheckPlayerProximity()
        {
            EnsurePlayerTransform();

            if (_playerTransform == null)
            {
                if (_isPlayerInRange)
                {
                    _isPlayerInRange = false;
                    HideContextMenu();
                }
                return;
            }

            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            bool inRange = distance <= _interactionDistance;

            if (inRange && !_isPlayerInRange)
            {
                _isPlayerInRange = true;
                ShowContextMenu();
            }
            else if (!inRange && _isPlayerInRange)
            {
                _isPlayerInRange = false;
                HideContextMenu();
            }
        }

        /// <summary>
        /// Lang nghe nut bam tuong tac khi Player dang o trong pham vi.
        /// </summary>
        private void HandleInteractionInput()
        {
            if (!_isPlayerInRange) return;

            if (Keyboard.current != null && Keyboard.current[_interactKey].wasPressedThisFrame)
            {
                Interact();
            }
        }

        /// <summary>
        /// Xoay context menu huong ve phia Camera moi frame (Billboard effect).
        /// </summary>
        private void UpdateBillboardRotation()
        {
            if (!_enableBillboard || _contextMenuObject == null || !_contextMenuObject.activeInHierarchy) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Transform target = _billboardTransform != null ? _billboardTransform : _contextMenuObject.transform;

            if (_rotateYOnly)
            {
                Vector3 lookDirection = _mainCamera.transform.position - target.position;
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0.001f)
                {
                    target.rotation = Quaternion.LookRotation(-lookDirection);
                }
            }
            else
            {
                target.rotation = _mainCamera.transform.rotation;
            }
        }

        /// <summary>
        /// Tim tham chieu Player hien tai qua IPlayerManager hoac Tag.
        /// </summary>
        private void EnsurePlayerTransform()
        {
            if (_playerTransform != null && _playerTransform.gameObject.activeInHierarchy) return;

            if (IPlayerManager.Instance != null)
            {
                var player = IPlayerManager.Instance.GetCurrentPlayer();
                if (player != null && player.GameObject != null)
                {
                    _playerTransform = player.GameObject.transform;
                    return;
                }
            }

            // Fallback tim qua Tag "Player" neu chua co IPlayerManager
            var playerGo = GameObject.FindWithTag("Player");
            if (playerGo != null)
            {
                _playerTransform = playerGo.transform;
            }
        }

        /// <summary>
        /// Tu dong tim kiem GameObject con dai dien cho Context Menu neu chua duoc gan qua Inspector.
        /// </summary>
        private void EnsureContextMenuReference()
        {
            if (_contextMenuObject != null) return;

            string[] candidateNames = { "ContextMenu", "Billboard", "Prompt", "ShopPrompt", "Dialog", "Interaction" };
            foreach (var name in candidateNames)
            {
                var child = transform.Find(name);
                if (child != null)
                {
                    _contextMenuObject = child.gameObject;
                    break;
                }
            }

            // Neu van chua thay, lay child dau tien neu co
            if (_contextMenuObject == null && transform.childCount > 0)
            {
                _contextMenuObject = transform.GetChild(0).gameObject;
            }
        }
        #endregion
    }
}

