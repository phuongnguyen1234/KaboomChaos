using System;
using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho controller quản lý các khiên của người chơi.
    /// </summary>
    public interface IPlayerShieldController
    {
        /// <summary>
        /// Kiểm tra xem có bất kỳ khiên nào đang hoạt động không.
        /// </summary>
        bool IsShieldActive { get; }

        /// <summary>
        /// Kiểm tra xem người chơi có đang mang Khiên Lửa không.
        /// </summary>
        bool HasFireShield { get; }

        /// <summary>
        /// Áp dụng một loại khiên mới cho người chơi.
        /// </summary>
        /// <param name="shieldMusic">Nhạc nền tùy chọn để phát cho khiên này (dùng cho Magic Shield).</param>
        void ApplyShield(IBaseShieldData shieldData);

        /// <summary>
        /// Kiểm tra xem một loại khiên cụ thể (dựa trên Type của data) có đang hoạt động không.
        /// </summary>
        /// <param name="shieldDataType">Type của lớp dữ liệu khiên (ví dụ: typeof(CrystalShieldData)).</param>
        /// <returns>True nếu khiên loại đó đang hoạt động.</returns>
        bool IsShieldTypeActive(Type shieldDataType);

        /// <summary>
        /// Đếm số lượng khiên đang hoạt động dựa trên loại dữ liệu khiên.
        /// </summary>
        int GetActiveShieldCount(Type shieldDataType);

        /// <summary>
        /// Xử lý một hiệu ứng trạng thái sắp được áp dụng.
        /// </summary>
        /// <returns>True nếu khiên chặn hiệu ứng, False nếu không.</returns>
        bool ProcessStatusEffect(StatusEffectType effect);

        /// <summary>
        /// Gỡ bỏ tất cả các khiên đang hoạt động khỏi người chơi.
        /// </summary>
        void RemoveAllShields();

        /// <summary>
        /// Xử lý sát thương đến.
        /// </summary>
        /// <returns>Lượng sát thương còn lại sau khi khiên đã xử lý.</returns>
        float ProcessDamage(float amount, DamageSourceType sourceType, StatusEffectType effectContext);
    }
}