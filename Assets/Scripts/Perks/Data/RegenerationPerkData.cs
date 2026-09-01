using UnityEngine;
using Core;
using Core.Interfaces;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk Regeneration.
    /// Hoi +5 HP moi 5 giay neu player khong nhan bat ky sat thuong nao trong 10 giay.
    /// </summary>
    [CreateAssetMenu(fileName = "RegenerationPerkData", menuName = "Kaboom Chaos/Perks/Regeneration")]
    public class RegenerationPerkData : BasePerkData
    {
        [Header("Thong So Regeneration")]
        [Tooltip("Luong HP hoi phuc moi lan tick.")]
        [SerializeField] private float _healPerTick = 5f;
        public float HealPerTick => _healPerTick;

        [Tooltip("Khoang thoi gian giua cac lan hoi phuc (giay).")]
        [SerializeField] private float _tickInterval = 5f;
        public float TickInterval => _tickInterval;

        [Tooltip("Cua so thoi gian an toan khong nhan sat thuong de kich hoat hoi phuc (giay).")]
        [SerializeField] private float _noDamageWindow = 10f;
        public float NoDamageWindow => _noDamageWindow;

        private RegenerationBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new RegenerationBehavior(this);
    }

    /// <summary>
    /// Hanh vi thuc thi cua Perk Regeneration.
    /// Theo doi su kien sat thuong nhan duoc de tinh cua so an toan.
    /// </summary>
    public class RegenerationBehavior : IPerkBehavior
    {
        private readonly RegenerationPerkData _data;

        // Thoi diem player nhan sat thuong lan cuoi (Time.time). float.MinValue nghia la chua nhan lan nao.
        private float _lastDamageTime = float.MinValue;
        private float _tickTimer;

        public RegenerationBehavior(RegenerationPerkData data)
        {
            _data = data;
        }

        public void Apply(IPlayer player)
        {
            // Reset trang thai moi khi trang bi/tao lai.
            _lastDamageTime = float.MinValue;
            _tickTimer = 0f;

            GameEvents.OnPlayerDamageTaken += HandleDamageTaken;
            GameEvents.OnRoundEndPlayerReset += HandleRoundEnd;
        }

        public void UpdateBehavior(IPlayer player, float deltaTime)
        {
            if (player == null) return;

            // Chi hoi phuc khi player khong nhan sat thuong trong cua so an toan.
            bool isSafe = Time.time - _lastDamageTime >= _data.NoDamageWindow;
            if (!isSafe)
            {
                // Vua nhan sat thuong: reset bo dem tick, cho khi het cua so an toan.
                _tickTimer = 0f;
                return;
            }

            _tickTimer += deltaTime;
            if (_tickTimer < _data.TickInterval) return;

            _tickTimer = 0f;

            var healable = player.GameObject.GetComponent<IHealable>();
            if (healable == null)
            {
                Debug.LogWarning("[RegenerationBehavior] Player GameObject khong co component IHealable!");
                return;
            }

            float healed = healable.Heal(_data.HealPerTick);
            if (healed > 0f)
            {
                Debug.Log($"[RegenerationBehavior] Hoi phuc {healed} HP (khong nhan sat thuong trong {_data.NoDamageWindow}s).");
            }
        }

        public void Remove(IPlayer player)
        {
            GameEvents.OnPlayerDamageTaken -= HandleDamageTaken;
            GameEvents.OnRoundEndPlayerReset -= HandleRoundEnd;
        }

        /// <summary>
        /// Reset lai trang thai theo round: player moi round bat dau voi moc thoi gian sat thuong moi.
        /// </summary>
        private void HandleRoundEnd()
        {
            _lastDamageTime = float.MinValue;
            _tickTimer = 0f;
        }

        private void HandleDamageTaken(IPlayer player, float amount, DamageSourceType sourceType, StatusEffectType effectContext)
        {
            _lastDamageTime = Time.time;
            _tickTimer = 0f;
        }
    }
}