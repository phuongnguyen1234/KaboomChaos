namespace Core.Interfaces
{
    /// <summary>
    /// Interface định nghĩa "hợp đồng" cho tất cả các hành vi của khiên.
    /// </summary>
    public interface IShieldBehavior
    {
        /// <summary>
        /// Được gọi khi khiên được áp dụng lần đầu cho người chơi.
        /// Dùng để thiết lập trạng thái ban đầu.
        /// </summary>
        void OnApply(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data);

        /// <summary>
        /// Được gọi mỗi FixedUpdate khi khiên đang hoạt động.
        /// Dùng cho các hiệu ứng vật lý liên tục.
        /// </summary>
        void OnFixedUpdate(IPlayer player, IBaseShieldData data);

        /// <summary>
        /// Được gọi khi khiên bị gỡ bỏ.
        /// Dùng để dọn dẹp các trạng thái đã thay đổi trên người chơi.
        /// </summary>
        void OnRemove(IPlayer player, IPlayerShieldController shieldController, IBaseShieldData data);

        /// <summary>
        /// Được gọi khi người chơi có khiên này nhận sát thương.
        /// Behavior có thể quyết định hấp thụ, phản lại, hoặc thay đổi sát thương.
        /// </summary>
        /// <returns>Lượng sát thương còn lại. Trả về giá trị âm để báo hiệu khiên cần bị phá vỡ.</returns>
        float OnDamageTaken(float damageAmount, DamageSourceType sourceType, StatusEffectType effectContext, IBaseShieldData data);

        /// <summary>
        /// Được gọi khi người chơi sắp nhận một hiệu ứng trạng thái.
        /// </summary>
        bool OnStatusEffectApplied(StatusEffectType effect, IBaseShieldData data);
    }
}