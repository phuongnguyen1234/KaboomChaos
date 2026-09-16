using UnityEngine;
using UnityEngine.UI;
using Core.Interfaces.UI;
using System.Threading.Tasks;
using System;

namespace UI
{
    /// <summary>
    /// Màn hình chính của game, bao gồm giả lập loading và các nút chức năng.
    /// </summary>
    public class HomeScreen : MonoBehaviour, IHomeScreen
    {
        #region Serialized Fields
        [Header("UI Groups")]
        [SerializeField] private GameObject _loadingOverlayGroup;
        [SerializeField] private GameObject _homeGroup;

        [Header("Buttons")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _exitButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _infoButton;

        [Header("Info Popup")]
        [Tooltip("InfoPopup gan truc tiep tu Scene de bat/tat. Neu de trong se emit event OnInfoClicked cho UIManager.")]
        [SerializeField] private InfoPopup _infoPopup;

        [Header("Audio")]
        [Tooltip("Thoi gian fade out nhac Home khi an Play.")]
        [SerializeField] private float _bgmFadeOutDuration = 1.5f;

        [Header("Animation")]
        [SerializeField] private Animations.HomeScreenAnimation _animation;
        #endregion

        #region Events
        public event Action OnPlayClicked;
        public event Action OnSettingsClicked;
        public event Action OnInfoClicked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Gan su kien cho nut Play
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            // Gan su kien cho nut Exit
            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitButtonClicked);
            }

            // Gan su kien cho nut Settings — emit event len parent (UIManager) de mo SettingsPopup.
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }

            // Gan su kien cho nut Info — mo InfoPopup truc tiep hoac emit event len UIManager.
            if (_infoButton != null)
            {
                _infoButton.onClick.AddListener(OnInfoButtonClicked);
            }
        }

        private void Start()
        {
            // Bat dau chuoi loading ngay khi bat
            Show();
        }

        private void OnDestroy()
        {
            if (_playButton != null)
            {
                _playButton.onClick.RemoveListener(OnPlayButtonClicked);
            }

            if (_exitButton != null)
            {
                _exitButton.onClick.RemoveListener(OnExitButtonClicked);
            }

            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OnSettingsButtonClicked);
            }

            if (_infoButton != null)
            {
                _infoButton.onClick.RemoveListener(OnInfoButtonClicked);
            }
        }
        #endregion

        #region Public Methods
        public void Show(bool skipLoading = false)
        {
            gameObject.SetActive(true);
            if (_infoPopup != null && _infoPopup.IsVisible)
            {
                _infoPopup.Hide(false);
            }

            if (_animation != null)
            {
                _animation.SetupInitialPositions();

                if (skipLoading)
                {
                    _loadingOverlayGroup.SetActive(false);
                    _animation.PlayHomeEnterAnimation();
                }
                else
                {
                    _animation.StartLoadingAnimation(() => {
                        _animation.PlayHomeEnterAnimation();
                    });
                }
            }
            else
            {
                // Fallback nếu không có Animation Script
                if (skipLoading)
                {
                    _loadingOverlayGroup.SetActive(false);
                    _homeGroup.SetActive(true);
                }
                else
                {
                    ShowLoadingFallback();
                }
            }
        }

        private async void ShowLoadingFallback()
        {
            _loadingOverlayGroup.SetActive(true);
            _homeGroup.SetActive(false);

            await Task.Delay(1500);

            if (this == null) return;

            _loadingOverlayGroup.SetActive(false);
            _homeGroup.SetActive(true);
        }

        public void Hide()
        {
            if (_infoPopup != null && _infoPopup.IsVisible)
            {
                _infoPopup.Hide(false);
            }
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Hien thi InfoPopup duoc gan truc tiep tren Scene.
        /// </summary>
        public void ShowInfoPopup()
        {
            if (_infoPopup != null)
            {
                _infoPopup.Show();
            }
        }

        /// <summary>
        /// An InfoPopup duoc gan truc tiep tren Scene.
        /// </summary>
        public void HideInfoPopup()
        {
            if (_infoPopup != null)
            {
                _infoPopup.Hide();
            }
        }
        #endregion

        #region Private Methods
        private void OnPlayButtonClicked()
        {
            // Fade out nhac Home ngay lap tuc khi an Play
            Core.GameEvents.TriggerMusicFadeOutRequested(_bgmFadeOutDuration);

            if (_animation != null)
            {
                _animation.PlayTransitionAnimation(
                    onSlideDown: () => {
                        // Goi truc tiep event hoac qua UIManager
                        Core.GameEvents.TriggerSpawnInitialPlayerRequest();
                    },
                    onSlideUp: () => {
                        Hide();
                        OnPlayClicked?.Invoke();
                        Debug.Log("[HomeScreen] Play button clicked - event emitted after transition.");
                    }
                );
            }
            else
            {
                // An man hinh Home
                Hide();

                // Spawn player (fallback)
                Core.GameEvents.TriggerSpawnInitialPlayerRequest();

                // Emit event len parent (UIManager)
                OnPlayClicked?.Invoke();
                Debug.Log("[HomeScreen] Play button clicked - event emitted.");
            }
        }

        /// <summary>
        /// Xu ly khi user nhan nut Settings tren Home: emit event len parent (UIManager) de mo SettingsPopup.
        /// </summary>
        private void OnSettingsButtonClicked()
        {
            OnSettingsClicked?.Invoke();
        }

        /// <summary>
        /// Xu ly khi user nhan nut Info tren Home: mo InfoPopup neu gan tren scene hoac emit event len UIManager.
        /// </summary>
        private void OnInfoButtonClicked()
        {
            if (_infoPopup != null)
            {
                _infoPopup.Show();
            }

            OnInfoClicked?.Invoke();
        }

        /// <summary>
        /// Xử lý khi người dùng nhấn nút Exit Game: hiện popup xác nhận,
        /// nếu đồng ý thì đóng ứng dụng.
        /// </summary>
        private void OnExitButtonClicked()
        {
            if (IConfirmationPopup.Instance == null)
            {
                Debug.LogWarning("[HomeScreen] IConfirmationPopup.Instance is null.");
                return;
            }

            IConfirmationPopup.Instance.Show(
                "Are you sure you want to exit the game?",
                // Xác nhận đóng game (trong editor sẽ không tắt nhưng vẫn chạy được).
                () => Application.Quit(),
                null
            );
        }
        #endregion
    }
}