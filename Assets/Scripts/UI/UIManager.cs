using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;
using Core.Interfaces.UI;
using Core;
using UnityEngine.InputSystem;

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
        [Tooltip("SFX canh bao phat song song khi bat dau 30 giay cuoi round (chi phat 1 lan).")]
        [SerializeField] private AudioClip _timerDangerSfx;
        [Tooltip("AudioSource dung de phat SFX canh bao timer. Neu de trong se tu tim AudioSource tren GameObject.")]
        [SerializeField] private AudioSource _timerSfxSource;

        // Tranh phat SFX lap lai moi lan goi SetTimerDangerState(true).
        private bool _timerDangerSfxPlayed;

        [Header("Intensity Bar")]
        [Tooltip("Panel chứa thanh cường độ.")]
        [SerializeField] private GameObject _intensityBarPanel;
        [Tooltip("Mũi tên chỉ báo trên thanh cường độ.")]
        [SerializeField] private RectTransform _intensityArrow;
        [Tooltip("Khu vực có thể di chuyển của mũi tên (dùng để tính toán vị trí).")]
        [SerializeField] private RectTransform _intensityRange;
        
        [Header("Intensity Bar Animation")]
        [Tooltip("Vị trí bắt đầu của thanh cường độ (ngoài màn hình, tính từ vị trí gốc).")]
        [SerializeField] private Vector2 _intensityBarStartOffset = new(0, 200f);
        [Tooltip("Thời gian cho hiệu ứng trượt vào của thanh cường độ.")]
        [SerializeField] private float _intensityBarSlideInDuration = 0.5f;
        [Tooltip("Ease type cho hiệu ứng trượt vào.")]
        [SerializeField] private Ease _intensityBarSlideInEase = Ease.OutQuad;
        [Tooltip("Thời gian cho hiệu ứng chạy của mũi tên.")]
        [SerializeField] private float _arrowMoveDuration = 0.8f;
        [Tooltip("Ease type cho hiệu ứng chạy của mũi tên.")]
        [SerializeField] private Ease _arrowMoveEase = Ease.InOutSine;

        [Header("Current Intensity Display")]
        [Tooltip("Panel hiển thị độ khó hiện tại của round.")]
        [SerializeField] private GameObject _currentIntensityPanel;
        [Tooltip("Text hiển thị giá trị độ khó hiện tại của round.")]
        [SerializeField] private TextMeshProUGUI _currentIntensityText;

        [Header("Countdown")]
        [Tooltip("Panel chứa text đếm ngược đầu round.")]
        [SerializeField] private GameObject _countdownPanel;
        [Tooltip("Text hiển thị số đếm ngược.")]
        [SerializeField] private TextMeshProUGUI _countdownText;

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

        [Tooltip("Popup Shop - là PopupTab, quản lý việc mua bán vật phẩm.")]
        [SerializeField] private ShopPopup _shopPopup;
        [Tooltip("Popup Inventory - là PopupTab, hiển thị vật phẩm người chơi sở hữu.")]
        [SerializeField] private InventoryPopup _inventoryPopup;
        [Tooltip("Popup Option - hiển thị các thiết lập của game.")]
        [SerializeField] private OptionPopup _optionPopup;

        // Coroutine đang chạy để có thể dừng lại nếu cần
        private Coroutine _notificationCoroutine;

        // Dependencies
        private IHomeScreen _homeScreen;
        private IPauseMenu _pauseMenu;
        private IScoreCard _scoreCard;

        // Cache vị trí gốc của thanh intensity để dùng cho animation
        private Vector2 _intensityBarOriginalPosition;
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

            // Inject IHomeScreen
            if (_homeScreenBehaviour != null)
            {
                _homeScreen = _homeScreenBehaviour;
                if (_homeScreen != null)
                {
                    // Subscribe vào event - pattern giống Vue parent-child communication
                    _homeScreen.OnPlayClicked += HandleHomeScreenPlayClicked;
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

            // Gán sự kiện cho nút mở Pause Menu thủ công (bên cạnh phím ESC)
            if (_pauseMenuButton != null)
            {
                _pauseMenuButton.onClick.AddListener(TogglePauseMenu);
            }

            // Gán sự kiện cho các nút Shop / Inventory / Option trên UI chính
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

            // Tu tim AudioSource de phat SFX canh bao timer neu chua gan.
            if (_timerSfxSource == null)
            {
                _timerSfxSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            _pauseAction.Enable();
            GameEvents.OnReturnToHomeRequest += HandleReturnToHome;
        }

        private void OnDisable()
        {
            _pauseAction.Disable();
            GameEvents.OnReturnToHomeRequest -= HandleReturnToHome;
        }

        private void OnDestroy()
        {
            // Unsubscribe để tránh memory leak
            if (_homeScreen != null)
            {
                _homeScreen.OnPlayClicked -= HandleHomeScreenPlayClicked;
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
        }

        private void Update()
        {
            if (_pauseAction.WasPressedThisFrame())
            {
                TogglePauseMenu();
            }
        }

        private void Start()
        {
            // Ẩn các panel khi bắt đầu
            if (_notificationPanel != null) _notificationPanel.SetActive(false);
            if (_timerPanel != null) _timerPanel.SetActive(false);
            if (_intensityBarPanel != null)
            {
                // Cache vị trí gốc của thanh intensity để dùng cho animation
                _intensityBarOriginalPosition = _intensityBarPanel.GetComponent<RectTransform>().anchoredPosition;
                _intensityBarPanel.SetActive(false);
            }
            if (_countdownPanel != null) _countdownPanel.SetActive(false);
            if (_currentIntensityPanel != null) _currentIntensityPanel.SetActive(false);
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
            if (_timerDangerSfx == null || _timerSfxSource == null) return;

            _timerSfxSource.PlayOneShot(_timerDangerSfx);
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
            // --- Safety Checks ---
            if (_intensityBarPanel == null || _intensityArrow == null || _intensityRange == null)
            {
                Debug.LogWarning("[UIManager] Intensity Bar components are not fully assigned. Skipping animation.", this);
                yield break;
            }

            // --- Animation Sequence ---

            // 1. Chuẩn bị: Di chuyển thanh intensity ra ngoài màn hình và kích hoạt nó.
            var intensityBarRect = _intensityBarPanel.GetComponent<RectTransform>();
            intensityBarRect.anchoredPosition = _intensityBarOriginalPosition + _intensityBarStartOffset;
            _intensityBarPanel.SetActive(true);

            // 2. Hiệu ứng trượt vào
            DOTween.To(() => intensityBarRect.anchoredPosition, x => intensityBarRect.anchoredPosition = x, _intensityBarOriginalPosition, _intensityBarSlideInDuration).SetEase(_intensityBarSlideInEase);
            yield return new WaitForSeconds(_intensityBarSlideInDuration);

            // 3. Hiệu ứng mũi tên chạy
            // SỬA LỖI: Mathf.InverseLerp trả về 0 khi min == max (tránh chia cho 0),
            // khiến mũi tên luôn đứng ở mép trái (vị trí số 1) dù intensity của round là bao nhiêu
            // (ví dụ test min = max = 6 thì mũi tên vẫn chỉ số 1 thay vì số 6).
            float normalizedValue;
            if (Mathf.Approximately(minIntensity, maxIntensity))
            {
                // Toàn bộ thanh chỉ đại diện cho MỘT giá trị duy nhất:
                // đạt đúng giá trị đó -> đẩy mũi tên hết cỡ về phía max, ngược lại giữ ở min.
                normalizedValue = currentIntensity >= maxIntensity ? 1f : 0f;
            }
            else
            {
                // Clamp01 để chống trường hợp currentIntensity nằm ngoài khoảng [min, max].
                normalizedValue = Mathf.Clamp01((currentIntensity - minIntensity) / (maxIntensity - minIntensity));
            }
            float rangeWidth = _intensityRange.rect.width;
            float arrowTargetX = normalizedValue * rangeWidth;
            DOTween.To(() => _intensityArrow.anchoredPosition, pos => _intensityArrow.anchoredPosition = pos, new Vector2(arrowTargetX, _intensityArrow.anchoredPosition.y), _arrowMoveDuration).SetEase(_arrowMoveEase);
            yield return new WaitForSeconds(_arrowMoveDuration);

            // 4. Chờ 1 giây
            yield return _waitForSeconds1;

            // 5. Ẩn thanh intensity và reset vị trí mũi tên cho lần sau
            _intensityBarPanel.SetActive(false);
            _intensityArrow.anchoredPosition = new Vector2(0, _intensityArrow.anchoredPosition.y);
        }

        /// <inheritdoc/>
        public void ShowCurrentIntensity(float currentIntensity)
        {
            if (_currentIntensityPanel != null && _currentIntensityText != null)
            {
                _currentIntensityText.text = $"{currentIntensity:F1}"; // Định dạng hiển thị 1 chữ số thập phân, kể cả là số 0.
                _currentIntensityPanel.SetActive(true);
            }
            else Debug.LogWarning("[UIManager] Current Intensity Panel or Text is not assigned. Cannot display current intensity.", this);
        }

        /// <inheritdoc/>
        public void HideIntensityBar()
        {
            if (_intensityBarPanel != null) _intensityBarPanel.SetActive(false);
        }

        /// <inheritdoc/>
        public void HideCurrentIntensity()
        {
            if (_currentIntensityPanel != null) _currentIntensityPanel.SetActive(false);
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
            // Không ẩn intensity bar ở đây. Nó sẽ được hiển thị trong suốt round
            // và chỉ được ẩn ở PostRoundStage.
            // HideIntensityBar();
            if (_countdownPanel == null || _countdownText == null)
            {
                Debug.LogWarning("[UIManager] Countdown panel or text is not assigned.", this);
                yield break;
            }

            _countdownPanel.SetActive(true);

            _countdownText.text = "3";
            yield return _waitForSeconds1;

            _countdownText.text = "2";
            yield return _waitForSeconds1;

            _countdownText.text = "1";
            yield return _waitForSeconds1;

            _countdownText.text = "Go!";
            yield return _waitForSeconds1;

            _countdownPanel.SetActive(false);
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

        public void TogglePauseMenu()
        {
            if (_pauseMenu != null)
            {
                _pauseMenu.Toggle();
            }
        }

        #endregion

        #region Main Menu Popups
        /// <summary>
        /// An tat ca cac popup chinh (Shop, Inventory, Option) de chi mot popup active tai mot thoi diem.
        /// </summary>
        private void CloseMainPopups()
        {
            if (_shopPopup != null)
            {
                _shopPopup.Hide();
            }
            if (_inventoryPopup != null)
            {
                _inventoryPopup.Hide();
            }
            if (_optionPopup != null)
            {
                _optionPopup.Hide();
            }
        }

        /// <summary>
        /// Mo popup Shop khi nguoi choi nhan nut Shop tren UI chinh.
        /// Dong het cac popup khac truoc khi mo de tranh nhieu popup cung hien thi.
        /// </summary>
        private void OpenShopPopup()
        {
            CloseMainPopups();

            if (_shopPopup != null)
            {
                _shopPopup.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] _shopPopup chua duoc gan tren Inspector.");
            }
        }

        /// <summary>
        /// Mo popup Inventory khi nguoi choi nhan nut Inventory tren UI chinh.
        /// Dong het cac popup khac truoc khi mo de tranh nhieu popup cung hien thi.
        /// </summary>
        private void OpenInventoryPopup()
        {
            CloseMainPopups();

            if (_inventoryPopup != null)
            {
                _inventoryPopup.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] _inventoryPopup chua duoc gan tren Inspector.");
            }
        }

        /// <summary>
        /// Mo popup Option khi nguoi choi nhan nut Option tren UI chinh.
        /// Dong het cac popup khac truoc khi mo de tranh nhieu popup cung hien thi.
        /// </summary>
        private void OpenOptionPopup()
        {
            CloseMainPopups();

            if (_optionPopup != null)
            {
                _optionPopup.Show();
            }
            else
            {
                Debug.LogWarning("[UIManager] _optionPopup chua duoc gan tren Inspector.");
            }
        }
        #endregion

        #region Event Handlers
        private void HandleHomeScreenPlayClicked()
        {
            // UIManager nhận event từ HomeScreen (HomeScreen đã tự ẩn trước khi emit event).
            // Hiện Main HUD và đảm bảo Pause Menu bị ẩn khi bắt đầu chơi.
            if (_mainHUD != null) _mainHUD.SetActive(true);
            _pauseMenu?.Hide();
            CloseMainPopups();

            // Bắn event yêu cầu chạy game ngay lập tức.
            // Player duoc spawn ngay; giai doan chao mung "Welcome to Kaboom Chaos!" 
            // duoc xu ly ben trong GameloopManager nhu mot stage dau tien cua game loop.
            Debug.Log("[UIManager] HomeScreen PlayClicked event received. Triggering GameEvents.OnStartGameRequest.");
            GameEvents.TriggerStartGameRequest();
        }

        /// <summary>
        /// Xử lý khi người chơi xác nhận "Back To Home" (từ PauseMenu → ConfirmationPopup):
        /// ẩn Main HUD + các panel gameplay, hiện lại HomeScreen (kèm chuỗi loading).
        /// </summary>
        private void HandleReturnToHome()
        {
            Debug.Log("[UIManager] ReturnToHomeRequest received. Hiding Main HUD and showing HomeScreen.");
            HideGameplayPanels();
            _pauseMenu?.Hide();
            CloseMainPopups();
            _homeScreen?.Show(true); // Quay straight to Home - without loading overlay.
        }
        #endregion

        #region Private Methods & Coroutines

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
            if (_notificationPanel != null) _notificationPanel.SetActive(false);
            if (_timerPanel != null) _timerPanel.SetActive(false);
            if (_intensityBarPanel != null) _intensityBarPanel.SetActive(false);
            if (_countdownPanel != null) _countdownPanel.SetActive(false);
            if (_currentIntensityPanel != null) _currentIntensityPanel.SetActive(false);
            if (_mainHUD != null) _mainHUD.SetActive(false);
            if (_scoreCard != null) _scoreCard.Hide();

            // Ẩn crosshair (dùng alpha để tránh giật lag khi bật/tắt nhanh)
            if (_shiftLockCrosshairCanvasGroup != null) _shiftLockCrosshairCanvasGroup.alpha = 0f;
            if (_firstPersonCrosshairCanvasGroup != null) _firstPersonCrosshairCanvasGroup.alpha = 0f;
        }
        #endregion
    }
}