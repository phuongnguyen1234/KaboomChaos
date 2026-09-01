using UnityEngine;
using Core;
using Core.Interfaces;

namespace Player
{
    /// <summary>
    /// Bo dieu khien Perk gan tren Player. Quan ly mot perk dang duoc trang bi (1 slot, tuong tu PlayerSkillController):
    /// - Equip/Unequip perk theo IPerkData (he thong UI hien tai).
    /// - Tra cuu du lieu perk tu IPerkDatabase (inject qua Inspector bang ScriptableObject,
    ///   vi assembly Player khong the reference assembly Perks).
    /// - Goi Apply/UpdateBehavior/Remove cua perk behavior tuong ung.
    /// - Don dep dang ky event khi controller bi huy (player bi destroy).
    /// Perk la hieu ung noi tai: luon hoat dong khi duoc trang bi, khong can kich hoat thu cong.
    /// </summary>
    public class PlayerPerkController : MonoBehaviour, IPerkController
    {
        #region Fields

        [Header("Perk Database")]
        [Tooltip("Doi tuong implement IPerkDatabase (ScriptableObject PerkDatabase o assembly Perks). Player khong the reference assembly Perks nen inject qua Inspector.")]
        [SerializeField] private ScriptableObject _perkDatabase;

        // Cache giao dien IPerkDatabase doc tu _perkDatabase.
        private IPerkDatabase _perkDb;

        // Player so huu controller nay.
        private IPlayer _player;

        // Perk hien tai dang duoc trang bi (null = chua trang bi).
        private IPerkData _currentPerk;

        #endregion

        #region Properties

        /// <summary>
        /// Perk hien tai dang duoc trang bi (null neu chua trang bi perk nao).
        /// </summary>
        public IPerkData CurrentPerk => _currentPerk;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            _perkDb = _perkDatabase as IPerkDatabase;
            _player = GetComponent<IPlayer>();

            if (_perkDb == null)
            {
                Debug.LogWarning("[PlayerPerkController] _perkDatabase chua duoc gan hoac khong implement IPerkDatabase. Perk se khong the ap dung hieu ung!", this);
            }

            if (_player == null)
            {
                Debug.LogWarning("[PlayerPerkController] Khong tim thay component IPlayer tren GameObject nay!", this);
            }
        }

        private void Start()
        {
            // Khoi phuc lai perk dang trang bi (da luu) khi player duoc spawn vao game.
            RestoreEquippedPerk();
        }

        private void Update()
        {
            // Perk la hieu ung noi tai luon hoat dong: tick logic moi frame khi dang trang bi.
            if (_player == null || _currentPerk == null) return;

            _currentPerk.Behavior?.UpdateBehavior(_player, Time.deltaTime);
        }

        private void OnDestroy()
        {
            // Don dep: huy hieu ung perk hien tai khi controller bi huy.
            _currentPerk?.Behavior?.Remove(_player);
            _currentPerk = null;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Trang bi mot perk moi cho Player. Neu truyen null hoac trung voi perk hien tai thi go bo trang bi.
        /// Perk cu se tu dong bi go bo hieu ung truoc khi perk moi duoc ap dung.
        /// </summary>
        /// <param name="perkData">Du lieu perk can trang bi, hoac null de go trang bi.</param>
        public void EquipPerk(IPerkData perkData)
        {
            // Neu truyen cung perk dang trang bi → go bo trang bi (toggle off).
            if (perkData != null && _currentPerk != null && perkData.Id == _currentPerk.Id)
            {
                perkData = null;
            }

            // Go bo hieu ung perk cu (neu co).
            if (_currentPerk != null)
            {
                _currentPerk.Behavior?.Remove(_player);
                Debug.Log($"[PlayerPerkController] Da go trang bi perk: {_currentPerk.DisplayName}");
            }

            _currentPerk = perkData;

            // Kich hoat hieu ung noi tai cua perk moi (neu co).
            if (_currentPerk != null)
            {
                _currentPerk.Behavior?.Apply(_player);
                Debug.Log($"[PlayerPerkController] Da trang bi perk: {_currentPerk.DisplayName}");
            }

            GameEvents.TriggerPlayerEquipmentChanged();
            SaveEquippedState();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Khoi phuc lai perk dang trang bi duoc luu boi PlayerDataManager khi vao game.
        /// </summary>
        private void RestoreEquippedPerk()
        {
            if (_perkDb == null) return;

            string savedPerkId = GameEvents.TriggerRequestEquippedPerkId();
            if (string.IsNullOrEmpty(savedPerkId)) return;

            IPerkData perkData = _perkDb.GetById(savedPerkId);
            if (perkData == null)
            {
                Debug.LogWarning($"[PlayerPerkController] Khong tim thay perk '{savedPerkId}' trong database khi khoi phuc trang thai.", this);
                return;
            }

            // Gan truc tiep de tranh trigger save/event khong can thiet khi khoi phuc.
            _currentPerk = perkData;
            _currentPerk.Behavior?.Apply(_player);
            Debug.Log($"[PlayerPerkController] Da khoi phuc perk: {_currentPerk.DisplayName}");

            // Bao HUD cap nhat icon sau khi khoi phuc.
            GameEvents.TriggerPlayerEquipmentChanged();
        }

        /// <summary>
        /// Bao PlayerDataManager luu lai perk dang trang bi hien tai.
        /// </summary>
        private void SaveEquippedState()
        {
            GameEvents.TriggerEquippedPerkIdChanged(_currentPerk?.Id);
        }

        #endregion
    }
}