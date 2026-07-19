using UnityEngine;
using Core.Interfaces;
using Core;
using System.Collections;

namespace Managers
{
    /// <summary>
    /// Quản lý vòng đời của người chơi, bao gồm việc sinh (spawn), hồi sinh (respawn),
    /// và cung cấp quyền truy cập vào đối tượng người chơi hiện tại.
    /// </summary>
    public class PlayerManager : MonoBehaviour, IPlayerManager
    {
        #region Singleton
        /// <summary>
        /// Thể hiện Singleton của PlayerManager, cho phép truy cập toàn cục.
        /// </summary>
        public static IPlayerManager Instance { get; private set; }
        #endregion

        #region Fields
        [Header("Player Settings")]
        [Tooltip("Prefab của người chơi sẽ được sinh ra trong game.")]
        [SerializeField] private GameObject _playerPrefab;

        // Phụ thuộc vào các manager khác
        private ISpawnManager _spawnManager;

        // Tham chiếu đến đối tượng người chơi hiện tại đã được sinh ra.
        private IPlayer _currentPlayer;
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
                DontDestroyOnLoad(gameObject);

                // Lấy tham chiếu đến các manager khác.
                // Giả định rằng các manager này cũng là Singleton và đã được khởi tạo trong Awake của chúng.
                _spawnManager = SpawnManager.Instance;
            }
        }

        private void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện người chơi chết từ GameEvents
            GameEvents.OnPlayerDied += HandlePlayerDeath;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh rò rỉ bộ nhớ khi đối tượng bị hủy
            GameEvents.OnPlayerDied -= HandlePlayerDeath;
        }
        #endregion

        #region Public Methods (IPlayerManager Implementation)

        /// <summary>
        /// Bắt đầu quá trình sinh người chơi lần đầu tiên khi game bắt đầu.
        /// </summary>
        public void SpawnInitialPlayer()
        {
            SpawnPlayer();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Tìm một điểm spawn ngẫu nhiên và sinh ra người chơi tại đó.
        /// </summary>
        private void SpawnPlayer()
        {
            if (_spawnManager == null)
            {
                Debug.LogError("SpawnManager instance not found! Cannot spawn player. Make sure a SpawnManager exists in the scene.", this);
                return;
            }

            PlayerSpawn spawnPoint = _spawnManager.GetRandomSpawnPoint();
            if (spawnPoint == null)
            {
                Debug.LogError("No PlayerSpawn found in the scene! Cannot spawn player.", this);
                return;
            }

            _currentPlayer = HandlePlayerSpawn(_playerPrefab, spawnPoint);
        }

        /// <summary>
        /// Được gọi khi sự kiện GameEvents.OnPlayerDied được kích hoạt.
        /// Hủy đối tượng người chơi cũ và bắt đầu coroutine hồi sinh.
        /// </summary>
        private void HandlePlayerDeath()
        {
            if (_currentPlayer == null) return;

            Debug.Log("[PlayerManager] Player died. Starting respawn timer...");

            // Bắt đầu coroutine để xử lý việc xóa và hồi sinh.
            // Truyền vào đối tượng player hiện tại để xóa sau một khoảng thời gian.
            StartCoroutine(RespawnPlayerCoroutine(3f, _currentPlayer));

            // Đặt _currentPlayer thành null ngay lập tức để các hệ thống khác
            // không cố gắng tương tác với người chơi đã "chết".
            _currentPlayer = null;
        }

        /// <summary>
        /// Coroutine chờ một khoảng thời gian, sau đó phá hủy đối tượng người chơi cũ và sinh ra người chơi mới.
        /// </summary>
        /// <param name="delay">Thời gian chờ trước khi hồi sinh.</param>
        /// <param name="playerToDestroy">Đối tượng người chơi cần phá hủy.</param>
        private IEnumerator RespawnPlayerCoroutine(float delay, IPlayer playerToDestroy)
        {
            // Chờ một khoảng thời gian. Trong lúc này, các mảnh vỡ của người chơi cũ (ragdoll) vẫn còn trên scene.
            yield return new WaitForSeconds(delay);

            // 1. Sau khi chờ, xóa đối tượng GameObject của người chơi cũ.
            if (playerToDestroy != null && playerToDestroy.GameObject != null)
            {
                Debug.Log("[PlayerManager] Respawn timer finished. Destroying old player object.");
                Destroy(playerToDestroy.GameObject);
            }

            // 2. Sinh ra người chơi mới.
            Debug.Log("[PlayerManager] Spawning new player...");
            SpawnPlayer();
            yield break; // Thêm dòng này để rõ ràng hơn
        }

        /// <summary>
        /// Xử lý việc sinh ra đối tượng người chơi tại một điểm spawn được chỉ định.
        /// </summary>
        /// <param name="playerPrefab">Prefab của người chơi để khởi tạo.</param>
        /// <param name="spawnPoint">Điểm spawn nơi người chơi sẽ xuất hiện.</param>
        /// <returns>Một interface IPlayer của đối tượng người chơi vừa được tạo, hoặc null nếu thất bại.</returns>
        public IPlayer HandlePlayerSpawn(GameObject playerPrefab, PlayerSpawn spawnPoint)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("Player Prefab is not assigned in PlayerManager! Cannot spawn player.", this);
                return null;
            }
            if (spawnPoint == null)
            {
                Debug.LogError("Invalid PlayerSpawn point provided! Cannot spawn player.", this);
                return null;
            }

            Vector3 finalSpawnPosition = spawnPoint.SpawnPoint;

            // Cố gắng lấy CapsuleCollider từ prefab để tính toán vị trí spawn chính xác.
            // Điều này sẽ đặt phần đáy của collider vật lý của người chơi ngay tại điểm spawn.
            if (playerPrefab.TryGetComponent<CapsuleCollider>(out var capsule))
            {
                // Tính toán độ dời theo chiều dọc để đảm bảo chân của CapsuleCollider (điểm thấp nhất)
                // được đặt chính xác trên SpawnPoint, thay vì pivot của GameObject.
                // Công thức: pivot.y = ground.y + (nửa chiều cao - vị trí tâm của collider theo trục y).
                float verticalOffset = (capsule.height / 2f) - capsule.center.y;
                // Áp dụng độ dời vào vị trí spawn cuối cùng.
                finalSpawnPosition += new Vector3(0, verticalOffset, 0);
            }

            // Sinh người chơi tại vị trí SpawnPoint đã xác định
            GameObject spawnedPlayerObject = Instantiate(playerPrefab, finalSpawnPosition, Quaternion.identity);
            Debug.Log($"[PlayerManager] Player spawned at: {finalSpawnPosition} (Base ground: {spawnPoint.SpawnPoint})", spawnedPlayerObject);

            // Kiểm tra xem prefab có triển khai interface IPlayer hay không.
            if (!spawnedPlayerObject.TryGetComponent<IPlayer>(out var playerInterface))
            {
                Debug.LogError($"Player Prefab '{playerPrefab.name}' does not have a component that implements IPlayer! Destroying spawned object.", spawnedPlayerObject);
                Destroy(spawnedPlayerObject);
                return null;
            }

            return playerInterface;
        }

        /// <summary>
        /// Lấy tham chiếu đến người chơi hiện tại đang được quản lý.
        /// </summary>
        /// <returns>Interface IPlayer của người chơi hiện tại, hoặc null nếu chưa có.</returns>
        public IPlayer GetCurrentPlayer()
        {
            return _currentPlayer;
        }

        /// <summary>
        /// Hồi sinh một người chơi tại một điểm spawn được chỉ định.
        /// </summary>
        /// <param name="player">Người chơi cần hồi sinh.</param>
        /// <param name="spawnPoint">Điểm spawn để hồi sinh người chơi. Nếu null, sẽ tìm một điểm ngẫu nhiên.</param>
        public void RespawnPlayer(IPlayer player, PlayerSpawn spawnPoint = null)
        {
            if (player == null)
            {
                Debug.LogError("Cannot respawn null player.", this);
                return;
            }
            if (spawnPoint == null)
            {
                // Nếu không có điểm spawn cụ thể, hãy lấy một điểm ngẫu nhiên.
                spawnPoint = _spawnManager?.GetRandomSpawnPoint();
                if (spawnPoint == null)
                {
                    Debug.LogError("Cannot respawn player, no valid spawn point found.", this);
                    return;
                }
            }

            // Di chuyển người chơi đến vị trí của điểm spawn.
            player.GameObject.transform.position = spawnPoint.SpawnPoint;
        }
        #endregion
    }
}