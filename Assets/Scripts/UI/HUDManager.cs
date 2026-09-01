using UnityEngine;
using UnityEngine.UI; // Can cho Slider
using TMPro; // Can cho TextMeshPro
using Core;
using Core.Enums;
using Core.Interfaces;
using System.Collections;

namespace UI
{
    /// <summary>
    /// Quản lý các thành phần của Heads-Up Display (HUD) như thanh máu, năng lượng, v.v.
    /// </summary>
    public class HUDManager : MonoBehaviour
    {
        [Header("Health Components")]
        [Tooltip("Slider hiển thị thanh máu.")]
        [SerializeField] private Slider _healthSlider;
        [Tooltip("Text hiển thị số máu hiện tại.")]
        [SerializeField] private TextMeshProUGUI _healthText;

        [Header("Energy Components")]
        [Tooltip("Slider hiển thị thanh năng lượng.")]
        [SerializeField] private Slider _energySlider;

        [Tooltip("Text hien thi thoi gian sac con lai (kieu '3.5'). Chi hien thi khi dang sac.")]
        [SerializeField] private TextMeshProUGUI _rechargeTimeText;

        [Tooltip("Sprite fill dung khi energy dang trong qua trinh tu sac (gray). NULL thi giu nguyen fill hien tai.")]
        [SerializeField] private Sprite _energyRechargeFillSprite;

        [Header("Equipped Skill & Perk Icons")]
        [Tooltip("Container chứa icon skill đang trang bi. Tu dong an khi khong co skill.")]
        [SerializeField] private GameObject _equippedSkillContainer;
        [Tooltip("Image hien thi icon skill dang trang bi.")]
        [SerializeField] private Image _equippedSkillIcon;
        [Tooltip("Container chua icon perk dang trang bi. Tu dong an khi khong co perk.")]
        [SerializeField] private GameObject _equippedPerkContainer;
        [Tooltip("Image hien thi icon perk dang trang bi.")]
        [SerializeField] private Image _equippedPerkIcon;
        [Tooltip("Sprite mac dinh dung khi skill khong co icon rieng.")]
        [SerializeField] private Sprite _defaultSkillIcon;
        [Tooltip("Sprite mac dinh dung cho perk (neu perk khong co icon hoac chua tra cuu duoc).")]
        [SerializeField] private Sprite _defaultPerkIcon;


        // Cache fill image cua energy slider de doi sprite fill khi dang sac.
        private Image _energyFillImage;
        private Sprite _energyOriginalFillSprite;
        private bool _energyFillCached;
        private bool _energyFillGrayActive;

        [Header("Health Bar")]
        [Tooltip("Image fill phu (secondary bar) cua thanh mau; se keo fillAmount sau 0.5s khi nhan sat thuong.")]
        [SerializeField] private Image _healthSecondaryFill;
        [Tooltip("Thoi gian (giay) doi truoc khi secondary bar bat dau keo ve gia tri moi.")]
        [SerializeField] private float _healthSecondaryDelay = 0.5f;
        [Tooltip("Thoi gian (giay) de secondary bar lerp ve gia tri muc tieu.")]
        [SerializeField] private float _healthSecondaryLerpDuration = 0.3f;
        private Coroutine _healthSecondaryRoutine;

        [Header("Player Attribute Panel")]
        [Tooltip("Panel chi muc cua Player. Chi hien thi khi Player dang trong round (PreRound/RoundActive), an khi khong trong round (o lobby).")]
        [SerializeField] private GameObject _playerAttributePanel;

        // Tranh setActive lap lai moi frame khi trang thai khong doi.
        private bool _attributePanelVisible;

        #region Unity Lifecycle

        private void Awake()
        {
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
            GameEvents.OnPlayerEnergyChanged += HandlePlayerEnergyChanged;
            GameEvents.OnPlayerEquipmentChanged += HandleEquipmentChanged;

            // Dong bo icon trang bi luc HUD bat (vi du: quay lai sau pause).
            RefreshEquippedIcons();

            // Dong bo thanh nang luong + trang thai sac neu HUD duoc bat lai giua chung.
            SyncEnergyDisplay();
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
            GameEvents.OnPlayerEnergyChanged -= HandlePlayerEnergyChanged;
            GameEvents.OnPlayerEquipmentChanged -= HandleEquipmentChanged;
        }

        private void Update()
        {
            // PlayerAttributePanel chi hien khi Player dang trong round (teleport vao arena).
            // An di khi khong trong round (o lobby / cac giai doan khac).
            bool inRound = IsPlayerInRound();
            if (inRound != _attributePanelVisible)
            {
                _attributePanelVisible = inRound;
                if (_playerAttributePanel != null)
                {
                    _playerAttributePanel.SetActive(inRound);
                }
            }
        }

        /// <summary>
        /// Cho biet Player hien tai co dang trong round (PreRound hoac RoundActive - da dua vao arena) hay khong.
        /// </summary>
        private bool IsPlayerInRound()
        {
            IGameStateProvider provider = IGameStateProvider.Instance;
            if (provider == null)
            {
                // Neu chua co GameState (vi du luc khoi dong) thi mac dinh khong hien panel.
                return false;
            }

            return provider.CurrentState == GameState.PreRound || provider.CurrentState == GameState.RoundActive;
        }

        #endregion

        #region Equipped Icons

        /// <summary>
        /// Phản hồi khi trang thái trang bi (skill/perk) của người chơi thay đổi để làm mới icon trên HUD.
        /// </summary>
        private void HandleEquipmentChanged()
        {
            RefreshEquippedIcons();
        }

        /// <summary>
        /// Đồng bộ icon skill/perk đang trang bi từ Player hiện tại.
        /// Tự động ẩn container khi không có skill (hoặc perk) nào đang trang bi.
        /// </summary>
        private void RefreshEquippedIcons()
        {
            IPlayerManager manager = IPlayerManager.Instance;
            IPlayer player = manager?.GetCurrentPlayer();

            // Skill đang trang bi
            ISkillController skillController = player?.GameObject?.GetComponentInChildren<ISkillController>();
            ISkillData currentSkill = skillController?.CurrentSkill;
            bool hasSkill = currentSkill != null;

            if (_equippedSkillContainer != null) _equippedSkillContainer.SetActive(hasSkill);
            if (_equippedSkillIcon != null)
            {
                _equippedSkillIcon.sprite = (currentSkill != null && currentSkill.Icon != null)
                    ? currentSkill.Icon
                    : _defaultSkillIcon;
            }

            // Perk dang trang bi — lay icon truc tiep tu CurrentPerk (tuong tu CurrentSkill).
            IPerkController perkController = player?.GameObject?.GetComponentInChildren<IPerkController>();
            IPerkData currentPerk = perkController?.CurrentPerk;
            bool hasPerk = currentPerk != null;

            if (_equippedPerkContainer != null) _equippedPerkContainer.SetActive(hasPerk);
            if (_equippedPerkIcon != null)
            {
                _equippedPerkIcon.sprite = (currentPerk != null && currentPerk.Icon != null)
                    ? currentPerk.Icon
                    : _defaultPerkIcon;
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePlayerHealthChanged(IPlayer player, float currentHealth, float maxHealth)
        {
            // Trong một game nhiều người chơi, bạn có thể muốn kiểm tra xem 'player'
            // có phải là người chơi cục bộ (local player) hay không trước khi cập nhật HUD.
            // if (player.IsLocalPlayer) { ... }

            UpdateHealth(currentHealth, maxHealth);
        }

        /// <summary>
        /// Cap nhat thanh nang luong tren HUD khi energy cua nguoi choi thay doi.
        /// </summary>
        /// <param name="player">Nguoi choi co energy thay doi.</param>
        /// <param name="currentEnergy">Nang luong hien tai.</param>
        /// <param name="maxEnergy">Nang luong toi da.</param>
        private void HandlePlayerEnergyChanged(IPlayer player, float currentEnergy, float maxEnergy)
        {
            UpdateEnergy(currentEnergy, maxEnergy);
            RefreshRechargeState(player);
        }

        /// <summary>
        /// Lam moi trang thai sac tren HUD: text thoi gian sac con lai (1 chu so thap phan,
        /// chi hien thi khi dang sac) va sprite fill thanh nang luong (GrayFillBar khi dang sac).
        /// </summary>
        private void RefreshRechargeState(IPlayer player)
        {
            IEnergyable energy = player?.GameObject?.GetComponentInChildren<IEnergyable>();
            bool recharging = energy != null && energy.IsRecharging;
            float remaining = energy != null ? energy.RechargeRemaining : 0f;

            if (_rechargeTimeText != null)
            {
                _rechargeTimeText.gameObject.SetActive(recharging);
                if (recharging)
                {
                    _rechargeTimeText.text = remaining.ToString("F1");
                }
            }

            SetEnergyFillSprite(!recharging);
        }

        /// <summary>
        /// Dong bo thanh nang luong voi Player hien tai khi HUD duoc bat lai.
        /// </summary>
        private void SyncEnergyDisplay()
        {
            IPlayer player = IPlayerManager.Instance?.GetCurrentPlayer();
            if (player == null) return;

            IEnergyable energy = player.GameObject?.GetComponentInChildren<IEnergyable>();
            if (energy == null) return;

            UpdateEnergy(energy.CurrentEnergy, energy.MaxEnergy);
            RefreshRechargeState(player);
        }

        /// <summary>
        /// Kich hoat coroutine de keo secondary fill cua thanh mau.
        /// HP giam -> doi _healthSecondaryDelay thi moi lerp xuong (hieu ung 2 thanh);
        /// HP hoi phuc -> dung coroutine, cap nhat fill ngay de khong bi tre.
        /// </summary>
        private void AnimateHealthSecondary(float targetPct)
        {
            if (_healthSecondaryFill == null) return;

            // HP giam: secondary bar se keo sau mot do tre, tao hieu ung "thay doi giua 2 thanh".
            if (targetPct < _healthSecondaryFill.fillAmount)
            {
                if (_healthSecondaryRoutine != null) StopCoroutine(_healthSecondaryRoutine);
                _healthSecondaryRoutine = StartCoroutine(AnimateHealthSecondaryRoutine(targetPct));
                return;
            }

            // HP hoi phuc: dung coroutine dang chay, cap nhat ngay.
            if (_healthSecondaryRoutine != null)
            {
                StopCoroutine(_healthSecondaryRoutine);
                _healthSecondaryRoutine = null;
            }
            _healthSecondaryFill.fillAmount = targetPct;
        }

        /// <summary>
        /// Coroutine: doi _healthSecondaryDelay giay roi lerp fillAmount cua secondary bar
        /// ve targetPct trong _healthSecondaryLerpDuration giay.
        /// </summary>
        private IEnumerator AnimateHealthSecondaryRoutine(float targetPct)
        {
            yield return new WaitForSeconds(_healthSecondaryDelay);

            float startPct = _healthSecondaryFill != null ? _healthSecondaryFill.fillAmount : targetPct;
            float elapsed = 0f;
            while (elapsed < _healthSecondaryLerpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _healthSecondaryLerpDuration);
                if (_healthSecondaryFill != null) _healthSecondaryFill.fillAmount = Mathf.Lerp(startPct, targetPct, t);
                yield return null;
            }

            if (_healthSecondaryFill != null) _healthSecondaryFill.fillAmount = targetPct;
            _healthSecondaryRoutine = null;
        }

        /// <summary>
        /// Doi sprite fill cua thanh nang luong: dung GrayFillBar (_energyRechargeFillSprite)
        /// khi dang sac, quay ve sprite goc khi sac day hoac khong dang sac (ke ca khi dung skill/rut energy).
        /// </summary>
        private void SetEnergyFillSprite(bool useOriginal)
        {
            if (_energySlider == null) return;

            // Cache lan dau: luu sprite goc cua fill image.
            if (!_energyFillCached)
            {
                _energyFillImage = _energySlider.fillRect != null ? _energySlider.fillRect.GetComponent<Image>() : null;
                if (_energyFillImage != null) _energyOriginalFillSprite = _energyFillImage.sprite;
                _energyFillCached = true;
            }

            if (_energyFillImage == null) return;

            // Tranh gan lai cung sprite/gia tri moi frame (su kien energy thay doi moi frame khi sac/rut).
            if (useOriginal && _energyFillGrayActive)
            {
                _energyFillImage.sprite = _energyOriginalFillSprite;
                _energyFillGrayActive = false;
            }
            else if (!useOriginal && !_energyFillGrayActive && _energyRechargeFillSprite != null)
            {
                _energyFillImage.sprite = _energyRechargeFillSprite;
                _energyFillGrayActive = true;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Cập nhật UI thanh máu.
        /// </summary>
        /// <param name="currentHealth">Máu hiện tại.</param>
        /// <param name="maxHealth">Máu tối đa.</param>
        public void UpdateHealth(float currentHealth, float maxHealth)
        {
            if (maxHealth <= 0) return;

            if (_healthSlider != null)
            {
                // Đảm bảo slider được cấu hình đúng với giá trị max, ví dụ 100.
                if (_healthSlider.maxValue != maxHealth) _healthSlider.maxValue = maxHealth;
                // Gán trực tiếp giá trị máu hiện tại, không phải dạng phần trăm (0-1).
                _healthSlider.value = currentHealth;
            }

            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.Clamp(currentHealth, 0, maxHealth)}";
            }

            // Secondary bar: keo sau _healthSecondaryDelay khi HP giam; hoi phuc cap nhat ngay.
            float targetPct = maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;
            AnimateHealthSecondary(targetPct);
        }

        /// <summary>
        /// Cập nhật UI thanh năng lượng.
        /// </summary>
        /// <param name="currentEnergy">Năng lượng hiện tại.</param>
        /// <param name="maxEnergy">Năng lượng tối đa.</param>
        public void UpdateEnergy(float currentEnergy, float maxEnergy)
        {
            if (maxEnergy <= 0) return;

            if (_energySlider != null)
            {
                _energySlider.value = currentEnergy / maxEnergy;
            }
        }

        #endregion
    }
}