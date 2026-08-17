using UnityEngine;
using System.Collections.Generic;
using System;

namespace Bombs.Data
{
    /// <summary>
    /// Defines a visual/audio stage during the bomb's fuse time.
    /// </summary>
    [Serializable]
    public class FuseStage
    {
        [Tooltip("Thời gian (tính bằng giây) kể từ khi kích hoạt mà stage này sẽ bắt đầu.")]
        public float startTime; // Giữ lại public field cho Inspector
        public float StartTime => startTime; // Triển khai interface (nếu có)

        [Header("External Effects")]
        [Tooltip("Hiệu ứng hình ảnh (particle) sẽ được tạo ra tại vị trí của bom.")]
        public GameObject visualEffect; // Giữ lại public field cho Inspector
        public GameObject VisualEffect => visualEffect; // Triển khai interface (nếu có)

        [Tooltip("Cao độ (pitch) của âm thanh sẽ được phát.")]
        [Range(0.1f, 3f)] public float stageSoundPitch = 1.0f;
        public float StageSoundPitch => stageSoundPitch; // Triển khai interface (nếu có)

        [Tooltip("Âm thanh sẽ được phát.")]
        public AudioClip stageSound; // Giữ lại public field cho Inspector
        public AudioClip StageSound => stageSound; // Triển khai interface (nếu có)

        [Header("External VFX Animation (if VFX has ExplosionEffectController)")]
        [Tooltip("Bán kính mục tiêu của hiệu ứng hình ảnh.")]
        public float vfxRadius = 1.0f;

        [Header("Internal Effects (Base Color Pulse)")]
        [Tooltip("Bật để kích hoạt hiệu ứng nháy màu nền. Yêu cầu shader hỗ trợ '_BaseColor'.")]
        public bool enableBaseColorPulse = false; // Giữ lại public field cho Inspector
        public bool EnableBaseColorPulse => enableBaseColorPulse; // Triển khai interface (nếu có)

        [ColorUsage(true, true)]
        [Tooltip("Màu nền sẽ được nháy tới.")]
        public Color basePulseColor = Color.red;
        public Color BasePulseColor => basePulseColor; // Triển khai interface (nếu có)

        [Tooltip("Thời gian nháy màu nền (giây).")]
        public float basePulseDuration = 0.2f;
        public float BasePulseDuration => basePulseDuration; // Triển khai interface (nếu có)

        [Header("Internal Effects (Decal Color Pulse)")]
        [Tooltip("Bật để kích hoạt hiệu ứng nháy màu họa tiết. Yêu cầu shader hỗ trợ '_DecalColor'.")]
        public bool enableDecalColorPulse = false; // Giữ lại public field cho Inspector
        public bool EnableDecalColorPulse => enableDecalColorPulse; // Triển khai interface (nếu có)

        [ColorUsage(true, true)]
        [Tooltip("Màu họa tiết sẽ được nháy tới.")]
        public Color decalPulseColor = Color.yellow;
        public Color DecalPulseColor => decalPulseColor; // Triển khai interface (nếu có)

        [Tooltip("Thời gian nháy màu họa tiết (giây).")]
        public float decalPulseDuration = 0.2f;
        public float DecalPulseDuration => decalPulseDuration; // Triển khai interface (nếu có)

        [Header("Internal Effects (Scale Pulse)")]
        [Tooltip("Bật để kích hoạt hiệu ứng nháy scale trên các bộ phận được chỉ định trong BombController (Parts To Pulse).")]
        public bool enableScalePulse = false; // Giữ lại public field cho Inspector
        public bool EnableScalePulse => enableScalePulse; // Triển khai interface (nếu có)
        [Tooltip("Hệ số scale mục tiêu (ví dụ: 1.2 cho lớn hơn 20%).")]
        public float pulseScaleMultiplier = 1.2f;
        [Tooltip("Thời gian nháy scale (giây).")]
        public float pulseScaleDuration = 0.2f;
    }

    /// <summary>
    /// ScriptableObject chứa tất cả dữ liệu cấu hình cho một loại bom.
    /// Điều này cho phép các nhà thiết kế tạo ra nhiều loại bom khác nhau mà không cần viết code mới.
    /// </summary>
    [CreateAssetMenu(fileName = "NewFuseBombData", menuName = "Kaboom Chaos/Bomb Types/Fuse Bomb")]
    public class BombData : BaseBombData
    {
        [Header("Kích hoạt & Hẹn giờ")]
        [Tooltip("Thời gian (giây) từ lúc kích hoạt đến lúc nổ.")]
        public float fuseTime = 3.0f; // Giữ lại public field cho Inspector
        public float FuseTime => fuseTime; // Triển khai interface (nếu có)

        [Tooltip("Bom có tự kích hoạt khi va chạm với đối tượng khác không?")]
        public bool isActivatedOnContact = false; // Giữ lại public field cho Inspector
        public bool IsActivatedOnContact => isActivatedOnContact; // Triển khai interface (nếu có)

        [Header("Hành vi Ngòi nổ (Fuse Behavior)")]
        [Tooltip("Bật để thay đổi màu sắc của bom và giữ nguyên màu đó khi ngòi nổ được kích hoạt.")]
        public bool changeColorOnFuse = false;
        [ColorUsage(true, true)]
        [Tooltip("Màu sắc mà bom sẽ chuyển sang khi ngòi nổ bắt đầu. Chỉ có tác dụng khi 'Change Color On Fuse' được bật.")]
        public Color fuseColor = Color.red;

        [Tooltip("Hiệu ứng particle sẽ được bật và đi theo bom trong suốt quá trình cháy. Sẽ bị hủy khi bom nổ.")]
        public GameObject persistentFuseVFX;

        [Tooltip("Các giai đoạn hình ảnh/âm thanh trong quá trình hẹn giờ.")]
        public List<FuseStage> fuseStages = new();
        public IReadOnlyList<FuseStage> FuseStages => fuseStages.AsReadOnly(); // Triển khai interface (nếu có)

        [Header("Vật lý (Fuse Bomb)")]
        [Tooltip("Lực hấp dẫn bổ sung tác dụng lên bom để nó rơi nhanh hơn. Đặt là 0 để dùng trọng lực mặc định của Unity.")]
        public float additionalGravity = 0f; // Giữ lại public field cho Inspector
        public float AdditionalGravity => additionalGravity; // Triển khai interface (nếu có)

        [Header("Âm thanh Hẹn giờ")]
        [Tooltip("Âm thanh khi bom đang chạy (ticking). Có thể để trống.")]
        [Range(0.1f, 3f)] public float tickingSoundPitch = 1.0f;
        public float TickingSoundPitch => tickingSoundPitch; // Triển khai interface (nếu có)

        public AudioClip tickingSound; // Giữ lại public field cho Inspector
        public AudioClip TickingSound => tickingSound; // Triển khai interface (nếu có)
    }
}