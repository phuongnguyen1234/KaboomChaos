using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// The perk duoc ban truc tiep trong Shop: hien thi Icon, ten, mo ta va gia.
    /// Gia duoc hien thi tren nut Buy (D). Khi bam nut se phat su kien onBuy.
    /// </summary>
    public class ShopPerkCard : MonoBehaviour
    {
        #region Fields
        [Tooltip("Image Icon cua perk.")]
        [SerializeField] private Image _icon;

        [Tooltip("Text hien thi ten perk.")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("Text hien thi mo ta cua perk.")]
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Tooltip("Nut Buy perk. Text cua nut hien thi gia.")]
        [SerializeField] private Button _buyButton;

        [Tooltip("Text cua nut Buy hien thi gia (vi du 100).")]
        [SerializeField] private TextMeshProUGUI _buyButtonText;
        #endregion

        #region Properties
        /// <summary>
        /// Du lieu perk ma the nay dai dien.
        /// </summary>
        public IPerkData PerkData { get; private set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Cau hinh the perk.
        /// </summary>
        /// <param name="perkData">Du lieu perk can hien thi.</param>
        /// <param name="onBuy">Callback khi nguoi dung bam nut Buy.</param>
        public void Setup(IPerkData perkData, Action onBuy)
        {
            PerkData = perkData;
            if (perkData == null) return;

            if (_icon != null) _icon.sprite = perkData.Icon;
            if (_titleText != null) _titleText.text = perkData.DisplayName;
            if (_descriptionText != null) _descriptionText.text = perkData.Description;

            if (_buyButtonText != null)
            {
                _buyButtonText.text = perkData.Price.ToString();
            }

            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveAllListeners();
                _buyButton.onClick.AddListener(() => onBuy?.Invoke());
            }
        }
        #endregion
    }
}