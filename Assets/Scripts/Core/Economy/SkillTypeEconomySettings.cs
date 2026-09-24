using System;
using UnityEngine;
using Core.Interfaces;

namespace Core.Economy
{
    /// <summary>
    /// Cau hinh kinh te cho mot nhom Skill trong Shop: gia khoi diem va he so tang.
    /// Gia hien tai cua nhom = BasePrice * (GrowthFactor^so lan quay thuong).
    /// Moi nhom co mot moc gia khoi diem va mot he so rieng.
    /// </summary>
    [Serializable]
    public class SkillTypeEconomySettings
    {
        [Tooltip("Loai nhom skill (Movement / Defensive).")]
        [SerializeField] private SkillType _skillType;

        [Tooltip("Gia khoi diem (Credits) de mua mot lan quay cua SkillType nay.")]
        [SerializeField] private int _basePrice = 100;

        [Tooltip("He so tang gia sau moi lan quay that cua BAT KY nhom skill nao. Gia = base * factor^soLanQuay.")]
        [SerializeField] private float _growthFactor = 1.15f;

        /// <summary>
        /// Loai nhom skill ma cau hinh nay ap dung.
        /// </summary>
        public SkillType Type => _skillType;

        /// <summary>
        /// Gia khoi diem (Credits).
        /// </summary>
        public int BasePrice => _basePrice;

        /// <summary>
        /// He so tang gia sau moi lan quay.
        /// </summary>
        public float GrowthFactor => _growthFactor;

        /// <summary>
        /// Tinh gia dung de mua mot lan quay cua nhom nay sau `spinCount` lan quay da thuc hien.
        /// </summary>
        /// <param name="spinCount">Tong so lan quay da mua (tren tat ca cac nhom).</param>
        public int GetPrice(int spinCount)
        {
            if (spinCount <= 0) return _basePrice;
            return Mathf.RoundToInt(_basePrice * Mathf.Pow(_growthFactor, spinCount));
        }
    }
}