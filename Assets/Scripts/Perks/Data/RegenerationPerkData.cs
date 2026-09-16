using UnityEngine;
using Core;
using Core.Interfaces;
using Perks.Behaviors;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk Regeneration.
    /// Hoi +5 HP moi 5 giay neu player khong nhan bat ky sat thuong nao trong 10 giay.
    /// </summary>
    [CreateAssetMenu(fileName = "RegenerationPerkData", menuName = "Kaboom Chaos/Perks/Regeneration")]
    public class RegenerationPerkData : BasePerkData
    {
        [Header("Thong So Regeneration")]
        [Tooltip("Luong HP hoi phuc moi lan tick.")]
        [SerializeField] private float _healPerTick = 5f;
        public float HealPerTick => _healPerTick;

        [Tooltip("Khoang thoi gian giua cac lan hoi phuc (giay).")]
        [SerializeField] private float _tickInterval = 5f;
        public float TickInterval => _tickInterval;

        [Tooltip("Cua so thoi gian an toan khong nhan sat thuong de kich hoat hoi phuc (giay).")]
        [SerializeField] private float _noDamageWindow = 10f;
        public float NoDamageWindow => _noDamageWindow;

        private RegenerationBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new RegenerationBehavior(this);
    }
}