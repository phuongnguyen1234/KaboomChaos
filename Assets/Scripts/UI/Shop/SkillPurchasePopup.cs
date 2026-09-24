using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Core;
using Core.Interfaces;
using Core.Utilities;
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

        [Tooltip("CanvasGroup chua 2 nut Equip va Close (an bang alpha = 0 trong qua trinh quay).")]
        [SerializeField] private CanvasGroup _buttonGroupCanvasGroup;

        [Tooltip("Data base cung cap danh sach Skill cua nhom de quay (inject qua Inspector).")]
        [SerializeField] private ScriptableObject _skillDatabase;

        [Header("Spin Settings")]
        [SerializeField] private float _spinDuration = 3f;
        [SerializeField] private float _iconChangeInterval = 0.1f;
        [SerializeField] private float _spinStartScale = 0.4f;
        [SerializeField] private float _spinEndScale = 1.0f;
        [SerializeField] private string _revealPropertyName = "_Reveal";

        [Header("Spin Effects & SFX")]
        [Tooltip("SFX phat 1 lan khi bat dau quay slot machine.")]
        [SerializeField] private AudioClip _spinTickSfx;

        [Tooltip("SFX phat khi quay trung skill.")]
        [SerializeField] private AudioClip _winSkillSfx;

        [Tooltip("Prefab VFX UI hien thi khi quay trung skill.")]
        [SerializeField] private GameObject _winVfxPrefab;

        [Tooltip("Container (RectTransform) cho VFX UI win skill. Neu null se tu dong lay RectTransform cua _spinImage.")]
        [SerializeField] private RectTransform _vfxContainer;

        [Tooltip("So luong VFX instance duoc sinh ra tren UI khi win skill.")]
        [SerializeField] private int _winVfxCount = 3;

        [Tooltip("Khoang cach toi thieu (px) giua cac VFX instance de khong bi chim/sat nhau.")]
        [SerializeField] private float _minVfxDistance = 80f;

        [Tooltip("Thoi gian tu dong thuc hien Destroy cho cac UI VFX instance (giay).")]
        [SerializeField] private float _vfxAutoDestroyDelay = 3f;

        [Header("Star Particles")]
        [Tooltip("Danh sach cac object StarUIParticle phat khi reveal ket qua quay skill.")]
        [SerializeField] private List<StarUIParticle> _starParticles = new();

        [Tooltip("Khoang thoi gian tre (giay) giua cac StarUIParticle duoc kich hoat tuan tu.")]
        [SerializeField] private float _starParticleInterval = 0.15f;

        [Header("Restricted Equip")]
        [Tooltip("Panel thong bao hien thi khi nguoi choi co trang bi skill trong giai doan khong duoc phep (arena).")]
        [SerializeField] private GameObject _restrictedEquipPanel;

        [Tooltip("Thoi gian panel 'Restricted Equip' hien thi truoc khi tu dong an (giay).")]
        [SerializeField] private float _restrictedEquipDuration = 2f;

        [Tooltip("SFX phat khi bi chan trang bi skill trong arena.")]
        [SerializeField] private AudioClip _restrictedEquipSfx;
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
        private AudioSource _spinAudioSource;
        private Coroutine _restrictedEquipCoroutine;
        private Coroutine _starParticlesCoroutine;
        private int _revealPropId;
        #endregion

        #region Unity Lifecycle
        protected override void Awake()
        {
            base.Awake();
            _skillDb = _skillDatabase as ISkillDatabase;
            _revealPropId = Shader.PropertyToID(_revealPropertyName);

            if (_buttonGroupCanvasGroup == null && _equipButton != null)
            {
                _buttonGroupCanvasGroup = _equipButton.GetComponentInParent<CanvasGroup>();
            }

            _equipButton?.onClick.AddListener(OnEquipClicked);
            StopStarParticles();
        }

        protected override void OnHidden()
        {
            TryStopSpin();
            HideRestrictedEquipPanel();
            StopStarParticles();

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
            StopStarParticles();

            if (_buttonGroupCanvasGroup != null)
            {
                _buttonGroupCanvasGroup.DOKill();
                _buttonGroupCanvasGroup.alpha = 0f;
                _buttonGroupCanvasGroup.interactable = false;
                _buttonGroupCanvasGroup.blocksRaycasts = false;
            }

            ShowInstant();

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

            // SFX phat 1 lan duy nhat khi bat dau quay slot machine.
            if (_spinTickSfx != null && SfxService.Instance != null)
            {
                _spinAudioSource = SfxService.Instance.PlaySfx(_spinTickSfx, 0.6f);
            }

            if (_spinImage != null)
            {
                if (_spinImage.material != null)
                {
                    _spinImage.material.SetFloat(_revealPropId, 0f);
                }
                _spinImage.transform.localScale = Vector3.one * _spinStartScale;
            }

            ISkillData lastSkill = null;
            float elapsed = 0f;
            while (elapsed < _spinDuration)
            {
                float progress = Mathf.Clamp01(elapsed / _spinDuration);
                float currentScale = Mathf.Lerp(_spinStartScale, _spinEndScale, progress);

                ISkillData randomSkill = allTypeSkills[Random.Range(0, allTypeSkills.Count)];
                if (allTypeSkills.Count > 1)
                {
                    while (randomSkill == lastSkill)
                    {
                        randomSkill = allTypeSkills[Random.Range(0, allTypeSkills.Count)];
                    }
                }
                lastSkill = randomSkill;

                if (_spinImage != null)
                {
                    _spinImage.sprite = randomSkill.Icon;
                    _spinImage.transform.localScale = Vector3.one * currentScale;
                }

                yield return new WaitForSeconds(Mathf.Min(_iconChangeInterval, _spinDuration - elapsed));
                elapsed += _iconChangeInterval;
            }

            _spinAudioSource = null;

            // Dung lai tai mot skill CHUA SO HUU: khong bao gio trung skill da co.
            _winningSkill = winnableSkills[Random.Range(0, winnableSkills.Count)];

            if (_spinImage != null)
            {
                if (_spinImage.material != null)
                {
                    _spinImage.material.SetFloat(_revealPropId, 1f);
                }
                _spinImage.sprite = _winningSkill.Icon;
                _spinImage.transform.DOKill();
                _spinImage.transform.localScale = Vector3.one * _spinStartScale;

                Sequence winSeq = DOTween.Sequence().SetUpdate(true);
                winSeq.Append(_spinImage.transform.DOScale(Vector3.one * 1.35f, 0.3f).SetEase(Ease.OutBack));
                winSeq.Append(_spinImage.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.InOutQuad));

                SpawnWinVfxOnUI();
                PlayStarParticles();
            }

            if (_winSkillSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_winSkillSfx);
            }

            if (_skillNameText != null) _skillNameText.text = _winningSkill.DisplayName;
            if (_descriptionText != null) _descriptionText.text = _winningSkill.Description;

            // Nguoi choi nhan skill nay (them vao so huu - deduplicate o PlayerDataManager).
            if (!string.IsNullOrEmpty(_winningSkill.Id))
            {
                GameEvents.TriggerAddOwnedSkill(_winningSkill.Id);
            }

            if (_equipButton != null) _equipButton.interactable = true;

            if (_buttonGroupCanvasGroup != null)
            {
                _buttonGroupCanvasGroup.DOKill();
                _buttonGroupCanvasGroup.DOFade(1f, 0.3f).SetUpdate(true);
                _buttonGroupCanvasGroup.interactable = true;
                _buttonGroupCanvasGroup.blocksRaycasts = true;
            }

            _spinCoroutine = null;
        }

        /// <summary>
        /// Xu ly khi nguoi dung bam nut Equip: trang bi skill trung thuong cho player roi dong.
        /// Neu dang trong arena, hien panel thong bao + phat SFX va khong cho phep trang bi.
        /// </summary>
        private void OnEquipClicked()
        {
            if (RoundStateHelper.IsInRound())
            {
                ShowRestrictedEquipPanel();
                return;
            }

            EquipWinningSkill();
            CloseAndRefresh();
        }

        /// <summary>
        /// Hien thi panel thong bao 'khong the trang bi trong arena' va phat SFX.
        /// </summary>
        private void ShowRestrictedEquipPanel()
        {
            if (_restrictedEquipSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_restrictedEquipSfx);
            }

            if (_restrictedEquipPanel == null) return;

            _restrictedEquipPanel.SetActive(true);

            if (_restrictedEquipCoroutine != null)
            {
                StopCoroutine(_restrictedEquipCoroutine);
                _restrictedEquipCoroutine = null;
            }

            _restrictedEquipCoroutine = StartCoroutine(HideRestrictedEquipPanelAfterDelay());
        }

        private IEnumerator HideRestrictedEquipPanelAfterDelay()
        {
            yield return new WaitForSeconds(_restrictedEquipDuration);
            _restrictedEquipCoroutine = null;

            if (_restrictedEquipPanel != null)
            {
                _restrictedEquipPanel.SetActive(false);
            }
        }

        private void HideRestrictedEquipPanel()
        {
            if (_restrictedEquipCoroutine != null)
            {
                StopCoroutine(_restrictedEquipCoroutine);
                _restrictedEquipCoroutine = null;
            }

            if (_restrictedEquipPanel != null)
            {
                _restrictedEquipPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Trang bi skill trung thuong len player hien tai (qua ISkillController).
        /// </summary>
        private void EquipWinningSkill()
        {
            if (_winningSkill == null) return;

            if (RoundStateHelper.IsInRound())
            {
                Debug.LogWarning("[SkillPurchasePopup] Nguoi choi dang trong arena/round. Khong the trang bi skill.");
                return;
            }

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
            if (_buttonGroupCanvasGroup != null)
            {
                _buttonGroupCanvasGroup.DOKill();
                _buttonGroupCanvasGroup.alpha = 1f;
                _buttonGroupCanvasGroup.interactable = true;
                _buttonGroupCanvasGroup.blocksRaycasts = true;
            }

            if (_spinAudioSource != null)
            {
                _spinAudioSource.Stop();
                _spinAudioSource = null;
            }

            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
                _spinCoroutine = null;
            }

            StopStarParticles();
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

        /// <summary>
        /// Tao cac instance VFX ngau nhien tren UI container khi win skill.
        /// Cac vi tri duoc dam bao khong bi sat nhau (cach nhau toi thieu _minVfxDistance).
        /// </summary>
        private void SpawnWinVfxOnUI()
        {
            if (_winVfxPrefab == null) return;

            RectTransform container = _vfxContainer != null ? _vfxContainer : (_spinImage != null ? _spinImage.rectTransform : null);
            if (container == null) return;

            Rect rect = container.rect;
            float paddingX = Mathf.Min(20f, rect.width * 0.1f);
            float paddingY = Mathf.Min(20f, rect.height * 0.1f);

            float minX = rect.xMin + paddingX;
            float maxX = rect.xMax - paddingX;
            float minY = rect.yMin + paddingY;
            float maxY = rect.yMax - paddingY;

            if (minX > maxX) minX = maxX = rect.center.x;
            if (minY > maxY) minY = maxY = rect.center.y;

            List<Vector2> spawnedPositions = new();

            for (int i = 0; i < _winVfxCount; i++)
            {
                Vector2 candidatePos = Vector2.zero;

                for (int attempt = 0; attempt < 30; attempt++)
                {
                    float rx = Random.Range(minX, maxX);
                    float ry = Random.Range(minY, maxY);
                    Vector2 pos = new Vector2(rx, ry);

                    bool isFarEnough = true;
                    foreach (Vector2 existingPos in spawnedPositions)
                    {
                        if (Vector2.Distance(pos, existingPos) < _minVfxDistance)
                        {
                            isFarEnough = false;
                            break;
                        }
                    }

                    if (isFarEnough)
                    {
                        candidatePos = pos;
                        break;
                    }

                    candidatePos = pos;
                }

                spawnedPositions.Add(candidatePos);

                GameObject vfxInstance = Instantiate(_winVfxPrefab, container, false);
                RectTransform vfxRect = vfxInstance.GetComponent<RectTransform>();
                if (vfxRect != null)
                {
                    vfxRect.anchoredPosition = candidatePos;
                }
                else
                {
                    vfxInstance.transform.localPosition = candidatePos;
                }

                if (_vfxAutoDestroyDelay > 0f)
                {
                    Destroy(vfxInstance, _vfxAutoDestroyDelay);
                }
            }
        }

        /// <summary>
        /// Kich hoat va phat cac doi tuong StarUIParticle theo thu tu trong list, cach nhau mot khoang thoi gian.
        /// </summary>
        private void PlayStarParticles()
        {
            if (_starParticles == null || _starParticles.Count == 0) return;

            if (_starParticlesCoroutine != null)
            {
                StopCoroutine(_starParticlesCoroutine);
                _starParticlesCoroutine = null;
            }

            _starParticlesCoroutine = StartCoroutine(PlayStarParticlesCoroutine());
        }

        private IEnumerator PlayStarParticlesCoroutine()
        {
            for (int i = 0; i < _starParticles.Count; i++)
            {
                var star = _starParticles[i];
                if (star != null)
                {
                    star.gameObject.SetActive(true);
                    star.Play();
                }

                if (i < _starParticles.Count - 1 && _starParticleInterval > 0f)
                {
                    yield return new WaitForSeconds(_starParticleInterval);
                }
            }

            _starParticlesCoroutine = null;
        }

        /// <summary>
        /// Dung va an cac doi tuong StarUIParticle da gan.
        /// </summary>
        private void StopStarParticles()
        {
            if (_starParticlesCoroutine != null)
            {
                StopCoroutine(_starParticlesCoroutine);
                _starParticlesCoroutine = null;
            }

            if (_starParticles != null)
            {
                foreach (var star in _starParticles)
                {
                    if (star != null)
                    {
                        star.Stop();
                        star.gameObject.SetActive(false);
                    }
                }
            }
        }
        #endregion
    }
}