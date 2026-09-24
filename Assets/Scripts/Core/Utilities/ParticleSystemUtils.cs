using UnityEngine;

namespace Core.Utilities
{
    /// <summary>
    /// Lớp tiện ích để thao tác với Particle Systems.
    /// </summary>
    public static class ParticleSystemUtils
    {
        /// <summary>
        /// Chuẩn bị các thiết lập chung cho Particle System trước khi cấu hình Shape.
        /// </summary>
        /// <returns>True nếu chuẩn bị thành công, False nếu có lỗi.</returns>
        private static bool Prepare(ParticleSystem ps, GameObject shapeSource, out ParticleSystem.ShapeModule shape)
        {
            shape = default;
            if (ps == null)
            {
                Debug.LogWarning("ParticleSystem không hợp lệ.", shapeSource);
                return false;
            }
            if (shapeSource == null)
            {
                Debug.LogWarning("GameObject nguồn hình dạng không hợp lệ.", ps.gameObject);
                return false;
            }

            var main = ps.main;
            shape = ps.shape;
            shape.enabled = true;

            // THAY ĐỔI: Chuyển sang Shape mode. Ở chế độ này, chỉ có hình dạng (shape) của vùng phát hạt
            // được scale theo transform, còn kích thước và tốc độ của các hạt thì không.
            // Điều này cho phép mở rộng vùng khí độc mà không làm cho các hạt khí bị phồng to một cách bất thường.
            // Chế độ Hierarchy sẽ scale tất cả mọi thứ, bao gồm cả kích thước hạt.
            main.scalingMode = ParticleSystemScalingMode.Shape;

            ps.transform.localPosition = Vector3.zero;
            ps.transform.localScale = Vector3.one;
            
            return true;
        }

        /// <summary>
        /// Cấu hình Particle System để phát hạt từ BỀ MẶT (surface) của một GameObject.
        /// Ưu tiên sử dụng các collider nguyên thủy (Box, Sphere) vì hiệu năng tốt hơn, sau đó fallback về Mesh.
        /// Lý tưởng cho các hiệu ứng như lửa, điện bám trên vật thể.
        /// </summary>
        public static void MatchShapeToSurface(ParticleSystem ps, GameObject shapeSource)
        {
            if (!Prepare(ps, shapeSource, out var shape)) return;

            // TỐI ƯU HÓA: Ưu tiên các collider nguyên thủy (Box, Sphere) trước tiên vì chúng có hiệu năng cao hơn rất nhiều
            // so với việc phát hạt từ Mesh, trong khi vẫn cho kết quả hình ảnh tương tự trên các khối đơn giản.
            // 1. Thử tìm các Collider nguyên thủy trước.
            if (shapeSource.TryGetComponent<Collider>(out var collider))
            {
                if (collider is BoxCollider boxCollider)
                {
                    shape.shapeType = ParticleSystemShapeType.BoxShell;
                    shape.scale = boxCollider.size;
                    shape.position = boxCollider.center;
                    return;
                }
                if (collider is SphereCollider sphereCollider)
                {
                    shape.shapeType = ParticleSystemShapeType.Sphere;
                    shape.radiusThickness = 1f; // Phát từ bề mặt
                    // Với Hierarchy mode, chỉ cần dùng local radius.
                    shape.radius = sphereCollider.radius;
                    shape.position = sphereCollider.center;
                    return;
                }
                // NEW: Xử lý MeshCollider ngay sau các collider nguyên thủy.
                // Điều này đảm bảo các khối có MeshCollider (như khối hình nêm) sẽ được sử dụng.
                if (collider is MeshCollider meshCollider && meshCollider.sharedMesh != null)
                {
                    shape.shapeType = ParticleSystemShapeType.Mesh;
                    shape.mesh = meshCollider.sharedMesh;
                    shape.meshShapeType = ParticleSystemMeshShapeType.Triangle; // Phát từ bề mặt
                    shape.scale = Vector3.one;
                    return;
                }
            }

            // 2. Nếu không có collider nguyên thủy phù hợp, fallback về MeshFilter.
            // Đây là trường hợp cho các vật thể có hình dạng phức tạp không thể biểu diễn bằng Box/Sphere.
            MeshFilter mf = shapeSource.GetComponent<MeshFilter>();
            if (mf != null && mf.sharedMesh != null)
            {
                shape.shapeType = ParticleSystemShapeType.Mesh;
                shape.mesh = mf.sharedMesh;
                shape.meshShapeType = ParticleSystemMeshShapeType.Triangle;
                // Với Hierarchy mode, scale của mesh được điều khiển bởi transform, nên ta đặt shape scale là 1.
                shape.scale = Vector3.one;
                return;
            }
            // 4. Fallback cuối cùng: Nếu không tìm thấy gì, dùng hình cầu mặc định.
            Debug.LogWarning($"Không tìm thấy MeshFilter hoặc Collider phù hợp trên '{shapeSource.name}' để cấu hình hình dạng bề mặt. Mặc định là Sphere Surface.", shapeSource);
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            shape.radiusThickness = 1f; // Phát từ bề mặt
        }

        /// <summary>
        /// Cấu hình Particle System để phát hạt từ THỂ TÍCH (volume) của một BoxCollider.
        /// Đảm bảo kích thước vùng phát hạt khớp chính xác với kích thước của BoxCollider trong không gian thế giới.
        /// Lý tưởng cho hiệu ứng như đám mây khí độc.
        /// </summary>
        public static void MatchShapeToBoxVolume(ParticleSystem ps, GameObject shapeSource)
        {
            if (!Prepare(ps, shapeSource, out var shape)) return;

            if (shapeSource.TryGetComponent<BoxCollider>(out var boxCollider))
            {
                shape.shapeType = ParticleSystemShapeType.Box;
                shape.boxThickness = Vector3.zero; // 0 = phát từ thể tích
                // Với Hierarchy mode, chỉ cần dùng local size của collider.
                shape.scale = boxCollider.size;
                shape.position = boxCollider.center;
                return;
            }

            // Fallback: Nếu không tìm thấy BoxCollider, dùng hình cầu mặc định.
            Debug.LogWarning($"Không tìm thấy BoxCollider trên '{shapeSource.name}' để cấu hình hình dạng thể tích. Mặc định là Sphere Volume.", shapeSource);
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;
            shape.radiusThickness = 0f; // Phát từ thể tích
        }
    }
}