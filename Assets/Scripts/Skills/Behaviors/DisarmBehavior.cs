using UnityEngine;
using System.Collections.Generic;
using Core;
using Core.Interfaces;
using Skills.Data;

namespace Skills.Behaviors
{
    /// <summary>
    /// Hanh vi thuc thi cua Skill Disarm: trong thoi gian duration hien thi VFX charge quanh player,
    /// het thoi gian thi go het bom trong ban kinh quanh player.
    /// </summary>
    public class DisarmBehavior : ISkillBehavior
    {
        private readonly DisarmSkillData _data;
        private IPlayer _activePlayer;
        private bool _fired;
        private float _elapsed;
        private GameObject _chargeVfxInstance;

        public DisarmBehavior(DisarmSkillData data)
        {
            _data = data;
        }

        public void Activate(IPlayer player)
        {
            _activePlayer = player;
            _fired = false;
            _elapsed = 0f;

            // Tao VFX particle 'charge' quanh player trong thoi gian duration (neu co cau hinh).
            SpawnChargeVfx(player);

            // Sat bool 'Charge' tren animator de chay anh nion charge trong thoi gian duration.
            SetChargeAnimation(player, true);

            // Neu skill khong co duration (tuc thoi), go bom ngay khi kich hoat.
            if (_data.Duration <= 0f)
            {
                DisarmBombs(player);
                _fired = true;
            }
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            if (_fired || player == null) return;

            _elapsed += deltaTime;
            // Sau khi het thoi gian duration (delay), thuc hien go het bom trong ban kinh.
            if (_elapsed >= _data.Duration)
            {
                DisarmBombs(player);
                _fired = true;
            }
        }

        public void Deactivate(IPlayer player)
        {
            // Dam bao bom van duoc go neu vui ly thoi gian ket thuc truoc khi khop ngay go.
            if (!_fired && player != null)
            {
                DisarmBombs(player);
                _fired = true;
            }

            // Don dep VFX charge con sot lai.
            DespawnChargeVfx();

            // Sat bool 'Charge' sau khong lung (ket thuc duration skill).
            SetChargeAnimation(player, false);
            _activePlayer = null;
        }

        /// <summary>
        /// Spawn VFX charge quanh player trong thoi gian duration. VFX duoc gan lam con cua player de luon di cung player.
        /// </summary>
        private void SpawnChargeVfx(IPlayer player)
        {
            if (_data.ChargeVfx == null || player == null || player.GameObject == null) return;

            // Spawn qua VFX pool (GameEvents) va gan lam con cua player (giong cach skill VFX di cung player).
            GameObject vfx = GameEvents.TriggerVFXSpawnRequest(_data.ChargeVfx, player.GameObject.transform.position, Quaternion.identity);
            if (vfx == null)
            {
                // Neu khong co VFX pool lang nghe, dung Instantiate thuong.
                vfx = Object.Instantiate(_data.ChargeVfx, player.GameObject.transform.position, Quaternion.identity);
            }

            if (vfx != null)
            {
                vfx.transform.SetParent(player.GameObject.transform, false);
                vfx.transform.localPosition = Vector3.zero;
                _chargeVfxInstance = vfx;

                // Neu prefab VFX la dang vu no (co ExplosionEffectController), go Trigger de no scale theo ban kinh Disarm
                // va tu dong tra ve pool sau khi hieu ung animation ket thuc (ExplosionEffectController tu xu ly despawn).
                ExplosionEffectController explosion = vfx.GetComponentInChildren<ExplosionEffectController>();
                if (explosion != null)
                {
                    explosion.Trigger(_data.Radius);
                }
            }
        }

        /// <summary>
        /// Tra VFX charge ve pool (hoac destroy) khi bom duoc go vo hoac skill ket thuc.
        /// </summary>
        private void DespawnChargeVfx()
        {
            if (_chargeVfxInstance == null) return;

            // Neu VFX la dang vu no, ExplosionEffectController da tu dong tra ve pool sau khi animation ket thuc
            // (object se tro thanh inactive). Chi despawn thu cong khi object van con dang hoat dong.
            if (_chargeVfxInstance.activeInHierarchy)
            {
                GameEvents.TriggerVFXDespawnRequest(_chargeVfxInstance);
            }

            _chargeVfxInstance = null;
        }

        /// <summary>
        /// Sat bool 'Charge' tren animator player: true trong thoi gian duration, false khi ket thuc.
        /// </summary>
        /// <param name="player">Nguoi choi dang su dung Disarm skill.</param>
        /// <param name="charging">True neu skill dang din thoi gian charge.</param>
        private void SetChargeAnimation(IPlayer player, bool charging)
        {
            if (player == null || player.GameObject == null) return;

            var animBridge = player.GameObject.GetComponent<ISkillAnimationBridge>();
            if (animBridge != null) animBridge.SetDisarmCharging(charging);
        }

        /// <summary>
        /// Xoa het bom trong ban kinh quanh player thong qua GameEvents (khong phu thuoc vao singleton manager).
        /// </summary>
        private void DisarmBombs(IPlayer player)
        {
            if (player == null || player.GameObject == null) return;

            // Luon tat VFX charge ngay thoi diem bom duoc go vo.
            DespawnChargeVfx();

            // Lay danh sach bom dang hoat dong qua GameEvents (do BombSpawnerManager cung cap).
            IReadOnlyList<GameObject> bombs = GameEvents.TriggerRequestActiveBombInstances();
            if (bombs == null || bombs.Count == 0)
            {
                Debug.Log("[DisarmBehavior] Khong co bom dang hoat dong de go vo.");
                return;
            }

            Vector3 center = player.GameObject.transform.position;
            float radius = _data.Radius;
            int removedCount = 0;

            foreach (var bomb in bombs)
            {
                if (bomb == null) continue;

                // Khoang cach ngang (XZ) giua bom va player de xac dinh co thuoc ban kinh hay khong.
                Vector3 delta = bomb.transform.position - center;
                delta.y = 0f;
                if (delta.sqrMagnitude > radius * radius) continue;

                // Tra bom ve pool (khong kich no) de go vo khoi man hinh.
                GameEvents.TriggerBombDespawnRequest(bomb);
                removedCount++;
            }

            Debug.Log($"[DisarmBehavior] Da go vo {removedCount} qua bom trong ban kinh {radius} quanh player.");
        }
    }
}