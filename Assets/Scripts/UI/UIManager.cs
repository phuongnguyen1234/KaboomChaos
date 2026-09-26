using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using Core.Interfaces.UI;
using Core;
using UnityEngine.InputSystem;
using Core.Interfaces;
using System;

namespace UI
{
    /// <summary>
    /// Quản lý các yếu tố UI chính của game như thông báo, timer.
    /// Đóng vai trò trung tâm điều phối các panel UI khác nhau.
    /// </summary>
    public class UIManager : MonoBehaviour, IUIManager
    {
        private static WaitForSeconds _waitForSeconds1 = new(1f);
        #region Fields
        [Header("Notification Panel")]
        [Tooltip("Panel chứa thông báo chung.")]
        [SerializeField] private GameObject _notificationPanel;
        [Tooltip("Text hiển thị nội dung thông báo.")]
        [SerializeField] private TextMeshProUGUI _notificationText;

        [Header("Timer Panel")]
        [Tooltip("Panel chứa đồng hồ đếm ngược.")]
        [SerializeField] private GameObject _timerPanel;
        [Tooltip("Text hiển thị thời gian.")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [Tooltip("Icon (Image) cua TimerPanel, thường la dong ho. Doi mau thanh do khi con 30 giay cuoi round.")]
        [SerializeField] private Image _timerIcon;

        [Header("Timer Danger State (30s cuoi round)")]
        [Tooltip("Mau binh thuong cua text timer (khong trong trang thai do).")]
        [SerializeField] private Color _timerTextNormalColor = Color.white;
        [Tooltip("Mau do cua text timer khi con 30 giay cuoi round.")]
        [SerializeField] private Color _timerTextDangerColor = Color.red;
        [Tooltip("Mau binh thuong cua icon timer.")]
        [SerializeField] private Color _timerIconNormalColor = Color.white;
        [Tooltip("Mau do cua icon timer khi con 30 giay cuoi round.")]
        [SerializeField] private Color _timerIconDangerColor = Color.red;
        [Tooltip("SFX dong ho canh bao phat song song khi bat dau 30 giay cuoi round (chi phat 1 lan).")]
        [SerializeField] private AudioClip _timerDangerSfx;
        [Tooltip("SFX tieng chuong canh bao phat song song khi bat dau 30 giay cuoi round (chi phat 1 lan).")]

        [SerializeField] private AudioClip _bellDangerSfx;
        [Tooltip("AudioSource dung de phat SFX canh bao timer. Neu de trong se tu tim AudioSource tren GameObject.")]
        [SerializeField] private AudioSource _timerSfxSource;

        // Tranh phat SFX lap lai moi lan goi SetTimerDangerState(true).
        private bool _timerDangerSfxPlayed;

        [Header("UI Animations & Effects")]
        [Tooltip("Component quan ly tat ca animation va SFX cua UIManager.")]
        [SerializeField] private UI.Animations.UIManagerAnimation _uiAnimation;

        [Header("Extreme Mode")]
        [Tooltip("Panel hien thi trang thai Extreme Mode. Hien khi bat, an khi tat.")]
        [SerializeField] private GameObject _extremeModePanel;

        [Header("Crosshair")]
        [Tooltip("CanvasGroup của crosshair cho Shift Lock. GameObject chứa nó phải có component CanvasGroup.")]
        [SerializeField] private CanvasGroup _shiftLockCrosshairCanvasGroup;
        [Tooltip("CanvasGroup của crosshair cho góc nhìn thứ nhất. GameObject chứa nó phải có component CanvasGroup.")]
        [SerializeField] private CanvasGroup _firstPersonCrosshairCanvasGroup;

        [Header("Dependencies")]
        [Tooltip("Kéo thả HomeScreen GameObject vào đây")]
        [SerializeField] private HomeScreen _homeScreenBehaviour;

        [Header("Pause Menu")]
        [Tooltip("Kéo thả PauseMenu GameObject vào đây")]
        [SerializeField] private PauseMenu _pauseMenuBehaviour;
        [Tooltip("Phím để mở Pause Menu (Mặc định: ESC)")]
        [SerializeField] private InputAction _pauseAction = new(type: InputActionType.Button, binding: "<Keyboard>/escape");

        [Header("Score Card")]
        [Tooltip("Kéo thả ScoreCard GameObject vào đây để hiển thị bảng điểm sau mỗi round (thắng/thua).")]
        [SerializeField] private ScoreCard _scoreCardBehaviour;

        [Header("Main HUD")]
        [Tooltip("Main HUD (thanh máu, năng lượng...) hiển thị khi bắt đầu vào chơi, ẩn khi quay về Home.")]
        [SerializeField] private GameObject _mainHUD;

        [Header("Floating Text Containers")]
        [Tooltip("Container UI chung cho cac text noi. Neu khong gan container rieng se dung container nay.")]
        [SerializeField] private RectTransform _floatingTextContainer;
        [Tooltip("Container UI rieng de chua floating text lien quan den HP (sat thuong, hoi mau).")]
        [SerializeField] private RectTransform _hpFloatingTextContainer;
        [Tooltip("Container UI rieng de chua floating text lien quan den Collectible (Coin, Battery...).")]
        [SerializeField] private RectTransform _collectibleFloatingTextContainer;

        public RectTransform FloatingTextContainer => _floatingTextContainer;
        public RectTransform HpFloatingTextContainer => _hpFloatingTextContainer != null ? _hpFloatingTextContainer : _floatingTextContainer;
        public RectTransform CollectibleFloatingTextContainer => _collectibleFloatingTextContainer != null ? _collectibleFloatingTextContainer : _floatingTextContainer;

        [Header("Pause Menu Button")]
        [Tooltip("Nút mở Pause Menu (thường đặt trong Main HUD). Ngoài phím ESC.")]
        [SerializeField] private Button _pauseMenuButton;

        [Header("Main Menu Buttons & Popups")]
        [Tooltip("Nút Shop trên UI chính. Mở popup Shop (dạng tab).")]
        [SerializeField] private Button _shopButton;
        [Tooltip("Nút Inventory trên UI chính. Mở popup Inventory (dạng tab).")]
        [SerializeField] private Button _inventoryButton;
        [Tooltip("Nút Option trên UI chính. Mở popup Option (không tab).")]
        [SerializeField] private Button _optionButton;
        [Tooltip("Nut Settings tren UI chinh. Mo popup Settings (Music/SFX/hotkeys).")]
        [SerializeField] private Button _settingsButton;
        [Tooltip("Nut Info tren UI chinh. Mo popup Info.")]
        [SerializeField] private Button _infoButton;

        [Tooltip("Popup Shop - la PopupTab, quan ly viec mua ban vat pham.")]
        [SerializeField] private ShopPopup _shopPopup;
        [Tooltip("Popup Inventory - la PopupTab, hien thi vat pham nguoi choi so huu.")]
        [SerializeField] private InventoryPopup _inventoryPopup;
        [Tooltip("Popup Option - hien thi cac thiet lap cua game.")]
        [SerializeField] private OptionPopup _optionPopup;
        [Tooltip("Popup Settings - chua SettingsUI (slider Music/SFX va rebind UseSkillKey).")]
        [SerializeField] private SettingsPopup _settingsPopup;
        [Tooltip("Popup Info - hien thi thong tin ve tro choi.")]
        [SerializeField] private InfoPopup _infoPopup;

        [Header("Sidebar Tooltips")]
        [Tooltip("Tooltip cho nut Shop tren Sidebar.")]
        [SerializeField] private UISidebarTooltip _shopTooltip;
        [Tooltip("Tooltip cho nut Inventory tren Sidebar.")]
        [SerializeField] private UISidebarTooltip _inventoryTooltip;
        [Tooltip("Tooltip cho nut Option tren Sidebar.")]
        [SerializeField] private UISidebarTooltip _optionTooltip;

        [Header("Popup Open SFX")]
        [Tooltip("SFX phat khi mo Shop Popup.")]
        [SerializeField] private AudioClip _openShopSfx;
        [Tooltip("SFX phat khi mo Inventory Popup.")]
        [SerializeField] private AudioClip _openInventorySfx;
        [Tooltip("SFX phat khi mo Option Popup.")]
        [SerializeField] private AudioClip _openOptionSfx;
        [Tooltip("SFX phat khi mo Info Popup.")]
        [SerializeField] private AudioClip _openInfoSfx;

        // Tranh trigger toggle kep trong cung 1 frame khi nhieu event/button cung goi OpenPopup
        private int _lastPopupActionFrame = -1;
        private BasePopup _lastActionPopup = null;

        // Coroutine dang chay de co the dung lai neu can
        private Coroutine _notificationCoroutine;

        // Trang thai cho biet nguoi choi da vao game (sau khi an Play va ket thuc transition) hay chua
        private bool _isInGame = false;

        // Dependencies
        private IHomeScreen _homeScreen;
        private IPauseMenu _pauseMenu;
        private IScoreCard _scoreCard;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (IUIManager.Instance != null && IUIManager.Instance as MonoBehaviour != this)
            {
                Destroy(gameObject);
            }
                        else
            {
                IUIManager.Instance = this;
                DontDestroyOnLoad(gameObject);
            }

            _uiAnimation ??= GetComponentInChildren<UI.Animations.UIManagerAnimation>();
            _infoPopup ??= GetComponentInChildren<InfoPopup>(true);
            _settingsPopup ??= GetComponentInChildren<SettingsPopup>(true);

            // Inject IHomeScreen
            if (_homeScreenBehaviour != null)
            {
                _homeScreen = _homeScreenBehaviour;
                if (_homeScreen != null)
                {
                    // Subscribe vao event - pattern giong Vue parent-child communication
                    _homeScreen.OnPlayClicked += HandleHomeScreenPlayClicked;
                    _homeScreen.OnSettingsClicked += HandleHomeScreenSettingsClicked;
                    _homeScreen.OnInfoClicked += HandleHomeScreenInfoClicked;
                }
            }

            if (_pauseMenuBehaviour != null)
            {
                _pauseMenu = _pauseMenuBehaviour;
            }

            // Inject IScoreCard
            if (_scoreCardBehaviour != null)
            {
                _scoreCard = _scoreCardBehaviour;
            }

            // Gan su kien cho nut mo Pause Menu thu cong (ben canh phim ESC)
            if (_pauseMenuButton != null)
            {
                _pauseMenuButton.onClick.AddListener(TogglePauseMenu);
            }

            // Gan su kien cho cac nut Shop / Inventory / Option / Settings / Info tren UI chinh
            if (_shopButton != null)
            {
                _shopButton.onClick.AddListener(OpenShopPopup);
            }
            if (_inventoryButton != null)
            {
                _inventoryButton.onClick.AddListener(OpenInventoryPopup);
            }
            if (_optionButton != null)
            {
                _optionButton.onClick.AddListener(OpenOptionPopup);
            }
            if (_settingsButton != null)
            {
                _settingsButton.onClick.AddListener(OpenSettingsPopup);
            }
            if (_infoButton != null)
            {
                _infoButton.onClick.AddListener(OpenInfoPopup);
            }

            // Tu tim AudioSource de phat SFX canh bao timer neu chua gan.
            if (_timerSfxSource == null)
            {
                _timerSfxSource = GetComponent<AudioSource>();
            }

            // Khoi tao cac tooltip hover cho Sidebar
            SetupSidebarTooltips();
        }

        private void OnEnable()
        {
            _pauseAction.Enable();
            GameEvents.OnReturnToHomeRequest += HandleReturnToHome;
            GameEvents.OnExtremeModeStateChanged += HandleExtremeModeChanged;

            // Khoi phuc trang thai Extreme Mode khi UIManager bat (da luu tru).
            RefreshExtremeModeIndicator();
        }

        private void OnDisable()
        {
            _pauseAction.Disable();
            GameEvents.OnReturnToHomeRequest -= HandleReturnToHome;
            GameEvents.OnExtremeModeStateChanged -= HandleExtremeModeChanged;
        }

        private void OnDestroy()
        {
            // Unsubscribe de tranh memory leak
            if (_homeScreen != null)
            {
                _homeScreen.OnPlayClicked -= HandleHomeScreenPlayClicked;
                _homeScreen.OnSettingsClicked -= HandleHomeScreenSettingsClicked;
                _homeScreen.OnInfoClicked -= HandleHomeScreenInfoClicked;
            }
            if (_pauseMenuButton != null)
            {
                _pauseMenuButton.onClick.RemoveListener(TogglePauseMenu);
            }
            if (_shopButton != null)
            {
                _shopButton.onClick.RemoveListener(OpenShopPopup);
            }
            if (_inventoryButton != null)
            {
                _inventoryButton.onClick.RemoveListener(OpenInventoryPopup);
            }
            if (_optionButton != null)
            {
                _optionButton.onClick.RemoveListener(OpenOptionPopup);
            }
            if (_settingsButton != null)
            {
                _settingsButton.onClick.RemoveListener(OpenSettingsPopup);
            }
            if (_infoButton != null)
            {
                _infoButton.onClick.RemoveListener(OpenInfoPopup);
            }
        }

        private void Update()
        {
            if (_pauseAction.WasPressedThisFrame())
            {
                // Neu co bat ky main popup nao dang mo tren UI, uu tien dong popup do truoc khi mo Pause Menu
                if (IsAnyMainPopupOpen())
                {
                    CloseMainPopups(true);
                    return;
                }

                TogglePauseMenu();
            }
        }

        private void Start()
        {
            _isInGame = false;

            // Ẩn các panel khi bắt đầu
            if (_notificationPanel != null) _notificationPanel.SetActive(false);
            if (_timerPanel != null) _timerPanel.SetActive(false);
            _uiAnimation?.HideIntensityBar();
            _uiAnimation?.HideCurrentIntensity();
            if (_scoreCard != null) _scoreCard.Hide();
            if (_mainHUD != null) _mainHUD.SetActive(false);          // Main HUD bắt đầu ẩn (chỉ hiện khi ấn Play)
            if (_pauseMenu != null) _pauseMenu.Hide();                 // Pause Menu bắt đầu ẩn
            CloseMainPopups();                                        // Dam bao khong mo san popup nao luc khoi dong

            // ConfirmationPopup đã được khởi tạo + đăng ký Instance trong Awake (phải active lúc boot),
            // nhưng cần ẩn ngay khi UI bắt đầu để không hiển thị lung tung.
            if (IConfirmationPopup.Instance != null) IConfirmationPopup.Instance.Hide();

            // Sử dụng alpha để ẩn crosshair thay vì SetActive(false)
            // Điều này giúp tránh giật lag khi bật/tắt nhanh (spam)
            if (_shiftLockCrosshairCanvasGroup != null)
            {
                _shiftLockCrosshairCanvasGroup.alpha = 0f;
            }
            if (_firstPersonCrosshairCanvasGroup != null)
            {
                _firstPersonCrosshairCanvasGroup.alpha = 0f;
            }
        }
        #endregion

        #region Public Methods (IUIManager Implementation)

        /// <summary>
        /// Hiển thị một thông báo trên màn hình trong một khoảng thời gian.
        /// </summary>
        /// <param name="message">Nội dung thông báo.</param>
        /// <param name="duration">Thời gian hiển thị (giây).</param>
        public void ShowNotification(string message, float duration)
        {
            if (_notificationPanel == null || _notificationText == null)
            {
                Debug.LogWarning("[UIManager] Notification panel or text is not assigned.", this);
                return;
            }

            // Nếu có thông báo cũ, dừng coroutine đó lại
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
            }

            _notificationCoroutine = StartCoroutine(ShowNotificationCoroutine(message, duration));
        }

        /// <summary>
        /// Hien thi thong bao tren man hinh va GIU NGUYEN (khong tu dong an sau khoang thoi gian).
        /// Dung cho cac thong bao ben trong gameloop de panel luon hien thi xuyen suot vong lap game.
        /// </summary>
        /// <param name="message">Noi dung thong bao.</param>
        public void ShowPersistentNotification(string message)
        {
            if (_notificationPanel == null || _notificationText == null)
            {
                Debug.LogWarning("[UIManager] Notification panel or text is not assigned.", this);
                return;
            }

            // Dung coroutine cu (neu co) de tranh viec no tu dong an panel sau khi het thoi gian.
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
                _notificationCoroutine = null;
            }

            _notificationText.text = message;
            _notificationPanel.SetActive(true);
        }

        /// <summary>
        /// Cập nhật và hiển thị thời gian trên timer.
        /// </summary>
        /// <param name="seconds">Số giây còn lại để hiển thị.</param>
        public void UpdateTimer(int seconds)
        {
            if (_timerPanel == null || _timerText == null)
            {
                Debug.LogWarning("[UIManager] Timer panel or text is not assigned.", this);
                return;
            }
            
            if (!_timerPanel.activeSelf) _timerPanel.SetActive(true);
            _timerText.text = $"{seconds / 60:0}:{seconds % 60:00}";
        }

        /// <inheritdoc/>
        public void HideTimer()
        {
            if (_timerPanel != null) _timerPanel.SetActive(false);

            // Reset trang thai cam bao khi an timer (ket thuc round), de round moi bat dau mau binh thuong.
            ApplyTimerDangerVisual(false);
            _timerDangerSfxPlayed = false;
        }

        /// <inheritdoc/>
        public void SetTimerDangerState(bool danger)
        {
            ApplyTimerDangerVisual(danger);

            if (danger && !_timerDangerSfxPlayed)
            {
                _timerDangerSfxPlayed = true;
                PlayTimerDangerSfx();
            }
        }

        /// <summary>
        /// Doi mau text + icon cua TimerPanel theo trang thai cam bao (danger hay binh thuong).
        /// </summary>
        /// <param name="danger">True = mau do, False = mau binh thuong.</param>
        private void ApplyTimerDangerVisual(bool danger)
        {
            if (_timerText != null)
            {
                _timerText.color = danger ? _timerTextDangerColor : _timerTextNormalColor;
            }

            if (_timerIcon != null)
            {
                _timerIcon.color = danger ? _timerIconDangerColor : _timerIconNormalColor;
            }
        }

        /// <summary>
        /// Phat SFX canh bao 30 giay cuoi (phan bo doc lap, song song voi nhac nen 30 giay cuoi).
        /// Chi phat neu co cau hinh AudioClip va tim duoc AudioSource.
        /// </summary>
        private void PlayTimerDangerSfx()
        {
            if (_timerDangerSfx == null || _bellDangerSfx == null || SfxService.Instance == null) return;

            SfxService.Instance.PlaySfx(_timerDangerSfx);
            SfxService.Instance.PlaySfx(_bellDangerSfx);
        }

        /// <inheritdoc/>
        public void HideNotification()
        {
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
                _notificationCoroutine = null;
            }
            if (_notificationPanel != null) _notificationPanel.SetActive(false);
        }

        /// <inheritdoc/>
        public IEnumerator AnimateIntensityBar(float currentIntensity, float minIntensity, float maxIntensity)
        {
            if (_uiAnimation != null)
            {
                yield return StartCoroutine(_uiAnimation.AnimateIntensityBar(currentIntensity, minIntensity, maxIntensity));
            }
        }

        /// <inheritdoc/>
        public void ShowCurrentIntensity(float currentIntensity)
        {
            _uiAnimation?.ShowCurrentIntensity(currentIntensity);
        }

        /// <inheritdoc/>
        public void HideIntensityBar()
        {
            _uiAnimation?.HideIntensityBar();
        }

        /// <inheritdoc/>
        public void HideCurrentIntensity()
        {
            _uiAnimation?.HideCurrentIntensity();
        }

        /// <inheritdoc/>
        public void ShowScoreCard(ScoreCardData data)
        {
            if (_scoreCard == null)
            {
                Debug.LogWarning("[UIManager] ScoreCard is not assigned. Cannot show Score Card.", this);
                return;
            }
            _scoreCard.Show(data);
        }

        /// <inheritdoc/>
        public void HideScoreCard()
        {
            _scoreCard?.Hide();
        }

        /// <inheritdoc/>
        public IEnumerator ShowCountdown()
        {
            if (_uiAnimation != null)
            {
                yield return StartCoroutine(_uiAnimation.ShowCountdown());
            }
        }

        /// <inheritdoc/>
        public void HideCountdown()
        {
            _uiAnimation?.HideCountdown();
        }

        /// <inheritdoc/>
        public void PlayTransition(Action onCovered, Action onComplete = null, float? holdDurationOverride = null)
        {
            if (_uiAnimation != null)
            {
                _uiAnimation.PlayTransition(onCovered, onComplete, holdDurationOverride);
            }
            else
            {
                onCovered?.Invoke();
                onComplete?.Invoke();
            }
        }

        /// <inheritdoc/>
        public void PlayRoundStartSfx()
        {
            _uiAnimation?.PlayRoundStartSfx();
        }

        /// <inheritdoc/>
        public void PlayRoundEndSfx()
        {
            _uiAnimation?.PlayRoundEndSfx();
        }

        /// <inheritdoc/>
        public void SetShiftLockCrosshair(bool active)
        {
            if (_shiftLockCrosshairCanvasGroup != null)
            {
                _shiftLockCrosshairCanvasGroup.alpha = active ? 1f : 0f;
            }
        }

        /// <inheritdoc/>
        public void SetFirstPersonCrosshair(bool active)
        {
            if (_firstPersonCrosshairCanvasGroup != null)
            {
                _firstPersonCrosshairCanvasGroup.alpha = active ? 1f : 0f;
            }
        }

        /// <summary>
        /// Bat / tat Pause Menu. Chi cho phep mo khi nguoi choi da thuc su vao game (sau khi an Play va ket thuc transition).
        /// </summary>
        public void TogglePauseMenu()
        {
            if (_pauseMenu == null) return;

            // Neu PauseMenu dang mo, cho phep toggle de dong
            if (_pauseMenu.IsVisible)
            {
                _pauseMenu.Hide();
                return;
            }

            // Chi cho phep mo Pause Menu khi da o trong game
            if (!_isInGame)
            {
                return;
            }

            // Khong mo Pause Menu neu man hinh HomeScreen van dang hien thi
            if (_homeScreen != null && _homeScreen.IsVisible)
            {
                return;
            }
            if (_homeScreenBehaviour != null && _homeScreenBehaviour.gameObject.activeInHierarchy)
            {
                return;
            }

            _pauseMenu.Show();
        }

        #endregion

        #region Main Menu Popups
        /// <summary>
        /// Kiem tra xem co bat ky popup chinh nao (Shop, Inventory, Option, Settings, Info) dang mo hay khong.
        /// </summary>
        private bool IsAnyMainPopupOpen()
        {
            return (_shopPopup != null && _shopPopup.IsVisible) ||
                   (_inventoryPopup != null && _inventoryPopup.IsVisible) ||
                   (_optionPopup != null && _optionPopup.IsVisible) ||
                   (_settingsPopup != null && _settingsPopup.IsVisible) ||
                   (_infoPopup != null && _infoPopup.IsVisible);
        }

        /// <summary>
        /// An tat ca cac popup chinh (Shop, Inventory, Option, Settings, Info).
        /// </summary>
        /// <param name="animate">True de chay animation slide out, False de an ngay.</param>
        private void CloseMainPopups(bool animate = true)
        {
            if (_shopPopup != null && _shopPopup.IsVisible) _shopPopup.Hide(animate);
            if (_inventoryPopup != null && _inventoryPopup.IsVisible) _inventoryPopup.Hide(animate);
            if (_optionPopup != null && _optionPopup.IsVisible) _optionPopup.Hide(animate);
            if (_settingsPopup != null && _settingsPopup.IsVisible) _settingsPopup.Hide(animate);
            if (_infoPopup != null && _infoPopup.IsVisible) _infoPopup.Hide(animate);
        }

        /// <summary>
        /// Mo mot popup. Neu chua co popup nao mo -> Slide in (animate = true).
        /// Neu dang co popup khac mo -> Hien thi ngay lap tuc khong slide in (animate = false).
        /// Neu nhan vao chinh popup dang mo -> Dong popup (slide out).
        /// Co guard debounce theo frame de tranh bi trigger kep trong cung 1 frame.
        /// </summary>
        private void OpenPopup(BasePopup targetPopup)
        {
            if (targetPopup == null) return;

            // Tranh loi click 1 lan nhung bi trigger 2 lan trong cung 1 frame (vi du ca Button.onClick va Event tu HomeScreen)
            if (_lastActionPopup == targetPopup && _lastPopupActionFrame == Time.frameCount)
            {
                return;
            }

            _lastPopupActionFrame = Time.frameCount;
            _lastActionPopup = targetPopup;

            if (targetPopup.IsVisible)
            {
                targetPopup.Hide(true);
                return;
            }

            bool wasAnyPopupOpen = IsAnyMainPopupOpen();

            // Dong cac popup khac (an ngay neu chuyen giua cac popup)
            CloseMainPopups(animate: !wasAnyPopupOpen);

            // Neu KHONG co popup nao dang mo -> Slide in (true)
            // Neu DA CO popup khac dang mo -> Hien thi ngay (false)
            targetPopup.Show(animate: !wasAnyPopupOpen);
        }

        /// <summary>
        /// Mo popup Shop khi nguoi choi nhan nut Shop hoac tuong tac voi NPC.
        /// </summary>
        public void OpenShopPopup()
        {
            if (_shopPopup != null && !_shopPopup.IsVisible)
            {
                if (_openShopSfx != null && SfxService.Instance != null)
                {
                    SfxService.Instance.PlaySfx(_openShopSfx);
                }
            }
            OpenPopup(_shopPopup);
        }

        /// <summary>
        /// Mo popup Inventory khi nguoi choi nhan nut Inventory tren UI chinh.
        /// </summary>
        private void OpenInventoryPopup()
        {
            if (_inventoryPopup != null && !_inventoryPopup.IsVisible)
            {
                if (_openInventorySfx != null && SfxService.Instance != null)
                {
                    SfxService.Instance.PlaySfx(_openInventorySfx);
                }
            }
            OpenPopup(_inventoryPopup);
        }

        /// <summary>
        /// Mo popup Option khi nguoi choi nhan nut Option tren UI chinh.
        /// </summary>
        private void OpenOptionPopup()
        {
            if (_optionPopup != null && !_optionPopup.IsVisible)
            {
                if (_openOptionSfx != null && SfxService.Instance != null)
                {
                    SfxService.Instance.PlaySfx(_openOptionSfx);
                }
            }
            OpenPopup(_optionPopup);
        }

        /// <summary>
        /// Mo popup Settings khi user nhan nut Settings.
        /// </summary>
        private void OpenSettingsPopup()
        {
            OpenPopup(_settingsPopup);
        }

        /// <summary>
        /// Mo popup Info khi user nhan nut Info tren UI chinh hoac Home.
        /// </summary>
        public void OpenInfoPopup()
        {
            _infoPopup ??= GetComponentInChildren<InfoPopup>(true);

            if (_infoPopup != null && !_infoPopup.IsVisible)
            {
                if (_openInfoSfx != null && SfxService.Instance != null)
                {
                    SfxService.Instance.PlaySfx(_openInfoSfx);
                }
            }
            OpenPopup(_infoPopup);
        }
        #endregion

        #region Event Handlers
        private void HandleHomeScreenPlayClicked()
        {
            // Danh dau da thuc su vao game
            _isInGame = true;

            // UIManager nhan event tu HomeScreen (HomeScreen da tu an truoc khi emit event).
            // Hien Main HUD va dam bao Pause Menu bi an khi bat dau choi.
            if (_mainHUD != null) _mainHUD.SetActive(true);
            _pauseMenu?.Hide();
            CloseMainPopups();

            // Ban event yeu cau chay game ngay lap tuc.
            Debug.Log("[UIManager] HomeScreen PlayClicked event received. Triggering GameEvents.OnStartGameRequest.");
            GameEvents.TriggerStartGameRequest();
        }

        /// <summary>
        /// Xu ly event Settings tu HomeScreen: mo popup Settings (Settings_Home).
        /// </summary>
        private void HandleHomeScreenSettingsClicked()
        {
            Debug.Log("[UIManager] HomeScreen SettingsClicked event received. Opening Settings popup.");
            OpenSettingsPopup();
        }

        /// <summary>
        /// Xu ly event Info tu HomeScreen: mo popup Info.
        /// </summary>
        private void HandleHomeScreenInfoClicked()
        {
            Debug.Log("[UIManager] HomeScreen InfoClicked event received. Opening Info popup.");
            OpenInfoPopup();
        }

        /// <summary>
        /// Xử lý khi người chơi xác nhận "Back To Home" (từ PauseMenu → ConfirmationPopup):
        /// ẩn Main HUD + các panel gameplay, hiện lại HomeScreen (kèm chuỗi loading).
        /// </summary>
        private void HandleReturnToHome()
        {
            _isInGame = false;
            Debug.Log("[UIManager] ReturnToHomeRequest received. Hiding Main HUD and showing HomeScreen.");
            HideGameplayPanels();
            _pauseMenu?.Hide();
            CloseMainPopups();
            _homeScreen?.Show(true); // Quay straight to Home - without loading overlay.
        }
        #endregion

        #region Private Methods & Coroutines

        /// <summary>
        /// Phan hoi khi trang thai Extreme Mode thay doi: dong bo indicator (text + icon) tren UIManager.
        /// </summary>
        /// <param name="enabled">True neu Extreme Mode dang bat.</param>
        private void HandleExtremeModeChanged(bool enabled)
        {
            RefreshExtremeModeIndicator();
        }

        /// <summary>
        /// Cap nhat panel Extreme Mode tren UIManager: hien/an theo trang thai da luu tru.
        /// </summary>
        private void RefreshExtremeModeIndicator()
        {
            bool enabled = GameEvents.TriggerRequestExtremeModeEnabled();

            if (_extremeModePanel != null)
            {
                _extremeModePanel.SetActive(enabled);
            }
        }

        private IEnumerator ShowNotificationCoroutine(string message, float duration)
        {
            _notificationText.text = message;
            _notificationPanel.SetActive(true);
            yield return new WaitForSeconds(duration);
            _notificationPanel.SetActive(false);
            _notificationCoroutine = null;
        }

        /// <summary>
        /// Ẩn toàn bộ các panel gameplay/HUD khi thoát về Home hoặc lúc khởi động.
        /// </summary>
        private void HideGameplayPanels()
        {
            if (_notificationCoroutine != null)
            {
                StopCoroutine(_notificationCoroutine);
                _notificationCoroutine = null;
            }

            if (_notificationPanel != null) _notificationPanel.SetActive(false);
            if (_timerPanel != null) _timerPanel.SetActive(false);
            if (_timerSfxSource != null) _timerSfxSource.Stop();
            if (_mainHUD != null) _mainHUD.SetActive(false);
            if (_scoreCard != null) _scoreCard.Hide();
            HideCountdown();
            _uiAnimation?.HideIntensityBar();
            _uiAnimation?.HideCurrentIntensity();

            // Ẩn crosshair (dùng alpha để tránh giật lag khi bật/tắt nhanh)
            if (_shiftLockCrosshairCanvasGroup != null) _shiftLockCrosshairCanvasGroup.alpha = 0f;
            if (_firstPersonCrosshairCanvasGroup != null) _firstPersonCrosshairCanvasGroup.alpha = 0f;
        }

        /// <summary>
        /// Khoi tao component tooltip hover cho cac nut Sidebar (Shop, Inventory, Option).
        /// </summary>
        private void SetupSidebarTooltips()
        {
            if (_shopButton != null && _shopTooltip == null)
            {
                _shopTooltip = _shopButton.GetComponent<UISidebarTooltip>() ?? _shopButton.gameObject.AddComponent<UISidebarTooltip>();
            }

            if (_inventoryButton != null && _inventoryTooltip == null)
            {
                _inventoryTooltip = _inventoryButton.GetComponent<UISidebarTooltip>() ?? _inventoryButton.gameObject.AddComponent<UISidebarTooltip>();
            }

            if (_optionButton != null && _optionTooltip == null)
            {
                _optionTooltip = _optionButton.GetComponent<UISidebarTooltip>() ?? _optionButton.gameObject.AddComponent<UISidebarTooltip>();
            }
        }
        #endregion
    }
}

