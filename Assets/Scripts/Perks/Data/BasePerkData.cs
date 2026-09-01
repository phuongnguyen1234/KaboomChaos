using UnityEngine;
using Core.Interfaces;

namespace Perks.Data
{
    /// <summary>
    /// Lop co so truu tuong cho tat ca du lieu cua Perk.
    /// Trien khai tu ScriptableObject va IPerkData.
    /// Perk la hieu ung noi tai: khong co duration/recharge/energy nhu Skill.
    /// </summary>
    public abstract class BasePerkData : ScriptableObject, IPerkData
    {
        [Header("Thong Tin Co Ban")]
        [Tooltip("ID duy nhat dung de phan biet cac perk.")]
        [SerializeField] protected string _id;
        public string Id => _id;

        [Tooltip("Ten hien thi tren giao dien nguoi choi.")]
        [SerializeField] protected string _displayName;
        public string DisplayName => _displayName;

        [Tooltip("Mo ta ngan gon ve chuc nang va tac dung cua perk.")]
        [TextArea(3, 5)]
        [SerializeField] protected string _description;
        public string Description => _description;

        [Tooltip("Anh dai dien cho perk.")]
        [SerializeField] protected Sprite _icon;
        public Sprite Icon => _icon;

        [Header("Shop")]
        [Tooltip("Gia ban trong Shop (tinh bang Coin).")]
        [SerializeField] protected int _price;
        public int Price => _price;

        /// <summary>
        /// Tra ve hanh vi thuc thi logic (Strategy) cua perk.
        /// Cac class con can override de cung cap logic tuong ung.
        /// </summary>
        public abstract IPerkBehavior Behavior { get; }
    }
}