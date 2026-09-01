using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien chua du lieu cau hinh tinh cua mot Skill.
    /// </summary>
    public interface ISkillData
    {
        /// <summary>
        /// Am thanh (SFX) phat tren Player khi su dung skill.
        /// Co the null neu skill khong co SFX rieng.
        /// </summary>
        AudioClip CastSfx { get; }

        /// <summary>
        /// Hieu ung hinh anh (VFX) duoc spawn tren Player khi su dung skill (se duoc VFXPoolManager quan ly - pool).
        /// Co the null neu skill khong co VFX.
        /// </summary>
        GameObject CastVfx { get; }
        /// <summary>
        /// ID duy nhat cua skill.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Ten hien thi cua skill.
        /// </summary>
        string DisplayName { get; }

        /// <summary>
        /// Mo ta ngan gon ve skill.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Icon hien thi cua skill tren UI.
        /// </summary>
        Sprite Icon { get; }

        /// <summary>
        /// Loai skill (Movement hoac Defensive).
        /// </summary>
        SkillType Type { get; }

        /// <summary>
        /// Gia tien de mua skill trong shop.
        /// </summary>
        int Price { get; }

        /// <summary>
        /// Thoi gian hieu luc cua skill (tinh bang giay).
        /// </summary>
        float Duration { get; }

        /// <summary>
        /// Thoi gian sac/cooldown cua skill (tinh bang giay).
        /// </summary>
        float RechargeTime { get; }

        /// <summary>
        /// Chien luoc thuc thi logic cua skill.
        /// </summary>
        ISkillBehavior Behavior { get; }
    }
}
