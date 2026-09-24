using UnityEngine;
using UnityEngine.UI;
using Core.Interfaces.UI;
using Core;
using DG.Tweening;

namespace UI
{
    /// <summary>
    /// Quản lý Pause Menu của trò chơi.
    /// </summary>
    public class PauseMenu : MonoBehaviour, IPauseMenu
    {
        [Header("Buttons")]
        [SerializeField] private Button _resumeButton;
        [SerializeField] private Button _backToHomeButton;
        [SerializeField] private Button _resetCharacterButton;

        [Header("Settings")]
        [Tooltip("(Option 1) Panel SettingsUI tamplasar direct tan Pause Menu (in-place panel).")]
        [SerializeField] private SettingsUI _settingsUI;

        [Header("Animation")]
        [Tooltip("CanvasGroup để thực hiện hiệu ứng fade in/out.")]
        [SerializeField] private CanvasGroup _canvasGroup;
        [Tooltip("Thời gian hiệu ứng fade.")]
        [SerializeField] private float _fadeDuration = 0.3f;

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(Hide);
            if (_backToHomeButton != null) _backToHomeButton.onClick.AddListener(OnBackToHomeClicked);
            if (_resetCharacterButton != null) _resetCharacterButton.onClick.AddListener(OnResetCharacterClicked);

            if (_canvasGroup == null) _canvasGroup = GetComponent<CanvasGroup>();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            
            if (_canvasGroup != null)
            {
                _canvasGroup.DOKill();
                _canvasGroup.alpha = 0f;
                _canvasGroup.DOFade(1f, _fadeDuration).SetUpdate(true);
            }

            Time.timeScale = 0f; // Tạm dừng game
            GameEvents.IsPauseMenuVisible = true; // Báo cho CameraController & các input handler ngừng hoạt động
            GameEvents.TriggerMusicPauseRequested(); // Tam dung BGM khi mo Pause Menu
        }

        public void Hide()
        {
            // Cancel orice rebind in curs cand user dong Pause Menu (si khong inchide popup).
            if (_settingsUI != null)
            {
                _settingsUI.CancelRebind();
            }

            Time.timeScale = 1f; // Tiếp tục game
            GameEvents.IsPauseMenuVisible = false;
            GameEvents.TriggerMusicResumeRequested(); // Tiep tuc BGM khi dong Pause Menu

            if (_canvasGroup != null)
            {
                _canvasGroup.DOKill();
                _canvasGroup.DOFade(0f, _fadeDuration).SetUpdate(true).OnComplete(() =>
                {
                    gameObject.SetActive(false);
                });
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        public void Toggle()
        {
            if (IsVisible) Hide();
            else Show();
        }

        private void OnBackToHomeClicked()
        {
            if (IConfirmationPopup.Instance == null)
            {
                Debug.LogWarning("[PauseMenu] IConfirmationPopup.Instance is null.");
                return;
            }
            
            IConfirmationPopup.Instance.Show(
                "Are you sure you want to return to Home?",
                () => 
                {
                    Time.timeScale = 1f;
                    // Kích hoạt event để về Home
                    GameEvents.TriggerReturnToHomeRequest(); 
                    Hide();
                }
            );
        }

        private void OnResetCharacterClicked()
        {
            if (IConfirmationPopup.Instance == null)
            {
                Debug.LogWarning("[PauseMenu] IConfirmationPopup.Instance is null.");
                return;
            }

            IConfirmationPopup.Instance.Show(
                "Are you sure you want to reset your character?",
                () => 
                {
                    ResetPlayerCharacter();
                    Hide();
                }
            );
        }

        private void ResetPlayerCharacter()
        {
            // Send the reset request through the global event (Core). PlayerManager resolves
            // the current player and the player itself listens and dies (Roblox-style reset).
            // No direct dependency on the Managers assembly is needed here.
            GameEvents.TriggerResetPlayerRequested();
        }
    }
}
