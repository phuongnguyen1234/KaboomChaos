using UnityEngine;
using Core;
using System.Collections;
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Phần mở rộng (partial) của lớp GameplayManager, chịu trách nhiệm xử lý logic liên quan đến người chơi
    /// như sinh (spawn), hồi sinh (respawn) và truy xuất thông tin người chơi.
    /// </summary>
    public partial class GameplayManager
    {
        #region Player Lifecycle Management

        /// <summary>
        /// Tìm một điểm spawn và sinh ra người chơi tại đó.
        /// </summary>
        private void SpawnPlayer()
        {
            PlayerSpawn spawnPoint = GetRandomSpawnPoint();
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

            Debug.Log("Player died. Starting respawn timer...");

            // Bắt đầu coroutine để xử lý việc xóa và hồi sinh.
            // Truyền vào đối tượng player hiện tại để xóa sau 3 giây.
            StartCoroutine(RespawnPlayerCoroutine(3f, _currentPlayer));

            // Đặt _currentPlayer thành null ngay lập tức để các hệ thống khác
            // không cố gắng tương tác với người chơi đã "chết".
            _currentPlayer = null;
        }

        private IEnumerator RespawnPlayerCoroutine(float delay, IPlayer playerToDestroy)
        {
            // Chờ một khoảng thời gian. Trong lúc này, các mảnh vỡ của người chơi cũ vẫn còn trên scene.
            yield return new WaitForSeconds(delay);

            // 1. Sau khi chờ, xóa đối tượng người chơi cũ.
            if (playerToDestroy != null && playerToDestroy.gameObject != null)
            {
                Debug.Log("Respawn timer finished. Destroying old player object.");
                Destroy(playerToDestroy.gameObject);
            }

            // 2. Sinh ra người chơi mới.
            Debug.Log("Spawning new player...");
            SpawnPlayer();
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
                Debug.LogError("Player Prefab is not assigned in GameplayManager! Cannot spawn player.", this);
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
            Debug.Log($"Player spawned at: {finalSpawnPosition} (Base ground: {spawnPoint.SpawnPoint})", spawnedPlayerObject);

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
                spawnPoint = GetRandomSpawnPoint();
                if (spawnPoint == null)
                {
                    Debug.LogError("Cannot respawn player, no valid spawn point found.", this);
                    return;
                }
            }

            // Di chuyển người chơi đến vị trí của điểm spawn.
            player.gameObject.transform.position = spawnPoint.SpawnPoint;
        }

        #endregion
    }
}