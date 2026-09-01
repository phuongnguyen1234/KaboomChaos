using UnityEngine;
using Core.Interfaces;
using Core;

namespace Player
{
    /// <summary>
    /// Quan ly nang luong (energy) cua nguoi choi.
    /// Energy day 100% la dieu kien de su dung Skill. Khi Skill duoc su dung, energy
    /// bi rut can ve 0 va bat dau qua trinh tu sac trong mot khoang thoi gian bang
    /// thoi gian hoi chieu cua Skill. Chi khi sac day 100% thi Skill moi dung duoc lan tiep.
    /// Vong doi cua energy giong HP: bat dau round voi gia tri day va reset cuoi round.
    /// </summary>
    public class PlayerEnergy : MonoBehaviour, IEnergyable
    {
        #region Fields

        [Header("Energy Settings")]
        [Tooltip("Luong energy toi da, tuong ung voi gia tri 100%.")]
        [SerializeField] private float _baseMaxEnergy = 100f;

        [Tooltip("Thoi gian sac mac dinh (giay) neu Skill khong cung cap thoi gian hoi chieu rieng.")]
        [SerializeField] private float _defaultRechargeDuration = 10f;

        private float _currentEnergy;
        private float _rechargeTimer;
        private float _activeRechargeDuration;
        private bool _isRecharging;

        // Trang thai rut energy tu tu (dung cho Skill co Duration): energy giam tu 100% ve 0
        // trong `_activeDrainDuration` giay, sau do moi bat dau sac lai.
        private bool _isDraining;
        private float _drainTimer;
        private float _activeDrainDuration;

        // Thoi gian sac se dung sau khi hoan tat rut het energy (lay tu RechargeTime cua Skill).
        private float _pendingRechargeDuration;

        // Cached components
        private IPlayer _player;
        private IDamageable _damageable;

        #endregion

        #region Properties

        /// <summary>
        /// Luong energy hien tai cua nguoi choi.
        /// </summary>
        public float CurrentEnergy => _currentEnergy;

        /// <summary>
        /// Luong energy toi da, tuong ung 100%.
        /// </summary>
        public float MaxEnergy => _baseMaxEnergy;

        /// <summary>
        /// True khi energy da day 100% va khong dang trong qua trinh sac.
        /// Chi luc nay Skill moi duoc phep su dung.
        /// </summary>
        public bool IsFullyCharged => !_isDraining && !_isRecharging && _currentEnergy >= _baseMaxEnergy;

        /// <summary>
        /// True khi energy dang trong qua trinh tu sac sau khi dung Skill.
        /// </summary>
        public bool IsRecharging => _isRecharging;

        /// <summary>
        /// Thoi gian sac con lai (giay).
        /// </summary>
        public float RechargeRemaining => _isRecharging ? Mathf.Max(0f, _activeRechargeDuration - _rechargeTimer) : 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _player = GetComponent<IPlayer>();
            _damageable = GetComponent<IDamageable>();

            // Bat dau round voi energy day 100%, dong bo voi cach PlayerHealth khoi tao mau day.
            _currentEnergy = _baseMaxEnergy;
            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);
        }

        private void OnEnable()
        {
            // Dang ky lang nghe su kien reset cuoi round giong PlayerHealth.
            GameEvents.OnRoundEndPlayerReset += ResetState;
        }

        private void OnDisable()
        {
            // Huy dang ky de tranh memory leak.
            GameEvents.OnRoundEndPlayerReset -= ResetState;
        }

        private void Update()
        {
            // Dung toan bo qua trinh (sac hoac rut) khi nguoi choi da chet; round moi se reset lai trang thai.
            if (_damageable != null && !_damageable.IsAlive) return;

            if (_isDraining)
            {
                UpdateDrainProgress();
                return;
            }

            if (!_isRecharging) return;

            UpdateRechargeProgress();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Tieu toan bo energy de su dung Skill. Chi thanh cong khi energy dang day 100%.
        /// Sau khi tieu, energy tu sac lai trong rechargeDuration giay
        /// (thuong bang voi thoi gian hoi chieu cua Skill).
        /// </summary>
        /// <param name="rechargeDuration">Thoi gian (giay) de sac day lai energy. Neu <= 0 thi dung thoi gian sac mac dinh.</param>
        /// <returns>True neu tieu thanh cong, False neu energy chua day hoac dang trong qua trinh sac.</returns>
        public bool TryConsumeFullEnergy(float rechargeDuration)
        {
            if (!IsFullyCharged) return false;

            _currentEnergy = 0f;
            _isRecharging = true;
            _rechargeTimer = 0f;

            // Thoi gian sac bang thoi gian hoi chieu cua Skill truyen vao; fallback ve gia tri mac dinh.
            float duration = rechargeDuration > 0f ? rechargeDuration : _defaultRechargeDuration;
            // Dam bao thoi gian sac luon duong de tranh chia cho 0 trong Update.
            _activeRechargeDuration = Mathf.Max(duration, Time.fixedDeltaTime);

            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);
            GameEvents.TriggerPlayerSkillEnergyConsumed(_player);

            return true;
        }

        /// <summary>
        /// Rut can energy tu tu tu day ve 0 trong `drainDuration` giay.
        /// Sau khi rut het, energy tu sac lai trong `rechargeDuration` giay.
        /// Chi thanh cong khi energy dang day 100% va khong dang sac.
        /// </summary>
        /// <param name="drainDuration">Thoi gian (giay) de rut energy tu day ve 0.</param>
        /// <param name="rechargeDuration">Thoi gian (giay) sac lai sau khi rut het. Neu <= 0 thi dung thoi gian sac mac dinh.</param>
        /// <returns>True neu bat dau rut thanh cong.</returns>
        public bool TryStartDrain(float drainDuration, float rechargeDuration)
        {
            if (!IsFullyCharged) return false;

            _isDraining = true;
            _drainTimer = 0f;
            // Dam bao thoi gian rut luon duong de tranh chia cho 0.
            _activeDrainDuration = Mathf.Max(drainDuration, Time.fixedDeltaTime);

            // Thoi gian sac sau khi het hieu luc skill; fallback ve gia tri mac dinh.
            _pendingRechargeDuration = Mathf.Max(rechargeDuration > 0f ? rechargeDuration : _defaultRechargeDuration, Time.fixedDeltaTime);

            _isRecharging = false;
            _rechargeTimer = 0f;
            _currentEnergy = _baseMaxEnergy;

            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);
            GameEvents.TriggerPlayerSkillEnergyConsumed(_player);

            return true;
        }

        /// <summary>
        /// Lam day energy ngay lap tuc (danh cho collectible Energy)
        /// va huy qua trinh sac dang chay neu co.
        /// </summary>
        /// <returns>Luong energy thuc te da duoc hoi phuc.</returns>
        public float RestoreFullEnergy()
        {
            // Khong hoi energy khi nguoi choi da chet, dong bo voi hanh vi cua Heal.
            if (_damageable != null && !_damageable.IsAlive) return 0f;

            float previousEnergy = _currentEnergy;

            // Huy qua trinh sac va rut dang chay, day ngay lap tuc.
            _isRecharging = false;
            _isDraining = false;
            _drainTimer = 0f;
            _rechargeTimer = 0f;
            _currentEnergy = _baseMaxEnergy;

            float restoredAmount = _currentEnergy - previousEnergy;
            if (restoredAmount > 0f)
            {
                GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);
            }

            return restoredAmount;
        }

        /// <summary>
        /// Reset lai trang thai energy ve day 100%, thuong duoc goi khi ket thuc mot round.
        /// Giong PlayerHealth.ResetState: chi reset neu nguoi choi con song.
        /// </summary>
        public void ResetState()
        {
            // Nguoi choi da chet se duoc quy trinh hoi sinh tao instance moi nen khong can reset.
            if (_damageable != null && !_damageable.IsAlive) return;

            _isRecharging = false;
            _isDraining = false;
            _drainTimer = 0f;
            _rechargeTimer = 0f;
            _currentEnergy = _baseMaxEnergy;

            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Cap nhat qua trinh rut energy moi frame: giam dan tu 100% ve 0.
        /// Khi rut het se chuyen sang qua trinh sac.
        /// </summary>
        private void UpdateDrainProgress()
        {
            _drainTimer += Time.deltaTime;
            float t = _activeDrainDuration > 0f ? Mathf.Clamp01(_drainTimer / _activeDrainDuration) : 1f;
            _currentEnergy = Mathf.Lerp(_baseMaxEnergy, 0f, t);

            // Phat su kien moi frame de HUD cap nhat thanh energy muot ma.
            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);

            if (t >= 1f)
            {
                CompleteDrain();
            }
        }

        /// <summary>
        /// Cap nhat qua trinh sac moi frame: energy tang dan tu 0 ve 100%.
        /// </summary>
        private void UpdateRechargeProgress()
        {
            _rechargeTimer += Time.deltaTime;
            float t = _activeRechargeDuration > 0f ? Mathf.Clamp01(_rechargeTimer / _activeRechargeDuration) : 1f;
            _currentEnergy = Mathf.Lerp(0f, _baseMaxEnergy, t);

            // Phat su kien moi frame de HUD cap nhat thanh energy muot ma.
            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);

            if (t >= 1f)
            {
                CompleteRecharge();
            }
        }

        /// <summary>
        /// Hoan tat qua trinh rut: energy ve 0 va bat dau sac lai bang thoi gian recharge da luu.
        /// </summary>
        private void CompleteDrain()
        {
            _isDraining = false;
            _drainTimer = 0f;
            _currentEnergy = 0f;

            // Bat dau sac ngay sau khi skill het hieu luc (het duration).
            _isRecharging = true;
            _rechargeTimer = 0f;
            _activeRechargeDuration = Mathf.Max(_pendingRechargeDuration, Time.fixedDeltaTime);

            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);
        }

        /// <summary>
        /// Hoan tat qua trinh sac: energy ve day 100% va phat su kien da sac day.
        /// </summary>
        private void CompleteRecharge()
        {
            _isRecharging = false;
            _rechargeTimer = 0f;
            _currentEnergy = _baseMaxEnergy;

            GameEvents.TriggerPlayerEnergyChanged(_player, _currentEnergy, _baseMaxEnergy);
            GameEvents.TriggerPlayerEnergyFullyCharged(_player);
        }

        #endregion
    }
}
