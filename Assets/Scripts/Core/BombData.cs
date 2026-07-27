using UnityEngine;
using System.Collections.Generic;
using System;

namespace Core
{
    /// <summary>
    /// Defines a visual/audio stage during the bomb's fuse time.
    /// </summary>
    [Serializable]
    public class FuseStage
    {
        [Tooltip("Thời gian (tính bằng giây) kể từ khi kích hoạt mà stage này sẽ bắt đầu.")]
        public float startTime;

        [Header("External Effects")]
        [Tooltip("Hiệu ứng hình ảnh (particle) sẽ được tạo ra tại vị trí của bom.")]
        public GameObject visualEffect;
        [Tooltip("Âm thanh sẽ được phát.")]
        public AudioClip stageSound;

        [Header("External VFX Animation (if VFX has ExplosionEffectController)")]
        [Tooltip("Bán kính mục tiêu của hiệu ứng hình ảnh.")]
        public float vfxRadius = 1.0f;

        [Header("Internal Effects (Base Color Pulse)")]
        [Tooltip("Bật để kích hoạt hiệu ứng nháy màu nền. Yêu cầu shader hỗ trợ '_BaseColor'.")]
        public bool enableBaseColorPulse = false;
        [ColorUsage(true, true)]
        [Tooltip("Màu nền sẽ được nháy tới.")]
        public Color basePulseColor = Color.red;
        [Tooltip("Thời gian nháy màu nền (giây).")]
        public float basePulseDuration = 0.2f;

        [Header("Internal Effects (Decal Color Pulse)")]
        [Tooltip("Bật để kích hoạt hiệu ứng nháy màu họa tiết. Yêu cầu shader hỗ trợ '_DecalColor'.")]
        public bool enableDecalColorPulse = false;
        [ColorUsage(true, true)]
        [Tooltip("Màu họa tiết sẽ được nháy tới.")]
        public Color decalPulseColor = Color.yellow;
        [Tooltip("Thời gian nháy màu họa tiết (giây).")]
        public float decalPulseDuration = 0.2f;
        [Header("Internal Effects (Scale Pulse)")]
        [Tooltip("Bật để kích hoạt hiệu ứng nháy scale trên các bộ phận được chỉ định trong BombController (Parts To Pulse).")]
        public bool enableScalePulse = false;
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
        public float fuseTime = 3.0f;
        [Tooltip("Bom có tự kích hoạt khi va chạm với đối tượng khác không?")]
        public bool isActivatedOnContact = false;
        [Tooltip("Các giai đoạn hình ảnh/âm thanh trong quá trình hẹn giờ.")]
        public List<FuseStage> fuseStages = new();

        [Header("Vật lý (Fuse Bomb)")]
        [Tooltip("Lực hấp dẫn bổ sung tác dụng lên bom để nó rơi nhanh hơn. Đặt là 0 để dùng trọng lực mặc định của Unity.")]
        public float additionalGravity = 0f;

        [Header("Âm thanh Hẹn giờ")]
        [Tooltip("Âm thanh khi bom đang chạy (ticking). Có thể để trống.")]
        public AudioClip tickingSound;
    }
}