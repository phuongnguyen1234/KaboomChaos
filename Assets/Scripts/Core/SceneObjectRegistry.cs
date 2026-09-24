using System.Collections.Generic;
using UnityEngine;

namespace Core
{
    /// <summary>
    /// Giữ các tham chiếu đến các đối tượng quan trọng trong Scene,
    /// cho phép các Manager được tạo từ Prefab có thể truy cập chúng một cách an toàn.
    /// Chỉ nên có một đối tượng này trong scene khởi động của bạn.
    /// </summary>
    public class SceneObjectRegistry : MonoBehaviour
    {
        public static SceneObjectRegistry Instance { get; private set; }

        [Header("Gameplay Areas")]
        public BoxCollider ArenaSpawnArea;
        public Transform TopBorder;
        public BoxCollider BombSpawnArea;

        [Header("Spawning")]
        public List<PlayerSpawn> PlayerSpawnPoints;

        [Header("Map Containers")]
        public GameObject MapContainer;
        public GameObject LavaContainer;
        public Transform UndergroundContainer;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Một SceneObjectRegistry khác đã tồn tại. Hủy bỏ instance này.", this);
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }
    }
}