using System.Collections.Generic;
using UnityEngine;
using Core.Interfaces;

namespace Core.Economy
{
    /// <summary>
    /// ScriptableObject chua toan bo cau hinh kinh te cho tung SkillType trong Shop.
    /// Cung cap phuong thuc tra gia hien tai cua mot SkillType sau mot so lan quay nhat dinh.
    /// </summary>
    [CreateAssetMenu(fileName = "SkillShopConfig", menuName = "Kaboom Chaos/Economy/Skill Shop Config")]
    public class SkillTypeConfig : ScriptableObject
    {
        [Tooltip("Danh sach cau hinh gia va he so tang cho tung SkillType.")]
        [SerializeField] private List<SkillTypeEconomySettings> _typeEconomies = new();

        /// <summary>
        /// Tim cau hinh kinh te cua mot SkillType. Tra ve null neu khong co.
        /// </summary>
        /// <param name="type">Loai SkillType.</param>
        public SkillTypeEconomySettings GetSettings(SkillType type)
        {
            if (_typeEconomies == null) return null;

            foreach (SkillTypeEconomySettings settings in _typeEconomies)
            {
                if (settings != null && settings.Type == type)
                {
                    return settings;
                }
            }
            return null;
        }

        /// <summary>
        /// Gia cua mot SkillType tai thoi diem da quay `spinCount` lan.
        /// </summary>
        /// <param name="type">Loai SkillType.</param>
        /// <param name="spinCount">Tong so lan quay tren tat ca cac SkillType.</param>
        public int GetPrice(SkillType type, int spinCount)
        {
            SkillTypeEconomySettings settings = GetSettings(type);
            return settings != null ? settings.GetPrice(spinCount) : 0;
        }
    }
}