namespace Core.Interfaces
{
    /// <summary>
    /// Các loại hiệu ứng trạng thái mà bom có thể gây ra.
    /// </summary>
    public enum StatusEffectType
    {
        None,
        Fire,
        Electrified,
        Frozen,
        
    }

    /// <summary>
    /// Interface cho các đối tượng có thể nhận và xử lý các hiệu ứng trạng thái (lửa, băng...).
    /// </summary>
    public interface IStatusEffectable
    {
        /// <summary>
        /// Áp dụng một hiệu ứng trạng thái lên đối tượng.
        /// </summary>
        void ApplyStatusEffect(StatusEffectType effect, float duration);
    }
}