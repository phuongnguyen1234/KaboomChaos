using UnityEngine;
using DG.Tweening;
using System.Collections.Generic;
using System;

namespace Core
{
    /// <summary>
    /// Một stage trong timeline animation của hiệu ứng.
    /// </summary>
    [Serializable]
    public class EffectAnimationStage
    {
        public enum StageType
        {
            [Tooltip("Thay đổi kích thước (scale) của hiệu ứng.")]
            Scale,
            [Tooltip("Thay đổi độ trong suốt (alpha) của hiệu ứng. Yêu cầu có MaterialEffectController.")]
            Fade,
            [Tooltip("Tạm dừng, giữ nguyên trạng thái hiện tại trong một khoảng thời gian.")]
            Hold
        }

        [Tooltip("Loại hành động của stage này.")]
        public StageType type = StageType.Scale;

        [Tooltip("Giá trị mục tiêu. Với Scale, đây là hệ số nhân kích thước (1 = kích thước đầy đủ, 0 = biến mất). Với Fade, đây là alpha (0-1). Không dùng cho Hold.")]
        public float targetValue = 1f;

        [Tooltip("Thời gian thực hiện stage này (giây).")]
        public float duration = 0.2f;

        [Tooltip("Đường cong easing cho stage này. Trục X là thời gian (0-1), trục Y là hệ số (0-1).")]
        public AnimationCurve easeCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Tooltip("Chạy stage này CÙNG LÚC với stage trước đó? (Tương đương DOTween.Join)")]
        public bool joinWithPrevious = false;
    }

    /// <summary>
    /// Điều khiển một hiệu ứng nổ phức tạp bằng cách xây dựng một timeline từ các stage.
    /// Vật thể được gắn script này nên là một mesh đơn giản như quả cầu.
    /// </summary>
    public class ExplosionEffectController : MonoBehaviour
    {
        [Header("Animation Timeline")]
        [Tooltip("Danh sách các stage tạo nên animation. Các stage sẽ chạy tuần tự, trừ khi 'Join With Previous' được bật.")]
        [SerializeField] private List<EffectAnimationStage> _animationStages = new();

        [Header("Initial State")]
        [Tooltip("Scale của hiệu ứng ngay trước khi animation bắt đầu. Thường là một giá trị rất nhỏ.")]
        [SerializeField] private Vector3 _initialScale = new(0.01f, 0.01f, 0.01f);
        [Tooltip("Alpha của hiệu ứng ngay trước khi animation bắt đầu. Yêu cầu có MaterialEffectController.")]
        [SerializeField, Range(0f, 1f)] private float _initialAlpha = 1.0f;

        [Header("Component References")]
        [Tooltip("Renderer chính của hiệu ứng, dùng để tính toán kích thước. Nếu bỏ trống, script sẽ tự tìm trong các object con.")]
        [SerializeField] private Renderer _mainRenderer;

        // Cached components
        private MaterialEffectController _materialEffectController;
        private Sequence _activeSequence;

        private void Awake()
        {
            if (_mainRenderer == null)
            {
                _mainRenderer = GetComponentInChildren<Renderer>();
            }
            // Tìm MaterialEffectController trong chính nó hoặc trong các object con.
            // Điều này cho phép cấu trúc prefab linh hoạt hơn.
            _materialEffectController = GetComponentInChildren<MaterialEffectController>();
        }

        private void OnDisable()
        {
            _activeSequence?.Kill();
            transform.DOKill();
        }

        /// <summary>
        /// Kích hoạt animation của vụ nổ dựa trên timeline đã định nghĩa.
        /// </summary>
        /// <param name="targetWorldRadius">Bán kính cuối cùng (world space) mà hiệu ứng sẽ đạt tới khi scale multiplier là 1.</param>
        public void Trigger(float targetWorldRadius)
        {
            // Dừng bất kỳ animation nào đang chạy trên instance này.
            _activeSequence?.Kill();

            // Đảm bảo renderer được bật khi hiệu ứng được kích hoạt lại từ pool.
            if (_mainRenderer != null) {
                _mainRenderer.enabled = true;
            }

            if (_mainRenderer == null)
            {
                Debug.LogError("ExplosionEffectController không tìm thấy Renderer để tính toán kích thước!", this);
                Destroy(gameObject);
                return;
            }

            // Lấy bán kính của mesh khi scale là (1,1,1).
            Vector3 meshSize = _mainRenderer.localBounds.size;
            float baseRadius = Mathf.Max(meshSize.x, meshSize.y, meshSize.z) / 2f;

            if (baseRadius <= 0.001f)
            {
                Debug.LogError("Mesh của hiệu ứng nổ có kích thước không hợp lệ!", this);
                Destroy(gameObject);
                return;
            }

            // Tính toán localScale cần thiết để đạt được bán kính mong muốn trong world space.
            float requiredScale = targetWorldRadius / baseRadius;
            Vector3 fullTargetScale = new(requiredScale, requiredScale, requiredScale);

            // Đảm bảo vật thể bắt đầu từ trạng thái khởi tạo đã được định nghĩa.
            transform.localScale = _initialScale;
            if (_materialEffectController != null) _materialEffectController.CustomAlpha = _initialAlpha;

            _activeSequence = DOTween.Sequence();
            _activeSequence.SetUpdate(UpdateType.Late, true);
            _activeSequence.SetAutoKill(true);

            // Xây dựng sequence từ các stage đã định nghĩa
            foreach (var stage in _animationStages)
            {
                Tween tween = null;
                switch (stage.type)
                {
                    case EffectAnimationStage.StageType.Scale:
                        Vector3 stageTargetScale = fullTargetScale * stage.targetValue;
                        tween = transform.DOScale(stageTargetScale, stage.duration).SetEase(stage.easeCurve);
                        break;

                    case EffectAnimationStage.StageType.Fade:
                        if (_materialEffectController != null)
                        {
                            tween = DOTween.To(() => _materialEffectController.CustomAlpha,
                                               x => _materialEffectController.CustomAlpha = x,
                                               stage.targetValue,
                                               stage.duration)
                                         .SetEase(stage.easeCurve);
                        }
                        else
                        {
                            Debug.LogWarning("Timeline đang cố chạy stage 'Fade' nhưng không tìm thấy MaterialEffectController.", this);
                        }
                        break;

                    case EffectAnimationStage.StageType.Hold:
                        break;
                }

                if (stage.joinWithPrevious)
                {
                    if (tween != null) _activeSequence.Join(tween);
                }
                else
                {
                    if (tween != null) _activeSequence.Append(tween);
                    else if (stage.type == EffectAnimationStage.StageType.Hold)
                    {
                        _activeSequence.AppendInterval(stage.duration);
                    }
                }
            }

            // Khi animation hoàn tất, trả object về pool thay vì hủy nó.
            _activeSequence.OnComplete(() =>
            {
                if (GameEvents.IsVFXPoolListening())
                    GameEvents.TriggerVFXDespawnRequest(gameObject);
                else
                {
                    Destroy(gameObject);
                }
            });
        }
    }
}