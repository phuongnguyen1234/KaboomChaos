using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace UI
{
    /// <summary>
    /// Lop goc cho tat ca popup.
    /// Quan ly cac hanh vi Show/Hide/Toggle va nu dong popup.
    /// Khong phu thuoc vao noi dung cu the cua tung popup.
    /// Bao gom animation hien thi (Slide Up/Down).
    /// </summary>
    public abstract class BasePopup : MonoBehaviour
    {
        #region Fields
        [Tooltip("Nu dong popup. Co the de trong neu popup khong can nu dong.")]
        [SerializeField] private Button _closeButton;

        [Header("Animation Settings")]
        [Tooltip("Container cua popup de thuc hien hieu ung slide. Neu trong, se tu dong tim the con ten 'Container' hoac lay the hien tai.")]
        [SerializeField] protected RectTransform _popupContainer;
        [Tooltip("CanvasGroup de fade (neu co). Neu trong, tu dong tim tren the hien tai.")]
        [SerializeField] protected CanvasGroup _canvasGroup;
        [Tooltip("Thoi gian hieu ung.")]
        [SerializeField] protected float _animationDuration = 0.4f;
        [Tooltip("Khoang cach slide up (y position offset).")]
        [SerializeField] protected float _slideOffset = -1500f;

        private Vector2 _originalContainerPos;
        private bool _isInitialized = false;
        #endregion

        #region Properties
        /// <summary>
        /// Tra ve true khi popup dang hien thi.
        /// </summary>
        public virtual bool IsVisible => gameObject.activeSelf;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// Gan su kien cho nu dong va khoi tao animation.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeAnimation();
            WireCloseButton();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Hien thi popup kem animation.
        /// </summary>
        public virtual void Show()
        {
            Show(true);
        }

        /// <summary>
        /// Hien thi popup, co tuy chon chay animation slide in hay hien thi ngay.
        /// </summary>
        /// <param name="animate">True de chay animation slide in, False de hien thi ngay.</param>
        public virtual void Show(bool animate)
        {
            InitializeAnimation();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            // Kich hoat raycast va interactable cho tat ca CanvasGroup
            EnableRaycastsAndInteraction(true);

            if (!animate)
            {
                if (_canvasGroup != null)
                {
                    _canvasGroup.DOKill();
                    _canvasGroup.alpha = 1f;
                }

                if (_popupContainer != null)
                {
                    _popupContainer.DOKill();
                    _popupContainer.anchoredPosition = _originalContainerPos;
                }

                OnShow();
                return;
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.DOKill();
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, _animationDuration).SetUpdate(true);
            }

            if (_popupContainer != null)
            {
                _popupContainer.DOKill();
                _popupContainer.anchoredPosition = new Vector2(_originalContainerPos.x, _originalContainerPos.y + _slideOffset);
                _popupContainer.DOAnchorPos(_originalContainerPos, _animationDuration).SetEase(Ease.OutBack).SetUpdate(true);
            }

            OnShow();
        }

        /// <summary>
        /// Hien thi popup ngay lap tuc khong chay animation.
        /// </summary>
        public virtual void ShowInstant()
        {
            Show(false);
        }

        /// <summary>
        /// An popup kem animation.
        /// </summary>
        public virtual void Hide()
        {
            Hide(true);
        }

        /// <summary>
        /// An popup, co tuy chon chay animation slide out hay an ngay.
        /// </summary>
        /// <param name="animate">True de chay animation slide out, False de an ngay.</param>
        public virtual void Hide(bool animate)
        {
            OnHidden();

            if (!gameObject.activeInHierarchy) return; // Da an roi

            // Tat raycast de khong block cac UI phia sau trong luc fade out
            EnableRaycastsAndInteraction(false);

            if (!animate)
            {
                if (_popupContainer != null)
                {
                    _popupContainer.DOKill();
                    _popupContainer.anchoredPosition = new Vector2(_originalContainerPos.x, _originalContainerPos.y + _slideOffset);
                }

                if (_canvasGroup != null)
                {
                    _canvasGroup.DOKill();
                    _canvasGroup.alpha = 0f;
                }

                gameObject.SetActive(false);
                return;
            }

            if (_popupContainer != null)
            {
                _popupContainer.DOKill();
                _popupContainer.DOAnchorPos(new Vector2(_originalContainerPos.x, _originalContainerPos.y + _slideOffset), _animationDuration)
                    .SetEase(Ease.InBack).SetUpdate(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.DOKill();
                _canvasGroup.DOFade(0f, _animationDuration).SetUpdate(true).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else if (_popupContainer != null)
            {
                DOVirtual.DelayedCall(_animationDuration, () => gameObject.SetActive(false)).SetUpdate(true);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// An popup ngay lap tuc khong chay animation.
        /// </summary>
        public virtual void HideInstant()
        {
            Hide(false);
        }

        /// <summary>
        /// Dong/mo popup theo trang thai hien tai.
        /// </summary>
        public virtual void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }
        #endregion

        #region Protected Virtual Hooks
        /// <summary>
        /// Hook duoc goi ngay sau khi popup duoc mo. Class con override de chua logic rieng.
        /// </summary>
        protected virtual void OnShow()
        {
        }

        /// <summary>
        /// Hook duoc goi ngay truoc khi popup duoc dong. Class con override de cleanup.
        /// </summary>
        protected virtual void OnHidden()
        {
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Khoi tao cac ref ve transform dung cho animation.
        /// </summary>
        private void InitializeAnimation()
        {
            if (_isInitialized) return;
            
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponentInChildren<CanvasGroup>();
                }
            }

            if (_popupContainer == null)
            {
                Transform containerObj = transform.Find("Container");
                if (containerObj != null)
                {
                    _popupContainer = containerObj.GetComponent<RectTransform>();
                }
                else
                {
                    // Fallback to self
                    _popupContainer = GetComponent<RectTransform>();
                }
            }
            
            if (_popupContainer != null)
            {
                _originalContainerPos = _popupContainer.anchoredPosition;
            }
            _isInitialized = true;
        }

        /// <summary>
        /// Bat hoac tat blocksRaycasts va interactable tren tat ca CanvasGroup cua popup.
        /// </summary>
        /// <param name="enable">True de cho phep tuong tac va nhan raycast, False de chan.</param>
        private void EnableRaycastsAndInteraction(bool enable)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = enable;
                _canvasGroup.interactable = enable;
            }

            var allCanvasGroups = GetComponentsInChildren<CanvasGroup>(true);
            for (int i = 0; i < allCanvasGroups.Length; i++)
            {
                allCanvasGroups[i].blocksRaycasts = enable;
                allCanvasGroups[i].interactable = enable;
            }
        }

        /// <summary>
        /// Gan lang nghe su kien click cho nut dong (tu dong tim neu chua duoc gan).
        /// </summary>
        private void WireCloseButton()
        {
            if (_closeButton == null)
            {
                Transform closeTrans = transform.Find("CloseButton") ??
                                       transform.Find("Container/CloseButton") ??
                                       transform.Find("Close") ??
                                       transform.Find("Container/Close") ??
                                       transform.Find("Btn_Close") ??
                                       transform.Find("Container/Btn_Close");
                if (closeTrans != null)
                {
                    _closeButton = closeTrans.GetComponent<Button>();
                }
            }

            if (_closeButton != null)
            {
                _closeButton.onClick.RemoveListener(HandleCloseButtonClicked);
                _closeButton.onClick.AddListener(HandleCloseButtonClicked);
            }
        }

        private void HandleCloseButtonClicked()
        {
            Hide(true);
        }
        #endregion
    }
}