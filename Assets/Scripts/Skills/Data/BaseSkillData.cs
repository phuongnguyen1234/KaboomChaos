using UnityEngine;
using Core.Interfaces;

namespace Skills.Data
{
    /// <summary>
    /// Lop co so truau tuong cho tat ca du lieu cua Skill.
    /// Trien khai tu ScriptableObject va ISkillData.
    /// </summary>
    public abstract class BaseSkillData : ScriptableObject, ISkillData
    {
        [Header("Thong Tin Co Ban")]
        [Tooltip("ID duy nhat dung de phan biet cac skill.")]
        [SerializeField] protected string _id;
        public string Id => _id;

        [Tooltip("Ten hien thi tren giao dien nguoi choi.")]
        [SerializeField] protected string _displayName;
        public string DisplayName => _displayName;

        [Tooltip("Mo ta ngan gon ve chuc nang va tac dung cua skill.")]
        [TextArea(3, 5)]
        [SerializeField] protected string _description;
        public string Description => _description;

        [Tooltip("Anh dai dien cho skill.")]
        [SerializeField] protected Sprite _icon;
        public Sprite Icon => _icon;

        [Tooltip("Phan loai skill.")]
        [SerializeField] protected SkillType _type;
        public SkillType Type => _type;

        [Header("Thong So Thoi Gian")]
        [Tooltip("Thoi gian duy tri hieu luc cua skill (tinh bang giay).")]
        [SerializeField] protected float _duration;
        public float Duration => _duration;

        [Tooltip("Thoi gian sac lai de co the tiep tuc su dung (tinh bang giay).")]
        [SerializeField] protected float _rechargeTime;
        public float RechargeTime => _rechargeTime;

        [Header("Shop")]
        [Tooltip("Gia ban trong Shop (tinh bang Coin).")]
        [SerializeField] protected int _price;
        public int Price => _price;

        [Header("SFX / VFX")]
        [Tooltip("Am thanh (SFX) phat tren Player khi su dung skill. De trong neu khong co.")]
        [SerializeField] protected AudioClip _castSfx;
        public AudioClip CastSfx => _castSfx;

        [Tooltip("Hieu ung hinh anh (VFX) spawn tren Player khi su dung skill. Duoc pool quan ly. De trong neu khong co.")]
        [SerializeField] protected GameObject _castVfx;
        public GameObject CastVfx => _castVfx;

        [Header("SFX / VFX Timing")]
        [Tooltip("Phat SFX va VFX ngay tai thoi diem kich hoat skill (mac dinh bat).")]
        [SerializeField] protected bool _playCastOnActivate = true;
        public bool PlayCastOnActivate => _playCastOnActivate;

        [Tooltip("Phat SFX va VFX sau khi het duration (ket thuc hieu luc skill). Mac dinh tat.")]
        [SerializeField] protected bool _playCastOnDeactivate = false;
        public bool PlayCastOnDeactivate => _playCastOnDeactivate;

        /// <summary>
        /// Xac dinh vi tri spawn VFX cua skill (CastVfx):
        /// LocalPlayer = gan VFX lam con de di cung Player, World = tao trong world space tai vi tri Player.
        /// </summary>
        public enum VfxSpawnMode
        {
            [Tooltip("Spawn VFX tai vi tri Player va gan lam con de di cung Player (mac dinh). VFX se bien mat theo khoang cach gan vung.\nDung cho hieu ung can theo sat player.")]
            LocalPlayer,
            [Tooltip("Spawn VFX trong world space tai vi tri Player, khong gan theo Player.\nDung cho hieu ung vu no/loa tren world de tranh bi nho do scale cua Player va de tu quan ly vong doi (pool).")]
            World
        }

        [Tooltip("Vi tri spawn VFX (CastVfx): LocalPlayer = gan lam con de di cung Player, World = tao trong world space tai vi tri Player (khong gan).")]
        [SerializeField] protected VfxSpawnMode _castVfxSpawnMode = VfxSpawnMode.LocalPlayer;
        public VfxSpawnMode CastVfxSpawnMode => _castVfxSpawnMode;

        [Tooltip("Ban kinh (world space) dung de Trigger ExplosionEffectController cua CastVfx khi chay (vi du khi het duration).\nChi co tac dung khi CastVfx co ExplosionEffectController; ParticleSystem thuong se bo qua.")]
        [SerializeField] protected float _castVfxExplosionRadius = 2f;
        public float CastVfxExplosionRadius => _castVfxExplosionRadius;

        /// <summary>
        /// Tra ve hanh vi thuc thi logic (Strategy) cua skill.
        /// Cac class con can override de cung cap logic tuong ung.
        /// </summary>
        public abstract ISkillBehavior Behavior { get; }

        /// <summary>
        /// Cho phep skill tu quyet dinh co duoc su dung trong dieu kien hien tai hay khong
        /// (vi du: Heal khong dung duoc khi dang day HP). Mac dinh luon cho phep.
        /// Duoc goi boi PlayerSkillController truoc khi tieu thu energy.
        /// </summary>
        /// <param name="player">Nguoi choi dang su dung skill.</param>
        /// <returns>True neu duoc phep su dung, false neu chan.</returns>
        public virtual bool CanActivate(IPlayer player) => true;
    }
}
