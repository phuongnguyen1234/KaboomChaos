using UnityEngine;
using UnityEngine.InputSystem;
using Core;
using Core.Interfaces;
using Skills.Data;

namespace Skills
{
    /// <summary>
    /// Bo dieu khien Skill gan tren Player. Phoi hop voi IEnergyable de quan ly dung va sac skill, dong thoi dieu khien hanh vi.
    /// </summary>
    public class PlayerSkillController : MonoBehaviour, ISkillController
    {
        #region Fields

        private BaseSkillData _equippedSkillData;

        [Tooltip("Phim kich hoat skill su dung he thong InputSystem.")]
        [SerializeField] private Key _useSkillKey = Key.E;

        [Header("Skill Database")]
        [Tooltip("Doi tuong implement ISkillDatabase (ScriptableObject SkillDatabase o assembly Skills). Dung de khoi phuc skill equip da luu khi vao game.")]
        [SerializeField] private ScriptableObject _skillDatabase;

        // Cache giao dien ISkillDatabase doc tu _skillDatabase.
        private ISkillDatabase _skillDb;

        [Header("Skill Audio")]
        [Tooltip("AudioSource dung de phat SFX khi dung skill. Neu de trong se tu tim AudioSource tren Player.")]
        [SerializeField] private AudioSource _skillSfxSource;

        private IPlayer _player;
        private IEnergyable _energyable;
        private bool _isActive;
        private float _durationRemaining;

        #endregion

        #region Properties

        public ISkillData CurrentSkill => _equippedSkillData;
        public bool IsActive => _isActive;

        // Uy quyen viec kiem tra trang thai sac cho component Energy cua Player de tranh trung lap logic
        public bool IsRecharging => _energyable != null && _energyable.IsRecharging;
        public float RechargeRemaining => _energyable != null ? _energyable.RechargeRemaining : 0f;
        public float DurationRemaining => _durationRemaining;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _player = GetComponent<IPlayer>();
            _energyable = GetComponent<IEnergyable>();
            _skillDb = _skillDatabase as ISkillDatabase;

            if (_player == null)
            {
                Debug.LogWarning("[PlayerSkillController] Khong tim thay component IPlayer tren GameObject nay!", this);
            }
            if (_energyable == null)
            {
                Debug.LogWarning("[PlayerSkillController] Khong tim thay component IEnergyable (PlayerEnergy) de dong bo thoi gian sac!", this);
            }

            // Tu tim AudioSource de phat SFX skill neu chua gan.
            if (_skillSfxSource == null)
            {
                _skillSfxSource = GetComponent<AudioSource>();
            }
            if (_skillSfxSource == null)
            {
                Debug.LogWarning("[PlayerSkillController] Khong tim thay AudioSource de phat SFX skill. SFX skill se khong duoc phat.", this);
            }
        }

        private void Start()
        {
            // Khoi phuc lai skill da trang bi (duoc luu) khi player duoc spawn vao game.
            RestoreEquippedSkill();
        }

        /// <summary>
        /// Khoi phuc skill dang trang bi duoc luu boi PlayerDataManager (qua GameEvents) khi vao game.
        /// </summary>
        private void RestoreEquippedSkill()
        {
            if (_skillDb == null)
            {
                return;
            }

            string equippedId = GameEvents.TriggerRequestEquippedSkillId();
            if (string.IsNullOrEmpty(equippedId))
            {
                return;
            }

            ISkillData skillData = _skillDb.GetById(equippedId);
            if (skillData == null)
            {
                Debug.LogWarning($"[PlayerSkillController] Khong tim thay skill '{equippedId}' trong database de khoi phuc trang bi.", this);
                return;
            }

            EquipSkill(skillData);
            Debug.Log($"[PlayerSkillController] Da khoi phuc skill equip tu luu: {skillData.DisplayName}.");
        }

        private void Update()
        {
            // Kiem tra dau vao qua InputSystem
            if (DetectUseSkillInput())
            {
                UseActiveSkill();
            }

            // Cap nhat logic thoi gian duy tri skill
            if (_isActive)
            {
                UpdateActiveSkill();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Trang bi mot skill moi cho Player.
        /// </summary>
        public void EquipSkill(ISkillData skillData)
        {
            if (_isActive)
            {
                // Huy kich hoat neu dang chay
                DeactivateSkill();
            }

            _equippedSkillData = skillData as BaseSkillData;
            _durationRemaining = 0f;

            // Reset lai trang thai nap nang luong khi trang bi skill moi
            if (_energyable != null)
            {
                _energyable.RestoreFullEnergy();
            }

            // Bao PlayerDataManager luu lai skill dang trang bi (null/rong nghia la go trang bi).
            GameEvents.TriggerEquippedSkillIdChanged(skillData?.Id);

            Debug.Log($"[PlayerSkillController] Da trang bi skill moi: {(skillData != null ? skillData.DisplayName : "None")}");
        }

        /// <summary>
        /// Kich hoat su dung skill dang trang bi.
        /// </summary>
        public void UseActiveSkill()
        {
            if (_equippedSkillData == null)
            {
                Debug.LogWarning("[PlayerSkillController] Khong the su dung: Chua trang bi skill!");
                return;
            }

            if (_isActive)
            {
                Debug.Log("[PlayerSkillController] Skill hien dang hoat dong!");
                return;
            }

            // Kiem tra va tieu thu energy.
            // Neu Skill co Duration: rut energy tu tu theo dung thoi gian duy tri, het duration moi sac.
            // Neu Skill khong co Duration: rut can ngay va sac luon.
            if (_equippedSkillData.Duration > 0f)
            {
                if (_energyable != null && !_energyable.TryStartDrain(_equippedSkillData.Duration, _equippedSkillData.RechargeTime))
                {
                    Debug.Log($"[PlayerSkillController] Khong the kich hoat: Chua du energy hoac dang sac/dung thau!");
                    return;
                }
            }
            else if (_energyable != null && !_energyable.TryConsumeFullEnergy(_equippedSkillData.RechargeTime))
            {
                Debug.Log($"[PlayerSkillController] Khong the kich hoat: Chua du energy hoac dang sac!");
                return;
            }

            // Bat dau kich hoat skill
            _isActive = true;
            _durationRemaining = _equippedSkillData.Duration;
            
            Debug.Log($"[PlayerSkillController] Kich hoat skill: {_equippedSkillData.DisplayName}. Thoi gian hieu luc: {_durationRemaining}s");

            if (_equippedSkillData.Behavior != null)
            {
                _equippedSkillData.Behavior.Activate(_player);
            }

            // Phat SFX + VFX khi dung skill tren vi tri Player.
            PlayCastEffects();
        }

        /// <summary>
        /// Phat SFX (qua AudioSource) va VFX (qua VFXPoolManager - GameEvents.TriggerVFXSpawnRequest)
        /// tren vi tri Player khi su dung skill. SFX/VFX lay tu BaseSkillData (CastSfx/CastVfx).
        /// </summary>
        private void PlayCastEffects()
        {
            if (_equippedSkillData == null) return;

            // Phat SFX neu skill co cau hinh am thanh va tim duoc AudioSource.
            if (_equippedSkillData.CastSfx != null && _skillSfxSource != null)
            {
                _skillSfxSource.PlayOneShot(_equippedSkillData.CastSfx);
            }

            // Spawn VFX qua pool (do VFXPoolManager quan ly - Goi qua GameEvents de tranh phu thuoc assembly Managers).
            if (_equippedSkillData.CastVfx != null && _player != null && _player.GameObject != null)
            {
                Vector3 spawnPos = _player.GameObject.transform.position;
                GameObject vfx = GameEvents.TriggerVFXSpawnRequest(_equippedSkillData.CastVfx, spawnPos, Quaternion.identity);
                if (vfx != null)
                {
                    Debug.Log($"[PlayerSkillController] Da spawn VFX skill: {_equippedSkillData.DisplayName}.");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kiem tra xem nguoi choi co nhan nut su dung skill hay khong thong qua Keyboard cua InputSystem.
        /// </summary>
        private bool DetectUseSkillInput()
        {
            if (Keyboard.current != null && Keyboard.current[_useSkillKey].wasPressedThisFrame)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Cap nhat thoi gian hieu luc cua skill moi frame va goi UpdateBehavior.
        /// </summary>
        private void UpdateActiveSkill()
        {
            _durationRemaining -= Time.deltaTime;

            if (_equippedSkillData != null && _equippedSkillData.Behavior != null)
            {
                _equippedSkillData.Behavior.UpdateBehavior(_player, Time.deltaTime);
            }

            if (_durationRemaining <= 0)
            {
                DeactivateSkill();
            }
        }

        /// <summary>
        /// Tat kich hoat skill va goi deactive logic.
        /// </summary>
        private void DeactivateSkill()
        {
            _isActive = false;
            _durationRemaining = 0f;

            Debug.Log($"[PlayerSkillController] Skill {_equippedSkillData?.DisplayName} het hieu luc.");

            if (_equippedSkillData != null && _equippedSkillData.Behavior != null)
            {
                _equippedSkillData.Behavior.Deactivate(_player);
            }
        }

        #endregion
    }
}
