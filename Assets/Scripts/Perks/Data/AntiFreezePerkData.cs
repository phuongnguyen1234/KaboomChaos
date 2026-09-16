using UnityEngine;
using Core;
using Core.Interfaces;
using Perks.Behaviors;

namespace Perks.Data
{
    /// <summary>
    /// Du lieu cau hinh cua Perk Anti-Freeze.
    /// Player khong the bi dong bang. Moi lan tiep xuc voi vu no bang se tang Max HP va hoi 10 HP.
    /// Toan bo bien doi chi co hieu luc trong round; khi quay ve lobby Max HP tu reset ve gia tri ban dau (100).
    /// </summary>
    [CreateAssetMenu(fileName = "AntiFreezePerkData", menuName = "Kaboom Chaos/Perks/Anti Freeze")]
    public class AntiFreezePerkData : BasePerkData
    {
        [Header("Thong So Anti Freeze")]
        [Tooltip("Luong Max HP tang them moi lan player tiep xuc voi mot vu no bang.")]
        [SerializeField] private float _maxHealthBonus = 10f;
        public float MaxHealthBonus => _maxHealthBonus;

        [Tooltip("Luong HP duoc hoi (heal) moi lan player tiep xuc voi mot vu no bang.")]
        [SerializeField] private float _healBonus = 10f;
        public float HealBonus => _healBonus;

        private AntiFreezeBehavior _behavior;

        public override IPerkBehavior Behavior => _behavior ??= new AntiFreezeBehavior(this);
    }
}