using UnityEngine;

namespace Bombs.Explosions
{
    /// <summary>
    /// Xử lý một vụ nổ đơn, tiêu chuẩn.
    /// </summary>
    public class DefaultExplosionStrategy : IExplosionStrategy
    {
        public void Execute(BombController controller)
        {
            controller.TriggerSingleExplosion(controller.transform.position, false);
            controller.HandleSpecialInteractionsForExplosion(controller.transform.position);
        }
    }
}