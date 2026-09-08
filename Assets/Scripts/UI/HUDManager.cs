using UnityEngine;
using UnityEngine.UI; // Can cho Slider
using TMPro; // Can cho TextMeshPro
using Core;
using Core.Enums;
using Core.Interfaces;
using System.Collections;
using DG.Tweening;

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

        [Header("Energy Notification")]
        [Tooltip("Text hien thi thong bao 'Charge Full' tren UI khi nhap Battery luc energy dang day. Tu dong an sau 2 giay.")]
        [SerializeField] private TextMeshProUGUI _chargeFullText;

        [Tooltip("Thoi gian (giay) hien thi thong bao 'Charge Full' tren UI truoc khi tu dong an.")]
        [SerializeField] private float _chargeFullTextDuration = 2f;

        [Tooltip("Am thanh (SFX) phat khi energy dong sac day tu dong (bao hieu da du 100% energy).")]
        [SerializeField] private AudioClip _energyFullChargedSfx;

        [Tooltip("Icon Energy tren UI se pulse scale khi energy day (de nguoi choi nhan biet). Neu bo trong, dung fillRect cua energy slider.")]
        [SerializeField] private RectTransform _energyIconTransform;

        [Tooltip("Thoi gian (giay) pulse scale icon Energy.")]
        [SerializeField] private float _energyPulseDuration = 0.3f;

        [Tooltip("Muc do phong to (ti le) khi pulse icon Energy.")]
        [SerializeField] private float _energyPulseScaleMultiplier = 1.15f;

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

        // AudioSource de phat SFX thong bao (tu tim tren GameObject HUD).
        private AudioSource _audioSource;
        // Coroutine an thong bao 'Charge Full' sau thoi gian.
        private Coroutine _chargeFullTextRoutine;
        private RectTransform _energyPulseTarget;

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

        [Header("Extreme Mode")]
        [Tooltip("Text hien thi trang thai Extreme Mode tren HUD (vi du: 'EXTREME').")]
        [SerializeField] private TextMeshProUGUI _extremeModeText;
        [Tooltip("Icon hien thi trang thai Extreme Mode tren HUD.")]
        [SerializeField] private Image _extremeModeIcon;

        #region Unity Lifecycle

        private void Awake()
        {
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }
            _energyPulseTarget = _energyIconTransform != null ? _energyIconTransform : (_energySlider != null ? _energySlider.fillRect : null);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
            GameEvents.OnPlayerEnergyChanged += HandlePlayerEnergyChanged;
            GameEvents.OnPlayerEquipmentChanged += HandleEquipmentChanged;
            GameEvents.OnBatteryCollectibleRefused += HandleBatteryCollectibleRefused;
            GameEvents.OnPlayerEnergyFullyCharged += HandlePlayerEnergyFullyCharged;
            GameEvents.OnExtremeModeStateChanged += HandleExtremeModeChanged;

            // Dong bo icon trang bi luc HUD bat (vi du: quay lai sau pause).
            RefreshEquippedIcons();

            // Khoi phuc trang thai Extreme Mode khi HUD bat (da luu tru).
            RefreshExtremeModeIndicator();

            // Dong bo thanh nang luong + trang thai sac neu HUD duoc bat lai giua chung.
            SyncEnergyDisplay();
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
            GameEvents.OnPlayerEnergyChanged -= HandlePlayerEnergyChanged;
            GameEvents.OnPlayerEquipmentChanged -= HandleEquipmentChanged;
            GameEvents.OnBatteryCollectibleRefused -= HandleBatteryCollectibleRefused;
            GameEvents.OnPlayerEnergyFullyCharged -= HandlePlayerEnergyFullyCharged;
            GameEvents.OnExtremeModeStateChanged -= HandleExtremeModeChanged;
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

        #region Extreme Mode

        /// <summary>
        /// Phan hoi khi trang thai Extreme Mode thay doi: dong bo indicator (text + icon) tren HUD.
        /// </summary>
        /// <param name="enabled">True neu Extreme Mode dang bat.</param>
        private void HandleExtremeModeChanged(bool enabled)
        {
            RefreshExtremeModeIndicator();
        }

        /// <summary>
        /// Cap nhat indicator Extreme Mode tren HUD: hien/an text + icon theo trang thai hien tai (da luu tru).
        /// </summary>
        private void RefreshExtremeModeIndicator()
        {
            bool enabled = GameEvents.TriggerRequestExtremeModeEnabled();

            if (_extremeModeText != null)
            {
                _extremeModeText.gameObject.SetActive(enabled);
                _extremeModeText.text = enabled ? "EXTREME" : string.Empty;
            }

            if (_extremeModeIcon != null)
            {
                _extremeModeIcon.gameObject.SetActive(enabled);
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
        /// Xu ly khi nguoi choi co gang nhap Battery nhung energy dang day (khong the nhap).
        /// Hien thi thong bao 'Charge Full' tren UI (tu dong an sau 2 giay) + phat SFX (lay tu Battery).
        /// </summary>
        private void HandleBatteryCollectibleRefused(IPlayer player, AudioClip chargeFullSfx)
        {
            ShowChargeFullNotification(chargeFullSfx);
        }

        /// <summary>
        /// Xu ly khi energy da sac day tu dong 100% (hoan tat qua trinh sac).
        /// Phat audio bao hieu sac day va pulse icon Energy tren UI.
        /// </summary>
        private void HandlePlayerEnergyFullyCharged(IPlayer player)
        {
            if (_audioSource != null && _energyFullChargedSfx != null)
            {
                _audioSource.PlayOneShot(_energyFullChargedSfx);
            }
            PulseEnergyIcon();
        }

        /// <summary>
        /// Hien thi thong bao 'Charge Full' tren UI (neu co text) show trong _chargeFullTextDuration giay,
        /// phat SFX (neu co), va pulse icon Energy.
        /// </summary>
        private void ShowChargeFullNotification(AudioClip chargeFullSfx)
        {
            if (_chargeFullText != null)
            {
                _chargeFullText.gameObject.SetActive(true);

                if (_chargeFullTextRoutine != null) StopCoroutine(_chargeFullTextRoutine);
                _chargeFullTextRoutine = StartCoroutine(HideChargeFullTextRoutine());
            }

            if (_audioSource != null && chargeFullSfx != null)
            {
                _audioSource.PlayOneShot(chargeFullSfx);
            }

            PulseEnergyIcon();
        }

        /// <summary>
        /// Coroutine tam dung de tu dong an thong bao 'Charge Full' sau _chargeFullTextDuration giay.
        /// </summary>
        private IEnumerator HideChargeFullTextRoutine()
        {
            yield return new WaitForSeconds(_chargeFullTextDuration);
            if (_chargeFullText != null)
            {
                _chargeFullText.gameObject.SetActive(false);
            }
            _chargeFullTextRoutine = null;
        }

        /// <summary>
        /// Pulse (phong to roi thu nho lai) scale cua icon Energy de thu hut su chu y khi energy day.
        /// </summary>
        private void PulseEnergyIcon()
        {
            if (_energyPulseTarget == null) return;
            if (!gameObject.activeInHierarchy) return;

            Vector3 originalScale = _energyPulseTarget.localScale;
            Vector3 targetScale = originalScale * _energyPulseScaleMultiplier;

            // Dung cac tween dang chay neu co, reset lai scale goc roi chay chu ky pulse.
            DOTween.Kill(_energyPulseTarget);
            _energyPulseTarget.localScale = originalScale;
            _energyPulseTarget.DOScale(targetScale, _energyPulseDuration * 0.5f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    if (_energyPulseTarget == null) return;
                    _energyPulseTarget.DOScale(originalScale, _energyPulseDuration * 0.5f)
                        .SetEase(Ease.InQuad);
                });
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