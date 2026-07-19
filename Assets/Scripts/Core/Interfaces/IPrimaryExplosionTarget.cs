using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Một interface đánh dấu (marker interface) để chỉ định một Collider là mục tiêu chính
    /// cho các tính toán của vụ nổ.
    /// Khi một vụ nổ phát hiện nhiều collider trên cùng một đối tượng,
    /// collider có component triển khai interface này sẽ được ưu tiên.
    /// </summary>
    public interface IPrimaryExplosionTarget
    {
    }
}