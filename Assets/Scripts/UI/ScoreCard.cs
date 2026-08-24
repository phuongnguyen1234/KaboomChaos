using UnityEngine;
using TMPro;
using System.Collections;
using DG.Tweening;
using Core;
using Core.Interfaces.UI;

namespace UI
{
    /// <summary>
    /// Bảng điểm (Score Card) hiển thị kết quả cuối round riêng cho từng người chơi:
    /// Survival Score, Base Multiplier, Win Multiplier và Total Credits.
    /// Score Card xuất hiện bằng cách trượt LÊN và biến mất bằng cách trượt XUỐNG
    /// sau <see cref="_displayDuration"/> giây.
    /// </summary>
    public class ScoreCard : MonoBehaviour, IScoreCard
    {
        #region Fields

        [Header("Panel")]
        [Tooltip("RectTransform gốc của Score Card (phải là child của UI Canvas).")]
        [SerializeField] private RectTransform _panelRect;

        [Header("Text Fields")]
        [Tooltip("Text hiển thị giá trị Survival Score.")]
        [SerializeField] private TextMeshProUGUI _survivalScoreText;
        [Tooltip("Text hiển thị Base Multiplier (vd: x1.0, x1.25).")]
        [SerializeField] private TextMeshProUGUI _baseMultiplierText;
        [Tooltip("Text hiển thị Win Multiplier (dựa trên win streak).")]
        [SerializeField] private TextMeshProUGUI _winMultiplierText;
        [Tooltip("Text hiển thị tổng credits nhận được.")]
        [SerializeField] private TextMeshProUGUI _totalCreditsText;

        [Header("Animation Settings")]
        [Tooltip("Thời gian (giây) Score Card di chuyển LÊN để xuất hiện trên màn hình.")]
        [SerializeField] private float _slideInDuration = 0.5f;
        [Tooltip("Thời gian (giây) Score Card di chuyển XUỐNG để biến mất.")]
        [SerializeField] private float _slideOutDuration = 0.4f;
        [Tooltip("Khoảng dịch chuyển theo trục Y khi trượt (dương = lên, âm = xuống).")]
        [SerializeField] private Vector2 _slideOffset = new(0, -350f);
        [Tooltip("Thời gian hiển thị (giây) trước khi Score Card tự động trượt xuống ẩn đi. Mặc định 5 giây theo yêu cầu.")]
        [SerializeField] private float _displayDuration = 5f;
        [Tooltip("Độ trễ (giây) trước khi Score Card bắt đầu xuất hiện sau khi Show() được gọi. Yêu cầu: 1 giây.")]
        [SerializeField] private float _showDelay = 1f;

        // Vị trí gốc của panel khi hiển thị bình thường (thường là giữa màn hình).
        // Sử dụng localPosition (Vector3) thay vì anchoredPosition vì ta dùng tween DOLocalMove (có sẵn trong mọi bản DOTween),
        // trong khi DOAnchorPos thuộc DOTween UI module - không phải lúc nào cũng được include.
        private Vector3 _originalLocalPosition;

        // Cờ đánh dấu đã cache vị trí gốc hay chưa - dùng để phòng hộ trường hợp
        // GameObject bị đặt inactive sẵn trong scene khiến Awake chưa từng chạy.
        private bool _isOriginalPositionCached;
        private Coroutine _autoHideCoroutine;
        private Coroutine _showDelayCoroutine;
        private Tween _slideTween;

        #endregion

        #region Properties

        /// <summary>
        /// Cho biết Score Card có đang hiển thị trên màn hình hay không.
        /// </summary>
        public bool IsVisible { get; private set; }

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (_panelRect != null)
            {
                // LƯU Ý THỨ TỰ: phải cache vị trí gốc TRƯỚC KHI deactivate và TRƯỚC KHI Show()
                // ghi đè localPosition (đưa panel ra ngoài màn hình trong thời gian trễ),
                // nếu không _originalLocalPosition sẽ lưu nhầm vị trí offset.
                _originalLocalPosition = _panelRect.localPosition;
                _isOriginalPositionCached = true;
                _panelRect.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            // Dừng tween nếu có để tránh callback sau khi object bị hủy.
            _slideTween?.Kill();
        }

        #endregion

        #region Public Methods (IScoreCard Implementation)

        /// <summary>
        /// Hiển thị Score Card với dữ liệu điểm đã tính toán. Panel sẽ tự ẩn sau _displayDuration giây.
        /// </summary>
        /// <param name="data">Dữ liệu điểm và kết quả thắng/thua.</param>
        public void Show(ScoreCardData data)
        {
            if (_panelRect == null)
            {
                Debug.LogWarning("[ScoreCard] Root RectTransform is not assigned! Cannot show Score Card.", this);
                return;
            }

            // Phòng hộ: nếu GameObject bị đặt inactive sẵn trong scene thì Awake chưa từng chạy,
            // phải cache vị trí gốc NGAY TRƯỚC khi ghi đè localPosition bằng vị trí offset.
            if (!_isOriginalPositionCached)
            {
                _originalLocalPosition = _panelRect.localPosition;
                _isOriginalPositionCached = true;
            }

            ApplyData(data);

            // Hủy toàn bộ animation/coroutine cũ (kể cả lần trễ chưa kịp chạy) trước khi lên lịch mới.
            CancelPendingAnimations();

            // SỬA LỖI "Coroutine couldn't be started because the game object 'ScoreCard' is inactive":
            // Script ScoreCard và _panelRect nằm CÙNG một GameObject, nên KHÔNG THỂ giữ GameObject
            // ở trạng thái inactive trong thời gian trễ (Unity từ chối StartCoroutine trên object inactive).
            // Giải pháp: kích hoạt GameObject NGAY LẬP TỨC nhưng đặt panel ở vị trí xuất phát của
            // hiệu ứng trượt lên (ngoài màn hình phía dưới) -> người chơi vẫn KHÔNG thấy gì
            // trong _showDelay giây, mà coroutine delay vẫn chạy bình thường.
            IsVisible = false;
            _panelRect.localPosition = _originalLocalPosition + new Vector3(0f, _slideOffset.y, 0f);
            _panelRect.gameObject.SetActive(true);

            if (_showDelay > 0f)
            {
                _showDelayCoroutine = StartCoroutine(DelayedShowCoroutine(_showDelay));
            }
            else
            {
                BeginSlideIn();
            }
        }

        /// <inheritdoc/>
        public void Hide()
        {
            // Hủy mọi coroutine/tween đang chờ hoặc đang chạy (bao gồm lần trễ chưa hiển thị).
            CancelPendingAnimations();
            IsVisible = false;
            if (_panelRect != null) _panelRect.gameObject.SetActive(false);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Điền dữ liệu vào các TextField của Score Card.
        /// </summary>
        private void ApplyData(ScoreCardData data)
        {
            if (_survivalScoreText != null) _survivalScoreText.text = data.SurvivalScore.ToString();
            if (_baseMultiplierText != null) _baseMultiplierText.text = $"x{data.BaseMultiplier:0.##}";
            if (_winMultiplierText != null) _winMultiplierText.text = $"x{data.WinMultiplier:0.##}";
            if (_totalCreditsText != null) _totalCreditsText.text = data.TotalCredits.ToString();
        }

        /// <summary>
        /// Coroutine chờ _showDelay giây rồi mới cho Score Card xuất hiện.
        /// </summary>
        private IEnumerator DelayedShowCoroutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            _showDelayCoroutine = null;
            BeginSlideIn();
        }

        /// <summary>
        /// Thực hiện hiệu ứng trượt LÊN để hiển thị panel và lên lịch tự ẩn sau _displayDuration giây.
        /// </summary>
        private void BeginSlideIn()
        {
            // Reset vị trí về trạng thái "trượt lên": bắt đầu từ dưới (offset âm), di chuyển lên vị trí gốc.
            _panelRect.localPosition = _originalLocalPosition + new Vector3(0f, _slideOffset.y, 0f);
            _panelRect.gameObject.SetActive(true);
            IsVisible = true;

            // Hiệu ứng xuất hiện: DI CHUYỂN LÊN. DOLocalMove là Transform extension có sẵn trong DOTween core
            // (không cần UI module như DOAnchorPos).
            _slideTween?.Kill();
            _slideTween = _panelRect
                .DOLocalMove(_originalLocalPosition, _slideInDuration)
                .SetEase(Ease.OutCubic);

            // Tự động ẩn sau _displayDuration giây (trượt xuống).
            _autoHideCoroutine = StartCoroutine(AutoHideCoroutine(_displayDuration));
        }

        /// <summary>
        /// Hủy mọi coroutine/tween đang treo (delay hiển thị, auto-hide, tween trượt).
        /// </summary>
        private void CancelPendingAnimations()
        {
            if (_showDelayCoroutine != null)
            {
                StopCoroutine(_showDelayCoroutine);
                _showDelayCoroutine = null;
            }
            if (_autoHideCoroutine != null)
            {
                StopCoroutine(_autoHideCoroutine);
                _autoHideCoroutine = null;
            }
            _slideTween?.Kill();
        }

        /// <summary>
        /// Coroutine chờ một khoảng thời gian rồi cho Score Card trượt xuống (biến mất).
        /// </summary>
        private IEnumerator AutoHideCoroutine(float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);

            if (_panelRect == null) yield break;

            // Hiệu ứng biến mất: DI CHUYỂN XUỐNG rồi mới ẩn GameObject.
            _slideTween?.Kill();
            _slideTween = _panelRect
                .DOLocalMove(_originalLocalPosition + new Vector3(0f, _slideOffset.y, 0f), _slideOutDuration)
                .SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    _panelRect.gameObject.SetActive(false);
                    IsVisible = false;
                });

            _autoHideCoroutine = null;
        }

        #endregion
    }
}