using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng có thể phản ứng với một vụ nổ một cách đặc biệt,
    /// thay vì chỉ nhận một lực vật lý thông thường.
    /// Ví dụ: người chơi có thể bị ragdoll hoặc chỉ bị đẩy lùi tùy thuộc vào lực tác động.
    /// </summary>
    public interface IExplosionReactable
    {
        /// <summary>
        /// Được gọi bởi nguồn nổ, truyền vào lực và điểm tác động.
        /// </summary>
        void OnExplosionHit(Vector3 force, Vector3 point);
    }
}