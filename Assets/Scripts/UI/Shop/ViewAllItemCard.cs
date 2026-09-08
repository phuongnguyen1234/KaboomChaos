using UnityEngine;
using UnityEngine.UI;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// The item trong ViewAllItemPopup: hien thi Icon va tick icon (neu item da duoc so huu).
    /// Dung chung cho Skill trong popup "View all" cua Shop.
    /// </summary>
    public class ViewAllItemCard : MonoBehaviour
    {
        #region Fields
        [Tooltip("Image hien thi icon cua skill.")]
        [SerializeField] private Image _icon;

        [Tooltip("Image/icon tick danh dau skill da duoc so huu. Hien khi so huu, an khi chua so huu.")]
        [SerializeField] private GameObject _tickIcon;

        [Tooltip("Image lam nen (de hieu ung khi da so huu). Tuy chon, de trong neu khong can.")]
        [SerializeField] private Image _background;
        #endregion

        #region Properties
        /// <summary>
        /// ID skill ma the nay dai dien.
        /// </summary>
        public string ItemId { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Cau hinh the: gan icon va trang thai so huu.
        /// </summary>
        /// <param name="skillData">Du lieu skill de hien thi.</param>
        /// <param name="isOwned">True neu nguoi choi da so huu skill nay (hien tick).</param>
        public void Setup(ISkillData skillData, bool isOwned)
        {
            if (skillData == null) return;

            ItemId = skillData.Id;

            if (_icon != null) _icon.sprite = skillData.Icon;
            SetOwned(isOwned);
        }

        /// <summary>
        /// Bat/tat trang thai so huu (hien/an tick).
        /// </summary>
        /// <param name="isOwned">True neu da so huu.</param>
        public void SetOwned(bool isOwned)
        {
            if (_tickIcon != null)
            {
                _tickIcon.SetActive(isOwned);
            }

            // Lam mo icon khi chua so huu, giu nguyen khi da so huu.
            if (_background != null)
            {
                _background.color = isOwned
                    ? new Color(1f, 1f, 1f, 1f)
                    : new Color(0.65f, 0.65f, 0.65f, 0.6f);
            }
        }
        #endregion
    }
}