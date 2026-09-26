using System;
using System.Collections;

namespace Core.Interfaces.UI
{
    /// <summary>
    /// Interface cho việc quản lý UI
    /// </summary>
    public interface IUIManager
    {
        /// <summary>
        /// Thể hiện Singleton toàn cục của IUIManager.
        /// </summary>
        static IUIManager Instance { get; set; }

        /// <summary>
        /// Transform UI mặc định chứa các text nổi (floating text) như chỉ số sát thương, hồi máu...
        /// </summary>
        UnityEngine.RectTransform FloatingTextContainer { get; }

        /// <summary>
        /// Transform UI chứa các text nổi liên quan đến HP (sát thương, hồi máu).
        /// </summary>
        UnityEngine.RectTransform HpFloatingTextContainer { get; }

        /// <summary>
        /// Transform UI chứa các text nổi liên quan đến Collectible (Coin, Battery...).
        /// </summary>
        UnityEngine.RectTransform CollectibleFloatingTextContainer { get; }

        /// <summary>
        /// Hiển thị một thông báo trên màn hình trong một khoảng thời gian.
        /// </summary>
        /// <param name="message">Nội dung thông báo.</param>
        /// <param name="duration">Thời gian hiển thị (giây).</param>
        void ShowNotification(string message, float duration);
        
        /// <summary> Ẩn thông báo hiện tại ngay lập tức. </summary>
        void HideNotification();

        /// <summary>
        /// Hien thi thong bao tren man hinh va GIU NGUYEN khong tu dong an.
        /// Panel thong bao se luon hien thi cho den khi co thong bao moi thay the
        /// hoac khi roi khoi gameloop (vi du quay ve Home).
        /// </summary>
        /// <param name="message">Noi dung thong bao.</param>
        void ShowPersistentNotification(string message);

        /// <summary> Cập nhật và hiển thị thời gian trên timer. </summary>
        void UpdateTimer(int seconds);

        /// <summary>
        /// Bat/tat trang thai cam bao (danger) cua TimerPanel: doi mau text + icon thanh do va phat SFX canh bao.
        /// Dung khi con 30 giay cuoi round.
        /// </summary>
        /// <param name="danger">True de bat trang thai do, False de tro lai mau binh thuong.</param>
        void SetTimerDangerState(bool danger);

        /// <summary> Ẩn panel của timer. </summary>
        void HideTimer();

        /// <summary> Hiển thị hoặc ẩn crosshair của Shift Lock. </summary>
        /// <param name="active">True để hiển thị, false để ẩn.</param>
        void SetShiftLockCrosshair(bool active);

        /// <summary> Hiển thị hoặc ẩn crosshair của góc nhìn thứ nhất. </summary>
        /// <param name="active">True để hiển thị, false để ẩn.</param>
        void SetFirstPersonCrosshair(bool active);

        /// <summary>
        /// Hiển thị thanh độ khó
        /// </summary>
        /// <param name="currentIntensity"></param>
        /// <param name="minIntensity"></param>
        /// <param name="maxIntensity"></param>
        IEnumerator AnimateIntensityBar(float currentIntensity, float minIntensity, float maxIntensity);

        /// <summary>
        /// Hiển thị panel text độ khó hiện tại của round.
        /// </summary>
        /// <param name="currentIntensity">Giá trị độ khó để hiển thị.</param>
        void ShowCurrentIntensity(float currentIntensity);

        /// <summary>
        /// Ẩn thanh độ khó (thanh có animation).
        /// </summary>
        void HideIntensityBar();

        /// <summary>
        /// Ẩn panel text độ khó hiện tại của round.
        /// </summary>
        void HideCurrentIntensity();

        /// <summary>
        /// Hiển thị đếm ngược vào round.
        /// </summary>
        /// <returns></returns>
        IEnumerator ShowCountdown();

        /// <summary>
        /// An panel dem nguoc ngay lap tuc.
        /// </summary>
        void HideCountdown();

        /// <summary>
        /// Mở/Đóng Pause Menu.
        /// </summary>
        void TogglePauseMenu();

        /// <summary>
        /// Hiển thị Score Card với dữ liệu điểm đã tính toán.
        /// Score Card sẽ tự động ẩn sau 5 giây (cấu hình trong component ScoreCard).
        /// </summary>
        /// <param name="data">Dữ liệu điểm: Survival Score, Multiplier, Win Multiplier, Total Credits, kết quả thắng/thua.</param>
        void ShowScoreCard(Core.ScoreCardData data);

        /// <summary>
        /// Hiển thị Score Card ngay lập tức.
        /// </summary>
        void HideScoreCard();

        /// <summary>
        /// Chạy hiệu ứng transition màn hình (Scale logo, bung CircleMask).
        /// </summary>
        /// <param name="onCovered">Callback thực hiện hành động khi màn hình đã che hết (VD: Teleport player, dọn dẹp map).</param>
        /// <param name="onComplete">Callback thực hiện khi transition hoàn tất.</param>
        /// <param name="holdDurationOverride">Thời gian giữ màn hình che kín tùy chọn (giây).</param>
        void PlayTransition(Action onCovered, Action onComplete = null, float? holdDurationOverride = null);

        /// <summary>
        /// Phát tiếng còi khi round bắt đầu.
        /// </summary>
        void PlayRoundStartSfx();

        /// <summary>
        /// Phát tiếng còi + tiếng chuông khi round kết thúc.
        /// </summary>
        void PlayRoundEndSfx();

        /// <summary>
        /// Mo popup Shop.
        /// </summary>
        void OpenShopPopup();
    }
}