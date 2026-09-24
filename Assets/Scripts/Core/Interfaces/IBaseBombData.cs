using UnityEngine;
using System.Collections.Generic;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho các biến thể bom.
    /// </summary>
    public interface IBombVariant
    {
        IBaseBombData VariantBombData { get; }
        AnimationCurve SpawnChanceByIntensity { get; }
    }

    /// <summary>
    /// Interface cơ sở cho tất cả các loại dữ liệu bom.
    /// Chứa các thuộc tính chung liên quan đến vụ nổ, sát thương, hiệu ứng và thông tin game.
    /// </summary>
    public interface IBaseBombData : ISpawnableData
    {
        string DisplayName { get; }
        string Description { get; }

        float Radius { get; }
        float Damage { get; }
        AnimationCurve DamageFalloff { get; }
        StatusEffectType Effect { get; }
        float EffectDuration { get; }
        LayerMask StatusEffectLayers { get; }

        bool ApplyForce { get; }
        float Force { get; }
        ForceMode ForceMode { get; }
        AnimationCurve ForceFalloff { get; }
        float UpwardsModifier { get; }

        LayerMask AffectedLayers { get; }
        LayerMask TriggerLayers { get; }
        bool CanDestroyTerrain { get; }
        AnimationCurve TerrainDestructionPowerFalloff { get; }

        bool ExplodeMultipleTimes { get; }
        int MinNumberOfExplosions { get; }
        int MaxNumberOfExplosions { get; }
        AudioClip SpawnSound { get; }
        float DelayBetweenExplosions { get; }
        float ExplosionSpreadRadius { get; }
        float SubExplosionDamage { get; }
        AnimationCurve SubExplosionDamageFalloff { get; }

        GameObject ExplosionVFX { get; }
        float ExplosionSoundPitch { get; }
        AudioClip ExplosionSound { get; }

        float MinPlayerSpawnChance { get; }
        float MaxPlayerSpawnChance { get; }
        float PlayerSpawnRadius { get; }

        List<IBombVariant> PossibleVariants { get; }

        AnimationCurve SpawnWeightByIntensity { get; }

        /// <summary>Số lượng bom tối đa cùng loại được phép sinh ra trong một đợt. 0 = không giới hạn.</summary>
        int MaxPerSpawnWave { get; }

        /// <summary>Số lượng bom tối đa cùng loại được phép tồn tại (active) trong màn chơi. 0 = không giới hạn.</summary>
        int MaxActiveInstances { get; }
    }
}