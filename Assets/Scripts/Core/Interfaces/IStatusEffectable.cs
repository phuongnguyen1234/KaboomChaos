namespace Core.Interfaces
{
    /// <summary>
    /// Các loại nguồn gây sát thương khác nhau.
    /// </summary>
    public enum DamageSourceType
    {
        Generic, // Sát thương chung, không có hiệu ứng đặc biệt
        Explosion, // Sát thương từ vụ nổ
        StatusEffectContact, // Sát thương khi chạm vào đối tượng có hiệu ứng trạng thái (Burning, Electrified)
        EnvironmentalContact, // Sát thương khi chạm vào môi trường (ví dụ: dung nham)
        StatusEffectDOT, // Sát thương theo thời gian từ hiệu ứng trạng thái đã áp dụng lên bản thân (Damage Over Time)
    }

    /// <summary>
    /// Các loại hiệu ứng trạng thái mà bom có thể gây ra.
    /// </summary>
    public enum StatusEffectType
    {
        None,
        Burning,
        Electrified,
        Frozen,
        Obsidian,
    }

    /// <summary>
    /// Interface cho các đối tượng có thể nhận và xử lý các hiệu ứng trạng thái (lửa, băng...).
    /// Việc triển khai interface này sẽ chịu trách nhiệm xử lý logic hình ảnh,
    /// ví dụ như thay đổi material hoặc shader của đối tượng.
    /// </summary>
    public interface IStatusEffectable
    {
        /// <summary>
        /// Áp dụng một hiệu ứng trạng thái lên đối tượng.
        /// </summary>
        /// <param name="effect">Loại hiệu ứng để áp dụng.</param>
        /// <param name="duration">Thời gian hiệu ứng tồn tại (giây).</param>
        void ApplyStatusEffect(StatusEffectType effect, float duration);
    }
}