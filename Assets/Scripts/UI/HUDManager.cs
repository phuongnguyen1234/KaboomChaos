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
        [Tooltip("Image hien thi icon trai tim cua thanh mau.")]
        [SerializeField] private Image _healthHeartIcon;
        [Tooltip("Transform cua icon trai tim de thuc hien animation scale (neu bo trong se tu dong lay rectTransform cua _healthHeartIcon).")]
        [SerializeField] private RectTransform _healthHeartTransform;

        [Header("Health Heart Icon Sprites (5 Stages)")]
        [Tooltip("Sprite trai tim Stage 1 (>75% HP).")]
        [SerializeField] private Sprite _heartStage1Sprite;
        [Tooltip("Sprite trai tim Stage 2 (50% - 75% HP).")]
        [SerializeField] private Sprite _heartStage2Sprite;
        [Tooltip("Sprite trai tim Stage 3 (25% - 50% HP).")]
        [SerializeField] private Sprite _heartStage3Sprite;
        [Tooltip("Sprite trai tim Stage 4 (0% < HP <= 25%).")]
        [SerializeField] private Sprite _heartStage4Sprite;
        [Tooltip("Sprite trai tim Stage 5 (HP <= 0% - chet/vo).")]
        [SerializeField] private Sprite _heartStage5Sprite;

        [Header("Health Heartbeat Animation")]
        [Tooltip("Ti le phong to nhe cua hieu ung nhip tim (heartbeat).")]
        [SerializeField] private float _heartbeatScaleMultiplier = 1.15f;
        [Tooltip("Thoi gian 1 chu ky scale up-down cua Stage 2 (50% - 75% HP).")]
        [SerializeField] private float _stage2HeartbeatDuration = 1.0f;
        [Tooltip("Thoi gian 1 chu ky scale up-down cua Stage 3 (25% - 50% HP).")]
        [SerializeField] private float _stage3HeartbeatDuration = 0.65f;
        [Tooltip("Thoi gian 1 chu ky scale up-down cua Stage 4 (0% < HP <= 25%).")]
        [SerializeField] private float _stage4HeartbeatDuration = 0.35f;
        [Tooltip("Thoi gian nhay/pulse khi nhan sat thuong.")]
        [SerializeField] private float _heartDamageFlashDuration = 0.25f;
        [Tooltip("Ti le phong to khi nhay sat thuong.")]
        [SerializeField] private float _heartDamageFlashScale = 1.25f;

        [Header("Energy Components")]
        [Tooltip("Slider hiển thị thanh năng lượng.")]
        [SerializeField] private Slider _energySlider;
        [Tooltip("Image hien thi icon Energy tren UI.")]
        [SerializeField] private Image _energyIconImage;
        [Tooltip("Sprite icon Energy den trang (gray) dung khi dang trong qua trinh tu sac (recharge). NULL thi giu nguyen icon hien tai.")]
        [SerializeField] private Sprite _energyRechargeIconSprite;

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
        [Tooltip("Text hien thi phim tat kich hoat skill (vi du: 'E', 'Space'). Tu dong dong bo theo Settings.")]
        [SerializeField] private TextMeshProUGUI _skillKeyText;
        [Tooltip("Container chua icon perk dang trang bi. Tu dong an khi khong co perk.")]
        [SerializeField] private GameObject _equippedPerkContainer;
        [Tooltip("Image hien thi icon perk dang trang bi.")]
        [SerializeField] private Image _equippedPerkIcon;
        [Tooltip("Sprite mac dinh dung khi skill khong co icon rieng.")]
        [SerializeField] private Sprite _defaultSkillIcon;
        [Tooltip("Sprite mac dinh dung cho perk (neu perk khong co icon hoac chua tra cuu duoc).")]
        [SerializeField] private Sprite _defaultPerkIcon;

        [Header("Equip Skill & Perk Animation & SFX")]
        [Tooltip("SFX phat khi trang bi Skill thanh cong.")]
        [SerializeField] private AudioClip _equipSkillSfx;

        [Tooltip("SFX phat khi trang bi Perk thanh cong.")]
        [SerializeField] private AudioClip _equipPerkSfx;

        [Tooltip("Ti le scale ban dau (lon hon 1.0) de scale down ve 1.0 khi trang bi nhan icon moi.")]
        [SerializeField] private float _equipScaleDownStartMultiplier = 1.4f;

        [Tooltip("Thoi gian (giay) dien ra hieu ung scale down icon trang bi.")]
        [SerializeField] private float _equipScaleDownDuration = 0.3f;

        // Cache fill image cua energy slider de doi sprite fill khi dang sac.
        private Image _energyFillImage;
        private Sprite _energyOriginalFillSprite;
        private bool _energyFillCached;
        private bool _energyFillGrayActive;

        // Cache icon image cua energy slider de doi sprite icon khi dang sac.
        private Sprite _energyOriginalIconSprite;
        private bool _energyIconCached;
        private bool _energyIconGrayActive;

        // Cache icon image va transform cua trai tim thanh mau.
        private Sprite _heartOriginalSprite;
        private RectTransform _heartTransformTarget;
        private float _lastHealth = -1f;
        private bool _hasPreviousHealth;
        private int _currentHeartbeatStage = 0;
        private Tween _heartbeatTween;
        private Sequence _heartDamageSequence;

        // Cache skill va perk trang bi truoc do de kiem tra thay doi
        private ISkillData _lastEquippedSkill;
        private IPerkData _lastEquippedPerk;

        // AudioSource de phat SFX thong bao (tu tim tren GameObject HUD).
        private AudioSource _audioSource;
        // Coroutine an thong bao 'Charge Full' sau thoi gian.
        private Coroutine _chargeFullTextRoutine;
        private RectTransform _energyPulseTarget;

        [Header("Health Bar")]
        [Tooltip("Slider phu (secondary slider) cua thanh mau; se keo gia tri slider sau 0.5s khi nhan sat thuong.")]
        [SerializeField] private Slider _secondaryHealthSlider;
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

            _heartTransformTarget = _healthHeartTransform != null ? _healthHeartTransform : (_healthHeartIcon != null ? _healthHeartIcon.rectTransform : null);
            if (_healthHeartIcon != null)
            {
                _heartOriginalSprite = _healthHeartIcon.sprite;
            }
            if (_energyIconImage != null)
            {
                _energyOriginalIconSprite = _energyIconImage.sprite;
                _energyIconCached = true;
            }

            if (_rechargeTimeText != null) _rechargeTimeText.gameObject.SetActive(false);
            if (_chargeFullText != null) _chargeFullText.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            GameEvents.OnPlayerHealthChanged += HandlePlayerHealthChanged;
            GameEvents.OnPlayerEnergyChanged += HandlePlayerEnergyChanged;
            GameEvents.OnPlayerEquipmentChanged += HandleEquipmentChanged;
            GameEvents.OnBatteryCollectibleRefused += HandleBatteryCollectibleRefused;
            GameEvents.OnPlayerEnergyFullyCharged += HandlePlayerEnergyFullyCharged;
            GameEvents.OnExtremeModeStateChanged += HandleExtremeModeChanged;
            GameEvents.OnSettingsUseSkillKeyChanged += HandleSettingsSkillKeyChanged;

            // Dong bo text phim tat kich hoat skill tu Settings.
            RefreshSkillKeyText();

            // Dong bo icon trang bi luc HUD bat (vi du: quay lai sau pause).
            RefreshEquippedIcons(playEffects: false);

            // Khoi phuc trang thai Extreme Mode khi HUD bat (da luu tru).
            RefreshExtremeModeIndicator();

            // Dong bo thanh nang luong + trang thai sac neu HUD duoc bat lai giua chung.
            SyncEnergyDisplay();

            // Dong bo thanh mau voi Player hien tai khi HUD duoc bat lai hoac khoi dong vao game.
            SyncHealthDisplay();
        }

        private void OnDisable()
        {
            GameEvents.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
            GameEvents.OnPlayerEnergyChanged -= HandlePlayerEnergyChanged;
            GameEvents.OnPlayerEquipmentChanged -= HandleEquipmentChanged;
            GameEvents.OnBatteryCollectibleRefused -= HandleBatteryCollectibleRefused;
            GameEvents.OnPlayerEnergyFullyCharged -= HandlePlayerEnergyFullyCharged;
            GameEvents.OnExtremeModeStateChanged -= HandleExtremeModeChanged;
            GameEvents.OnSettingsUseSkillKeyChanged -= HandleSettingsSkillKeyChanged;

            if (_heartbeatTween != null && _heartbeatTween.IsActive())
            {
                _heartbeatTween.Kill();
                _heartbeatTween = null;
            }
            if (_heartDamageSequence != null && _heartDamageSequence.IsActive())
            {
                _heartDamageSequence.Kill();
                _heartDamageSequence = null;
            }
            if (_heartTransformTarget != null)
            {
                _heartTransformTarget.DOKill();
                _heartTransformTarget.localScale = Vector3.one;
            }
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
        /// Phan hoi khi trang thai trang bi (skill/perk) cua nguoi choi thay doi de lam moi icon va chay animation/SFX tren HUD.
        /// </summary>
        private void HandleEquipmentChanged()
        {
            RefreshEquippedIcons(playEffects: true);
        }

        /// <summary>
        /// Dong bo icon skill/perk dang trang bi tu Player hien tai.
        /// Chay hieu ung scale down va phat SFX khi co skill hoac perk moi duoc trang bi.
        /// </summary>
        /// <param name="playEffects">True neu can chay animation scale down va SFX khi co thay doi trang bi.</param>
        private void RefreshEquippedIcons(bool playEffects = false)
        {
            IPlayer managerPlayer = IPlayerManager.Instance?.GetCurrentPlayer();

            // Skill dang trang bi
            ISkillController skillController = managerPlayer?.GameObject?.GetComponentInChildren<ISkillController>();
            ISkillData currentSkill = skillController?.CurrentSkill;
            bool hasSkill = currentSkill != null;

            if (_equippedSkillContainer != null) _equippedSkillContainer.SetActive(hasSkill);
            if (_equippedSkillIcon != null)
            {
                _equippedSkillIcon.sprite = (currentSkill != null && currentSkill.Icon != null)
                    ? currentSkill.Icon
                    : _defaultSkillIcon;
            }

            if (playEffects && currentSkill != _lastEquippedSkill)
            {
                if (hasSkill)
                {
                    AnimateEquipIcon(_equippedSkillIcon != null ? _equippedSkillIcon.transform : _equippedSkillContainer?.transform);
                    if (_equipSkillSfx != null && SfxService.Instance != null)
                    {
                        SfxService.Instance.PlaySfx(_equipSkillSfx);
                    }
                }
            }
            _lastEquippedSkill = currentSkill;

            // Perk dang trang bi
            IPerkController perkController = managerPlayer?.GameObject?.GetComponentInChildren<IPerkController>();
            IPerkData currentPerk = perkController?.CurrentPerk;
            bool hasPerk = currentPerk != null;

            if (_equippedPerkContainer != null) _equippedPerkContainer.SetActive(hasPerk);
            if (_equippedPerkIcon != null)
            {
                _equippedPerkIcon.sprite = (currentPerk != null && currentPerk.Icon != null)
                    ? currentPerk.Icon
                    : _defaultPerkIcon;
            }

            if (playEffects && currentPerk != _lastEquippedPerk)
            {
                if (hasPerk)
                {
                    AnimateEquipIcon(_equippedPerkIcon != null ? _equippedPerkIcon.transform : _equippedPerkContainer?.transform);
                    AudioClip perkSfx = _equipPerkSfx != null ? _equipPerkSfx : _equipSkillSfx;
                    if (perkSfx != null && SfxService.Instance != null)
                    {
                        SfxService.Instance.PlaySfx(perkSfx);
                    }
                }
            }
            _lastEquippedPerk = currentPerk;
        }

        /// <summary>
        /// Phan hoi khi phim tat kich hoat skill trong Settings thay doi: dong bo lai text tren HUD.
        /// </summary>
        /// <param name="keyName">Ten phim tat moi (vi du: 'E', 'Space').</param>
        private void HandleSettingsSkillKeyChanged(string keyName)
        {
            SetSkillKeyText(keyName);
        }

        /// <summary>
        /// Dong bo text phim tat kich hoat skill voi gia tri dang duoc luu trong Settings.
        /// </summary>
        private void RefreshSkillKeyText()
        {
            string keyName = SettingsService.Instance != null ? SettingsService.Instance.UseSkillKey : "E";
            SetSkillKeyText(keyName);
        }

        /// <summary>
        /// Gan noi dung hien thi cho text phim tat kich hoat skill.
        /// </summary>
        /// <param name="keyName">Ten phim tat can hien thi.</param>
        private void SetSkillKeyText(string keyName)
        {
            if (_skillKeyText != null)
            {
                _skillKeyText.text = !string.IsNullOrEmpty(keyName) ? keyName : "E";
            }
        }

        /// <summary>
        /// Chay hieu ung scale down cho icon trang bi tren HUD (scale tu _equipScaleDownStartMultiplier ve 1.0x).
        /// </summary>
        private void AnimateEquipIcon(Transform targetTransform)
        {
            if (targetTransform == null) return;
            if (!gameObject.activeInHierarchy) return;

            targetTransform.DOKill();
            targetTransform.localScale = Vector3.one * _equipScaleDownStartMultiplier;
            targetTransform.DOScale(Vector3.one, _equipScaleDownDuration)
                .SetEase(Ease.OutBack)
                .SetUpdate(true);
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
        /// chi hien thi khi dang sac), icon energy den trang va sprite fill thanh nang luong (GrayFillBar khi dang sac).
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

            SetEnergyIconSprite(!recharging);
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
        /// Dong bo thanh mau voi Player hien tai khi HUD duoc bat lai hoac khi vao game.
        /// Su dung IDamageable interface (Core.Interfaces) de tranh phu thuoc truc tiep vao assembly Player.
        /// </summary>
        private void SyncHealthDisplay()
        {
            IPlayer player = IPlayerManager.Instance?.GetCurrentPlayer();
            if (player == null) return;

            IDamageable damageable = player.GameObject?.GetComponentInChildren<IDamageable>();
            if (damageable == null) return;

            _hasPreviousHealth = false;
            UpdateHealth(damageable.CurrentHealth, damageable.MaxHealth);
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
        /// Doi icon va fill ve sprite goc truoc, sau do phat audio bao hieu sac day va pulse icon Energy tren UI.
        /// </summary>
        private void HandlePlayerEnergyFullyCharged(IPlayer player)
        {
            SetEnergyIconSprite(true);
            SetEnergyFillSprite(true);

            if (_energyFullChargedSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_energyFullChargedSfx);
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

            if (chargeFullSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(chargeFullSfx);
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
        /// Kich hoat coroutine de keo gia tri secondary slider cua thanh mau.
        /// HP giam -> doi _healthSecondaryDelay thi moi lerp xuong (hieu ung 2 thanh);
        /// HP hoi phuc -> dung coroutine, cap nhat gia tri slider ngay de khong bi tre.
        /// </summary>
        private void AnimateHealthSecondary(float currentHealth)
        {
            if (_secondaryHealthSlider == null) return;

            // HP giam: secondary bar se keo sau mot do tre, tao hieu ung "thay doi giua 2 thanh".
            if (currentHealth < _secondaryHealthSlider.value)
            {
                if (_healthSecondaryRoutine != null) StopCoroutine(_healthSecondaryRoutine);
                _healthSecondaryRoutine = StartCoroutine(AnimateHealthSecondaryRoutine(currentHealth));
                return;
            }

            // HP hoi phuc: dung coroutine dang chay, cap nhat ngay.
            if (_healthSecondaryRoutine != null)
            {
                StopCoroutine(_healthSecondaryRoutine);
                _healthSecondaryRoutine = null;
            }
            _secondaryHealthSlider.value = currentHealth;
        }

        /// <summary>
        /// Coroutine: doi _healthSecondaryDelay giay roi lerp value cua secondary slider
        /// ve targetHealth trong _healthSecondaryLerpDuration giay.
        /// </summary>
        private IEnumerator AnimateHealthSecondaryRoutine(float targetHealth)
        {
            yield return new WaitForSeconds(_healthSecondaryDelay);

            float startVal = _secondaryHealthSlider != null ? _secondaryHealthSlider.value : targetHealth;
            float elapsed = 0f;
            while (elapsed < _healthSecondaryLerpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _healthSecondaryLerpDuration);
                if (_secondaryHealthSlider != null) _secondaryHealthSlider.value = Mathf.Lerp(startVal, targetHealth, t);
                yield return null;
            }

            if (_secondaryHealthSlider != null) _secondaryHealthSlider.value = targetHealth;
            _healthSecondaryRoutine = null;
        }

        /// <summary>
        /// Doi sprite icon cua energy: dung _energyRechargeIconSprite (icon den trang) khi dang sac,
        /// quay ve sprite goc khi sac day hoac khong dang sac.
        /// </summary>
        private void SetEnergyIconSprite(bool useOriginal)
        {
            if (_energyIconImage == null) return;

            if (!_energyIconCached)
            {
                _energyOriginalIconSprite = _energyIconImage.sprite;
                _energyIconCached = true;
            }

            if (useOriginal && _energyIconGrayActive)
            {
                _energyIconImage.sprite = _energyOriginalIconSprite;
                _energyIconGrayActive = false;
            }
            else if (!useOriginal && !_energyIconGrayActive && _energyRechargeIconSprite != null)
            {
                _energyIconImage.sprite = _energyRechargeIconSprite;
                _energyIconGrayActive = true;
            }
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

        /// <summary>
        /// Xac dinh stage trai tim theo ti le phan tram mau hien tai (1..5).
        /// </summary>
        private int GetHeartStage(float currentHealth, float maxHealth)
        {
            if (currentHealth <= 0f) return 5;
            if (maxHealth <= 0f) return 1;

            float pct = currentHealth / maxHealth;
            if (pct > 0.75f) return 1;
            if (pct > 0.50f) return 2;
            if (pct > 0.25f) return 3;
            return 4;
        }

        /// <summary>
        /// Cap nhat sprite cho icon trai tim theo stage hien tai.
        /// </summary>
        private void UpdateHeartSprite(int stage)
        {
            if (_healthHeartIcon == null) return;

            Sprite targetSprite = stage switch
            {
                1 => _heartStage1Sprite != null ? _heartStage1Sprite : _heartOriginalSprite,
                2 => _heartStage2Sprite != null ? _heartStage2Sprite : _heartStage1Sprite,
                3 => _heartStage3Sprite != null ? _heartStage3Sprite : _heartStage2Sprite,
                4 => _heartStage4Sprite != null ? _heartStage4Sprite : _heartStage3Sprite,
                5 => _heartStage5Sprite != null ? _heartStage5Sprite : _heartStage4Sprite,
                _ => _heartStage1Sprite != null ? _heartStage1Sprite : _heartOriginalSprite
            };

            if (targetSprite != null && _healthHeartIcon.sprite != targetSprite)
            {
                _healthHeartIcon.sprite = targetSprite;
            }
        }

        /// <summary>
        /// Nhay/pulse icon trai tim khi nhan sat thuong, sau do tiep tuc hieu ung nhip tim (heartbeat) neu con song.
        /// </summary>
        private void FlashHeartIcon(int stage)
        {
            if (_heartTransformTarget == null || !gameObject.activeInHierarchy) return;

            // Dung tween heartbeat dang chay
            if (_heartbeatTween != null && _heartbeatTween.IsActive())
            {
                _heartbeatTween.Kill();
                _heartbeatTween = null;
            }
            _currentHeartbeatStage = 0;

            if (_heartDamageSequence != null && _heartDamageSequence.IsActive())
            {
                _heartDamageSequence.Kill();
            }

            _heartTransformTarget.DOKill();
            _heartTransformTarget.localScale = Vector3.one;

            _heartDamageSequence = DOTween.Sequence();
            _heartDamageSequence.Append(_heartTransformTarget.DOScale(Vector3.one * _heartDamageFlashScale, _heartDamageFlashDuration * 0.5f).SetEase(Ease.OutQuad))
                .Append(_heartTransformTarget.DOScale(Vector3.one, _heartDamageFlashDuration * 0.5f).SetEase(Ease.InQuad))
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    _heartDamageSequence = null;
                    UpdateHeartbeatAnimation(stage);
                });
        }

        /// <summary>
        /// Cap nhat animation scale up-down (nhip tim) deu dan nhe nhang theo stage.
        /// Stage 1 hoac 5: dung nhip tim, tro ve scale 1.0.
        /// Stage 2..4: chay tween loop Yoyo, stage cang cao chu ky cang ngan.
        /// </summary>
        private void UpdateHeartbeatAnimation(int stage)
        {
            if (_heartTransformTarget == null || !gameObject.activeInHierarchy) return;

            // Neu dang chay animation nhay sat thuong, de flash hoan tat truoc
            if (_heartDamageSequence != null && _heartDamageSequence.IsActive()) return;

            if (stage == 1 || stage == 5)
            {
                if (_heartbeatTween != null && _heartbeatTween.IsActive())
                {
                    _heartbeatTween.Kill();
                    _heartbeatTween = null;
                }
                _currentHeartbeatStage = 0;
                _heartTransformTarget.DOKill();
                _heartTransformTarget.localScale = Vector3.one;
                return;
            }

            // Neu stage khong doi va tween van dang chay thi giu nguyen
            if (_currentHeartbeatStage == stage && _heartbeatTween != null && _heartbeatTween.IsActive())
            {
                return;
            }

            if (_heartbeatTween != null && _heartbeatTween.IsActive())
            {
                _heartbeatTween.Kill();
                _heartbeatTween = null;
            }

            _currentHeartbeatStage = stage;
            float duration = stage switch
            {
                2 => _stage2HeartbeatDuration,
                3 => _stage3HeartbeatDuration,
                4 => _stage4HeartbeatDuration,
                _ => _stage2HeartbeatDuration
            };

            _heartTransformTarget.DOKill();
            _heartTransformTarget.localScale = Vector3.one;

            _heartbeatTween = _heartTransformTarget.DOScale(Vector3.one * _heartbeatScaleMultiplier, duration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetUpdate(true);
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

            bool isDamage = _hasPreviousHealth && currentHealth < _lastHealth;
            _lastHealth = currentHealth;
            _hasPreviousHealth = true;

            int stage = GetHeartStage(currentHealth, maxHealth);
            UpdateHeartSprite(stage);

            if (isDamage)
            {
                FlashHeartIcon(stage);
            }
            else
            {
                UpdateHeartbeatAnimation(stage);
            }

            if (_healthSlider != null)
            {
                // Đảm bảo slider được cấu hình đúng với giá trị max, ví dụ 100.
                if (_healthSlider.maxValue != maxHealth) _healthSlider.maxValue = maxHealth;
                // Gán trực tiếp giá trị máu hiện tại, không phải dạng phần trăm (0-1).
                _healthSlider.value = currentHealth;
            }

            if (_secondaryHealthSlider != null)
            {
                if (_secondaryHealthSlider.maxValue != maxHealth) _secondaryHealthSlider.maxValue = maxHealth;
            }

            if (_healthText != null)
            {
                _healthText.text = $"{Mathf.Clamp(currentHealth, 0, maxHealth)}";
            }

            // Secondary bar: keo sau _healthSecondaryDelay khi HP giam; hoi phuc cap nhat ngay.
            AnimateHealthSecondary(currentHealth);
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

