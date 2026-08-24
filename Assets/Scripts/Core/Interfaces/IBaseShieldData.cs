using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho dữ liệu cấu hình của một hiệu ứng khiên.
    /// </summary>
    public interface IBaseShieldData
    {
        /// <summary>Tên của hiệu ứng.</summary>
        string EffectName { get; }

        /// <summary>Thời gian hiệu lực của khiên (giây).</summary>
        float Duration { get; }

        /// <summary>Prefab hiệu ứng hình ảnh sẽ được gắn vào người chơi.</summary>
        GameObject ShieldVFX { get; }

        /// <summary>Màu sắc của hiệu ứng khiên (nếu VFX hỗ trợ).</summary>
        Color ShieldColor { get; }

        /// <summary>
        /// Đặt true để khiên có thời gian tồn tại không giới hạn.
        /// Khi true, khiên sẽ không tự hết hạn theo Duration, mà chỉ bị phá vỡ bởi sát thương hoặc khi round kết thúc.
        /// </summary>
        bool UnlimitedDuration { get; }
    }
}