using Core.Interfaces;
using Collectibles.Data;

namespace Collectibles.Behaviors
{
    /// <summary>
    /// Lớp cơ sở triển khai IShieldBehavior, cung cấp các hành vi mặc định.
    /// Đây là một lớp C# thông thường, không phải ScriptableObject.
    /// </summary>
    public class BaseShieldBehavior : IShieldBehavior
    {
        protected IBaseShieldData _data;

        public BaseShieldBehavior(BaseShieldData data)
        {
            _data = data;
        }

        public virtual void OnApply(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data)
        {
            // Mặc định không làm gì khi áp dụng
        }

        public virtual void OnFixedUpdate(IPlayer player, IBaseShieldData data)
        {
            // Mặc định không có logic chạy liên tục
        }

        public virtual void OnRemove(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data)
        {
            // Mặc định không cần dọn dẹp gì đặc biệt
        }

        public virtual float OnDamageTaken(float damageAmount, DamageSourceType sourceType, StatusEffectType effectContext, IBaseShieldData data)
        {
            // Hành vi mặc định: hấp thụ toàn bộ sát thương.
            // Nếu là khiên dùng một lần (duration <= 0), nó sẽ bị phá vỡ.
            // Nếu là khiên có thời hạn (duration > 0), nó sẽ tồn tại.
            if (data.Duration <= 0)
            {
                return -1f; // Hấp thụ và báo hiệu để phá vỡ
            }
            return 0f; // Chỉ hấp thụ
        }

        public virtual bool OnStatusEffectApplied(StatusEffectType effect, IBaseShieldData data)
        {
            // Mặc định, khiên không chặn hiệu ứng trạng thái.
            return false;
        }
    }
}