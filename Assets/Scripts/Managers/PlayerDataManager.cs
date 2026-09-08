using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Core;
using Core.Utilities;

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

        // Trang thai Extreme Mode cua nguoi choi (bat/tat). Luu tru de khoi phuc khi vao game.
        private bool _extremeModeEnabled;

        // Gia tri Extreme Mode dang CHO ap dung khi dang trong round (chi valid neu _hasPendingExtremeModeEnabled = true).
        // Dung de tri hoan thay doi Extreme Mode neu nguoi choi bat/tat luc dang trong round,
        // ap dung sau khi round ket thuc (khong ap dung giua round dang dien ra).
        private bool _pendingExtremeModeEnabled;
        private bool _hasPendingExtremeModeEnabled;

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

        private void Update()
        {
            // Ap dung thay doi Extreme Mode (neu dang CHO) khi khong con trong round
            // (luc dang o lobby / giua cac round). Dieu nay dam bao ta cau
            // bat/tat Extreme Mode trong round khong ap dung giua chung.
            if (_hasPendingExtremeModeEnabled && !RoundStateHelper.IsInRound())
            {
                bool pendingValue = _pendingExtremeModeEnabled;
                _hasPendingExtremeModeEnabled = false;
                ApplyExtremeModeEnabled(pendingValue);
            }
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
            GameEvents.OnExtremeModeChanged += SetExtremeModeEnabled;
            GameEvents.OnRequestExtremeModeEnabled += GetExtremeModeEnabled;
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
            GameEvents.OnExtremeModeChanged -= SetExtremeModeEnabled;
            GameEvents.OnRequestExtremeModeEnabled -= GetExtremeModeEnabled;
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
            _extremeModeEnabled = data.ExtremeModeEnabled;

            Debug.Log($"[PlayerDataManager] Du lieu da duoc tai. Credits: {_currentCredits}, Skills: {_ownedSkillIds.Count}, Perks: {_ownedPerkIds.Count}, Spins: {_skillSpinCount}, EquippedSkill: {_equippedSkillId}, EquippedPerk: {_equippedPerkId}, ExtremeMode: {_extremeModeEnabled}");

            // Thông báo cho các hệ thống khác về dữ liệu đã tải.
            GameEvents.TriggerCreditsChanged(_currentCredits);
            GameEvents.TriggerOwnedSkillsChanged(_ownedSkillIds);
            GameEvents.TriggerOwnedPerksChanged(_ownedPerkIds);

            // Phat lai trang thai Extreme Mode da luu de cac he thong (PlayerHealth, PlayerPerkController, HUD, UIManager)
            // khoi phuc lai logic va UI tuong ung khi vao game.
            GameEvents.TriggerExtremeModeStateChanged(_extremeModeEnabled);
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
                    EquippedPerkId = _equippedPerkId,
                    ExtremeModeEnabled = _extremeModeEnabled
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
        /// Xu ly khi nguoi choi bat/tat Extreme Mode tu Option Menu:
        /// - Neu dang TRONG round: tri hoan ap dung cho toi khi round ket thuc (khong ap dung giua round).
        /// - Neu khong trong round: ap dung lap tuc (luu tru + cap nhat logic/UI).
        /// </summary>
        /// <param name="enabled">True neu bat Extreme Mode, false neu tat.</param>
        private void SetExtremeModeEnabled(bool enabled)
        {
            // Neu trung voi trang thai dang ap dung -> khong lam gi (va xoa pending neu co).
            if (_extremeModeEnabled == enabled)
            {
                _hasPendingExtremeModeEnabled = false;
                return;
            }

            // Dang trong round: luu tam, ap dung sau khi round ket thuc
            // (de max HP/perk/score khong thay doi giua round dang dien ra).
            if (RoundStateHelper.IsInRound())
            {
                _pendingExtremeModeEnabled = enabled;
                _hasPendingExtremeModeEnabled = true;
                Debug.Log($"[PlayerDataManager] Dang trong round - luu trang thai Extreme Mode = {enabled} tam, se ap dung sau khi round ket thuc.");
                return;
            }

            ApplyExtremeModeEnabled(enabled);
        }

        /// <summary>
        /// Ap dung gia tri Extreme Mode moi vao trang thai (luu tru) va phat su kien global
        /// OnExtremeModeStateChanged de cac thanh phan (PlayerHealth, PlayerPerkController, HUD, UIManager)
        /// cap nhat logic va UI theo trang thai moi.
        /// </summary>
        /// <param name="enabled">True neu bat Extreme Mode, false neu tat.</param>
        private void ApplyExtremeModeEnabled(bool enabled)
        {
            _extremeModeEnabled = enabled;
            Debug.Log($"[PlayerDataManager] Extreme Mode da thay doi: {(_extremeModeEnabled ? "BAT" : "TAT")}");

            SaveData();

            // Thong bao cho cac thanh phan khac de ap dung logic (max HP, vô hiệu perk) va UI.
            GameEvents.TriggerExtremeModeStateChanged(_extremeModeEnabled);
        }

        /// <summary>
        /// Tra ve trang thai Extreme Mode hien tai (da luu).
        /// </summary>
        private bool GetExtremeModeEnabled()
        {
            return _extremeModeEnabled;
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
