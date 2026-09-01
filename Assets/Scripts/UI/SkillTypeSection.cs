using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Mot nhom skill trong tab Skills: gom Text GroupName va mot GridLayout
    /// chua cac phan tu ItemButtonCard. Moi skill so huu se them mot ItemButtonCard vao grid.
    /// </summary>
    public class SkillTypeSection : MonoBehaviour
    {
        #region Fields
        [Tooltip("Text hien thi ten nhom (vi du: Movement / Defensive).")]
        [SerializeField] private TextMeshProUGUI _nameText;

        [Tooltip("GridLayout dung de sap xep cac ItemButtonCard cua nhom.")]
        [SerializeField] private GridLayoutGroup _gridLayout;

        [Tooltip("Prefab ItemButtonCard duoc nhan ban cho moi skill trong nhom.")]
        [SerializeField] private ItemButtonCard _itemCardPrefab;
        #endregion

        #region Public Methods
        /// <summary>
        /// Gan ten hien thi cho nhom.
        /// </summary>
        /// <param name="groupName">Ten nhom (vi du: Movement).</param>
        public void Setup(string name)
        {
            if (_nameText != null)
            {
                _nameText.text = name;
            }
        }

        /// <summary>
        /// Them mot skill vao grid bang cach tao mot ItemButtonCard.
        /// </summary>
        /// <param name="skillData">Du lieu skill can them.</param>
        /// <param name="isEquipped">True neu skill dang duoc trang bi.</param>
        /// <param name="onSelect">Callback khi nguoi dung bam vao the skill nay.</param>
        public void AddSkill(ISkillData skillData, bool isEquipped, Action<ISkillData> onSelect)
        {
            if (_gridLayout == null || _itemCardPrefab == null)
            {
                Debug.LogWarning("[SkillTypeSection] Thieu _gridLayout hoac _itemCardPrefab. Khong the them skill.");
                return;
            }

            if (skillData == null) return;

            ItemButtonCard card = Instantiate(_itemCardPrefab, _gridLayout.transform);
            card.Setup(skillData.Id, skillData.Icon, isEquipped);

            // Khi click the, thong bao len inventory popup de hien thi thong tin + equip.
            card.OnClicked += _ => onSelect?.Invoke(skillData);
        }
        #endregion
    }
}