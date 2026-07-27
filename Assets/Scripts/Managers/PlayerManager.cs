using UnityEngine;
using Core.Interfaces;
using Core;
using System.Collections;
using System.Collections.Generic;

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

        // Danh sách những người chơi đang hoạt động trong màn.
        private readonly List<IPlayer> _activePlayers = new();
        
        // Danh sách những người chơi đang tham gia round đấu hiện tại.
        private readonly List<IPlayer> _playersInRound = new();
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

        /// <inheritdoc/>
        public List<IPlayer> GetAllPlayers()
        {
            // Trả về một bản sao của danh sách để tránh sửa đổi từ bên ngoài
            return new List<IPlayer>(_activePlayers);
        }

        public int GetAlivePlayerCount() => _playersInRound.Count;

        public void StartRound()
        {
            _playersInRound.Clear();
            foreach (var player in _activePlayers)
            {
                if (player != null && player.GameObject != null)
                {
                    player.GameObject.SetActive(true); // Đảm bảo người chơi được kích hoạt
                    _playersInRound.Add(player);
                }
            }
            Debug.Log($"[PlayerManager] Started round with {_playersInRound.Count} players.");
        }

        /// <inheritdoc/>
        public List<IPlayer> GetPlayersNotInCurrentRound()
        {
            List<IPlayer> notInRound = new List<IPlayer>();
            foreach (var player in _activePlayers)
            {
                // Nếu người chơi đang hoạt động nhưng chưa có trong danh sách _playersInRound
                if (player != null && player.GameObject != null && !_playersInRound.Contains(player))
                {
                    notInRound.Add(player);
                }
            }
            return notInRound;
        }

        /// <inheritdoc/>
        public void AddPlayerToCurrentRound(IPlayer player)
        {
            if (player != null && player.GameObject != null && !_playersInRound.Contains(player))
            {
                player.GameObject.SetActive(true); // Đảm bảo người chơi được kích hoạt
                _playersInRound.Add(player);
            }
        }
        public void EndRound()
        {
            _playersInRound.Clear();
            // Kích hoạt lại tất cả người chơi (để họ xuất hiện ở lobby)
            foreach (var player in _activePlayers)
            {
                if (player != null && player.GameObject != null && !player.GameObject.activeSelf)
                {
                    player.GameObject.SetActive(true);
                }
            }
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

            IPlayer newPlayer = HandlePlayerSpawn(_playerPrefab, spawnPoint);
            if (newPlayer != null)
            {
                _activePlayers.Add(newPlayer);
                Debug.Log($"[PlayerManager] Player spawned and added to active list. Total players: {_activePlayers.Count}", newPlayer.GameObject);
            }
        }

        /// <summary>
        /// Được gọi khi sự kiện GameEvents.OnPlayerDied được kích hoạt.
        /// Hủy đối tượng người chơi cũ và bắt đầu coroutine hồi sinh.
        /// </summary>
        private void HandlePlayerDeath(IPlayer player)
        {
            if (player == null) return;

            // Nếu người chơi đang trong round, loại họ ra khỏi danh sách người chơi còn sống của round đó.
            if (_playersInRound.Contains(player))
            {
                Debug.Log($"[PlayerManager] Player {player.GameObject.name} eliminated from the round.", player.GameObject);
                _playersInRound.Remove(player);
                // KHÔNG vô hiệu hóa GameObject ngay lập tức để hiệu ứng ragdoll có thể diễn ra.
            }

            // Bất kể chết trong round hay ở lobby, bắt đầu cùng một quy trình hồi sinh.
            // Quy trình này sẽ cho phép ragdoll hiển thị, sau đó phá hủy và tạo lại người chơi.
            Debug.Log($"[PlayerManager] Player {player.GameObject.name} died. Starting universal respawn process...", player.GameObject);
            StartCoroutine(UnifiedRespawnCoroutine(player, 3f)); // 3 giây là thời gian chờ hồi sinh
        }

        /// <summary>
        /// Coroutine xử lý việc hồi sinh người chơi: phá hủy người chơi cũ, đợi, và tạo người chơi mới.
        /// </summary>
        private IEnumerator UnifiedRespawnCoroutine(IPlayer playerToDestroy, float respawnDelay)
        {
            // Xóa người chơi cũ khỏi danh sách quản lý chính.
            if (playerToDestroy != null)
            {
                _activePlayers.Remove(playerToDestroy);
            }

            // Đợi một khoảng thời gian để hiệu ứng "vỡ tung" (shatter) có thời gian diễn ra.
            yield return new WaitForSeconds(respawnDelay);

            // Sau khi đợi, phá hủy đối tượng người chơi cũ.
            if (playerToDestroy != null && playerToDestroy.GameObject != null)
            {
                Destroy(playerToDestroy.GameObject);
            }

            // Hồi sinh một người chơi hoàn toàn mới ngay lập tức tại một điểm spawn ở lobby.
            Debug.Log("[PlayerManager] Respawning new player in lobby.");
            SpawnPlayer(); // SpawnPlayer sẽ tự tìm điểm spawn ngẫu nhiên.
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
            return _activePlayers.Count > 0 ? _activePlayers[0] : null;
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
            player.Teleport(spawnPoint.SpawnPoint);
        }

        /// <summary>
        /// Dịch chuyển những người chơi còn sống trong round về sảnh chờ.
        /// </summary>
        public void ReturnRoundSurvivorsToLobby()
        {
            foreach (var player in _playersInRound)
            {
                if (player != null && player.GameObject != null)
                {
                    // Người chơi còn sống thì đã active, chỉ cần dịch chuyển họ.
                    RespawnPlayer(player); // Respawn sẽ tìm một điểm spawn ngẫu nhiên ở sảnh và dịch chuyển.
                }
            }
        }

        /// <summary>
        /// Xóa tất cả các đối tượng người chơi đang hoạt động.
        /// </summary>
        public void ClearAllPlayers()
        {
            // Tạo một bản sao của danh sách để lặp qua, vì việc hủy đối tượng có thể kích hoạt OnDisable và sửa đổi danh sách gốc.
            var playersToClear = new List<IPlayer>(_activePlayers);
            foreach (var player in playersToClear) Destroy(player.GameObject);
            _activePlayers.Clear();
        }
        #endregion
    }
}