using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Economy;
using Core.Interfaces;
using Random = UnityEngine.Random;

namespace UI
{
    /// <summary>
    /// Noi dung cua tab Skills trong Shop: vertical layout chua cac SkillTypeCard
    /// (mot card cho moi nhom skill co ton tai skill). Dung du lieu skill tu ISkillDatabase
    /// va cau hinh kinh te (SkillTypeConfig) de tinh gia tensor da so huu / tong.
    /// </summary>
    public class ShopSkillsView : MonoBehaviour
    {
        #region Fields
        [Tooltip("Prefab SkillTypeCard duoc nhan ban cho moi SkillType.")]
        [SerializeField] private SkillTypeCard _cardPrefab;

        [Tooltip("Container (vertical layout) chua cac SkillTypeCard.")]
        [SerializeField] private RectTransform _cardsContainer;

        [Tooltip("Data base cung cap danh sach Skill (inject qua Inspector).")]
        [SerializeField] private ScriptableObject _skillDatabase;

        [Tooltip("Config kinh te (gia + he so tang) cua cac SkillType.")]
        [SerializeField] private SkillTypeConfig _typeConfig;
        #endregion

        #region Unity Lifecycle
        private ISkillDatabase _skillDb;

        private void Awake()
        {
            _skillDb = _skillDatabase as ISkillDatabase;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Populate cac SkillType: tao mot SkillTypeCard cho moi SkillType co it nhat 1 skill.
        /// </summary>
        /// <param name="ownedSkillIds">Tap ID skill nguoi choi dang so huu.</param>
        /// <param name="spinCount">Tong so lan quay da mua (de tinh gia).</param>
        /// <param name="onBuy">Callback khi nguoi dung bam nut Buy cua mot nhom.</param>
        /// <param name="onViewAll">Callback khi nguoi dung bam nut 'View all' cua mot nhom. Nhan SkillType cua nhom do.</param>
        public void Populate(IReadOnlyList<string> ownedSkillIds, int spinCount, Action<SkillType> onBuy, Action<SkillType> onViewAll)
        {
            Clear();

            if (_skillDb == null || _cardPrefab == null || _cardsContainer == null)
            {
                Debug.LogWarning("[ShopSkillsView] Thieu _skillDatabase, _cardPrefab hoac _cardsContainer.");
                return;
            }

            // Chuyen danh sach so huu sang tap hop cho tra cuu nhanh.
            HashSet<string> ownedSet = new(ownedSkillIds ?? Array.Empty<string>());

            foreach (SkillType type in System.Enum.GetValues(typeof(SkillType)))
            {
                // Lay toan bo skill thuoc nhom nay.
                List<ISkillData> groupSkills = GetSkillsOfType(type);
                if (groupSkills.Count == 0) continue;

                // Dem so skill da so huu trong nhom.
                int ownedCount = 0;
                foreach (ISkillData skill in groupSkills)
                {
                    if (skill != null && ownedSet.Contains(skill.Id))
                    {
                        ownedCount++;
                    }
                }

                // Icon: chon ngau nhien mot skill trong nhom lam dai dien.
                Sprite icon = groupSkills[Random.Range(0, groupSkills.Count)].Icon;

                int price = _typeConfig != null ? _typeConfig.GetPrice(type, spinCount) : 0;

                SkillTypeCard card = Instantiate(_cardPrefab, _cardsContainer);
                card.Setup(type, icon, ownedCount, groupSkills.Count, price, () => onBuy?.Invoke(type), () => onViewAll?.Invoke(type));
            }
        }

        /// <summary>
        /// Xoa toan bo cac SkillTypeCard dang hien thi.
        /// </summary>
        public void Clear()
        {
            if (_cardsContainer == null) return;

            for (int i = _cardsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_cardsContainer.GetChild(i).gameObject);
            }
        }

        /// <summary>
        /// Lay toan bo skill thuoc mot nhom (theo SkillType) tu database.
        /// </summary>
        private List<ISkillData> GetSkillsOfType(SkillType type)
        {
            List<ISkillData> result = new();
            IReadOnlyList<ISkillData> all = _skillDb.AllSkills;
            if (all == null) return result;

            foreach (ISkillData skill in all)
            {
                if (skill != null && skill.Type == type)
                {
                    result.Add(skill);
                }
            }
            return result;
        }
        #endregion
    }
}