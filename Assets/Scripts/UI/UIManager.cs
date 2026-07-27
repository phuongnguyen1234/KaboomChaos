using UnityEngine;
using Core.Interfaces;
using TMPro; // Cần cho TextMeshPro
using System.Collections;
using DG.Tweening;

namespace UI
{
    /// <summary>
    /// Quản lý các yếu tố UI chính của game như thông báo, timer.
    /// Đóng vai trò trung tâm điều phối các panel UI khác nhau.
    /// </summary>
    public class UIManager : MonoBehaviour, IUIManager
    {
        #region Fields
        [Header("Notification Panel")]
        [Tooltip("Panel chứa thông báo chung.")]
        [SerializeField] private GameObject _notificationPanel;
        [Tooltip("Text hiển thị nội dung thông báo.")]
        [SerializeField] private TextMeshProUGUI _notificationText;

        [Header("Timer Panel")]
        [Tooltip("Panel chứa đồng hồ đếm ngược.")]
        [SerializeField] private GameObject _timerPanel;
        [Tooltip("Text hiển thị thời gian.")]
        [SerializeField] private TextMeshProUGUI _timerText;

        [Header("Intensity Bar")]
        [Tooltip("Panel chứa thanh cường độ.")]
        [SerializeField] private GameObject _intensityBarPanel;
        [Tooltip("Mũi tên chỉ báo trên thanh cường độ.")]
        [SerializeField] private RectTransform _intensityArrow;
        [Tooltip("Khu vực có thể di chuyển của mũi tên (dùng để tính toán vị trí).")]
        [SerializeField] private RectTransform _intensityRange;
        
        [Header("Intensity Bar Animation")]
        [Tooltip("Vị trí bắt đầu của thanh cường độ (ngoài màn hình, tính từ vị trí gốc).")]
        [SerializeField] private Vector2 _intensityBarStartOffset = new Vector2(0, 200f);
        [Tooltip("Thời gian cho hiệu ứng trượt vào của thanh cường độ.")]
        [SerializeField] private float _intensityBarSlideInDuration = 0.5f;
        [Tooltip("Ease type cho hiệu ứng trượt vào.")]
        [SerializeField] private Ease _intensityBarSlideInEase = Ease.OutQuad;
        [Tooltip("Thời gian cho hiệu ứng chạy của mũi tên.")]
        [SerializeField] private float _arrowMoveDuration = 0.8f;
        [Tooltip("Ease type cho hiệu ứng chạy của mũi tên.")]
        [SerializeField] private Ease _arrowMoveEase = Ease.InOutSine;

        [Header("Current Intensity Display")]
        [Tooltip("Panel hiển thị độ khó hiện tại của round.")]
        [SerializeField] private GameObject _currentIntensityPanel;
        [Tooltip("Text hiển thị giá trị độ khó hiện tại của round.")]
        [SerializeField] private TextMeshProUGUI _currentIntensityText;

        [Header("Countdown")]
        [Tooltip("Panel chứa text đếm ngược đầu round.")]
        [SerializeField] private GameObject _countdownPanel;
        [Tooltip("Text hiển thị số đếm ngược.")]
        [SerializeField] private TextMeshProUGUI _countdownText;

        [Header("Crosshair")]
        [Tooltip("CanvasGroup của crosshair cho Shift Lock. GameObject chứa nó phải có component CanvasGroup.")]
        [SerializeField] private CanvasGroup _shiftLockCrosshairCanvasGroup;
        [Tooltip("CanvasGroup của crosshair cho góc nhìn thứ nhất. GameObject chứa nó phải có component CanvasGroup.")]
        [SerializeField] private CanvasGroup _firstPersonCrosshairCanvasGroup;

        // Coroutine đang chạy để có thể dừng lại nếu cần
        private Coroutine _notificationCoroutine;

        // Cache vị trí gốc của thanh intensity để dùng cho animation
        private Vector2 _intensityBarOriginalPosition;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (IUIManager.Instance != null && IUIManager.Instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
            else
            {
                IUIManager.Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // Ẩn các panel khi bắt đầu
            if (_notificationPanel != null) _notificationPanel.SetActive(false);
            if (_timerPanel != null) _timerPanel.SetActive(false);
            if (_intensityBarPanel != null)
            {
                // Cache vị trí gốc của thanh intensity để dùng cho animation
                _intensityBarOriginalPosition = _intensityBarPanel.GetComponent<RectTransform>().anchoredPosition;
                _intensityBarPanel.SetActive(false);
            }
            if (_countdownPanel != null) _countdownPanel.SetActive(false);
            if (_currentIntensityPanel != null) _currentIntensityPanel.SetActive(false);

            // Sử dụng alpha để ẩn crosshair thay vì SetActive(false)
            // Điều này giúp tránh giật lag khi bật/tắt nhanh (spam)
            if (_shiftLockCrosshairCanvasGroup != null)
            {
                _shiftLockCrosshairCanvasGroup.alpha = 0f;
            }
            if (_firstPersonCrosshairCanvasGroup != null)
            {
                _firstPersonCrosshairCanvasGroup.alpha = 0f;
            }
        }
        #endregion

        #region Public Methods (IUIManager Implementation)

        /// <summary>
        /// Hiển thị một thông báo trên màn hình trong một khoảng thời gian.
        /// </summary>
        /// <param name="message">Nội dung thông báo.</param>
        /// <param name="duration">Thời gian hiển thị (giây).</param>
        public void ShowNotification(string message, float duration)
        {
            if (_notificationPanel == null || _notificationText == null)
            {
                Debug.LogWarning("[UIManager] Notification panel or text is not assigned.", this);
                return;
            }

            // Nếu có thông báo cũ, dừng coroutine đó lại
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
            }

            _notificationCoroutine = StartCoroutine(ShowNotificationCoroutine(message, duration));
        }

        /// <summary>
        /// Cập nhật và hiển thị thời gian trên timer.
        /// </summary>
        /// <param name="seconds">Số giây còn lại để hiển thị.</param>
        public void UpdateTimer(int seconds)
        {
            if (_timerPanel == null || _timerText == null)
            {
                Debug.LogWarning("[UIManager] Timer panel or text is not assigned.", this);
                return;
            }
            
            if (!_timerPanel.activeSelf) _timerPanel.SetActive(true);
            _timerText.text = $"{seconds / 60:0}:{seconds % 60:00}";
        }

        /// <inheritdoc/>
        public void HideTimer()
        {
            if (_timerPanel != null) _timerPanel.SetActive(false);
        }

        /// <inheritdoc/>
        public void HideNotification()
        {
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
                _notificationCoroutine = null;
            }
            if (_notificationPanel != null) _notificationPanel.SetActive(false);
        }

        /// <inheritdoc/>
        public IEnumerator AnimateIntensityBar(float currentIntensity, float minIntensity, float maxIntensity)
        {
            // --- Safety Checks ---
            if (_intensityBarPanel == null || _intensityArrow == null || _intensityRange == null)
            {
                Debug.LogWarning("[UIManager] Intensity Bar components are not fully assigned. Skipping animation.", this);
                yield break;
            }

            // --- Animation Sequence ---

            // 1. Chuẩn bị: Di chuyển thanh intensity ra ngoài màn hình và kích hoạt nó.
            var intensityBarRect = _intensityBarPanel.GetComponent<RectTransform>();
            intensityBarRect.anchoredPosition = _intensityBarOriginalPosition + _intensityBarStartOffset;
            _intensityBarPanel.SetActive(true);

            // 2. Hiệu ứng trượt vào
            DOTween.To(() => intensityBarRect.anchoredPosition, x => intensityBarRect.anchoredPosition = x, _intensityBarOriginalPosition, _intensityBarSlideInDuration).SetEase(_intensityBarSlideInEase);
            yield return new WaitForSeconds(_intensityBarSlideInDuration);

            // 3. Hiệu ứng mũi tên chạy
            float normalizedValue = Mathf.InverseLerp(minIntensity, maxIntensity, currentIntensity);
            float rangeWidth = _intensityRange.rect.width;
            float arrowTargetX = normalizedValue * rangeWidth;
            DOTween.To(() => _intensityArrow.anchoredPosition, pos => _intensityArrow.anchoredPosition = pos, new Vector2(arrowTargetX, _intensityArrow.anchoredPosition.y), _arrowMoveDuration).SetEase(_arrowMoveEase);
            yield return new WaitForSeconds(_arrowMoveDuration);

            // 4. Chờ 1 giây
            yield return new WaitForSeconds(1f);

            // 5. Ẩn thanh intensity và reset vị trí mũi tên cho lần sau
            _intensityBarPanel.SetActive(false);
            _intensityArrow.anchoredPosition = new Vector2(0, _intensityArrow.anchoredPosition.y);
        }

        /// <inheritdoc/>
        public void ShowCurrentIntensity(float currentIntensity)
        {
            if (_currentIntensityPanel != null && _currentIntensityText != null)
            {
                _currentIntensityText.text = $"{currentIntensity:F1}"; // Định dạng hiển thị 1 chữ số thập phân, kể cả là số 0.
                _currentIntensityPanel.SetActive(true);
            }
            else Debug.LogWarning("[UIManager] Current Intensity Panel or Text is not assigned. Cannot display current intensity.", this);
        }

        /// <inheritdoc/>
        public void HideIntensityBar()
        {
            if (_intensityBarPanel != null) _intensityBarPanel.SetActive(false);
        }

        /// <inheritdoc/>
        public void HideCurrentIntensity()
        {
            if (_currentIntensityPanel != null) _currentIntensityPanel.SetActive(false);
        }

        /// <inheritdoc/>
        public IEnumerator ShowCountdown()
        {
            // Không ẩn intensity bar ở đây. Nó sẽ được hiển thị trong suốt round
            // và chỉ được ẩn ở PostRoundStage.
            // HideIntensityBar();
            if (_countdownPanel == null || _countdownText == null)
            {
                Debug.LogWarning("[UIManager] Countdown panel or text is not assigned.", this);
                yield break;
            }

            _countdownPanel.SetActive(true);

            _countdownText.text = "3";
            yield return new WaitForSeconds(1f);

            _countdownText.text = "2";
            yield return new WaitForSeconds(1f);

            _countdownText.text = "1";
            yield return new WaitForSeconds(1f);

            _countdownText.text = "Go!";
            yield return new WaitForSeconds(1f);

            _countdownPanel.SetActive(false);
        }

        /// <inheritdoc/>
        public void SetShiftLockCrosshair(bool active)
        {
            if (_shiftLockCrosshairCanvasGroup != null)
            {
                _shiftLockCrosshairCanvasGroup.alpha = active ? 1f : 0f;
            }
        }

        /// <inheritdoc/>
        public void SetFirstPersonCrosshair(bool active)
        {
            if (_firstPersonCrosshairCanvasGroup != null)
            {
                _firstPersonCrosshairCanvasGroup.alpha = active ? 1f : 0f;
            }
        }

        #endregion

        #region Private Methods & Coroutines

        private IEnumerator ShowNotificationCoroutine(string message, float duration)
        {
            _notificationText.text = message;
            _notificationPanel.SetActive(true);
            yield return new WaitForSeconds(duration);
            _notificationPanel.SetActive(false);
            _notificationCoroutine = null;
        }
        #endregion
    }
}