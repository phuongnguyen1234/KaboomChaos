using System;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// The bieu dien mot item trong Inventory (la Button voi object con Image Icon).
    /// Hien thi them image danh dau trang thai dang duoc trang bi (equip) hay khong.
    /// Khi click se phat su kien OnClicked de cap nhat InfoPanel ben ngoai.
    /// Dung chung cho Skill va Perk.
    /// </summary>
    public class ItemButtonCard : MonoBehaviour
    {
        #region Fields
        [Tooltip("Image (object con) dung de hien thi Icon cua item.")]
        [SerializeField] private Image _iconImage;

        [Tooltip("Image danh dau item dang duoc trang bi. Se bat/tat khi trang thai thay doi.")]
        [SerializeField] private Image _equipMarkerImage;

        [Tooltip("Button cua the. Neu khong gan se tu tim component Button tren GameObject nay.")]
        [SerializeField] private Button _button;
        #endregion

        #region Properties
        /// <summary>
        /// ID cua item (Skill hoac Perk) ma the nay dai dien.
        /// </summary>
        public string ItemId { get; private set; }

        /// <summary>
        /// Su kien xay ra khi nguoi dung bam vao the item.
        /// </summary>
        public event Action<ItemButtonCard> OnClicked;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }

            if (_button != null)
            {
                _button.onClick.AddListener(OnButtonClicked);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Setup the item: gan icon, ID va trang thai trang bi.
        /// </summary>
        /// <param name="itemId">ID cua item.</param>
        /// <param name="icon">Sprite icon (co the null neu khong co).</param>
        /// <param name="isEquipped">True neu item dang duoc trang bi.</param>
        public void Setup(string itemId, Sprite icon, bool isEquipped)
        {
            ItemId = itemId;
            SetIcon(icon);
            SetEquipped(isEquipped);
        }

        /// <summary>
        /// Gan icon hien thi cho the.
        /// </summary>
        /// <param name="icon">Sprite icon can hien thi.</param>
        public void SetIcon(Sprite icon)
        {
            if (_iconImage != null)
            {
                _iconImage.sprite = icon;
            }
        }

        /// <summary>
        /// Bat/tat image danh dau trang thai dang duoc trang bi.
        /// </summary>
        /// <param name="isEquipped">True de hien marker (dang trang bi).</param>
        public void SetEquipped(bool isEquipped)
        {
            if (_equipMarkerImage != null)
            {
                _equipMarkerImage.gameObject.SetActive(isEquipped);
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Khi bam vao the, phat su kien OnClicked.
        /// </summary>
        private void OnButtonClicked()
        {
            OnClicked?.Invoke(this);
        }
        #endregion
    }
}