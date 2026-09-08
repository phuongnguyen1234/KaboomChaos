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
        [Header("Animation")]
        [SerializeField] private Animations.HomeScreenAnimation _animation;
        #endregion

        #region Events
        public event Action OnPlayClicked;
        public event Action OnSettingsClicked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            // Gán sự kiện cho nút Play
            if (_playButton != null)
            {
                _playButton.onClick.AddListener(OnPlayButtonClicked);
            }

            // Gán sự kiện cho nút Exit
            if (_exitButton != null)
            {
                _exitButton.onClick.AddListener(OnExitButtonClicked);
            }

            // Gán sự kiện cho nút Settings — emit event lên parent (UIManager) de mo SettingsPopup.
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OnSettingsButtonClicked);
            }
        }

        private void Start()
        {
            // Bắt đầu chuỗi loading ngay khi bật
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
        }
        #endregion

        #region Public Methods
        public void Show(bool skipLoading = false)
        {
            gameObject.SetActive(true);

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
            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        private void OnPlayButtonClicked()
        {
            if (_animation != null)
            {
                _animation.PlayTransitionAnimation(
                    onSlideDown: () => {
                        // Gọi trực tiếp event hoặc qua UIManager
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
                // Ẩn màn hình Home
                Hide();

                // Spawn player (fallback)
                Core.GameEvents.TriggerSpawnInitialPlayerRequest();

                // Emit event lên parent (UIManager)
                OnPlayClicked?.Invoke();
                Debug.Log("[HomeScreen] Play button clicked - event emitted.");
            }
        }

        /// <summary>
        /// Xu ly khi user nhan nut Settings tren Home: emit event lên parent
        /// (UIManager) care mo popup Settings.
        /// </summary>
        private void OnSettingsButtonClicked()
        {
            OnSettingsClicked?.Invoke();
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