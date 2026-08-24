namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng có thể nhận sát thương.
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Áp dụng một lượng sát thương lên đối tượng.
        /// </summary>
        /// <param name="amount">Lượng sát thương.</param>
        /// <param name="damageSource">Loại sát thương</param>
        void TakeDamage(float amount, DamageSourceType sourceType = DamageSourceType.Generic, StatusEffectType effectContext = StatusEffectType.None);

        /// <summary>
        /// Kiểm tra nếu đối tượng còn sống
        /// </summary>
        bool IsAlive { get; }

        float CurrentHealth {get;}
    }
}