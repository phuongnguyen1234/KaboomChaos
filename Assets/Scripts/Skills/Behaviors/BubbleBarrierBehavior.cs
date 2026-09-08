using UnityEngine;
using Core;
using Core.Interfaces;
using Skills.Data;

namespace Skills.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Skill Bubble Barrier: tao Bubble Barrier tai vi tri player..
    /// </summary>
    public class BubbleBarrierBehavior : ISkillBehavior
    {
        private readonly BubbleBarrierSkillData _data;

        public BubbleBarrierBehavior(BubbleBarrierSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (_data.BubblePrefab == null)
            {
                Debug.LogWarning("[BubbleBarrierBehavior] Chua gan BubblePrefab de tao Bubble Barrier!");
                return;
            }

            if (player == null || player.GameObject == null) return;

            Vector3 pos = player.GameObject.transform.position;
            Quaternion rot = Quaternion.identity;

            // Spawn qua VFX pool de LifetimeController tu dong tra ve pool khi het thoi gian.
            GameObject bubble = GameEvents.TriggerVFXSpawnRequest(_data.BubblePrefab, pos, rot);
            if (bubble == null)
            {
                // Neu khong co VFX pool lang nghe, dung Instantiate thuong.
                bubble = Object.Instantiate(_data.BubblePrefab, pos, rot);
            }

            Debug.Log("[BubbleBarrierBehavior] Da tao Bubble Barrier quanh player.");
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Thoi gian duy tri do LifetimeController tren prefab quan ly, khong can logic moi frame.
        }

        public void Deactivate(IPlayer player)
        {
            Debug.Log("[BubbleBarrierBehavior] Bubble Barrier ket thuc hieu luc.");
        }
    }
}