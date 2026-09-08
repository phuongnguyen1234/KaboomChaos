using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Core;
using Core.Interfaces;
using Random = UnityEngine.Random;

namespace UI
{
    /// <summary>
    /// Popup quay thuong Skill trong Shop. Chay hieu ung slot machine (thay doi icon ngau nhien)
    /// trong 3 giay roi dung lai tai mot Skill bat ky cua nhom, do la Skill nguoi choi nhan duoc.
    /// Hien thi ten + mo ta skill trung thuong va cung cap 2 nut: Equip (trang bi + close) va Close.
    /// </summary>
    public class SkillPurchasePopup : BasePopup
    {
        #region Fields
        [Header("Spin UI")]
        [Tooltip("Image hien thi icon quay slot (thay doi ngau nhien moi frame trong 3 giay).")]
        [SerializeField] private Image _spinImage;

        [Tooltip("Text hien thi ten skill trung thuong.")]
        [SerializeField] private TextMeshProUGUI _skillNameText;

        [Tooltip("Text hien thi mo ta skill trung thuong.")]
        [SerializeField] private TextMeshProUGUI _descriptionText;

        [Tooltip("Button Equip: trang bi skill cho Player roi close popup.")]
        [SerializeField] private Button _equipButton;

        [Tooltip("Data base cung cap danh sach Skill cua nhom de quay (inject qua Inspector).")]
        [SerializeField] private ScriptableObject _skillDatabase;

        [Header("Spin Settings")]
        [SerializeField] private float _spinDuration = 3f;
        [SerializeField] private float _iconChangeInterval = 0.1f;
        #endregion

        #region Events
        /// <summary>
        /// Duoc goi khi popup dong lai (ca Equip va Close) de Shop lam moi lai trang thai hien thi.
        /// </summary>
        public event Action OnClosed;
        #endregion

        #region Fields (Runtime)
        private ISkillDatabase _skillDb;
        private ISkillData _winningSkill;
        private Coroutine _spinCoroutine;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _skillDb = _skillDatabase as ISkillDatabase;

            _equipButton?.onClick.AddListener(OnEquipClicked);
        }

        protected override void OnHidden()
        {
            TryStopSpin();

            // Ca nut Equip lan nut Close deu quay ve Shop: bao Shop lam moi lai (so huu, gia).
            OnClosed?.Invoke();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Bat dau quay thuong cho mot nhom skill. Chay ngau nhien icon 3 giay roi dung tai mot skill bat ky.
        /// </summary>
        /// <param name="type">Loai SkillType can quay.</param>
        public void BeginSpin(SkillType type)
        {
            // Reset trang thai truoc khi quay.
            _winningSkill = null;
            if (_skillNameText != null) _skillNameText.text = string.Empty;
            if (_descriptionText != null) _descriptionText.text = string.Empty;
            if (_equipButton != null) _equipButton.interactable = false;

            Show();

            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
            }
            _spinCoroutine = StartCoroutine(SpinCoroutine(type));
        }
        #endregion

#region Private Methods
        /// <summary>
        /// Coroutine quay slot: thay doi icon ngau nhien cho toi khi het thoi gian, roi dung lai skill trong nhom.
        /// </summary>
        /// <param name="type">SkillType dang quay.</param>
        private IEnumerator SpinCoroutine(SkillType type)
        {
            // Toan bo skill thuoc SkillType nay (dung cho hieu ung quay + neu dat tai skill da so huu van hien thi).
            List<ISkillData> allTypeSkills = GetSkillsOfType(type);
            if (allTypeSkills.Count == 0 || _skillDb == null)
            {
                Debug.LogWarning($"[SkillPurchasePopup] Khong co skill nao thuoc {type}. Khong the quay.");
                yield break;
            }

            // Tap skill CHUA SO HUU - chi nhung skill nay moi duoc trung, tranh tinh trang trung lap khi quay.
            IReadOnlyList<string> ownedIds = GameEvents.TriggerRequestOwnedSkillIds();
            HashSet<string> ownedSet = ownedIds != null ? new HashSet<string>(ownedIds) : new HashSet<string>();

            List<ISkillData> winnableSkills = new();
            foreach (ISkillData skill in allTypeSkills)
            {
                if (skill != null && !ownedSet.Contains(skill.Id))
                {
                    winnableSkills.Add(skill);
                }
            }

            // Da so huu het skill cua SkillType nay -> khong con gi de quay (an toan khi nut Buy khong the bam).
            if (winnableSkills.Count == 0)
            {
                Debug.LogWarning($"[SkillPurchasePopup] Da so huu tat ca skill thuoc {type}. Khong con skill nao de quay.");
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < _spinDuration)
            {
                ISkillData randomSkill = allTypeSkills[Random.Range(0, allTypeSkills.Count)];
                if (_spinImage != null) _spinImage.sprite = randomSkill.Icon;

                yield return new WaitForSeconds(Mathf.Min(_iconChangeInterval, _spinDuration - elapsed));
                elapsed += _iconChangeInterval;
            }

            // Dung lai tai mot skill CHUA SO HUU: khong bao gio trung skill da co.
            _winningSkill = winnableSkills[Random.Range(0, winnableSkills.Count)];

            if (_spinImage != null) _spinImage.sprite = _winningSkill.Icon;
            if (_skillNameText != null) _skillNameText.text = _winningSkill.DisplayName;
            if (_descriptionText != null) _descriptionText.text = _winningSkill.Description;

            // Nguoi choi nhan skill nay (them vao so huu - deduplicate o PlayerDataManager).
            if (!string.IsNullOrEmpty(_winningSkill.Id))
            {
                GameEvents.TriggerAddOwnedSkill(_winningSkill.Id);
            }

            if (_equipButton != null) _equipButton.interactable = true;

            _spinCoroutine = null;
        }

        /// <summary>
        /// Xu ly khi nguoi dung bam nut Equip: trang bi skill trung thuong cho player roi dong.
        /// </summary>
        private void OnEquipClicked()
        {
            EquipWinningSkill();
            CloseAndRefresh();
        }

        /// <summary>
        /// Trang bi skill trung thuong len player hien tai (qua ISkillController).
        /// </summary>
        private void EquipWinningSkill()
        {
            if (_winningSkill == null) return;

            IPlayerManager manager = IPlayerManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning("[SkillPurchasePopup] IPlayerManager.Instance chua khoi tao. Khong the trang bi skill.");
                return;
            }

            IPlayer player = manager.GetCurrentPlayer();
            if (player == null)
            {
                Debug.LogWarning("[SkillPurchasePopup] Khong co Player hien tai. Khong the trang bi skill.");
                return;
            }

            // Dung GetComponentInChildren de an toan neven neu PlayerSkillController dat tren mot child transform.
            ISkillController skillController = player.GameObject.GetComponentInChildren<ISkillController>();
            if (skillController == null)
            {
                Debug.LogWarning("[SkillPurchasePopup] Da co Player nhung khong tim thay ISkillController tren Player (hoac child cua no). " +
                                 "Kiem tra Player prefab da gan PlayerSkillController chua.");
                return;
            }

            skillController.EquipSkill(_winningSkill);

            // Bao hieu HUD cap nhat icon skill dang trang bi.
            GameEvents.TriggerPlayerEquipmentChanged();
        }

        /// <summary>
        /// Dong popup va bao de Shop lam moi lai du lieu hien thi (so huu, gia).
        /// </summary>
        private void CloseAndRefresh()
        {
            // OnClosed duoc phat trong OnHidden nen ca nut Equip lan nut Close
            // deu quay ve Shop va lam moi du lieu hien thi.
            Hide();
        }

        /// <summary>
        /// Dung coroutine quay neu dang chay (an toan khi popup bi dong giua chung).
        /// </summary>
        private void TryStopSpin()
        {
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }
        }

        /// <summary>
        /// Lay toan bo skill thuoc mot nhom tu database.
        /// </summary>
        private List<ISkillData> GetSkillsOfType(SkillType type)
        {
            List<ISkillData> result = new();
            IReadOnlyList<ISkillData> all = _skillDb?.AllSkills;
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