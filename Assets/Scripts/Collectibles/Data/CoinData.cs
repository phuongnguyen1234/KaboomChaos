// Tạo file tại: Assets/Scripts/Collectibles/Data/CoinData.cs
using UnityEngine;

namespace Collectibles.Data
{
    [CreateAssetMenu(fileName = "NewCoinData", menuName = "Kaboom Chaos/Collectibles/Coin")]
    public class CoinData : BaseCollectibleData
    {
        [Header("Coin Specific")]
        [Tooltip("How many credits this coin is worth.")]
        [SerializeField] private int _creditValue = 10;
        public int CreditValue => _creditValue;

        [Tooltip("TODO: Bonus HP awarded on collection.")]
        [SerializeField] private float _bonusHP = 0f;
        public float BonusHP => _bonusHP;
    }
}
