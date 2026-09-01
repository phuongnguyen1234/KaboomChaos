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

        /// <summary>
        /// Tra ve hanh vi thuc thi logic (Strategy) cua skill.
        /// Cac class con can override de cung cap logic tuong ung.
        /// </summary>
        public abstract ISkillBehavior Behavior { get; }
    }
}
