using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien chua du lieu cau hinh tinh cua mot Perk.
    /// Khac voi Skill: Perk la hieu ung noi tai, khong co duration/recharge/energy,
    /// luon kich hoat khi duoc trang bi trong round.
    /// </summary>
    public interface IPerkData
    {
        /// <summary>
        /// ID duy nhat cua perk.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Ten hien thi cua perk.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Mo ta ngan gon ve perk.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Icon hien thi cua perk tren UI.
        /// </summary>
        Sprite Icon { get; }

        /// <summary>
        /// Gia tien de mua perk trong shop.
        /// </summary>
        int Price { get; }

        /// <summary>
        /// Chien luoc thuc thi logic (Strategy) cua perk.
        /// </summary>
        IPerkBehavior Behavior { get; }
    }
}