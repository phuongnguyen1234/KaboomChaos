using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Core;

namespace Managers
{
    /// <summary>
    /// Quản lý việc đọc, ghi và lưu trữ dữ liệu của người chơi bằng file JSON.
    /// Dữ liệu gồm credits, danh sách skill và perk mà người chơi sở hữu (dùng chung cho Shop/Inventory).
    /// File được lưu trong Application.persistentDataPath.
    /// Lắng nghe các yêu cầu từ GameEvents để thực hiện các hành động.
    /// </summary>
    public class PlayerDataManager : MonoBehaviour
    {
        // Tên file lưu dữ liệu JSON.
        private const string SAVE_FILE_NAME = "player_save.json";

        // Dữ liệu được lưu trong bộ nhớ để truy cập nhanh, tránh đọc file mỗi lần.
        private int _currentCredits;
        private readonly List<string> _ownedSkillIds = new();
        private readonly List<string> _ownedPerkIds = new();
        private int _skillSpinCount;

        // ID skill dang duoc trang bi (null/rong = chua trang bi).
        private string _equippedSkillId;
        // ID perk dang duoc trang bi (null/rong = chua trang bi).
        private string _equippedPerkId;

        /// <summary>
        /// Đường dẫn đầy đủ tới file lưu dữ liệu.
        /// </summary>
        private static string SaveFilePath => Path.Combine(Application.persistentDataPath, SAVE_FILE_NAME);

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
            GameEvents.OnAddOwnedSkill += AddOwnedSkill;
            GameEvents.OnRequestOwnedSkillIds += GetOwnedSkillIds;
            GameEvents.OnAddOwnedPerk += AddOwnedPerk;
            GameEvents.OnRequestOwnedPerkIds += GetOwnedPerkIds;
            GameEvents.OnRequestSkillSpinCount += GetSkillSpinCount;
            GameEvents.OnIncreaseSkillSpinCount += IncreaseSkillSpinCount;
            GameEvents.OnEquippedSkillIdChanged += SetEquippedSkillId;
            GameEvents.OnRequestEquippedSkillId += GetEquippedSkillId;
            GameEvents.OnEquippedPerkIdChanged += SetEquippedPerkId;
            GameEvents.OnRequestEquippedPerkId += GetEquippedPerkId;
        }

        private void OnDisable()
        {
            // Hủy đăng ký để tránh memory leak
            GameEvents.OnSavePlayerDataRequest -= SaveData;
            GameEvents.OnAddCreditsRequest -= AddCredits;
            GameEvents.OnRequestCurrentCredits -= GetCurrentCredits;
            GameEvents.OnAddOwnedSkill -= AddOwnedSkill;
            GameEvents.OnRequestOwnedSkillIds -= GetOwnedSkillIds;
            GameEvents.OnAddOwnedPerk -= AddOwnedPerk;
            GameEvents.OnRequestOwnedPerkIds -= GetOwnedPerkIds;
            GameEvents.OnRequestSkillSpinCount -= GetSkillSpinCount;
            GameEvents.OnIncreaseSkillSpinCount -= IncreaseSkillSpinCount;
            GameEvents.OnEquippedSkillIdChanged -= SetEquippedSkillId;
            GameEvents.OnRequestEquippedSkillId -= GetEquippedSkillId;
            GameEvents.OnEquippedPerkIdChanged -= SetEquippedPerkId;
            GameEvents.OnRequestEquippedPerkId -= GetEquippedPerkId;
        }

        /// <summary>
        /// Tải dữ liệu từ file JSON nếu tồn tại, ngược lại khởi tạo dữ liệu mặc định (rỗng).
        /// Sau khi tải sẽ thông báo cho các hệ thống khác.
        /// </summary>
        private void LoadData()
        {
            PlayerSaveData data = null;

            try
            {
                if (File.Exists(SaveFilePath))
                {
                    string json = File.ReadAllText(SaveFilePath);
                    data = JsonUtility.FromJson<PlayerSaveData>(json);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerDataManager] Lỗi khi đọc file dữ liệu: {ex.Message}");
            }

            // Fallback dữ liệu mặc định nếu file không tồn tại hoặc không đọc được.
            data ??= new PlayerSaveData();

            _currentCredits = data.Credits;
            _skillSpinCount = data.SkillSpinCount;
            CopyIds(data.OwnedSkillIds, _ownedSkillIds);
            CopyIds(data.OwnedPerkIds, _ownedPerkIds);
            _equippedSkillId = data.EquippedSkillId;
            _equippedPerkId = data.EquippedPerkId;

            Debug.Log($"[PlayerDataManager] Du lieu da duoc tai. Credits: {_currentCredits}, Skills: {_ownedSkillIds.Count}, Perks: {_ownedPerkIds.Count}, Spins: {_skillSpinCount}, EquippedSkill: {_equippedSkillId}, EquippedPerk: {_equippedPerkId}");

            // Thông báo cho các hệ thống khác về dữ liệu đã tải.
            GameEvents.TriggerCreditsChanged(_currentCredits);
            GameEvents.TriggerOwnedSkillsChanged(_ownedSkillIds);
            GameEvents.TriggerOwnedPerksChanged(_ownedPerkIds);
        }

        /// <summary>
        /// Ghi toàn bộ dữ liệu hiện tại xuống file JSON.
        /// Được gọi thủ công qua event hoặc tự động sau mỗi thay đổi.
        /// </summary>
        private void SaveData()
        {
            try
            {
                PlayerSaveData data = new()
                {
                    Credits = _currentCredits,
                    OwnedSkillIds = _ownedSkillIds,
                    OwnedPerkIds = _ownedPerkIds,
                    SkillSpinCount = _skillSpinCount,
                    EquippedSkillId = _equippedSkillId,
                    EquippedPerkId = _equippedPerkId
                };

                // PrettyPrint để dễ kiểm tra/debug file lưu.
                string json = JsonUtility.ToJson(data, prettyPrint: true);

                // Đảm bảo thư mục tồn tại trước khi ghi.
                string directory = Path.GetDirectoryName(SaveFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(SaveFilePath, json);

                Debug.Log($"[PlayerDataManager] Dữ liệu đã được lưu tới: {SaveFilePath}. Credits: {_currentCredits}, Skills: {_ownedSkillIds.Count}, Perks: {_ownedPerkIds.Count}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerDataManager] Lỗi khi ghi file dữ liệu: {ex.Message}");
            }
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

        /// <summary>
        /// Thêm một skill vào danh sách sở hữu (tránh trùng lặp) và tự động lưu lại.
        /// </summary>
        /// <param name="skillId">ID của skill cần thêm.</param>
        private void AddOwnedSkill(string skillId)
        {
            if (string.IsNullOrEmpty(skillId) || _ownedSkillIds.Contains(skillId)) return;

            _ownedSkillIds.Add(skillId);
            Debug.Log($"[PlayerDataManager] Đã thêm skill sở hữu: {skillId}. Tổng: {_ownedSkillIds.Count}");
            GameEvents.TriggerOwnedSkillsChanged(_ownedSkillIds);
            SaveData();
        }

        /// <summary>
        /// Trả về danh sách skill mà người chơi đang sở hữu khi có yêu cầu.
        /// </summary>
        /// <returns>Danh sách ID skill.</returns>
        private IReadOnlyList<string> GetOwnedSkillIds()
        {
            return _ownedSkillIds;
        }

        /// <summary>
        /// Thêm một perk vào danh sách sở hữu (tránh trùng lặp) và tự động lưu lại.
        /// </summary>
        /// <param name="perkId">ID của perk cần thêm.</param>
        private void AddOwnedPerk(string perkId)
        {
            if (string.IsNullOrEmpty(perkId) || _ownedPerkIds.Contains(perkId)) return;

            _ownedPerkIds.Add(perkId);
            Debug.Log($"[PlayerDataManager] Đã thêm perk sở hữu: {perkId}. Tổng: {_ownedPerkIds.Count}");
            GameEvents.TriggerOwnedPerksChanged(_ownedPerkIds);
            SaveData();
        }

        /// <summary>
        /// Trả về danh sách perk mà người chơi đang sở hữu khi có yêu cầu.
        /// </summary>
        /// <returns>Danh sách ID perk.</returns>
        private IReadOnlyList<string> GetOwnedPerkIds()
        {
            return _ownedPerkIds;
        }

        /// <summary>
        /// Trả về tổng số lần quay thưởng skill đã thực hiện khi có yêu cầu.
        /// </summary>
        /// <returns>Số lần quay.</returns>
        private int GetSkillSpinCount()
        {
            return _skillSpinCount;
        }

        /// <summary>
        /// Tăng tổng số lần quay thưởng skill lên 1 và tự động lưu lại.
        /// Việc tăng này dùng để tăng giá của tất cả các nhóm skill.
        /// </summary>
        private void IncreaseSkillSpinCount()
        {
            _skillSpinCount++;
            Debug.Log($"[PlayerDataManager] Số lần quay thưởng skill đã tăng. Tổng: {_skillSpinCount}");
            SaveData();
        }

        /// <summary>
        /// Cap nhat ID skill dang duoc trang bi va tu dong luu lai.
        /// </summary>
        /// <param name="skillId">ID skill moi dang trang bi, hoac null/rong neu go trang bi.</param>
        private void SetEquippedSkillId(string skillId)
        {
            if (_equippedSkillId == skillId) return;

            _equippedSkillId = skillId;
            Debug.Log($"[PlayerDataManager] Skill trang bi da thay doi: {(string.IsNullOrEmpty(skillId) ? "None" : skillId)}");
            SaveData();
        }

        /// <summary>
        /// Tra ve ID skill dang duoc trang bi (da luu).
        /// </summary>
        private string GetEquippedSkillId()
        {
            return _equippedSkillId;
        }

        /// <summary>
        /// Cap nhat ID perk dang duoc trang bi va tu dong luu lai.
        /// </summary>
        /// <param name="perkId">ID perk moi dang duoc trang bi, hoac null/rong neu go trang bi.</param>
        private void SetEquippedPerkId(string perkId)
        {
            _equippedPerkId = perkId;
            Debug.Log($"[PlayerDataManager] Perk dang trang bi da thay doi: '{_equippedPerkId}'");
            SaveData();
        }

        /// <summary>
        /// Tra ve ID perk dang duoc trang bi (da luu).
        /// </summary>
        private string GetEquippedPerkId()
        {
            return _equippedPerkId;
        }

        /// <summary>
        /// Sao chép danh sách ID từ dữ liệu đọc được sang danh sách trong bộ nhớ (tránh trùng lặp).
        /// </summary>
        /// <param name="source">Danh sách nguồn.</param>
        /// <param name="target">Danh sách đích để điền vào.</param>
        private void CopyIds(List<string> source, List<string> target)
        {
            target.Clear();
            if (source == null) return;

            foreach (string id in source)
            {
                if (!string.IsNullOrEmpty(id) && !target.Contains(id))
                {
                    target.Add(id);
                }
            }
        }
    }
}
