using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using Core.Interfaces.UI;

namespace UI
{
    /// <summary>
    /// Popup xác nhận với nút Đồng ý và Hủy.
    /// </summary>
    public class ConfirmationPopup : MonoBehaviour, IConfirmationPopup
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;

        private void Awake()
        {
            if (IConfirmationPopup.Instance != null && IConfirmationPopup.Instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
                return;
            }

            IConfirmationPopup.Instance = this;

            if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmClicked);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(OnCancelClicked);

            // QUAN TRỌNG (trả lời câu hỏi khởi tạo):
            // Singleton đăng ký tại Awake, tức là NGAY TẠI THỜI ĐIỂM KHỞI ĐỘNG GAME
            // (gameObject nằm sẵn trong scene/prefab và PHẢI ACTIVE lúc boot để Awake chạy).
            // Sau khi đăng ký, popup được ẨN NGAY để không hiển thị lung tung.
            // Show() sẽ được gọi lại (SetActive(true)) khi người chơi cần xác nhận.
            gameObject.SetActive(false);
        }

        public void Show(string message, Action onConfirm, Action onCancel = null)
        {
            if (_messageText != null) _messageText.text = message;
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
            _onConfirm = null;
            _onCancel = null;
        }

        private void OnConfirmClicked()
        {
            _onConfirm?.Invoke();
            Hide();
        }

        private void OnCancelClicked()
        {
            _onCancel?.Invoke();
            Hide();
        }
    }
}
