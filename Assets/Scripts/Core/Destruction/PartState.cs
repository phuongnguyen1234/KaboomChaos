namespace Core
{
    /// <summary>
    /// Định nghĩa các trạng thái có thể có của một mảnh vỡ trong hệ thống phá hủy.
    /// </summary>
    public enum PartState
    {
        /// <summary>
        /// Trạng thái ban đầu, nguyên vẹn, là một phần của công trình và có isKinematic = true.
        /// </summary>
        Intact,
        /// <summary>
        /// Lỏng lẻo, chuyển động tự do, isKinematic = false, chịu tác động của vật lý.
        /// </summary>
        Loose
    }
}