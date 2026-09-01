using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;
using Core.Enums;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Popup Inventory dang Tab (PopupTab).
    /// Gom 2 tab: Skills va Perks. Hien thi cac item nguoi choi so huu,
    /// kem so du (balance) va InfoPanel de xem chi tiet + Equip/Unequip item len Player.
    /// </summary>
    public class InventoryPopup : BaseTabUI
    {
        #region Constants
        /// <summary>Chi so cua tab Skills trong danh sach tab (Index 0).</summary>
        private const int SkillsTabIndex = 0;
        /// <summary>Chi so cua tab Perks trong danh sach tab (Index 1).</summary>
        private const int PerksTabIndex = 1;
        #endregion

        #region Enums
        /// <summary>
        /// Loai item dang duoc chon/duoc xem thong tin.
        /// </summary>
        private enum ItemKind
        {
            None,
            Skill,
            Perk
        }
        #endregion

        #region Fields
        [Header("Inventory HUD")]
        [Tooltip("Text hien thi so credits (so du) cua nguoi choi.")]
        [SerializeField] private TextMeshProUGUI _balanceText;

        [Header("Info Panel")]
        [Tooltip("Root GameObject cua InfoPanel. Se an/hien khi chon item.")]
        [SerializeField] private GameObject _infoPanel;
        [Tooltip("Image hien thi icon cua item dang chon.")]
        [SerializeField] private Image _infoIconImage;
        [Tooltip("Text hien thi ten item dang chon.")]
        [SerializeField] private TextMeshProUGUI _infoNameText;
        [Tooltip("Text hien thi mo ta item dang chon.")]
        [SerializeField] private TextMeshProUGUI _infoDescriptionText;
        [Tooltip("Button Equip/Unequip item len Player.")]
        [SerializeField] private Button _equipButton;
        [Tooltip("Text cua _equipButton de doi nhan (Equip/Unequip).")]
        [SerializeField] private TextMeshProUGUI _equipButtonText;

        [Header("Inventory Data")]
        [Tooltip("Doi tuong implement ISkillDatabase (ScriptableObject). UI khong the reference assembly Skills nen inject qua Inspector.")]
        [SerializeField] private ScriptableObject _skillDatabase;

        [Tooltip("Doi tuong implement IPerkDatabase (ScriptableObject). UI khong the reference assembly Perks nen inject qua Inspector.")]
        [SerializeField] private ScriptableObject _perkDatabase;

        // Cache giao dien ISkillDatabase doc tu _skillDatabase.
        private ISkillDatabase _skillDb;

        // Cache giao dien IPerkDatabase doc tu _perkDatabase.
        private IPerkDatabase _perkDb;

        [Header("Restricted Equip")]
        [Tooltip("Panel thong bao hien thi khi nguoi choi co trang bi skill/perk trong giai doan khong duoc phep (arena).")]
        [SerializeField] private GameObject _restrictedEquipPanel;

        [Tooltip("Thoi gian panel 'Restricted Equip' hien thi truoc khi tu dong an (giay).")]
        [SerializeField] private float _restrictedEquipDuration = 2f;

        // Coroutine dang dem gio de tu dong an panel 'Restricted Equip'.
        private Coroutine _restrictedEquipCoroutine;

        // Trang thai item dang duoc chon.
        private ItemKind _selectedKind = ItemKind.None;
        private string _selectedId;
        // Luu ISkillData dang chon de co the Equip (EquipSkill can ISkillData, khong chi ID).
        private ISkillData _pendingSkillData;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _skillDb = _skillDatabase as ISkillDatabase;
            _perkDb = _perkDatabase as IPerkDatabase;

            if (_equipButton != null)
            {
                _equipButton.onClick.AddListener(OnEquipButtonClicked);
            }
        }
        #endregion

        #region Protected Virtual Hooks
        /// <summary>
        /// Lam moi so du va tab dang hien thi moi khi mo Inventory.
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
            RefreshBalance();
            RefreshCurrentTab();
        }

        /// <summary>
        /// Khi Inventory dong: an panel thong bao 'Restricted Equip' (neu dang hien).
        /// </summary>
        protected override void OnHidden()
        {
            base.OnHidden();

            if (_restrictedEquipCoroutine != null)
            {
                StopCoroutine(_restrictedEquipCoroutine);
                _restrictedEquipCoroutine = null;
            }

            if (_restrictedEquipPanel != null)
            {
                _restrictedEquipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Khi chon tab, nap du lieu tuong ung (Skills hoac Perks) vao noi dung tab.
        /// </summary>
        protected override void OnTabSelected(int index, RectTransform contentInstance)
        {
            base.OnTabSelected(index, contentInstance);

            switch (index)
            {
                case SkillsTabIndex:
                    PopulateSkillsTab(contentInstance);
                    break;
                case PerksTabIndex:
                    PopulatePerksTab(contentInstance);
                    break;
            }
        }
        #endregion

        #region Private Methods - Populate Tabs
        /// <summary>
        /// Nap cac skill nguoi choi so huu vao tab Skills.
        /// </summary>
        private void PopulateSkillsTab(RectTransform contentInstance)
        {
            SkillsContentView view = contentInstance != null
                ? contentInstance.GetComponentInChildren<SkillsContentView>()
                : null;

            if (view == null)
            {
                Debug.LogWarning("[InventoryPopup] Khong tim thay SkillsContentView trong noi dung tab Skills. Kiem tra lai prefab tab.");
                return;
            }

            if (_skillDb == null)
            {
                Debug.LogWarning("[InventoryPopup] _skillDatabase chua duoc gan hoac khong implement ISkillDatabase. Khong the load skill.");
                return;
            }

            IReadOnlyList<string> ownedSkillIds = GameEvents.TriggerRequestOwnedSkillIds();

            List<ISkillData> ownedSkills = new();
            foreach (string id in ownedSkillIds)
            {
                ISkillData data = _skillDb.GetById(id);
                if (data != null)
                {
                    ownedSkills.Add(data);
                }
            }

            HashSet<string> equippedSkillIds = GetEquippedSkillIds();
            view.Populate(ownedSkills, equippedSkillIds, HandleSkillSelected);
        }

        /// <summary>
        /// Nap cac perk nguoi choi so huu vao tab Perks.
        /// </summary>
        private void PopulatePerksTab(RectTransform contentInstance)
        {
            PerksContentView view = contentInstance != null
                ? contentInstance.GetComponentInChildren<PerksContentView>()
                : null;

            if (view == null)
            {
                Debug.LogWarning("[InventoryPopup] Khong tim thay PerksContentView trong noi dung tab Perks. Kiem tra lai prefab tab.");
                return;
            }

            IReadOnlyList<string> ownedPerkIds = GameEvents.TriggerRequestOwnedPerkIds();
            HashSet<string> equippedPerkIds = GetEquippedPerkIds();
            view.Populate(ownedPerkIds, equippedPerkIds, HandlePerkSelected);
        }
        #endregion
#region Private Methods - Selection & Info
        /// <summary>
        /// Xu ly khi nguoi dung chon mot skill.
        /// </summary>
        private void HandleSkillSelected(ISkillData skill)
        {
            if (skill == null) return;

            _selectedKind = ItemKind.Skill;
            _selectedId = skill.Id;
            _pendingSkillData = skill;

            SetInfoPanel(skill.Icon, skill.DisplayName, skill.Description);
            RefreshEquipButton();
        }

        /// <summary>
        /// Xu ly khi nguoi dung chon mot perk.
        /// </summary>
        private void HandlePerkSelected(string perkId)
        {
            if (string.IsNullOrEmpty(perkId)) return;

            _selectedKind = ItemKind.Perk;
            _selectedId = perkId;
            _pendingSkillData = null;

            // Lay du lieu perk tu database de hien thi ten/mo ta/icon trong InfoPanel.
            IPerkData perkData = _perkDb != null ? _perkDb.GetById(perkId) : null;
            if (perkData != null)
            {
                SetInfoPanel(perkData.Icon, perkData.DisplayName, perkData.Description);
            }
            else
            {
                SetInfoPanel(null, perkId, "Khong tim thay thong tin chi tiet cua perk nay.");
            }

            RefreshEquipButton();
        }

        /// <summary>
        /// Do du lieu hien thi cua item dang chon vao InfoPanel.
        /// </summary>
        private void SetInfoPanel(Sprite icon, string name, string description)
        {
            if (_infoPanel != null)
            {
                _infoPanel.SetActive(true);
            }

            if (_infoIconImage != null) _infoIconImage.sprite = icon;
            if (_infoNameText != null) _infoNameText.text = name;
            if (_infoDescriptionText != null) _infoDescriptionText.text = description;
        }

        /// <summary>
        /// Cap nhat so du credits hien tai.
        /// </summary>
        private void RefreshBalance()
        {
            if (_balanceText == null) return;

            int credits = GameEvents.TriggerRequestCurrentCredits();
            _balanceText.text = credits.ToString();
        }
        #endregion

        #region Private Methods - Equip / Unequip
        /// <summary>
        /// Xu ly khi nguoi dung bam nut Equip/Unequip.
        /// </summary>
        private void OnEquipButtonClicked()
        {
            // Viec trang bi skill/perk chi duoc phep o giai doan binh chon map (MapVoting),
            // xay map (Building) va sau khi round ket thuc (PostRound). Khi da dua vao arena
            // (PreRound/RoundActive) thi khong duoc phep -> hien panel thong bao va khong thuc hien.
            if (!IsEquipAllowed())
            {
                ShowRestrictedEquipPanel();
                return;
            }

            switch (_selectedKind)
            {
                case ItemKind.Skill:
                    ToggleEquipSkill();
                    break;
                case ItemKind.Perk:
                    ToggleEquipPerk();
                    break;
            }

            // Rebuild tab de cap nhat hinh danh dau equip tren cac the.
            RefreshCurrentTab();
            RefreshEquipButton();

            // Bao hieu HUD cap nhat icon skill/perk dang trang bi.
            GameEvents.TriggerPlayerEquipmentChanged();
        }

        /// <summary>
        /// Equip/Unequip skill dang chon tren Player (thong qua ISkillController).
        /// </summary>
        private void ToggleEquipSkill()
        {
            ISkillController skillController = GetSkillController();
            if (skillController == null)
            {
                Debug.LogWarning("[InventoryPopup] Khong tim thay ISkillController tren Player hien tai. Khong the trang bi skill.");
                return;
            }

            bool isEquipped = _selectedId != null && skillController.CurrentSkill?.Id == _selectedId;

            if (isEquipped)
            {
                skillController.EquipSkill(null);
            }
            else if (_pendingSkillData != null)
            {
                skillController.EquipSkill(_pendingSkillData);
            }
        }

        /// <summary>
        /// Equip/Unequip perk dang chon tren Player (thong qua IPerkController).
        /// Lay IPerkData tu _perkDb truoc khi truyen vao controller (vi controller khong nhan ID nua).
        /// </summary>
        private void ToggleEquipPerk()
        {
            IPerkController perkController = GetPerkController();
            if (perkController == null)
            {
                Debug.LogWarning("[InventoryPopup] Khong tim thay IPerkController tren Player hien tai. Khong the trang bi perk.");
                return;
            }

            // Lay IPerkData tu database (popup da co _perkDb).
            IPerkData perkData = _perkDb?.GetById(_selectedId);
            if (perkData == null)
            {
                Debug.LogWarning($"[InventoryPopup] Khong tim thay du lieu perk '{_selectedId}' trong database.");
                return;
            }

            // EquipPerk tu xu ly toggle: neu truyen cung perk hien tai thi se unequip.
            perkController.EquipPerk(perkData);
        }

        /// <summary>
        /// Cap nhat trang thai (nhan + tuong tac) cua nut Equip/Unequip.
        /// </summary>
        private void RefreshEquipButton()
        {
            bool hasSelection = _selectedKind != ItemKind.None && !string.IsNullOrEmpty(_selectedId);
            bool isEquipped = IsSelectedEquipped();

            if (_equipButtonText != null)
            {
                _equipButtonText.text = isEquipped ? "Unequip" : "Equip";
            }

            if (_equipButton != null)
            {
                bool hasController = _selectedKind switch
                {
                    ItemKind.Skill => GetSkillController() != null,
                    ItemKind.Perk => GetPerkController() != null,
                    _ => false
                };

                _equipButton.interactable = hasSelection && hasController;
            }
        }

        /// <summary>
        /// Cho biet item dang chon co dang duoc trang bi hay khong.
        /// </summary>
        private bool IsSelectedEquipped()
        {
            if (_selectedKind == ItemKind.None || string.IsNullOrEmpty(_selectedId)) return false;

            switch (_selectedKind)
            {
                case ItemKind.Skill:
                    ISkillController sc = GetSkillController();
                    return sc != null && sc.CurrentSkill?.Id == _selectedId;

                case ItemKind.Perk:
                    IPerkController pc = GetPerkController();
                    return pc != null && pc.CurrentPerk?.Id == _selectedId;

                default:
                    return false;
            }
        }
        #endregion

        #region Private Methods - Player Access
        /// <summary>
        /// Lay ISkillController cua Player hien tai (qua IPlayerManager). Tra ve null neu khong co.
        /// </summary>
        private ISkillController GetSkillController()
        {
            IPlayer player = GetCurrentPlayer();
            // Dung GetComponentInChildren de an toan neven neu controller dat tren child transform cua Player.
            return player?.GameObject != null ? player.GameObject.GetComponentInChildren<ISkillController>() : null;
        }

        /// <summary>
        /// Lay IPerkController cua Player hien tai (qua IPlayerManager). Tra ve null neu khong co.
        /// </summary>
        private IPerkController GetPerkController()
        {
            IPlayer player = GetCurrentPlayer();
            // Dung GetComponentInChildren de an toan neven neu controller dat tren child transform cua Player.
            return player?.GameObject != null ? player.GameObject.GetComponentInChildren<IPerkController>() : null;
        }

        /// <summary>
        /// Lay Player hien tai thong qua singleton IPlayerManager.
        /// </summary>
        private IPlayer GetCurrentPlayer()
        {
            IPlayerManager manager = IPlayerManager.Instance;
            return manager != null ? manager.GetCurrentPlayer() : null;
        }

        /// <summary>
        /// Lay tap ID cac skill dang duoc trang bi tren Player.
        /// </summary>
        private HashSet<string> GetEquippedSkillIds()
        {
            HashSet<string> result = new();
            ISkillController sc = GetSkillController();
            if (sc != null && sc.CurrentSkill != null && !string.IsNullOrEmpty(sc.CurrentSkill.Id))
            {
                result.Add(sc.CurrentSkill.Id);
            }
            return result;
        }

        /// <summary>
        /// Lay tap ID cac perk dang duoc trang bi tren Player.
        /// </summary>
        private HashSet<string> GetEquippedPerkIds()
        {
            HashSet<string> result = new();
            IPerkController pc = GetPerkController();
            string currentPerkId = pc?.CurrentPerk?.Id;
            if (!string.IsNullOrEmpty(currentPerkId))
            {
                result.Add(currentPerkId);
            }
            return result;
        }
        #endregion

        #region Private Methods - Restricted Equip
        /// <summary>
        /// Cho biet hien tai co duoc phep trang bi skill/perk hay khong.
        /// Chi duoc phep o: MapVoting (binh chon map), Building (xay map), PostRound (sau round)
        /// va None (chua co vong choi, vi du dang o Home). Khong duoc phep khi da dua vao arena
        /// (PreRound, RoundActive). Neu khong co IGameStateProvider thi khong chan (de phong).
        /// </summary>
        private bool IsEquipAllowed()
        {
            IGameStateProvider provider = IGameStateProvider.Instance;
            if (provider == null)
            {
                return true;
            }

            switch (provider.CurrentState)
            {
                case GameState.None:
                case GameState.MapVoting:
                case GameState.Building:
                case GameState.PostRound:
                    return true;

                case GameState.PreRound:
                case GameState.RoundActive:
                default:
                    return false;
            }
        }

        /// <summary>
        /// Hien thi panel thong bao 'khong the trang bi trong arena' va tu dong an sau
        /// _restrictedEquipDuration giay. Neu panel dang hien, coroutine cu duoc dung va dem lai tu dau.
        /// </summary>
        private void ShowRestrictedEquipPanel()
        {
            if (_restrictedEquipPanel == null) return;

            _restrictedEquipPanel.SetActive(true);

            if (_restrictedEquipCoroutine != null)
            {
                StopCoroutine(_restrictedEquipCoroutine);
                _restrictedEquipCoroutine = null;
            }

            _restrictedEquipCoroutine = StartCoroutine(HideRestrictedEquipPanelAfterDelay());
        }

        /// <summary>
        /// Coroutine an panel 'Restricted Equip' sau mot khoang thoi gian nhat dinh.
        /// </summary>
        private IEnumerator HideRestrictedEquipPanelAfterDelay()
        {
            yield return new WaitForSeconds(_restrictedEquipDuration);
            _restrictedEquipCoroutine = null;

            if (_restrictedEquipPanel != null)
            {
                _restrictedEquipPanel.SetActive(false);
            }
        }
        #endregion
    }
}