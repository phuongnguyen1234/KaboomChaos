using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// Lop goc cho cac popup dang Tab (PopupTab) nhu Shop, Inventory.
    /// Chua mot RectTransform (container) de nhan prefab UI noi dung tuong ung
    /// voi moi nu Tab. Khi chon tab, prefab noi dung se duoc nhan ban vao container
    /// va nu tab dang chon duoc highlight. Cac class con override OnTabSelected
    /// de gan du lieu / logic cho tung tab.
    /// </summary>
    public abstract class BaseTabUI : BasePopup
    {
        #region Fields
        [Header("Tab UI")]
        [Tooltip("Container (RectTransform) de cai prefab noi dung cua tab dang duoc chon.")]
        [SerializeField] private RectTransform _tabContentContainer;

        [Tooltip("Danh sach cac tab (nu tab + prefab noi dung tuong ung).")]
        [SerializeField] private List<TabItem> _tabs = new();

        [Tooltip("Chi so cua tab duoc chon mac dinh luc khoi tao.")]
        [SerializeField] private int _defaultTabIndex;

        [Tooltip("Mau cua tab dang duoc chon.")]
        [SerializeField] private Color _selectedTabColor = new(1f, 1f, 1f, 1f);

        [Tooltip("Mau cua tab khong duoc chon.")]
        [SerializeField] private Color _deselectedTabColor = new(0.7f, 0.7f, 0.7f, 1f);
        #endregion

        #region Properties
        /// <summary>
        /// Cong chi so cua tab dang duoc chon.
        /// </summary>
        public int CurrentTabIndex { get; private set; }

        /// <summary>
        /// Instance noi dung (da duoc nhan ban) cua tab dang hien thi. Tra ve null neu chua co.
        /// </summary>
        public RectTransform CurrentTabInstance { get; private set; }

        /// <summary>
        /// Tra ve so luong tab dang duoc cau hinh.
        /// </summary>
        public int TabCount => _tabs == null ? 0 : _tabs.Count;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            WireTabButtons();
        }

        protected virtual void Start()
        {
            // Chon tab mac dinh khi khoi dong.
            if (_tabs != null && _tabs.Count > 0)
            {
                int index = Mathf.Clamp(_defaultTabIndex, 0, _tabs.Count - 1);
                SelectTab(index);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Chon tab theo chi so: cai prefab noi dung tuong ung vao container va cap nhat trang thai nu tab.
        /// </summary>
        /// <param name="index">Chi so cua tab can chon.</param>
        public void SelectTab(int index)
        {
            if (_tabs == null || index < 0 || index >= _tabs.Count)
            {
                Debug.LogWarning($"[BaseTabUI] Chi so tab khong hop le: {index}");
                return;
            }

            TabItem target = _tabs[index];
            if (target == null || target.ContentPrefab == null)
            {
                Debug.LogWarning($"[BaseTabUI] Tab thu {index} chua duoc cau hinh (thieu content prefab).");
                return;
            }

            CurrentTabIndex = index;

            ClearCurrentTabContent();

            // Nhan ban prefab noi dung moi vao container.
            RectTransform content = Instantiate(target.ContentPrefab, _tabContentContainer);
            content.anchoredPosition = Vector2.zero;
            content.anchorMin = Vector2.zero;
            content.anchorMax = Vector2.one;
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            content.gameObject.SetActive(true);

            CurrentTabInstance = content;

            UpdateTabVisualState();

            OnTabSelected(index, content);
        }

        /// <summary>
        /// Tai tao noi dung tab hien tai. Dung de lam moi du lieu sau khi co thay doi.
        /// </summary>
        public void RefreshCurrentTab()
        {
            SelectTab(CurrentTabIndex);
        }
        #endregion

        #region Protected Virtual Hooks
        /// <summary>
        /// Hook duoc goi sau khi noi dung cua tab da duoc cai vao container.
        /// Class con override de gan du lieu / logic cho tab.
        /// </summary>
        /// <param name="index">Chi so cua tab duoc chon.</param>
        /// <param name="contentInstance">Instance noi dung da duoc tao.</param>
        protected virtual void OnTabSelected(int index, RectTransform contentInstance)
        {
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Gan lang nghe su kien click cho moi nu tab.
        /// </summary>
        private void WireTabButtons()
        {
            if (_tabs == null) return;

            for (int i = 0; i < _tabs.Count; i++)
            {
                TabItem tab = _tabs[i];
                if (tab == null || tab.TabButton == null) continue;

                int capturedIndex = i;
                tab.TabButton.onClick.AddListener(() => SelectTab(capturedIndex));
            }
        }

        /// <summary>
        /// Xoa instance noi dung cua tab dang tai thi (neu co) de chuan bi cho tab moi.
        /// </summary>
        private void ClearCurrentTabContent()
        {
            if (CurrentTabInstance != null)
            {
                Destroy(CurrentTabInstance.gameObject);
                CurrentTabInstance = null;
            }
        }

        /// <summary>
        /// Cap nhat mau highlight cho cac tab theo tab dang duoc chon.
        /// </summary>
        private void UpdateTabVisualState()
        {
            if (_tabs == null) return;

            for (int i = 0; i < _tabs.Count; i++)
            {
                TabItem tab = _tabs[i];
                if (tab == null || tab.TabButton == null) continue;

                ColorBlock colors = tab.TabButton.colors;
                colors.normalColor = (i == CurrentTabIndex) ? _selectedTabColor : _deselectedTabColor;
                colors.selectedColor = colors.normalColor;
                colors.highlightedColor = colors.normalColor;
                colors.pressedColor = colors.normalColor;
                tab.TabButton.colors = colors;
            }
        }
        #endregion
    }
}