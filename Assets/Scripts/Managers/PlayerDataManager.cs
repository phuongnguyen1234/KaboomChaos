using UnityEngine;
using Core;

namespace Managers
{
    /// <summary>
    /// Quản lý việc đọc, ghi và lưu trữ dữ liệu của người chơi bằng PlayerPrefs.
    /// Lắng nghe các yêu cầu từ GameEvents để thực hiện các hành động.
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        // Khóa để lưu dữ liệu trong PlayerPrefs.
        private const string CREDITS_KEY = "Credits";

        // Dữ liệu được lưu trong bộ nhớ để truy cập nhanh.
        private int _currentCredits;

        private void Awake()
        {
            // Đây không phải là singleton vì nó chỉ hoạt động dựa trên event,
            // nhưng chúng ta vẫn cần đảm bảo nó tồn tại giữa các scene.
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        private void OnEnable()
        {
            // Đăng ký lắng nghe các event
            GameEvents.OnSavePlayerDataRequest += SaveData;
            GameEvents.OnAddCreditsRequest += AddCredits;
            GameEvents.OnRequestCurrentCredits += GetCurrentCredits;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh memory leak
            GameEvents.OnSavePlayerDataRequest -= SaveData;
            GameEvents.OnAddCreditsRequest -= AddCredits;
            GameEvents.OnRequestCurrentCredits -= GetCurrentCredits;
        }

        /// <summary>
        /// Tải dữ liệu credits từ PlayerPrefs.
        /// </summary>
        private void LoadData()
        {
            _currentCredits = PlayerPrefs.GetInt(CREDITS_KEY, 0);
            Debug.Log($"[PlayerDataManager] Dữ liệu đã được tải. Credits: {_currentCredits}");
            // Thông báo cho các hệ thống khác về số credit đã tải.
            GameEvents.TriggerCreditsChanged(_currentCredits);
        }

        /// <summary>
        /// Lưu số credits hiện tại vào PlayerPrefs.
        /// </summary>
        private void SaveData()
        {
            PlayerPrefs.SetInt(CREDITS_KEY, _currentCredits);
            PlayerPrefs.Save(); // Bắt buộc gọi để ghi dữ liệu xuống đĩa.
            Debug.Log($"[PlayerDataManager] Dữ liệu đã được lưu. Credits: {_currentCredits}");
        }

        /// <summary>
        /// Thêm hoặc bớt credits và tự động lưu lại.
        /// </summary>
        /// <param name="amount">Số lượng cần thêm (có thể âm).</param>
        private void AddCredits(int amount)
        {
            _currentCredits += amount;
            Debug.Log($"[PlayerDataManager] Đã thêm {amount} credits. Tổng mới: {_currentCredits}");
            // Thông báo cho các hệ thống khác về sự thay đổi.
            GameEvents.TriggerCreditsChanged(_currentCredits);
            // Tự động lưu lại mỗi khi có thay đổi.
            SaveData();
        }

        /// <summary>
        /// Trả về số credits hiện tại khi có yêu cầu.
        /// </summary>
        /// <returns>Số credits hiện tại.</returns>
        private int GetCurrentCredits()
        {
            return _currentCredits;
        }
    }
}
