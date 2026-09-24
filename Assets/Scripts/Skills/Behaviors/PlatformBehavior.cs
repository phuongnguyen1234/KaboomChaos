using UnityEngine;
using Core.Interfaces;
using Skills.Data;

namespace Skills.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Skill Platform: tao platform tai vi tri duoi chan player voi Y offset.
    /// </summary>
    public class PlatformBehavior : ISkillBehavior
    {
        private readonly PlatformSkillData _data;

        public PlatformBehavior(PlatformSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            if (_data.PlatformPrefab == null)
            {
                Debug.LogWarning("[PlatformBehavior] Chua gan PlatformPrefab de tao Platform!");
                return;
            }

            if (player == null || player.GameObject == null) return;

            // Kich hoat anh nion Summon tren player khi active skill.
            var animBridge = player.GameObject.GetComponent<ISkillAnimationBridge>();
            if (animBridge != null) animBridge.TriggerSummonAnimation();

            Vector3 playerPos = player.GameObject.transform.position;

            // Buoc 1: Dua player len cao mot chut (day/thoi theo truc Y) de player khong bi
            // roi xuong truoc khi Platform duoc tao. Only ap dung khi co tinh nang nay (Impulse > 0).
            if (_data.PlayerLiftImpulse > 0f)
            {
                player.AddMomentum(Vector3.up * _data.PlayerLiftImpulse);
            }

            // Buoc 2: Tao Platform tai vi tri duoi chan player, lech xuong mot khoang theo Y offset.
            Vector3 spawnPos = new Vector3(playerPos.x, playerPos.y - _data.SpawnYOffset, playerPos.z);

            GameObject platform = Object.Instantiate(_data.PlatformPrefab, spawnPos, Quaternion.identity);
            if (platform != null)
            {
                Debug.Log($"[PlatformBehavior] Da dua player len cao va tao Platform tai {spawnPos}. Thoi gian ton tai do LifetimeController quan ly.");
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            // Skill tuc thoi, khong can cap nhat moi frame trong duration.
        }

        public void Deactivate(IPlayer player)
        {
            Debug.Log("[PlatformBehavior] Platform skill ket thuc.");
        }
    }
}