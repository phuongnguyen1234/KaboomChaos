using UnityEngine;
using UnityEngine.UI;
using System;
using Core.Interfaces.UI;
using Core;
using Core.Interfaces;

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

        public bool IsVisible => gameObject.activeSelf;

        private void Awake()
        {
            if (_resumeButton != null) _resumeButton.onClick.AddListener(Hide);
            if (_backToHomeButton != null) _backToHomeButton.onClick.AddListener(OnBackToHomeClicked);
            if (_resetCharacterButton != null) _resetCharacterButton.onClick.AddListener(OnResetCharacterClicked);
        }

                public void Show()
        {
            gameObject.SetActive(true);
            Time.timeScale = 0f; // Tạm dừng game
            GameEvents.IsPauseMenuVisible = true; // Báo cho CameraController & các input handler ngừng hoạt động
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            Time.timeScale = 1f; // Tiếp tục game
            GameEvents.IsPauseMenuVisible = false;
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
