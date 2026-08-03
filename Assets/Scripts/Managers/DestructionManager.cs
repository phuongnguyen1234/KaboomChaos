using UnityEngine;
using Core.Interfaces;
using Destruction;
using Core;

namespace Managers
{
    /// <summary>
    /// Quản lý hệ thống phá hủy kiến trúc dựa trên đồ thị.
    /// </summary>
    public class DestructionManager : MonoBehaviour, IDestructionManager
    {
        #region Singleton
        public static IDestructionManager Instance { get; private set; }
        #endregion

        #region Fields
        [Tooltip("Layer chứa các mảnh vỡ có thể phá hủy.")]
        [SerializeField] private LayerMask _destructibleLayer;

        // Reusable array for non-allocating physics queries to avoid garbage collection.
        private const int MAX_COLLIDERS_TO_CHECK = 512;
        private readonly Collider[] _hitColliders = new Collider[MAX_COLLIDERS_TO_CHECK];

        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
            }
        }

        #endregion

        #region Public Methods (IDestructionManager)
        /// <summary>
        /// Xử lý một vụ nổ, tìm các mảnh bị ảnh hưởng và kiểm tra kết nối.
        /// </summary>
        public void HandleExplosion(Vector3 position, float radius, float force)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(position, radius, _hitColliders, _destructibleLayer);

            // If the number of hits equals the buffer size, some objects may have been missed.
            if (hitCount == MAX_COLLIDERS_TO_CHECK)
            {
                Debug.LogWarning($"[DestructionManager] Explosion hit detection reached the limit of {MAX_COLLIDERS_TO_CHECK}. Some destructible pieces might have been missed. Consider increasing MAX_COLLIDERS_TO_CHECK.", this);
            }

            for (int i = 0; i < hitCount; i++)
            {
                var hit = _hitColliders[i];
                if (hit.TryGetComponent<DestructiblePiece>(out var piece))
                {
                    // Một mảnh chỉ có thể bị ảnh hưởng nếu nó còn nguyên vẹn hoặc đang rơi.
                    if (piece.CurrentState == PieceState.Intact || piece.CurrentState == PieceState.Falling)
                    {
                        piece.RegisterHit();
                        piece.ApplyExplosionForce(position, force, radius);

                        // Nếu mảnh vỡ đã nhận đủ hit, phá hủy nó.
                        if (piece.HitCount >= piece.MaxHits)
                            piece.gameObject.SetActive(false);
                    }
                }
            }
        }
        #endregion
    }
}