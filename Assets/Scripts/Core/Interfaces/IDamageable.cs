using UnityEngine;

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
        void TakeDamage(float amount);

        bool IsAlive { get; }
    }
}