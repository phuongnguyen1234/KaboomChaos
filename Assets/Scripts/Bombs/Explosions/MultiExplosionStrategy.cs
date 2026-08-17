using System.Collections;
using UnityEngine;

namespace Bombs.Explosions
{
    /// <summary>
    /// Xử lý một vụ nổ chính theo sau bởi nhiều vụ nổ phụ.
    /// </summary>
    public class MultiExplosionStrategy : IExplosionStrategy
    {
        public void Execute(BombController controller)
        {
            // Vụ nổ ban đầu, chính
            controller.TriggerSingleExplosion(controller.transform.position, false);
            controller.HandleSpecialInteractionsForExplosion(controller.transform.position);

            // Bắt đầu coroutine cho các vụ nổ phụ
            controller.StartCoroutine(SubExplosionRoutine(controller));
        }

        private IEnumerator SubExplosionRoutine(BombController controller)
        {
            var bombData = controller.BombData;
            int actualNumberOfExplosions = Random.Range(bombData.MinNumberOfExplosions, bombData.MaxNumberOfExplosions + 1);
            for (int i = 0; i < actualNumberOfExplosions; i++)
            {
                if (bombData.DelayBetweenExplosions > 0)
                {
                    yield return new WaitForSeconds(bombData.DelayBetweenExplosions);
                }

                Vector3 explosionCenter = controller.transform.position + (Random.insideUnitSphere * bombData.ExplosionSpreadRadius);
                controller.TriggerSingleExplosion(explosionCenter, true); // Đánh dấu là vụ nổ phụ
                controller.HandleSpecialInteractionsForExplosion(explosionCenter);
            }
        }
    }
}