using UnityEngine;
using UnityEngine.Serialization;
using Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace Bombs.Data
{
    /// <summary>
    /// Định nghĩa một biến thể bom và xác suất xuất hiện của nó.
    /// </summary>
    [System.Serializable] // Giữ lại Serializable để hiển thị trong Inspector
    public class BombVariant : IBombVariant
    {
        [Tooltip("Biến thể bom sẽ được sinh ra.")]
        public BaseBombData variantBombData;
        IBaseBombData IBombVariant.VariantBombData => variantBombData; // Explicit interface implementation

        [Tooltip("Đường cong xác suất (0-100) mà biến thể này sẽ xuất hiện thay cho bom gốc, dựa trên intensity. Trục X là intensity, trục Y là xác suất (%).")]
        [FormerlySerializedAs("spawnChanceByDifficulty")]
        public AnimationCurve spawnChanceByIntensity = AnimationCurve.Linear(0, 5, 1, 1);
        AnimationCurve IBombVariant.SpawnChanceByIntensity => spawnChanceByIntensity; // Explicit interface implementation
    }

    /// <summary>
    /// Lớp cơ sở trừu tượng cho tất cả các loại dữ liệu bom.
    /// Chứa các thuộc tính chung liên quan đến vụ nổ, sát thương, hiệu ứng và thông tin game.
    /// </summary>
    public abstract class BaseBombData : ScriptableObject, IBaseBombData // Triển khai interface mới
    {
        [Header("Thông tin cơ bản")]
        [Tooltip("ID định danh duy nhất cho loại bom này.")]
        public string id = "default-bomb";
        public string Id => id; // Triển khai interface

        [Tooltip("Tên hiển thị của bom.")]
        public string displayName = "Default Bomb";
        public string DisplayName => displayName; // Triển khai interface

        [Tooltip("Mô tả ngắn về bom.")]
        [TextArea] public string description = "Một quả bom cổ điển, quen thuộc.";
        public string Description => description; // Triển khai interface

        [Header("Vụ nổ")]
        [Tooltip("Bán kính của vụ nổ (ảnh hưởng đến sát thương, hiệu ứng và phá hủy địa hình).")]
        public float radius = 5.0f;
        public float Radius => radius; // Triển khai interface

        [Tooltip("Sát thương gây ra ở tâm vụ nổ.")]
        public float damage = 100.0f;
        public float Damage => damage; // Triển khai interface

        [Tooltip("Đường cong hệ số sát thương theo khoảng cách. Trục X là khoảng cách chuẩn hóa (0 = tâm nổ, 1 = rìa vụ nổ). Trục Y là hệ số nhân sát thương (1 = 100% sát thương, 0 = 0% sát thương).")]
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0, 1, 1, 0);
        public AnimationCurve DamageFalloff => damageFalloff; // Triển khai interface

        [Tooltip("Loại hiệu ứng trạng thái mà vụ nổ gây ra.")]
        public StatusEffectType effect = StatusEffectType.None;
        public StatusEffectType Effect => effect; // Triển khai interface

        [Tooltip("Thời gian hiệu ứng trạng thái (nếu có).")]
        public float effectDuration = 5.0f;
        public float EffectDuration => effectDuration; // Triển khai interface

        [Tooltip("Các layer sẽ bị ảnh hưởng bởi hiệu ứng trạng thái (lửa, băng...). Nếu để trống (Nothing), sẽ không có hiệu ứng nào được áp dụng.")]
        public LayerMask statusEffectLayers;
        public LayerMask StatusEffectLayers => statusEffectLayers; // Triển khai interface

        [Header("Tác động Vật lý")]
        [Tooltip("Vụ nổ có tạo ra lực đẩy không?")]
        public bool applyForce = true;
        public bool ApplyForce => applyForce; // Triển khai interface

        [Tooltip("Lực đẩy tác dụng lên các đối tượng di động (người chơi, vật thể) ở tâm vụ nổ.")]
        public float force = 1000.0f;
        public float Force => force; // Triển khai interface

        [Tooltip("Chế độ áp dụng lực (Impulse là tức thời, Force là liên tục).")]
        public ForceMode forceMode = ForceMode.Impulse;
        public ForceMode ForceMode => forceMode; // Triển khai interface

        [Tooltip("Đường cong hệ số lực đẩy theo khoảng cách. Trục X là khoảng cách chuẩn hóa (0 = tâm nổ, 1 = rìa vụ nổ). Trục Y là hệ số nhân lực đẩy (1 = 100% lực, 0 = 0% lực).")]
        public AnimationCurve forceFalloff = AnimationCurve.Linear(0, 1, 1, 0);
        public AnimationCurve ForceFalloff => forceFalloff; // Triển khai interface

        [Tooltip("Hệ số đẩy lên trên của vụ nổ. Giá trị 0 tạo ra một vụ nổ đẩy ra thuần túy. Giá trị lớn hơn sẽ thêm một lực đẩy lên trên, làm cho các vật thể bị hất tung lên trời.")]
        [Range(0f, 5f)] public float upwardsModifier = 1.0f;
        public float UpwardsModifier => upwardsModifier; // Triển khai interface

        [Header("Mục tiêu & Tương tác")]
        [Tooltip("Các layer mà vụ nổ sẽ ảnh hưởng (sát thương và lực đẩy).")]
        public LayerMask affectedLayers;
        public LayerMask AffectedLayers => affectedLayers; // Triển khai interface

        [Tooltip("Các layer sẽ kích hoạt bom khi va chạm (ví dụ: tên lửa nổ khi chạm đất). Nếu để trống (Nothing), sẽ sử dụng 'Affected Layers' làm mặc định cho các hành vi có hỗ trợ.")]
        public LayerMask triggerLayers;
        public LayerMask TriggerLayers => triggerLayers; // Triển khai interface

        [Tooltip("Bom có thể phá hủy địa hình không?")]
        public bool canDestroyTerrain = true;
        public bool CanDestroyTerrain => canDestroyTerrain; // Triển khai interface

        [Tooltip("Đường cong sức mạnh phá hủy địa hình theo khoảng cách. Trục X là khoảng cách chuẩn hóa (0 = tâm, 1 = rìa). Trục Y là GIÁ TRỊ SỨC MẠNH PHÁ HỦY tuyệt đối (không phải hệ số), sẽ được làm tròn thành số nguyên. Mặc định là đường thẳng từ 5 xuống 1.")]
        public AnimationCurve terrainDestructionPowerFalloff = AnimationCurve.Linear(0, 5, 1, 1);
        public AnimationCurve TerrainDestructionPowerFalloff => terrainDestructionPowerFalloff; // Triển khai interface

        [Header("Hành vi đặc biệt")]
        [Tooltip("Bom có nổ nhiều lần không?")]
        public bool explodeMultipleTimes = false;
        public bool ExplodeMultipleTimes => explodeMultipleTimes; // Triển khai interface

        [Header("Multiple Explosions Range")]
        [Tooltip("Số lần nổ tối thiểu (nếu 'Explode Multiple Times' được bật).")]
        public int minNumberOfExplosions = 1;
        public int MinNumberOfExplosions => minNumberOfExplosions; // Triển khai interface

        [Tooltip("Số lần nổ tối đa (nếu 'Explode Multiple Times' được bật).")]
        public int maxNumberOfExplosions = 3;
        public int MaxNumberOfExplosions => maxNumberOfExplosions; // Triển khai interface

        [Header("Âm thanh khi sinh ra")]
        [Tooltip("Âm thanh sẽ phát khi bom được sinh ra.")]
        public AudioClip spawnSound;
        public AudioClip SpawnSound => spawnSound; // Triển khai interface

        [Tooltip("Thời gian chờ giữa các lần nổ.")]
        public float delayBetweenExplosions = 0.5f;
        public float DelayBetweenExplosions => delayBetweenExplosions; // Triển khai interface

        [Tooltip("Bán kính phân tán của các vụ nổ phụ. Nếu bằng 0, tất cả các vụ nổ sẽ xảy ra tại cùng một điểm.")]
        public float explosionSpreadRadius = 2f;
        public float ExplosionSpreadRadius => explosionSpreadRadius; // Triển khai interface

        [Header("Sub-Explosion Settings (for Multiple Explosions)")]
        [Tooltip("Sát thương gây ra bởi mỗi vụ nổ phụ (nếu 'Explode Multiple Times' được bật).")]
        public float subExplosionDamage = 25f;
        public float SubExplosionDamage => subExplosionDamage; // Triển khai interface
        [Tooltip("Đường cong giảm sát thương cho các vụ nổ phụ.")]
        public AnimationCurve subExplosionDamageFalloff = AnimationCurve.Linear(0, 1, 1, 0);
        public AnimationCurve SubExplosionDamageFalloff => subExplosionDamageFalloff; // Triển khai interface

        [Header("Hình ảnh & Âm thanh Vụ Nổ")]
        [Tooltip("Prefab hiệu ứng hình ảnh cho vụ nổ chính. Nếu là hiệu ứng scale, prefab này nên có component 'ExplosionEffectController'.")]
        public GameObject explosionVFX;
        public GameObject ExplosionVFX => explosionVFX; // Triển khai interface

        [Tooltip("Cao độ (pitch) của âm thanh vụ nổ chính.")]
        [Range(0.1f, 3f)] public float explosionSoundPitch = 1.0f;
        public float ExplosionSoundPitch => explosionSoundPitch; // Triển khai interface

        [Tooltip("Âm thanh của vụ nổ chính.")]
        public AudioClip explosionSound;
        public AudioClip ExplosionSound => explosionSound; // Triển khai interface

        [Header("Player Spawn Chance")]
        [Tooltip("Xác suất TỐI THIỂU (0-100) mà bom này sẽ spawn gần một người chơi.")]
        [Range(0f, 100f)] public float minPlayerSpawnChance = 0f;
        public float MinPlayerSpawnChance => minPlayerSpawnChance; // Triển khai interface

        [Tooltip("Xác suất TỐI ĐA (0-100) mà bom này sẽ spawn gần một người chơi.")]
        [Range(0f, 100f)] public float maxPlayerSpawnChance = 10f;
        public float MaxPlayerSpawnChance => maxPlayerSpawnChance; // Triển khai interface

        [Tooltip("Bán kính ngẫu nhiên xung quanh người chơi mà bom có thể spawn.")]
        public float playerSpawnRadius = 2f;
        public float PlayerSpawnRadius => playerSpawnRadius; // Triển khai interface

        [Header("Variants (Các biến thể)")]
        [Tooltip("Danh sách các biến thể có thể thay thế cho bom này khi được sinh ra. Hệ thống sẽ duyệt qua danh sách này từ trên xuống dưới và chọn biến thể đầu tiên thỏa mãn xác suất.")]
        public List<BombVariant> possibleVariants = new();
        public List<IBombVariant> PossibleVariants => possibleVariants.Cast<IBombVariant>().ToList(); // Triển khai interface

        [Header("Thông tin Game & Xác suất xuất hiện")]
        [Tooltip("Đường cong trọng số xuất hiện theo intensity. Trục X là intensity của game. Trục Y là trọng số tương đối (càng cao càng dễ xuất hiện). Mặc định được thiết lập cho intensity từ 1-6, bom sẽ hiếm hơn khi intensity tăng.")]
        [FormerlySerializedAs("spawnWeightByDifficulty")]
        public AnimationCurve spawnWeightByIntensity = AnimationCurve.Linear(1, 100, 6, 20);
        public AnimationCurve SpawnWeightByIntensity => spawnWeightByIntensity; // Triển khai interface

        [Header("Giới hạn Sinh (Spawning Limits)")]
        [Tooltip("Số lượng bom tối đa cùng loại được phép sinh ra trong một đợt. 0 = không giới hạn.")]
        public int maxPerSpawnWave = 1;
        public int MaxPerSpawnWave => maxPerSpawnWave; // Triển khai interface

        [Tooltip("Số lượng bom tối đa cùng loại được phép tồn tại (active) trong màn chơi. 0 = không giới hạn.")]
        public int maxActiveInstances = 0;
        public int MaxActiveInstances => maxActiveInstances; // Triển khai interface
    }
}