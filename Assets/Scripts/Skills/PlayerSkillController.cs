using UnityEngine;
using UnityEngine.InputSystem;
using Core;
using Core.Interfaces;
using Skills.Data;
using System;
using System.Collections.Generic;

namespace Skills
{
    /// <summary>
    /// Bo dieu khien Skill gan tren Player. Phoi hop voi IEnergyable de quan ly dung va sac skill, dong thoi dieu khien hanh vi.
    /// </summary>
    public class PlayerSkillController : MonoBehaviour, ISkillController
    {
        #region Fields

        private BaseSkillData _equippedSkillData;

        [Tooltip("Phim kich hoat skill su dung he thong InputSystem.\nMac dinh = E. Co the thay doi trong Settings (UseSkillKey).")]
        [SerializeField] private Key _useSkillKey = Key.E;

        // Cache key dang duoc sync tu SettingsManager.Instance.UseSkillKey de lam giam viec parse moi frame.
        // Parse lai chi khi ten key trong settings thay doi.
        private Key _cachedUseSkillKey = Key.E;
        private string _lastUseSkillKeyName;

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

        // Luu danh sach cac VFX (CastVfx) duoc spawn tu pool trong luc kich hoat skill.
        // Khi skill het duration, cac VFX con hoat dong se duoc tra ve pool de tranh chong chat (stack) trong scene.
        private readonly List<GameObject> _activeCastVfx = new();

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

        /// <summary>
        /// Component cua Player duoc huy/recreated (vi du khi player chut hoac rot moi khoi thuc):
        /// tra ve pool tat ca VFX track de tranh khong roi sen sau 'stack' trong scene.
        /// Chi tac dung khi skill dang hoat dong va con co VFX luu dang hoat.
        /// </summary>
        private void OnDestroy()
        {
            DespawnActiveCastVfx();
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

            // Cho phep skill tu chan viec su dung tuong ung voi dieu kien hien tai
            // (vi du: Heal khong dung duoc khi player dang day HP).
            if (_equippedSkillData.CanActivate(_player) == false)
            {
                Debug.Log($"[PlayerSkillController] Khong the su dung skill {_equippedSkillData.DisplayName} trong dieu kien hien tai.");
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

            // Phat SFX + VFX ngay tai thoi diem kich hoat skill (neu duoc cau hinh PlayCastOnActivate).
            if (_equippedSkillData.PlayCastOnActivate)
            {
                PlayCastEffects();
            }
        }

        /// <summary>
        /// Phat SFX (qua SfxManager) va VFX (qua VFXPoolManager - GameEvents.TriggerVFXSpawnRequest)
        /// tren vi tri Player khi su dung skill. SFX/VFX lay tu BaseSkillData (CastSfx/CastVfx).
        /// </summary>
        /// <param name="trackVfx">
        /// True (mac dinh): VFX nay thuoc danh sach _activeCastVfx de co the tra ve pool khi skill het duration.
        /// Neu skill khong co ExplosionEffectController (chi la particle/shader/mesh thuong), nay la bat buoc.
        /// False: dung cho one-shot burst (PlayCastOnDeactivate) na se tu dong tra ve pool sau animation;
        /// khong theo do trong _activeCastVfx de tranh goi despawn ngay sau spawn.
        /// </param>
        private void PlayCastEffects(bool trackVfx = true)
        {
            if (_equippedSkillData == null) return;

            // Phat SFX neu skill co cau hinh am thanh (prin SfxManager de live volume).
            if (_equippedSkillData.CastSfx != null && SfxService.Instance != null && _player != null)
            {
                SfxService.Instance.PlaySfx(_equippedSkillData.CastSfx, _player.GameObject.transform.position);
            }

            // Spawn VFX qua pool (do VFXPoolManager quan ly - Goi qua GameEvents de tranh phu thuoc assembly Managers).
            if (_equippedSkillData.CastVfx != null && _player != null && _player.GameObject != null)
            {
                GameObject vfx = GameEvents.TriggerVFXSpawnRequest(_equippedSkillData.CastVfx, _player.GameObject.transform.position, Quaternion.identity);
                if (vfx == null)
                {
                    // Neu khong co VFX pool lang nghe, dung Instantiate thuong de khong mat hieu ung.
                    vfx = UnityEngine.Object.Instantiate(_equippedSkillData.CastVfx, _player.GameObject.transform.position, Quaternion.identity);
                }

                if (vfx != null)
                {
                    // Luu VFX nay de co the tra ve pool khi skill het duration (responde quan ly pool sau het duration).
                    // Voi vfx cu ExplosionEffectController se tu dong tra ve pool sau animation,
                    // dar de aici da luu ia 'activeInHierarchy' truoc go despawn de tranh double-enqueue trong pool.
                    if (trackVfx)
                    {
                        _activeCastVfx.Add(vfx);
                    }

                    // Tuy thuoc vao cau hinh spawn mode tu BaseSkillData:
                    // - LocalPlayer: gan VFX lam con cua Player de luon di cung player (giong cach Shield parent VFX len Player).
                    // - World: giu VFX trong world space tai vi tri Player, khong gan theo de tranh bi nho do scale
                    //   cua Player, dong thoi cho phep VFX tu quan ly vong doi (LifetimeController/ExplosionEffectController).
                    if (_equippedSkillData.CastVfxSpawnMode == BaseSkillData.VfxSpawnMode.LocalPlayer)
                    {
                        vfx.transform.SetParent(_player.GameObject.transform, false);
                        vfx.transform.localPosition = Vector3.zero;
                    }

                    // Neu prefab CastVfx la dang vu no (co ExplosionEffectController), go Trigger de no chay sequence
                    // (scale theo ban kinh -> fade -> tu dong tra ve pool sau khi animation ket thuc).
                    // Bat buoc phai go Trigger, neu khong VFX se dung o initialScale (rat nho), khong chay sequence
                    // va khong bao gio tu tra ve pool.
                    ExplosionEffectController explosion = vfx.GetComponentInChildren<ExplosionEffectController>();
                    if (explosion != null)
                    {
                        explosion.Trigger(_equippedSkillData.CastVfxExplosionRadius);
                    }
                    else
                    {
                        // Neu la ParticleSystem (prefab root rong voi cac object con ParticleSystem),
                        // Explicitly Clear va Play de dam bao tat ca child particle system chay dung va sach khi lay tu pool.
                        ParticleSystem[] particleSystems = vfx.GetComponentsInChildren<ParticleSystem>();
                        if (particleSystems != null && particleSystems.Length > 0)
                        {
                            foreach (var ps in particleSystems)
                            {
                                if (ps != null)
                                {
                                    ps.Clear(true);
                                    ps.Play(true);
                                }
                            }
                        }
                    }

                    Debug.Log($"[PlayerSkillController] Da spawn VFX skill: {_equippedSkillData.DisplayName} (mode: {_equippedSkillData.CastVfxSpawnMode}).");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Kiem tra xem nguoi choi co nhan nut su dung skill hay khong thong qua Keyboard cua InputSystem.
        /// Key duoc citest tu SettingsManager.UseSkillKey (rebind din Settings UI).
        /// </summary>
        private bool DetectUseSkillInput()
        {
            if (Keyboard.current != null && Keyboard.current[ResolveUseSkillKey()].wasPressedThisFrame)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Resolveaza key duoc dung de kich hoat skill: citest tu SettingsManager.UseSkillKey
        /// (rebind din Settings UI), cu fallback la _useSkillKey (serialized din Inspector).
        /// Cacheaza rezultatul de tranh limpia parse o moi frame (parse lai chi cand ten key thay doi).
        /// </summary>
        private Key ResolveUseSkillKey()
        {
            ISettingsManager settings = SettingsService.Instance;
            string configuredName = settings != null ? settings.UseSkillKey : _useSkillKey.ToString();

            if (_lastUseSkillKeyName != configuredName)
            {
                _lastUseSkillKeyName = configuredName;
                _cachedUseSkillKey = ParseUseSkillKey(configuredName);
            }

            return _cachedUseSkillKey;
        }

        /// <summary>
        /// Conversie sam nume Key (care luu de SettingsManager) din enum Key din InputSystem.
        /// Neu nu tim thay sam, tra ve fallback serialized (_useSkillKey).
        /// </summary>
        /// <param name="keyName">Sam nume Key (vi du "E", "Space").</param>
        private Key ParseUseSkillKey(string keyName)
{
        if (Enum.TryParse(keyName, true, out Key resultKey))
        {
            return resultKey;
        }

        return _useSkillKey;
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

            // Phat SFX + VFX sau khi het duration (neu skill duoc cau hinh PlayCastOnDeactivate).
            // VFX nay la one-shot burst (chi se tu dong tra ve pool sau animation ket thuc noi
            // ExplosionEffectController/LifetimeController), khong theo do trong _activeCastVfx.
            if (_equippedSkillData != null && _equippedSkillData.PlayCastOnDeactivate)
            {
                PlayCastEffects(false);
            }

            // Tra ve pool tat ca VFX (CastVfx) duoc spawn trong luc kich hoat skill.
            // Chi go nay sau PlayCastOnDeactivate de tranh danh sach _activeCastVfx khong goi
            // su dung bang (spawn burst moi) de lai roi sau clear -> goi leak/stack.
            DespawnActiveCastVfx();
        }

        /// <summary>
        /// Tra ve pool (Despawn) tat ca VFX duoc spawn tu pool trong luc kich hoat skill.
        /// VFX duoc pool quan ly se duoc tra ve sau het duration de dung se luu.
        /// Chi VFX con dang hoat dong duoc despawn; VFX sau duoc tra ve tu dong (inactive)
        /// de tranh goi despawn 2 lan gay trung lap trong pool.
        /// </summary>
        private void DespawnActiveCastVfx()
        {
            if (_activeCastVfx.Count == 0) return;

            foreach (var vfx in _activeCastVfx)
            {
                if (vfx == null) continue;

                // VFX cu ExplosionEffectController duoc tra ve pool (+ inactive) sau animation ket thuc.
                // Chi despawn nhung object van con dang hoat dong de tranh double-enqueue trong pool.
                if (vfx.activeInHierarchy)
                {
                    GameEvents.TriggerVFXDespawnRequest(vfx);
                }
            }

            _activeCastVfx.Clear();
        }

        #endregion
    }
}


