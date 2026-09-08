using Core;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface for any controllable bomb object in the game world.
    /// Provides a contract for external systems to interact with bombs
    /// without needing to know their concrete implementation.
    /// </summary>
    public interface IBombController : ISpawnableController<IBaseBombData>
    {
        /// <summary>
        /// Activates the bomb's primary function (e.g., starts the fuse, begins falling).
        /// </summary>
        void Activate();

        /// <summary>
        /// Gets the current activation state of the bomb.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Resets the bomb's internal state so it can be reused by an object pool.
        /// </summary>
        void ResetState();

        /// <summary>
        /// Khởi tạo bộ điều khiển bom với dữ liệu định nghĩa và các manager cần thiết.
        /// </summary>
        void Initialize(IBaseBombData bombData, IBombSpawnerManager bombSpawnerManager, IPlayerManager playerManager, IDestructionManager destructionManager);

        // Các thuộc tính public để các strategy truy cập manager
        IBombSpawnerManager BombSpawnerManager { get; }
        IPlayerManager PlayerManager { get; }
        IDestructionManager DestructionManager { get; }

        /// <summary>
        /// Dữ liệu chung để các strategy hành vi nhận cấu hình khởi tạo cụ thể từ nhà sinh bom.
        /// Ví dụ: Thủy lôi (Naval Mine) cần nhận một <see cref="UnityEngine.GameObject"/> làm điểm neo (anchor).
        /// </summary>
        object BehaviorData { get; set; }

        /// <summary>
        /// Cho biết loại bom này có yêu cầu phải được gắn vào một điểm neo trên cấu trúc map
        /// trước khi kích hoạt hay không (ví dụ: Thủy lôi).
        /// Spawner sẽ tìm một anchor hợp lệ và gán vào <see cref="BehaviorData"/> trước khi gọi <see cref="Activate"/>.
        /// </summary>
        bool RequiresAnchorForSpawn { get; }

        /// <summary>
        /// Cho biết bom này có phải là tên lửa (missile) hay không.
        /// Dùng bởi Bubble Barrier để giữ tư thế đâm thẳng xuống khi bị đẩy ra.

        /// </summary>
        bool IsProjectile { get; }
    }
}