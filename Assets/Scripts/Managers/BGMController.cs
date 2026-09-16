using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Core.Database;
using Core.Enums;

namespace Managers
{
    /// <summary>
    /// Quản lý nhạc nền (BGM) cho toàn game.
    /// Hoạt động ở cấp scene và được giữ xuyên suột (DontDestroyOnLoad),
    /// KHÔNG gắn trên player → khi player bị xóa (quay về Home) nhạc vẫn tiếp tục.
    /// Tự khởi tạo ngay khi trò chơi load nếu chưa được đặt trong scene.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class BGMController : MonoBehaviour
    {
        #region Fields

        [Tooltip("Database chứa tất cả các file nhạc. Nếu bỏ trống, script tự tìm asset BGMDatabase đang được load.")]
        [SerializeField] private BGMDatabase _bgmDatabase;

        private AudioSource _audioSource;
        private AudioLowPassFilter _lowPassFilter;
        private Coroutine _musicCoroutine;
        private Coroutine _muffleCoroutine;
        private Coroutine _fadeCoroutine;
        private AudioClip _lastPlayedClip;

        private const float NormalCutoffFrequency = 22000f;
        private const float MuffledCutoffFrequency = 1000f;

        /// <summary>
        /// Cờ chặn BGM khi có nhạc riêng của khiên (Magic Shield) đang phát.
        /// </summary>
        private bool _isSuppressed = false;

        /// <summary>
        /// Phân loại nhạc đang phát, giúp phát lại đúng bài sau khi bị khiên tạm chiếm kênh BGM.
        /// </summary>
        private enum BgmMode
        {
            None,
            Home,
            Lobby,
            Gameplay,
            Last30s
        }

        private BgmMode _currentMode = BgmMode.None;
        private float _currentIntensity = 1f;

        // Trạng thái phục hồi nhạc bị gián đoạn vì Magic Shield tạm dừng nhạc nền.
        // Dùng để ResumeMusic() phát LẠI đúng clip ở đúng vị trí (tạm dừng thật sự, không restart bài mới).
        private AudioClip _interruptedClip;
        private float _interruptedTime;
        private bool _interruptedLoop;
        private bool _hasInterruptedState;

        #endregion

        #region Properties

        /// <summary>
        /// Thể hiện Singleton của BGMController.
        /// </summary>
        public static BGMController Instance { get; private set; }

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Khoi tao singleton va cau hinh AudioSource cho BGMController.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Tự tìm BGMDatabase nếu chưa được gán trong Inspector.
            if (_bgmDatabase == null)
            {
                BGMDatabase[] databases = Resources.FindObjectsOfTypeAll<BGMDatabase>();
                if (databases != null && databases.Length > 0)
                {
                    _bgmDatabase = databases[0];
                }
                else
                {
                    Debug.LogWarning("[BGMController] BGMDatabase not assigned and not found in memory. " +
                                     "Please assign it in the Inspector for background music to play.", this);
                }
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _lowPassFilter = GetComponent<AudioLowPassFilter>();
            if (_lowPassFilter == null)
            {
                _lowPassFilter = gameObject.AddComponent<AudioLowPassFilter>();
            }
            _lowPassFilter.cutoffFrequency = NormalCutoffFrequency;

            // Cấu hình AudioSource cho nhạc nền 2D, không bị ảnh hưởng bởi vị trí.
            _audioSource.spatialBlend = 0f;
            _audioSource.playOnAwake = false;

            // Ap dung music volume din Settings (0-100) cu AudioSource (0-1) backend la boot.
            ApplyMusicVolume(SettingsManager.Instance != null ? SettingsManager.Instance.MusicVolume : 100f);
        }

        private void OnEnable()
        {
            GameEvents.OnStartGameRequest += HandleStartGameRequest;
            GameEvents.OnReturnToHomeRequest += HandleReturnToHomeRequest;
            GameEvents.OnMusicPauseRequested += PauseMusic;
            GameEvents.OnMusicResumeRequested += ResumeMusic;
            GameEvents.OnMusicFadeOutRequested += FadeOutMusic;
            GameEvents.OnSettingsMusicVolumeChanged += ApplyMusicVolume;
            GameEvents.OnBgmAudioMuffleRequested += SetMuffled;
        }

        private void OnDisable()
        {
            GameEvents.OnStartGameRequest -= HandleStartGameRequest;
            GameEvents.OnReturnToHomeRequest -= HandleReturnToHomeRequest;
            GameEvents.OnMusicPauseRequested -= PauseMusic;
            GameEvents.OnMusicResumeRequested -= ResumeMusic;
            GameEvents.OnMusicFadeOutRequested -= FadeOutMusic;
            GameEvents.OnSettingsMusicVolumeChanged -= ApplyMusicVolume;
            GameEvents.OnBgmAudioMuffleRequested -= SetMuffled;
        }

                private void ResolveDatabaseIfMissing()
        {
            if (_bgmDatabase != null) return;

            // Có thể BGMController được khởi tạo (qua AutoBootstrap) trước khi prefab chứa
            // database được load vào bộ nhớ, nên thử tìm lại lần nữa lúc này.
            BGMDatabase[] databases = Resources.FindObjectsOfTypeAll<BGMDatabase>();
            if (databases != null && databases.Length > 0)
            {
                _bgmDatabase = databases[0];
            }
        }

        private void Start()
        {
            // Game khởi động ở màn hình Home → phát nhạc Home (fallback nhạc Lobby nếu chưa có).
            ResolveDatabaseIfMissing();
            PlayHomeMusic();
        }

        #endregion
#region Public Methods

        /// <summary>
        /// Ap dung gia tri music volume (0-100 din Settings) cu AudioSource BGM (0-1).
        /// Asculta la GameEvents.OnSettingsMusicVolumeChanged de schimba live din Settings UI.
        /// </summary>
        /// <param name="value">Music volume in [0, 100].</param>
        public void ApplyMusicVolume(float value)
        {
            if (_audioSource == null)
            {
                return;
            }

            _audioSource.volume = Mathf.Clamp01(value / 100f);
        }

        /// <summary>
        /// Thiet lap hoac khoi phuc hieu ung lam mo (muffle) nhac nen qua AudioLowPassFilter.
        /// </summary>
        /// <param name="muffle">True de lam mo BGM, false de khoi phuc binh thuong.</param>
        public void SetMuffled(bool muffle)
        {
            if (_lowPassFilter == null) return;

            if (_muffleCoroutine != null)
            {
                StopCoroutine(_muffleCoroutine);
            }
            _muffleCoroutine = StartCoroutine(SmoothMuffleRoutine(muffle ? MuffledCutoffFrequency : NormalCutoffFrequency, 0.3f));
        }

        private IEnumerator SmoothMuffleRoutine(float targetCutoff, float duration)
        {
            float startCutoff = _lowPassFilter.cutoffFrequency;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (_lowPassFilter != null) _lowPassFilter.cutoffFrequency = Mathf.Lerp(startCutoff, targetCutoff, t);
                yield return null;
            }

            if (_lowPassFilter != null) _lowPassFilter.cutoffFrequency = targetCutoff;
            _muffleCoroutine = null;
        }

        /// <summary>
        /// Gia tri volume muc tieu tu cai dat (0-1).
        /// </summary>
        private float TargetVolume => (SettingsManager.Instance != null ? SettingsManager.Instance.MusicVolume : 100f) / 100f;

        /// <summary>
        /// Phat nhac nen man hinh chinh (Home) kem hieu ung transition crossfade. Reset shield suppression va interrupted state.
        /// </summary>
        public void PlayHomeMusic()
        {
            _isSuppressed = false;
            _hasInterruptedState = false;
            if (_bgmDatabase == null) return;

            AudioClip clip = _bgmDatabase.HomeMusic != null ? _bgmDatabase.HomeMusic : _bgmDatabase.LobbyMusic;
            _currentMode = BgmMode.Home;
            PlayClipWithTransition(clip, true, 0.6f);
        }

        /// <summary>
        /// Phát nhạc sảnh chờ (trong giai đoạn chọn map / chờ giữa các round) kèm hiệu ứng transition crossfade.
        /// </summary>
        public void PlayLobbyMusic()
        {
            _isSuppressed = false;
            _hasInterruptedState = false;
            if (_bgmDatabase == null) return;

            _currentMode = BgmMode.Lobby;
            PlayClipWithTransition(_bgmDatabase.LobbyMusic, true, 0.6f);
        }

        /// <summary>
        /// Fade out nhạc nền hiện tại về 0 và dừng nhạc (dùng khi teleport vào arena hoặc chuyển cảnh).
        /// </summary>
        /// <param name="fadeDuration">Thời gian (giây) để fade out nhạc nền.</param>
        public void FadeOutMusic(float fadeDuration = 1.0f)
        {
            if (_audioSource == null || !_audioSource.isPlaying)
            {
                StopMusic();
                return;
            }

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }
            _fadeCoroutine = StartCoroutine(FadeOutRoutine(fadeDuration));
        }

        private IEnumerator FadeOutRoutine(float duration)
        {
            float startVolume = _audioSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (_audioSource != null)
                {
                    _audioSource.volume = Mathf.Lerp(startVolume, 0f, t);
                }
                yield return null;
            }

            StopAudioInternal();
            ApplyMusicVolume(SettingsManager.Instance != null ? SettingsManager.Instance.MusicVolume : 100f);
            _fadeCoroutine = null;
        }

        /// <summary>
        /// Phát một audio clip với hiệu ứng transition (fade out clip hiện tại / fade in clip mới).
        /// </summary>
        public void PlayClipWithTransition(AudioClip clip, bool loop, float fadeDuration = 0.6f)
        {
            if (clip == null) return;

            // Cùng bài và đang phát thì giữ nguyên.
            if (_audioSource != null && _audioSource.clip == clip && _audioSource.isPlaying && !_isSuppressed) return;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(FadeTransitionRoutine(clip, loop, fadeDuration));
        }

        private IEnumerator FadeTransitionRoutine(AudioClip newClip, bool loop, float duration)
        {
            float targetVol = TargetVolume;

            if (_audioSource != null && _audioSource.isPlaying && _audioSource.clip != null && duration > 0f)
            {
                float startVol = _audioSource.volume;
                float elapsed = 0f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    if (_audioSource != null)
                    {
                        _audioSource.volume = Mathf.Lerp(startVol, 0f, t);
                    }
                    yield return null;
                }
            }

            StopAudioInternal();

            if (newClip != null)
            {
                _audioSource.clip = newClip;
                _audioSource.loop = loop;
                _audioSource.volume = 0f;
                _audioSource.Play();

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    if (_audioSource != null)
                    {
                        _audioSource.volume = Mathf.Lerp(0f, targetVol, t);
                    }
                    yield return null;
                }
                _audioSource.volume = targetVol;
            }

            _fadeCoroutine = null;
        }

        /// <summary>
        /// Bắt đầu playlist nhạc gameplay dựa trên độ khó hiện tại.
        /// </summary>
        /// <param name="intensity">Độ khó của round đấu.</param>
        public void PlayGameplayMusic(float intensity)
        {
            if (_bgmDatabase == null) return;

            _currentIntensity = intensity;
            // SỬA LỖI: Luôn ghi nhận mode/độ khó TRƯỚC guard _isSuppressed, để nếu đang bị
            // Magic Shield tạm dừng, lần ResumeMusic() sau đó vẫn phát đúng loại nhạc gameplay.
            _currentMode = BgmMode.Gameplay;

            if (_isSuppressed) return;

            StopMusic();

            List<AudioClip> playlist = intensity < 4 ? _bgmDatabase.NormalGameplayMusic : _bgmDatabase.IntenseGameplayMusic;
            if (playlist == null || playlist.Count == 0)
            {
                Debug.LogWarning("[BGMController] No gameplay music playlist for intensity: " + intensity + ".", this);
                return;
            }

            _musicCoroutine = StartCoroutine(PlaylistCoroutine(playlist));
        }

        /// <summary>
        /// Phát nhạc 30 giây cuối của round đấu.
        /// </summary>
        /// <param name="intensity">Độ khó của round đấu.</param>
        public void PlayLast30sMusic(float intensity)
        {
            if (_bgmDatabase == null) return;

            _currentIntensity = intensity;
            // SỬA LỖI #1/#3: LUÔN cập nhật mode thành Last30s KỂ CẢ khi đang bị Magic Shield chặn.
            // Nhờ đó khi khiên hết hạn, ResumeMusic() sẽ phát nhạc 30s cuối thay vì nhạc gameplay thường.
            _currentMode = BgmMode.Last30s;

            if (_isSuppressed) return;

            AudioClip clipToPlay = intensity < 4 ? _bgmDatabase.Last30sNormalMusic : _bgmDatabase.Last30sIntenseMusic;
            PlayClip(clipToPlay, true);
        }

        /// <summary>
        /// Dung audio source va dung playlist coroutine ma KHONG tu huy _fadeCoroutine dang chay.
        /// </summary>
        private void StopAudioInternal()
        {
            if (_musicCoroutine != null)
            {
                StopCoroutine(_musicCoroutine);
                _musicCoroutine = null;
            }
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop();
            }
        }

        /// <summary>
        /// Dừng hoàn toàn mọi nhạc nền đang phát.
        /// </summary>
        public void StopMusic()
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }
            StopAudioInternal();
        }

        /// <summary>
        /// Tạm dừng nhạc nền để nhường chỗ cho nhạc riêng của khiên (Magic Shield).
        /// Ghi nhận clip + vị trí đang phát TRƯỚC khi dừng, để ResumeMusic() phát LẠI đúng bài/đúng chỗ
        /// (hành vi = PAUSE nhạc nền, không phải restart sang 1 bài ngẫu nhiên mới).
        /// </summary>
        public void PauseMusic()
        {
            _isSuppressed = true;

            // Ghi nhận trạng thái nhạc đang phát (nếu có) trước khi dừng.
            if (_audioSource.isPlaying && _audioSource.clip != null)
            {
                _interruptedClip = _audioSource.clip;
                _interruptedTime = _audioSource.time;
                _interruptedLoop = _audioSource.loop;
                _hasInterruptedState = true;
            }
            else
            {
                _hasInterruptedState = false;
            }

            StopMusic();
        }

        /// <summary>
        /// Tiến hành lại nhạc nền sau khi khiên hết hạn.
        /// Phát lại đúng loại nhạc trước khi bị tạm dừng; nếu đó là nhạc gameplay → phát lại ĐÚNG bài ở ĐÚNG vị trí.
        /// Nếu ngưỡng 30s cuối xảy ra trong lúc khiên đang chặn → phát nhạc 30s cuối, đồng bộ offset
        /// sao cho bài hết đúng lúc round kết thúc. Nếu round đã kết thúc → chuyển về nhạc sảnh chờ.
        /// </summary>
        public void ResumeMusic()
        {
            if (!_isSuppressed) return;

            _isSuppressed = false;

            // SỬA LỖI: Nếu round đã kết thúc (hoặc không còn diễn ra) mà khiên mới hết, không được
            // phát lại nhac gameplay/30s nữa — phải về đúng nhạc sảnh chờ.
            if (_currentMode == BgmMode.Gameplay || _currentMode == BgmMode.Last30s)
            {
                var gameloop = GameloopManager.Instance;
                bool isRoundLive = gameloop != null && (gameloop.CurrentState == GameState.RoundActive || gameloop.CurrentState == GameState.PreRound);
                if (!isRoundLive)
                {
                    _hasInterruptedState = false;
                    PlayLobbyMusic();
                    return;
                }
            }

            switch (_currentMode)
            {
                case BgmMode.Home:
                    if (_hasInterruptedState && _interruptedClip != null)
                    {
                        PlayClipAtTime(_interruptedClip, _interruptedTime, _interruptedLoop);
                    }
                    else PlayHomeMusic();
                    break;
                case BgmMode.Last30s:
                    ResumeLast30sMusic();
                    break;
                case BgmMode.Gameplay:
                    ResumeGameplayMusic();
                    break;
                default:
                    PlayLobbyMusic();
                    break;
            }

            _hasInterruptedState = false;
        }

        #endregion

        #region Event Handlers

        private void HandleStartGameRequest()
        {
            // Bắt đầu game → giai đoạn sảnh chờ (chọn map / xây map).
            PlayLobbyMusic();
        }

        private void HandleReturnToHomeRequest()
        {
            // Quay về màn hình chính → phát nhạc Home, reset suppression state.
            _isSuppressed = false;
            _hasInterruptedState = false;
            PlayHomeMusic();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Phục hồi nhạc gameplay SAU KHI bị Magic Shield tạm dừng:
        /// phát lại ĐÚNG clip ở ĐÚNG thời điểm đã tạm dừng rồi tiếp tục playlist (không chọn bài ngẫu nhiên mới).
        /// Hành vi = PAUSE/RESUME nhạc nền (bug #2).
        /// </summary>
        private void ResumeGameplayMusic()
        {
            if (_bgmDatabase == null) return;

            var playlist = _currentIntensity < 4 ? _bgmDatabase.NormalGameplayMusic : _bgmDatabase.IntenseGameplayMusic;
            if (playlist == null || playlist.Count == 0) return;

            StopMusic();

            if (_hasInterruptedState && _interruptedClip != null && playlist.Contains(_interruptedClip))
            {
                _musicCoroutine = StartCoroutine(PlaylistCoroutine(playlist, _interruptedClip, _interruptedTime));
            }
            else
            {
                _musicCoroutine = StartCoroutine(PlaylistCoroutine(playlist));
            }
        }

        /// <summary>
        /// Phục hồi nhạc 30 giây cuối của round.
        /// - Nếu nhạc 30s đã TỪNG PHÁT trước khi khiên xuất hiện → phát lại đúng vị trí bị ngắt quãng.
        /// - Nếu ngưỡng 30s XẢY RA trong lúc khiên đang chặn (mode đã ghi nhận Last30s nhưng chưa phát)
        ///   → bắt đầu ở offset = clip.length - thời gian còn lại của round, để bài hết đúng lúc round hết (bug #3).
        /// </summary>
        private void ResumeLast30sMusic()
        {
            if (_bgmDatabase == null) return;

            AudioClip clip = _currentIntensity < 4 ? _bgmDatabase.Last30sNormalMusic : _bgmDatabase.Last30sIntenseMusic;
            if (clip == null) return;

            if (_hasInterruptedState && _interruptedClip == clip)
            {
                // Nhạc 30s đang phát thì bị tạm dừng → resume đúng vị trí cũ.
                PlayClipAtTime(clip, _interruptedTime, true);
                return;
            }

            // 30s trigger bị bỏ lỡ khi khiên đang chặn → bắt đầu ở OFFSET sao cho khớp thời điểm round kết thúc.
            float roundRemaining = GameloopManager.Instance != null ? GameloopManager.Instance.CurrentRoundTimeRemaining : 0f;
            float startTime = Mathf.Max(0f, clip.length - roundRemaining);

            PlayClipAtTime(clip, startTime, true);
        }

        /// <summary>
        /// Phát một clip tại một vị trí bắt đầu cụ thể (dùng để resume nhạc nền sau khi tạm dừng vì khiên).
        /// </summary>
        private void PlayClipAtTime(AudioClip clip, float startTime, bool loop)
        {
            StopMusic();
            if (clip == null) return;

            if (_audioSource.clip == clip && _audioSource.isPlaying && !_isSuppressed) return;

            _audioSource.clip = clip;
            _audioSource.loop = loop;
            _audioSource.time = Mathf.Clamp(startTime, 0f, clip.length);
            _audioSource.Play();
        }

        private IEnumerator PlaylistCoroutine(List<AudioClip> playlist, AudioClip resumeClip = null, float resumeTime = 0f)
        {
            var playableClips = new List<AudioClip>(playlist);

            // Nếu đang phục hồi sau khi khiên tạm dừng: phát lại đúng clip tại đúng vị trí trước,
            // rồi mới tiếp tục chọn bài ngẫu nhiên như bình thường.
            if (resumeClip != null && playableClips.Contains(resumeClip))
            {
                _audioSource.clip = resumeClip;
                _audioSource.loop = false;
                _audioSource.time = Mathf.Clamp(resumeTime, 0f, resumeClip.length);
                _audioSource.Play();
                _lastPlayedClip = resumeClip;

                yield return new WaitWhile(() => _audioSource.isPlaying);
                yield return new WaitForSeconds(0.2f);
            }

            while (playableClips.Count > 0)
            {
                var availableClips = playableClips.Where(c => c != _lastPlayedClip).ToList();
                if (availableClips.Count == 0 && playableClips.Count > 0) availableClips = playableClips;

                AudioClip clipToPlay = availableClips[Random.Range(0, availableClips.Count)];

                _audioSource.clip = clipToPlay;
                _audioSource.loop = false;
                _audioSource.Play();
                _lastPlayedClip = clipToPlay;

                // Chờ clip phát hết rồi mới chuyển bài tiếp theo.
                yield return new WaitWhile(() => _audioSource.isPlaying);
                yield return new WaitForSeconds(0.2f);
            }
        }

        private void PlayClip(AudioClip clip, bool loop)
        {
            StopMusic();
            if (clip == null) return;

            // Cùng bài & đang phát thì không làm gì cả (tránh phát lại từ đầu).
            if (_audioSource.clip == clip && _audioSource.isPlaying && !_isSuppressed) return;

            _audioSource.clip = clip;
            _audioSource.loop = loop;
            _audioSource.Play();
        }

        #endregion
    }
}