using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Noi dung cua tab Perks trong Shop: scroll rect voi content la Grid (GridLayoutGroup)
    /// chua cac ShopPerkCard. Chi hien thi cac perk NGUOI CHOI CHUA SO HUU,
    /// perk da mua se khong xuat hien tren Shop. Neu da so huu toan bo perk thi hien panel Sold Out.
    /// </summary>
    public class ShopPerksView : MonoBehaviour
    {
        #region Fields
        [Tooltip("Prefab ShopPerkCard duoc nhan ban cho moi perk con ban.")]
        [SerializeField] private ShopPerkCard _cardPrefab;

        [Tooltip("Content (GridLayoutGroup) ben trong ScrollRect chua cac ShopPerkCard.")]
        [SerializeField] private RectTransform _cardsContainer;

        [Tooltip("Data base cung cap danh sach Perk (inject qua Inspector).")]
        [SerializeField] private ScriptableObject _perkDatabase;

        [Tooltip("Panel 'Sold Out' hien thi khi nguoi choi da so huu toan bo perk.")]
        [SerializeField] private GameObject _soldOutPanel;
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
        /// Populate cac ShopPerkCard: chi tao card cho nhung perk nguoi choi chua so huu.
        /// Neu khong con perk nao de ban thi hien panel Sold Out.
        /// </summary>
        /// <param name="ownedPerkIds">Tap ID perk nguoi choi dang so huu.</param>
        /// <param name="onBuy">Callback khi nguoi dung bam nut Buy cua mot card.</param>
        public void Populate(IReadOnlyList<string> ownedPerkIds, Action<IPerkData> onBuy)
        {
            Clear();

            // Thu lai cast moi khi populate: dam bao nhan duoc database neu tham chieu vua moi duoc gan
            // (vi du nguoi dung bo quen gan tam thoi hoac tham chieu duoc thay doi luc runtime).
            if (_perkDatabase != null) ResolveDatabase();

            if (_perkDb == null)
            {
                Debug.LogWarning(
                    $"[ShopPerksView] _perkDatabase chua duoc gan hoac khong implement IPerkDatabase (component tren '{name}'). " +
                    "Hay gan Assets/Perks/PerkDatabase.asset vao field _perkDatabase cua ShopPerksView trong prefab noi dung tab Perks (Shop_Perks.prefab).",
                    this);
                return;
            }

            // Chuyen danh sach so huu sang tap hop cho tra cuu nhanh.
            HashSet<string> ownedSet = new(ownedPerkIds ?? Array.Empty<string>());

            // Chi hien thi perk chua so huu (da mua => khong ban lai).
            bool hasAvailablePerk = false;
            IReadOnlyList<IPerkData> allPerks = _perkDb.AllPerks;
            if (allPerks == null) return;

            foreach (IPerkData perk in allPerks)
            {
                if (perk == null || ownedSet.Contains(perk.Id)) continue;

                // Canh bao giup phat hien perk asset dang thieu cau hinh (Id/DisplayName/Icon/Price) de nguoi dung
                // hoan thien du lieu trong Inspector. Khong chan tao card, chi ghi log de de debug.
                if (string.IsNullOrEmpty(perk.Id) || string.IsNullOrEmpty(perk.DisplayName) || perk.Price <= 0)
                {
                    Debug.LogWarning(
                        $"[ShopPerksView] Perk dang thieu du lieu cau hinh (Id/DisplayName/Icon/Price). " +
                        $"DisplayName: '{perk.DisplayName}', Price: {perk.Price}. Hay cau hinh day du trong Inspector cua asset perk.",
                        this);
                }

                hasAvailablePerk = true;

                ShopPerkCard card = Instantiate(_cardPrefab, _cardsContainer);
                card.Setup(perk, () => onBuy?.Invoke(perk));
            }

            // Da so huu toan bo perk => hien panel Sold Out.
            if (_soldOutPanel != null)
            {
                _soldOutPanel.SetActive(!hasAvailablePerk);
            }
        }

        /// <summary>
        /// Xoa toan bo cac ShopPerkCard dang hien thi va an panel Sold Out.
        /// </summary>
        public void Clear()
        {
            if (_cardsContainer == null) return;

            for (int i = _cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardsContainer.GetChild(i).gameObject);
            }

            if (_soldOutPanel != null)
            {
                _soldOutPanel.SetActive(false);
            }
        }
        #endregion
    }
}