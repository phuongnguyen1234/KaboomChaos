using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// The nhom skill trong Shop: gom icon (random skill cua nhom), ten nhom,
    /// text so huu/tong ("2/6") va nut Buy (text la gia). Neu da so huu het thi an nut Buy.
    /// </summary>
    public class SkillTypeCard : MonoBehaviour
    {
        #region Fields
        [Tooltip("Image Icon (lay tu mot skill ngau nhien cua nhom).")]
        [SerializeField] private Image _icon;

        [Tooltip("Text hien thi ten nhom (vi du: Movement).")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("Text hien thi so skill so huu tren tong so skill cua nhom (vi du 2/6).")]
        [SerializeField] private TextMeshProUGUI _progressText;

        [Tooltip("Nut Buy. Bi an khi da so huu het toan bo skill cua nhom.")]
        [SerializeField] private Button _buyButton;

        [Tooltip("Text cua nut Buy hien thi gia (vi du 100).")]
        [SerializeField] private TextMeshProUGUI _buyButtonText;

        [Tooltip("Panel 'Sold Out' hien thi khi da so huu het toan bo skill cua nhom.")]
        [SerializeField] private GameObject _soldOutPanel;

        [Tooltip("Nut/Text 'View all' (hoat dong nhu hyperlink): bam de mo ViewAllItemPopup hien thi tat ca skill cua nhom.")]
        [SerializeField] private Button _viewAllButton;
        #endregion

        #region Properties
        /// <summary>
        /// Loai nhom skill cua the nay.
        /// </summary>
        public SkillType Type { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Cau hinh the nhom skill.
        /// </summary>
        /// <param name="type">Loai SkillType.</param>
        /// <param name="icon">Icon hien thi cua nhom.</param>
        /// <param name="owned">So skill da so huu.</param>
        /// <param name="total">Tong Skill cua nhom.</param>
        /// <param name="price">Gia de mua mot lan quay.</param>
        /// <param name="onBuy">Callback khi nguoi dung bam nut Buy.</param>
        /// <param name="onViewAll">Callback khi nguoi dung bam nut 'View all'.</param>
        public void Setup(SkillType type, Sprite icon, int owned, int total, int price, Action onBuy, Action onViewAll)
        {
            Type = type;

            if (_icon != null) _icon.sprite = icon;
            if (_titleText != null) _titleText.text = type.ToString();
            if (_progressText != null) _progressText.text = $"{owned}/{total}";

            // Da so huu het toan bo skill cua nhom -> an nut Buy.
            bool allOwned = total > 0 && owned >= total;

            if (_buyButton != null)
            {
                _buyButton.gameObject.SetActive(!allOwned);
                if (!allOwned)
                {
                    _buyButton.onClick.RemoveAllListeners();
                    _buyButton.onClick.AddListener(() => onBuy?.Invoke());
                }
            }

            if (_buyButtonText != null)
            {
                _buyButtonText.text = price.ToString();
            }

            // Da so huu het -> bat panel 'Sold Out' len de bao hieu het hang.
            if (_soldOutPanel != null)
            {
                _soldOutPanel.SetActive(allOwned);
            }

            // Wire nut 'View all' (hyperlink): luon kha dung, bat thuong da so huu het.
            if (_viewAllButton != null)
            {
                _viewAllButton.onClick.RemoveAllListeners();
                _viewAllButton.onClick.AddListener(() => onViewAll?.Invoke());
            }
        }
        #endregion
    }
}