using UnityEngine;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Lớp cơ sở trừu tượng cho tất cả các loại dữ liệu bom.
    /// Chứa các thuộc tính chung liên quan đến vụ nổ, sát thương, hiệu ứng và thông tin game.
    /// </summary>
    public abstract class BaseBombData : ScriptableObject
    {
        [Header("Thông tin cơ bản")]
        [Tooltip("ID định danh duy nhất cho loại bom này.")]
        public string id = "BOMB_DEFAULT";
        [Tooltip("Tên hiển thị của bom.")]
        public string displayName = "Classic Bomb";
        [Tooltip("Mô tả ngắn về bom.")]
        [TextArea] public string description = "Một quả bom cổ điển, quen thuộc.";
        [Tooltip("Prefab của quả bom này (phải có component BombController và đã gán sẵn Data).")]
        public GameObject bombPrefab;

        [Header("Vụ nổ")]
        [Tooltip("Bán kính của vụ nổ (ảnh hưởng đến sát thương, hiệu ứng và phá hủy địa hình).")]
        public float radius = 5.0f;
        [Tooltip("Sát thương gây ra ở tâm vụ nổ.")]
        public float damage = 100.0f;
        [Tooltip("Đường cong hệ số sát thương theo khoảng cách. Trục X là khoảng cách chuẩn hóa (0 = tâm nổ, 1 = rìa vụ nổ). Trục Y là hệ số nhân sát thương (1 = 100% sát thương, 0 = 0% sát thương).")]
        public AnimationCurve damageFalloff = AnimationCurve.Linear(0, 1, 1, 0);
        [Tooltip("Loại hiệu ứng trạng thái mà vụ nổ gây ra.")]
        public StatusEffectType effect = StatusEffectType.None;
        [Tooltip("Thời gian hiệu ứng trạng thái (nếu có).")]
        public float effectDuration = 5.0f;
        [Tooltip("Các layer sẽ bị ảnh hưởng bởi hiệu ứng trạng thái (lửa, băng...). Nếu để trống (Nothing), sẽ không có hiệu ứng nào được áp dụng.")]
        public LayerMask statusEffectLayers;

        [Header("Tác động Vật lý")]
        [Tooltip("Vụ nổ có tạo ra lực đẩy không?")]
        public bool applyForce = true;
        [Tooltip("Lực đẩy tác dụng lên các đối tượng di động (người chơi, vật thể) ở tâm vụ nổ.")]
        public float force = 1000.0f;
        [Tooltip("Chế độ áp dụng lực (Impulse là tức thời, Force là liên tục).")]
        public ForceMode forceMode = ForceMode.Impulse;
        [Tooltip("Đường cong hệ số lực đẩy theo khoảng cách. Trục X là khoảng cách chuẩn hóa (0 = tâm nổ, 1 = rìa vụ nổ). Trục Y là hệ số nhân lực đẩy (1 = 100% lực, 0 = 0% lực).")]
        public AnimationCurve forceFalloff = AnimationCurve.Linear(0, 1, 1, 0);
        [Tooltip("Hệ số đẩy lên trên của vụ nổ. Giá trị 0 tạo ra một vụ nổ đẩy ra thuần túy. Giá trị lớn hơn sẽ thêm một lực đẩy lên trên, làm cho các vật thể bị hất tung lên trời.")]
        [Range(0f, 5f)] public float upwardsModifier = 1.0f;

        [Header("Mục tiêu & Tương tác")]
        [Tooltip("Các layer mà vụ nổ sẽ ảnh hưởng (sát thương và lực đẩy).")]
        public LayerMask affectedLayers;
        [Tooltip("Bom có thể phá hủy địa hình không?")]
        public bool canDestroyTerrain = true;
        [Tooltip("Đường cong sức mạnh phá hủy địa hình theo khoảng cách. Trục X là khoảng cách chuẩn hóa (0 = tâm, 1 = rìa). Trục Y là GIÁ TRỊ SỨC MẠNH PHÁ HỦY tuyệt đối (không phải hệ số), sẽ được làm tròn thành số nguyên. Mặc định là đường thẳng từ 5 xuống 1.")]
        public AnimationCurve terrainDestructionPowerFalloff = AnimationCurve.Linear(0, 5, 1, 1);

        [Header("Hành vi đặc biệt")]
        [Tooltip("Bom có nổ nhiều lần không?")]
        public bool explodeMultipleTimes = false;
        [Tooltip("Số lần nổ (nếu 'Explode Multiple Times' được bật).")]
        public int numberOfExplosions = 3;
        [Tooltip("Thời gian chờ giữa các lần nổ.")]
        public float delayBetweenExplosions = 0.5f;
        [Tooltip("Bán kính phân tán của các vụ nổ phụ. Nếu bằng 0, tất cả các vụ nổ sẽ xảy ra tại cùng một điểm.")]
        public float explosionSpreadRadius = 2f;

        [Header("Hình ảnh & Âm thanh Vụ Nổ")]
        [Tooltip("Prefab hiệu ứng hình ảnh cho vụ nổ chính. Nếu là hiệu ứng scale, prefab này nên có component 'ExplosionEffectController'.")]
        public GameObject explosionVFX;
        [Tooltip("Âm thanh của vụ nổ chính.")]
        public AudioClip explosionSound;

        [Header("Thông tin Game & Xác suất xuất hiện")]
        [Tooltip("Đường cong trọng số xuất hiện theo độ khó. Trục X là độ khó của game. Trục Y là trọng số tương đối (càng cao càng dễ xuất hiện). Mặc định được thiết lập cho độ khó từ 1-5, bom sẽ hiếm hơn khi độ khó tăng.")]
        public AnimationCurve spawnWeightByDifficulty = AnimationCurve.Linear(1, 100, 5, 20);
    }
}