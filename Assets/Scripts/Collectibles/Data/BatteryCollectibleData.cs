using UnityEngine;

namespace Collectibles.Data
{
    /// <summary>
    /// Du lieu cau hinh cua collectible Battery: sac day 100% energy khi player dang trong qua trinh tu sac (recharge).
    /// Khong the nhap khi energy dang day.
    /// </summary>
    [CreateAssetMenu(fileName = "Collectible_Battery", menuName = "Kaboom Chaos/Collectibles/Battery")]
    public class BatteryCollectibleData : BaseCollectibleData
    {
        [Header("Battery Properties")]
        [Tooltip("So luong energy toi da (luong energy se duoc sac ve day).")]
        [SerializeField] private float _maxEnergyCharge = 100f;
        public float MaxEnergyCharge => _maxEnergyCharge;

        [Tooltip("Am thanh phat khi khong the nhap vi energy dang day (Charge Full).")]
        [SerializeField] private AudioClip _chargeFullSfx;
        public AudioClip ChargeFullSfx => _chargeFullSfx;
    }
}