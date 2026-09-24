using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Noi dung cua tab Skills: bo tri theo vertical layout, moi SkillType la mot SkillTypeSection.
    /// Cac skill duoc nhom theo SkillType (Movement / Defensive).
    /// Gan len prefab noi dung cua tab Skills (co san mot VerticalLayoutGroup chua cac SkillTypeSection).
    /// </summary>
    public class SkillsContentView : MonoBehaviour
    {
        #region Fields
        [Tooltip("Prefab SkillTypeSection duoc nhan ban cho moi SkillType.")]
        [SerializeField] private SkillTypeSection _sectionPrefab;

        [Tooltip("Container (vertical layout) chua cac SkillTypeSection duoc tao ra.")]
        [SerializeField] private RectTransform _sectionsContainer;

        [Tooltip("(Tuy chon) Text hien thi khi nguoi choi chua so huu skill nao. De trong neu khong can.")]
        [SerializeField] private TextMeshProUGUI _emptyStateText;
        #endregion

        #region Public Methods
        /// <summary>
        /// Nap danh sach skill so huu vao UI: nhom theo SkillType va tao SkillTypeSection tuong ung.
        /// Chi tao nhom khi co it nhat mot skill thuoc loai do (tranh group trong).
        /// Neu khong co skill nao se hien thi _emptyStateText (neu co cau hinh).
        /// </summary>
        /// <param name="ownedSkills">Danh sach skill nguoi choi dang so huu.</param>
        /// <param name="equippedSkillIds">Tap ID cac skill dang duoc trang bi.</param>
        /// <param name="onSelect">Callback khi nguoi dung bam vao mot the skill.</param>
        public void Populate(IReadOnlyList<ISkillData> ownedSkills, HashSet<string> equippedSkillIds, Action<ISkillData> onSelect)
        {
            Clear();

            if (ownedSkills == null || _sectionPrefab == null || _sectionsContainer == null)
            {
                Debug.LogWarning("[SkillsContentView] Thieu du lieu de populate (ownedSkills, _sectionPrefab hoac _sectionsContainer).");
                return;
            }

            bool hasAnySkill = false;

            // Duyet theo thu tu enum de cac nhom duoc tao theo thu tu on dinh (Movement truoc, Defensive sau).
            foreach (SkillType type in Enum.GetValues(typeof(SkillType)))
            {
                SkillTypeSection section = null;

                foreach (ISkillData skill in ownedSkills)
                {
                    if (skill == null || skill.Type != type) continue;

                    hasAnySkill = true;

                    // Chi tao section khi co it nhat mot skill thuoc SkillType nay.
                    if (section == null)
                    {
                        section = Instantiate(_sectionPrefab, _sectionsContainer);
                        section.Setup(type.ToString());
                    }

                    bool isEquipped = equippedSkillIds != null && equippedSkillIds.Contains(skill.Id);
                    section.AddSkill(skill, isEquipped, onSelect);
                }
            }

            // Neu khong co skill nao, hien thi thong bao (neu duoc cau hinh).
            if (_emptyStateText != null)
            {
                _emptyStateText.gameObject.SetActive(!hasAnySkill);
            }
        }

        /// <summary>
        /// Xoa toan bo cac SkillTypeSection dang hien thi de tao lai tu dau.
        /// </summary>
        public void Clear()
        {
            if (_sectionsContainer == null) return;

            for (int i = _sectionsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_sectionsContainer.GetChild(i).gameObject);
            }
        }
        #endregion
    }
}