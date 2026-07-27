using System.Collections;
using UnityEngine;

namespace Core.Interfaces
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
        /// Hiển thị một thông báo trên màn hình trong một khoảng thời gian.
        /// </summary>
        /// <param name="message">Nội dung thông báo.</param>
        /// <param name="duration">Thời gian hiển thị (giây).</param>
        void ShowNotification(string message, float duration);
        
        /// <summary> Ẩn thông báo hiện tại ngay lập tức. </summary>
        void HideNotification();

        /// <summary> Cập nhật và hiển thị thời gian trên timer. </summary>
        void UpdateTimer(int seconds);

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
    }
}