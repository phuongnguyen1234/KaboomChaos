using System.Collections;
using Core;
using Core.Interfaces;
using TMPro;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// Popup Shop dang Tab (PopupTab) gom 2 tab: Skills va Perks.
    /// Tab Skills hien thi cac SkillTypeCard (1 card cho moi SkillType). Mua khong truc tiep
    /// ma la quay thuong: nhan Buy tru di credits, tang gia cac nhom, roi mo SkillPurchasePopup
    /// de quay ra mot skill ngau nhien trong nhom.
    /// Tab Perks hien thi cac ShopPerkCard trong ScrollRect/Grid, perk duoc ban truc tiep:
    /// nhan Buy tru credits va them perk vao danh sach so huu (perk da mua bien mat khoi Shop).
    /// </summary>
    public class ShopPopup : BaseTabUI
    {
        #region Constants
        /// <summary>Chi so cua tab Skills trong danh sach tab (Index 0).</summary>
        private const int SkillsTabIndex = 0;
        /// <summary>Chi so cua tab Perks trong danh sach tab (Index 1).</summary>
        private const int PerksTabIndex = 1;
        #endregion

        #region Fields
        [Header("Shop Balance")]
        [Tooltip("Text hien thi so credits dang co cua nguoi choi.")]
        [SerializeField] private TextMeshProUGUI _balanceText;

        [Header("Shop Data")]
        [Tooltip("Config kinh te (gia + he so tang) cua cac nhom skill.")]
        [SerializeField] private ScriptableObject _typeConfig;

        [Tooltip("Prefab popup quay thuong skill, duoc goi ra khi nhan Buy.")]
        [SerializeField] private SkillPurchasePopup _skillPurchasePopup;

        [Tooltip("Popup 'View all' hien thi tat ca skill cua mot nhom. Duoc mo khi nhan nut 'View all' tren card.")]
        [SerializeField] private ViewAllItemPopup _viewAllPopup;

        [Header("Not Enough Credits")]
        [Tooltip("Panel chua text 'Not Enough Credits'. Duoc hien thi khi mua khong du credits va tu dong an sau 2 giay.")]
        [SerializeField] private GameObject _notEnoughCreditsPanel;

        [Tooltip("Thoi gian panel 'Not Enough Credits' hien thi truoc khi tu dong an (giay).")]
        [SerializeField] private float _notEnoughCreditsDuration = 2f;

        // Coroutine dang dem gio de tu dong an panel 'Not Enough Credits'.
        private Coroutine _notEnoughCreditsCoroutine;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();

            if (_skillPurchasePopup != null)
            {
                _skillPurchasePopup.OnClosed += HandlePurchasePopupClosed;
            }
        }
        #endregion

        #region Protected Virtual Hooks
        /// <summary>
        /// Lam moi so du va tab dang hien thi moi khi mo Shop.
        /// </summary>
        protected override void OnShow()
        {
            base.OnShow();
            HideNotEnoughCredits();
            RefreshBalance();
            RefreshCurrentTab();
        }

        /// <summary>
        /// Khi Shop dong (nut Close, chuyen sang popup khac hoac thoat): an panel thong bao
        /// 'Not Enough Credits' va an popup quay thuong (neu dang mo) de tranh nhieu popup cung hien thi.
        /// </summary>
        protected override void OnHidden()
        {
            base.OnHidden();
            HideNotEnoughCredits();

            // Popup quay thuong la dialog con cua Shop: dong theo khi Shop dong.
            if (_skillPurchasePopup != null)
            {
                _skillPurchasePopup.Hide();
            }

            // Dong luon popup 'View all' (dialog con) khi Shop dong.
            if (_viewAllPopup != null)
            {
                _viewAllPopup.Hide();
            }
        }

        /// <summary>
        /// Khi chon tab, nap du lieu tuong ung: Skills (cac SkillTypeCard) hoac Perks.
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

        #region Private Methods - Populate
        /// <summary>
        /// Populate cac SkillTypeCard trong noi dung tab Skills.
        /// </summary>
        private void PopulateSkillsTab(RectTransform contentInstance)
        {
            ShopSkillsView view = contentInstance != null
                ? contentInstance.GetComponentInChildren<ShopSkillsView>()
                : null;

            if (view == null)
            {
                Debug.LogWarning("[ShopPopup] Khong tim thay ShopSkillsView trong noi dung tab Skills. Kiem tra lai prefab tab.");
                return;
            }

            int spinCount = GameEvents.TriggerRequestSkillSpinCount();
            var ownedSkillIds = GameEvents.TriggerRequestOwnedSkillIds();
            view.Populate(ownedSkillIds, spinCount, HandleBuyClicked, HandleViewAllClicked);
        }

        /// <summary>
        /// Xu ly khi nguoi dung bam nut 'View all' tren card: mo ViewAllItemPopup
        /// hien thi tat ca skill cua nhom, danh dau tick cac skill da so huu.
        /// </summary>
        /// <param name="type">Loai SkillType cua nhom can xem.</param>
        private void HandleViewAllClicked(SkillType type)
        {
            if (_viewAllPopup == null)
            {
                Debug.LogWarning("[ShopPopup] _viewAllPopup chua duoc gan tren Inspector. Khong the mo View All.");
                return;
            }

            var ownedSkillIds = GameEvents.TriggerRequestOwnedSkillIds();
            _viewAllPopup.Open(type, ownedSkillIds);
        }

        /// <summary>
        /// Populate cac ShopPerkCard trong noi dung tab Perks.
        /// Chi hien thi cac perk nguoi choi chua so huu.
        /// </summary>
        private void PopulatePerksTab(RectTransform contentInstance)
        {
            ShopPerksView view = contentInstance != null
                ? contentInstance.GetComponentInChildren<ShopPerksView>()
                : null;

            if (view == null)
            {
                Debug.LogWarning("[ShopPopup] Khong tim thay ShopPerksView trong noi dung tab Perks. Kiem tra lai prefab tab.");
                return;
            }

            var ownedPerkIds = GameEvents.TriggerRequestOwnedPerkIds();
            view.Populate(ownedPerkIds, HandlePerkBuyClicked);
        }
        #endregion

        #region Private Methods - Buy
        /// <summary>
        /// Xu ly khi nguoi dung bam nut Buy cua mot nhom skill.
        /// </summary>
        /// <param name="type">Loai SkillType can quay.</param>
        private void HandleBuyClicked(SkillType type)
        {
            int price = GetCurrentPrice(type);
            int credits = GameEvents.TriggerRequestCurrentCredits();

            if (credits < price)
            {
                Debug.Log($"[ShopPopup] Khong du credits. Can {price}, dang co {credits}.");
                ShowNotEnoughCredits();
                return;
            }

            // Tru credits va tang gia tat ca cac nhom (moi nhom tang theo he so rieng).
            GameEvents.TriggerAddCreditsRequest(-price);
            GameEvents.TriggerIncreaseSkillSpinCount();

            RefreshBalance();

            // Mo popup quay thuong de xac dinh skill nhan duoc trong nhom.
            if (_skillPurchasePopup != null)
            {
                _skillPurchasePopup.BeginSpin(type);
            }
        }

        /// <summary>
        /// Xu ly khi nguoi dung bam nut Buy cua mot perk trong tab Perks.
        /// Perk ban truc tiep: tru credits, them perk vao danh sach so huu va lam moi tab
        /// (perk vua mua se bien mat khoi Shop, neu het perk thi hien Sold Out).
        /// </summary>
        /// <param name="perkData">Du lieu perk can mua.</param>
        private void HandlePerkBuyClicked(IPerkData perkData)
        {
            if (perkData == null) return;

            int price = perkData.Price;
            int credits = GameEvents.TriggerRequestCurrentCredits();

            if (credits < price)
            {
                Debug.Log($"[ShopPopup] Khong du credits. Can {price}, dang co {credits}.");
                ShowNotEnoughCredits();
                return;
            }

            // Tru credits va them perk vao danh sach so huu (duoc tu dong luu boi PlayerDataManager).
            GameEvents.TriggerAddCreditsRequest(-price);
            GameEvents.TriggerAddOwnedPerk(perkData.Id);

            Debug.Log($"[ShopPopup] Da mua perk: {perkData.DisplayName}.");
            RefreshBalance();
            RefreshCurrentTab();
        }

        /// <summary>
        /// Tinh gia hien tai cua mot nhom skill (base * growth^spinCount).
        /// </summary>
        private int GetCurrentPrice(SkillType type)
        {
            int spinCount = GameEvents.TriggerRequestSkillSpinCount();

            if (_typeConfig is Core.Economy.SkillTypeConfig config)
            {
                return config.GetPrice(type, spinCount);
            }

            return 0;
        }

        /// <summary>
        /// Goi khi popup quay thuong dong lai: lam moi lai so du va tab de cap nhat so huu/price.
        /// </summary>
        private void HandlePurchasePopupClosed()
        {
            RefreshBalance();
            RefreshCurrentTab();
        }
        #endregion

        #region Private Methods - Not Enough Credits
        /// <summary>
        /// Hien thi panel 'Not Enough Credits' va tu dong an sau _notEnoughCreditsDuration giay.
        /// Neu panel dang hien, coroutine cu duoc dung va dem lai tu dau.
        /// </summary>
        private void ShowNotEnoughCredits()
        {
            if (_notEnoughCreditsPanel == null) return;

            _notEnoughCreditsPanel.SetActive(true);

            if (_notEnoughCreditsCoroutine != null)
            {
                StopCoroutine(_notEnoughCreditsCoroutine);
                _notEnoughCreditsCoroutine = null;
            }

            _notEnoughCreditsCoroutine = StartCoroutine(HideNotEnoughCreditsAfterDelay());
        }

        /// <summary>
        /// Coroutine an panel 'Not Enough Credits' sau mot khoang thoi gian nhat dinh.
        /// </summary>
        private IEnumerator HideNotEnoughCreditsAfterDelay()
        {
            yield return new WaitForSeconds(_notEnoughCreditsDuration);
            _notEnoughCreditsCoroutine = null;

            if (_notEnoughCreditsPanel != null)
            {
                _notEnoughCreditsPanel.SetActive(false);
            }
        }

        /// <summary>
        /// An ngay panel 'Not Enough Credits' va dung coroutine dang chay (neu co).
        /// </summary>
        private void HideNotEnoughCredits()
        {
            if (_notEnoughCreditsCoroutine != null)
            {
                StopCoroutine(_notEnoughCreditsCoroutine);
                _notEnoughCreditsCoroutine = null;
            }

            if (_notEnoughCreditsPanel != null)
            {
                _notEnoughCreditsPanel.SetActive(false);
            }
        }
        #endregion

        #region Private Methods - HUD
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
    }
}