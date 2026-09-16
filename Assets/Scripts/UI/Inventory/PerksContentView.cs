using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Noi dung cua tab Perks trong Inventory: ScrollRect voi content la Grid (GridLayoutGroup),
    /// cac ItemButtonCard duoc load THANG vao Grid (perk khong co type nen khong can nhom nhu Skill).
    /// Moi perk so huu se tao mot ItemButtonCard hien thi icon + trang thai trang bi.
    /// Gan len prefab noi dung cua tab Perks.
    /// </summary>
    public class PerksContentView : MonoBehaviour
    {
        #region Fields
        [Tooltip("Prefab ItemButtonCard duoc nhan ban cho moi perk.")]
        [SerializeField] private ItemButtonCard _itemCardPrefab;

        [Tooltip("Content (GridLayoutGroup) ben trong ScrollRect chua cac ItemButtonCard.")]
        [SerializeField] private RectTransform _itemsContainer;

        [Tooltip("Data base cung cap thong tin Perk (inject qua Inspector). Dung de lay icon/ten cua perk.")]
        [SerializeField] private ScriptableObject _perkDatabase;

        [Tooltip("(Tuy chon) Text hien thi khi nguoi choi chua so huu perk nao. De trong neu khong can.")]
        [SerializeField] private TextMeshProUGUI _emptyStateText;

        [Tooltip("Sprite nen/khung card hien thi cho cac Perk trong tab Perks.")]
        [SerializeField] private Sprite _perkCardSprite;
        #endregion

        #region Unity Lifecycle
        private IPerkDatabase _perkDb;

        private void Awake()
        {
            ResolveDatabase();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Giai quyet (cast) ScriptableObject _perkDatabase sang IPerkDatabase.
        /// Goi lai moi khi can de tranh tinh trang tham chieu duoc gan sau Awake ma khong nhan.
        /// </summary>
        /// <returns>True neu _perkDb da san sang, false neu chua gan hoac khong dung loai.</returns>
        private bool ResolveDatabase()
        {
            if (_perkDb == null)
            {
                _perkDb = _perkDatabase as IPerkDatabase;
            }
            return _perkDb != null;
        }

        /// <summary>
        /// Nap danh sach perk so huu vao UI: tao mot ItemButtonCard cho moi perk
        /// (load thang vao Grid, khong nhom theo type).
        /// Neu khong co perk nao se hien thi _emptyStateText (neu co cau hinh).
        /// </summary>
        /// <param name="ownedPerkIds">Danh sach ID perk nguoi choi dang so huu.</param>
        /// <param name="equippedPerkIds">Tap ID cac perk dang duoc trang bi.</param>
        /// <param name="onSelect">Callback khi nguoi dung bam vao mot the perk.</param>
        public void Populate(IReadOnlyList<string> ownedPerkIds, HashSet<string> equippedPerkIds, Action<string> onSelect)
        {
            ClearItems();

            if (ownedPerkIds == null || _itemCardPrefab == null || _itemsContainer == null)
            {
                Debug.LogWarning("[PerksContentView] Thieu du lieu de populate (ownedPerkIds, _itemCardPrefab hoac _itemsContainer).");
                return;
            }

            // Thu lai cast moi khi populate: dam bao lay duoc icon/ten perk tu database
            // ke ca khi tham chieu vua moi duoc gan sau Awake.
            if (_perkDatabase != null) ResolveDatabase();

            if (_perkDb == null)
            {
                Debug.LogWarning(
                    $"[PerksContentView] _perkDatabase chua duoc gan hoac khong implement IPerkDatabase (component tren '{name}'). " +
                    "Icon/ten cua perk se khong hien thi. Hay gan Assets/Perks/PerkDatabase.asset vao field _perkDatabase cua PerksContentView trong prefab noi dung tab Perks (Inventory_Perks.prefab).",
                    this);
            }

            bool hasAnyPerk = false;

            foreach (string perkId in ownedPerkIds)
            {
                if (string.IsNullOrEmpty(perkId)) continue;

                hasAnyPerk = true;

                // Lay du lieu perk tu database de hien thi icon tren card.
                IPerkData perkData = _perkDb != null ? _perkDb.GetById(perkId) : null;

                ItemButtonCard card = Instantiate(_itemCardPrefab, _itemsContainer);
                bool isEquipped = equippedPerkIds != null && equippedPerkIds.Contains(perkId);
                card.Setup(perkId, perkData != null ? perkData.Icon : null, isEquipped, _perkCardSprite);

                string capturedId = perkId;
                card.OnClicked += _ => onSelect?.Invoke(capturedId);
            }

            if (_emptyStateText != null)
            {
                _emptyStateText.gameObject.SetActive(!hasAnyPerk);
            }
        }

        /// <summary>
        /// Xoa toan bo cac ItemButtonCard dang hien thi de tao lai tu dau.
        /// </summary>
        public void ClearItems()
        {
            if (_itemsContainer == null) return;

            for (int i = _itemsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_itemsContainer.GetChild(i).gameObject);
            }
        }
        #endregion
    }
}