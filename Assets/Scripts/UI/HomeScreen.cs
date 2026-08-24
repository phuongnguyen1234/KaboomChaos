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
        #endregion

        #region Events
        public event Action OnPlayClicked;
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
        }
        #endregion

        #region Public Methods
        public async void Show(bool skipLoading = false)
        {
            gameObject.SetActive(true);

            // Quay thẳng para Home (vídeo: Back to Home) - salta la pantalla de loading.
            if (skipLoading)
            {
                _loadingOverlayGroup.SetActive(false);
                _homeGroup.SetActive(true);
                return;
            }

            // 1. Hiển thị loading overlay, ẩn Home group
            _loadingOverlayGroup.SetActive(true);
            _homeGroup.SetActive(false);

            // 2. Delay 1.5s để giả lập quá trình tải tài nguyên
            await Task.Delay(1500);

            // Kiểm tra null trong trường hợp object bị destroy khi đang đợi Task
            if (this == null) return;

            // 3. Tắt loading overlay, bật Home group
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
            // Ẩn màn hình Home
            Hide();

            // Emit event lên parent (UIManager) - pattern giống Vue
            // Parent sẽ xử lý logic game loop
            OnPlayClicked?.Invoke();
            Debug.Log("[HomeScreen] Play button clicked - event emitted.");
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