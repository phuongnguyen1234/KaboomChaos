using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Mở rộng IDamageable cho các đối tượng có thể xử lý sát thương và vật lý từ một vụ nổ trong một lệnh gọi duy nhất.
    /// Điều này giúp giải quyết các vấn đề về thứ tự thực thi khi một đối tượng có thể bị phá hủy bởi sát thương trước khi lực vật lý được áp dụng.
    /// </summary>
    public interface IExplosionDamageable : IDamageable
    {
        /// <summary>
        /// Áp dụng sát thương và lực từ một vụ nổ.
        /// </summary>
        /// <param name="bombData">Dữ liệu của quả bom gây ra vụ nổ, để có thể xử lý các hiệu ứng đặc biệt.</param>
        void TakeExplosionDamage(float amount, Vector3 force, Vector3 point, IBaseBombData bombData);
    }
}