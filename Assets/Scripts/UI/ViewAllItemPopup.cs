using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Core.Interfaces;

namespace UI
{
    /// <summary>
    /// Popup "View all" cho mot nhom Skill trong Shop. Tieu de la "{TenNhom} Skills".
    /// Noi dung la ScrollRect + GridLayout chua cac ViewAllItemCard (icon + tick icon).
    /// Neu item da so huu thi hien tick icon tren card.
    /// </summary>
    public class ViewAllItemPopup : BasePopup
    {
        #region Fields
        [Tooltip("Text hien thi tieu de popup, vi du: 'Movement Skills'.")]
        [SerializeField] private TextMeshProUGUI _titleText;

        [Tooltip("Prefab ViewAllItemCard duoc nhan ban cho moi skill trong nhom.")]
        [SerializeField] private ViewAllItemCard _itemCardPrefab;

        [Tooltip("Content (GridLayoutGroup) ben trong ScrollRect chua cac ViewAllItemCard.")]
        [SerializeField] private RectTransform _itemsContainer;

        [Tooltip("Data base cung cap danh sach Skill (inject qua Inspector). UI khong the reference assembly Skills nen inject qua ScriptableObject.")]
        [SerializeField] private ScriptableObject _skillDatabase;
        #endregion

        #region Runtime
        private ISkillDatabase _skillDb;
        private SkillType _currentType;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _skillDb = _skillDatabase as ISkillDatabase;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Mo popup "View all" cho mot nhom skill: hien thi toan bo skill thuoc nhom,
        /// danh dau tick tren cac skill nguoi choi da so huu.
        /// </summary>
        /// <param name="type">Loai SkillType cua nhom can xem.</param>
        /// <param name="ownedSkillIds">Danh sach ID skill nguoi choi dang so huu.</param>
        public void Open(SkillType type, IReadOnlyList<string> ownedSkillIds)
        {
            _currentType = type;

            if (_titleText != null)
            {
                _titleText.text = $"{type} Skills";
            }

            BuildGrid(ownedSkillIds);

            Show();
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Tao cac ViewAllItemCard cho tat ca skill thuoc nhom hien tai.
        /// </summary>
        /// <param name="ownedSkillIds">Danh sach ID skill dang so huu de danh dau tick.</param>
        private void BuildGrid(IReadOnlyList<string> ownedSkillIds)
        {
            Clear();

            if (_skillDb == null || _itemCardPrefab == null || _itemsContainer == null)
            {
                Debug.LogWarning("[ViewAllItemPopup] Thieu _skillDatabase, _itemCardPrefab hoac _itemsContainer.", this);
                return;
            }

            HashSet<string> ownedSet = new(ownedSkillIds ?? Array.Empty<string>());

            IReadOnlyList<ISkillData> all = _skillDb.AllSkills;
            if (all == null) return;

            foreach (ISkillData skill in all)
            {
                if (skill == null || skill.Type != _currentType) continue;

                ViewAllItemCard card = Instantiate(_itemCardPrefab, _itemsContainer);
                card.Setup(skill, ownedSet.Contains(skill.Id));
            }
        }

        /// <summary>
        /// Xoa cac item card dang hien thi.
        /// </summary>
        public void Clear()
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