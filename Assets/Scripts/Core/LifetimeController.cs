using UnityEngine;
using System.Collections;
using Core;

namespace Core
{
    /// <summary>
    /// Quản lý vòng đời của một GameObject, khiến nó tự hủy (trả về pool) sau một khoảng thời gian.
    /// Có thể được sử dụng cho các hiệu ứng tạm thời như khí độc, khói, hoặc các mảnh vỡ tạm thời.
    /// </summary>
    public class LifetimeController : MonoBehaviour
    {
        [Header("Lifetime Settings")]
        [Tooltip("Thời gian (giây) đối tượng này sẽ tồn tại trước khi tự hủy.")]
        [SerializeField] private float _lifetime = 10f;

        private Coroutine _lifetimeCoroutine;

        private void OnEnable()
        {
            // Khi đối tượng được kích hoạt (ví dụ: khi được lấy ra từ pool), bắt đầu đếm ngược vòng đời.
            // Dừng coroutine cũ (nếu có) để đảm bảo không có bộ đếm nào chạy chồng chéo từ "kiếp trước".
            if (_lifetimeCoroutine != null)
            {
                StopCoroutine(_lifetimeCoroutine);
            }
            _lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
        }

        private IEnumerator LifetimeRoutine()
        {
            yield return new WaitForSeconds(_lifetime);
            GameEvents.TriggerVFXDespawnRequest(gameObject);
        }
    }
}