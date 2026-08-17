using UnityEngine;

namespace Bombs.Behaviors
{
    /// <summary>
    /// Interface cho Strategy Pattern, định nghĩa các hành vi độc nhất của một loại bom.
    /// LƯU Ý: File này nên được di chuyển đến thư mục 'Assets/Scripts/Bombs/Behaviors/'
    /// Khi đã ở trong assembly 'Bombs', nó có thể tham chiếu trực tiếp đến 'BombController'.
    /// </summary>
    public interface IBombBehavior
    {
        /// <summary>
        /// Được gọi khi BombController được khởi tạo lần đầu hoặc được reset.
        /// Dùng để thiết lập các thuộc tính ban đầu như loại collider.
        /// </summary>
        /// <param name="controller">Bomb controller mà hành vi này được gắn vào.</param>
        void OnSetup(BombController controller);

        /// <summary>
        /// Logic để thực thi khi bom được kích hoạt.
        /// </summary>
        /// <param name="controller">Bomb controller mà hành vi này được gắn vào.</param>
        void OnActivate(BombController controller);

        /// <summary>
        /// Logic để thực thi mỗi FixedUpdate. Chủ yếu dùng cho việc di chuyển.
        /// </summary>
        /// <param name="controller">Bomb controller mà hành vi này được gắn vào.</param>
        void OnFixedUpdate(BombController controller);

        /// <summary>
        /// Logic để thực thi khi có va chạm vật lý.
        /// </summary>
        /// <param name="controller">Bomb controller mà hành vi này được gắn vào.</param>
        /// <param name="collision">Dữ liệu va chạm.</param>
        void OnCollisionEnter(BombController controller, Collision collision);

        /// <summary>
        /// Logic để thực thi khi đang tiếp xúc liên tục với một collider vật lý khác.
        /// </summary>
        /// <param name="controller">Bomb controller mà hành vi này được gắn vào.</param>
        /// <param name="collision">Dữ liệu va chạm.</param>
        void OnCollisionStay(BombController controller, Collision collision);

        /// <summary>
        /// Logic để thực thi khi đi vào một trigger.
        /// </summary>
        /// <param name="controller">Bomb controller mà hành vi này được gắn vào.</param>
        /// <param name="other">Collider khác.</param>
        void OnTriggerEnter(BombController controller, Collider other);

        /// <summary>
        /// Logic để thực thi khi bom bị tác động bởi một vụ nổ khác.
        /// </summary>
        /// <param name="controller">Bomb controller mà hành vi này được gắn vào.</param>
        /// <param name="force">Vector lực tác động.</param>
        /// <param name="point">Điểm tác động của lực.</param>
        /// <param name="triggeringBombData">Dữ liệu của quả bom gây ra vụ nổ.</param>
        void OnExplosionHit(BombController controller, Vector3 force, Vector3 point, Core.Interfaces.IBaseBombData triggeringBombData);
    }
}
