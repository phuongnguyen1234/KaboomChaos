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

        // Trang thai Extreme Mode: khi bat, perk khong tác dụng (khong Apply/Update/Remove)
        // nhu van được equip va hien thi tren UI.
        private bool _extremeModeEnabled;

        // Danh dau xem behavior cua perk hien tai co dang duoc Apply hay khong.
        // Dung de tranh goi Apply/Remove khi extreme mode dang tat (perk đang hoat dong).
        private bool _perkBehaviorActive;

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

            // Khoi phuc trang thai Extreme Mode de biet perk co hoạt động hay khong.
            _extremeModeEnabled = GameEvents.TriggerRequestExtremeModeEnabled();
        }

        private void OnEnable()
        {
            GameEvents.OnExtremeModeStateChanged += HandleExtremeModeChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnExtremeModeStateChanged -= HandleExtremeModeChanged;
        }

        private void Start()
        {
            // Khoi phuc lai perk dang trang bi (da luu) khi player duoc spawn vao game.
            RestoreEquippedPerk();
        }

        private void Update()
        {
            // Perk la hieu ung noi tai luon hoat dong: tick logic moi frame khi dang trang bi.
            // Nhung khi bat Extreme Mode, perk bi vô hiệu (khong tick). Van equip/hien thi binh thuong.
            if (_player == null || _currentPerk == null || _extremeModeEnabled || !_perkBehaviorActive) return;

            _currentPerk.Behavior?.UpdateBehavior(_player, Time.deltaTime);
        }

        private void OnDestroy()
        {
            // Don dep: huy hieu ung perk hien tai khi controller bi huy (chi neu behavior dang duoc Apply).
            RemovePerkBehavior();
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
            RemovePerkBehavior();
            Debug.Log($"[PlayerPerkController] Da go trang bi perk: {(_currentPerk != null ? _currentPerk.DisplayName : "None")}");

            _currentPerk = perkData;

            // Kich hoat hieu ung noi tai cua perk moi (neu co va extreme mode dang TAT).
            // Neu extreme mode BAT, perk van duoc trang bi (equip) nhung khong tao tac (dung theo yeu cau).
            if (_currentPerk != null && !_extremeModeEnabled)
            {
                ApplyPerkBehavior();
                Debug.Log($"[PlayerPerkController] Da trang bi perk: {_currentPerk.DisplayName}");
            }
            else if (_currentPerk != null)
            {
                Debug.Log($"[PlayerPerkController] Da trang bi perk: {_currentPerk.DisplayName} (Extreme Mode BAT - hieu ung bi vô hiệu).");
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
            // Chi Apply behavior khi extreme mode TAT; khoi phuc UI icon bang event.
            if (!_extremeModeEnabled)
            {
                ApplyPerkBehavior();
                Debug.Log($"[PlayerPerkController] Da khoi phuc perk: {_currentPerk.DisplayName}");
            }
            else
            {
                Debug.Log($"[PlayerPerkController] Da khoi phuc perk: {_currentPerk.DisplayName} (Extreme Mode BAT - hieu ung bi vô hiệu).");
            }

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

        /// <summary>
        /// Xu ly khi trang thai Extreme Mode thay doi.
        /// - BAT: go bo hieu ung perk hien tai (neu dang hoat dong) de perk khong tac dung.
        /// - TAT: Apply lai hieu ung perk hien tai (neu con trang bi) de perk hoat dong binh thuong.
        /// Van giu nguyen trang thai trang bi va icon HUD (chi hieu ung bi vô hiệu).
        /// </summary>
        /// <param name="enabled">True neu Extreme Mode dang bat.</param>
        private void HandleExtremeModeChanged(bool enabled)
        {
            _extremeModeEnabled = enabled;

            if (_extremeModeEnabled)
            {
                // Tat extreme: go bo hieu ung perk de khong con tac dung.
                RemovePerkBehavior();
            }
            else
            {
                // Tat bat: apply lai hieu ung perk neu con trang bi.
                ApplyPerkBehavior();
            }

            // Bao HUD/UIManager cap nhat lai hien thi (icon trang bi van giu, chi mau/trang thai thay doi).
            GameEvents.TriggerPlayerEquipmentChanged();
        }

        /// <summary>
        /// Apply behavior cua perk hien tai len player (chi khi extreme mode TAT va co perk).
        /// </summary>
        private void ApplyPerkBehavior()
        {
            if (_currentPerk == null || _extremeModeEnabled) return;

            _currentPerk.Behavior?.Apply(_player);
            _perkBehaviorActive = _currentPerk.Behavior != null;
        }

        /// <summary>
        /// Go bo behavior cua perk hien tai (chi neu dang duoc Apply) de tranh goi Remove khong can thiet.
        /// </summary>
        private void RemovePerkBehavior()
        {
            if (!_perkBehaviorActive) return;

            _currentPerk?.Behavior?.Remove(_player);
            _perkBehaviorActive = false;
        }

        #endregion
    }
}
