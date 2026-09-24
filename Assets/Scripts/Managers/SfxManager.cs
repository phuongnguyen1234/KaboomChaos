using UnityEngine;
using System.Collections.Generic;
using Core;
using Core.Interfaces;

namespace Managers
{
    /// <summary>
    /// Manager quan tam cho hieu ung am thanh (SFX) cua game. Tat ca hieu ung am thanh
    /// deu duoc phat thong qua class nay, ap dung volume SFX tu SettingsManager (slider SFX)
    /// va cap nhat theo thoi gian thuc.
    /// </summary>
    /// <remarks>
    /// Cac AudioSource duoc tai su dung thong qua mot object pool (ke thua BaseGameObjectPoolManager).
    /// Khi lay source tu pool, GameObject duoc SetActive(true) truoc khi Play() nên tranh duoc
    /// loi "Can not play a disabled audio source". Volume mg de chay = SettingsManager.SfxVolume * volume base.
    /// Khi user doi slider SFX (OnSettingsSfxVolumeChanged), tat ca AudioSource dang phat se duoc cap nhat.
    /// </remarks>
    public class SfxManager : BaseGameObjectPoolManager, ISfxManager
    {
        #region Singleton

        /// <summary>
        /// Instance singleton cua SfxManager.
        /// </summary>
        public static SfxManager Instance { get; private set; }

        #endregion

        #region Constants

        // So luong source SFX duoc khoi tao san trong pool de giam latency khi phat lan dau.
        private const int PoolInitialSize = 12;

        #endregion

        #region Fields

        // "Prefab nen" tao ra luc runtime: dung lam mau de pool instantiate cac AudioSource moi.
        private GameObject _sourcePrefab;

        // Danh sach cac playback dang hoat dong (one-shot hoac loop) duoc theo doi de cap nhat volume.
        private readonly List<SfxPlayback> _active = new();

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Khoi tao singleton, dang ky SfxService, pool container va prewarm pool nguon SFX.
        /// </summary>
        protected override void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            base.Awake(); // Tao container chua cac source khong hoat dong.

            Instance = this;
            SfxService.Instance = this;
            DontDestroyOnLoad(gameObject);

            _initialPoolSize = PoolInitialSize;
            EnsureSourcePrefab();
            PreWarmPool();
        }

        private void OnEnable()
        {
            // Khi user doi slider SFX, cap nhat tat ca AudioSource dang phat.
            GameEvents.OnSettingsSfxVolumeChanged += HandleSfxVolumeChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnSettingsSfxVolumeChanged -= HandleSfxVolumeChanged;
        }

        #endregion

        #region One-Shot API

        /// <summary>
        /// Phat mot SFX one-shot 3D positional (truong bat, pickup, hit...).
        /// Source duoc tai su dung tu dong sau khi clip ket thuc.
        /// </summary>
        /// <param name="clip">AudioClip can phat.</param>
        /// <param name="position">Vi tri world cho audio 3D.</param>
        /// <param name="volume">Volume base (0-1) cua clip (nhan voi SFX volume trong Settings).</param>
        /// <param name="pitch">Pitch cua clip.</param>
        /// <param name="spatialBlend">Muc do spatial: 0 = 2D, 1 = 3D. Mac dinh 0.5 (trung gian).</param>
        /// <returns>AudioSource dang phat, hoac null neu clip == null.</returns>
        public AudioSource PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 0.5f)
        {
            return StartPlayback(clip, position, null, spatialBlend, volume, pitch, false);
        }

        /// <summary>
        /// Phat mot SFX one-shot 2D (khong chieu, cho UI/HUD/menu canh bao).
        /// </summary>
        /// <param name="clip">AudioClip can phat.</param>
        /// <param name="volume">Volume base (0-1).</param>
        /// <param name="pitch">Pitch cua clip.</param>
        /// <returns>AudioSource dang phat, hoac null n neu clip == null.</returns>
        public AudioSource PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
        {
            return StartPlayback(clip, Vector3.zero, null, 0f, volume, pitch, false);
        }

        #endregion

        #region Loop API

        /// <summary>
        /// Phat mot SFX loop (tic-tac, hum, beep) va tra ve handle de stop/pitch.
        /// Source duoc gan theo object parent, do vay am thanh di theo object va vi tri duoc update.
        /// </summary>
        /// <param name="clip">AudioClip can phat.</param>
        /// <param name="parent">Transform parent (object di chuyen) de vi tri trong khong gian local.</param>
        /// <param name="volume">Volume base (0-1).</param>
        /// <param name="pitch">Pitch base cua loop.</param>
        /// <param name="spatial">True = 3D spatial, false = 2D.</param>
        /// <returns>Handle de goi .Stop(), hoac null n neu clip == null.</returns>
        public ISfxLoopHandle PlaySfxLoop(AudioClip clip, Transform parent, float volume = 1f, float pitch = 1f, bool spatial = true)
        {
            if (clip == null) return null;

            SfxPlayback pb = StartPlayInternal(clip, Vector3.zero, parent, spatial ? 1f : 0f, volume, pitch, true);
            return pb != null ? new SfxLoopHandle(this, pb) : null;
        }

        /// <summary>
        /// Tra source ve pool, duoc goi tu handle.Stop().
        /// </summary>
        /// <param name="playback">Playback can dung.</param>
        private void StopLoop(SfxPlayback playback)
        {
            Release(playback);
        }

        #endregion

        #region Volume

        /// <summary>
        /// SFX volume hien tai (0-1) lay tu SettingsManager.
        /// </summary>
        public float SfxVolume01 => SettingsManager.Instance != null ? SettingsManager.Instance.SfxVolume / 100f : 1f;

        /// <summary>
        /// Cap nhat volume cac AudioSource dang phat khi user doi slider SFX.
        /// </summary>
        /// <param name="value">SFX volume moi (0-100 trong Settings).</param>
        private void HandleSfxVolumeChanged(float value)
        {
            float newVolume = Mathf.Clamp01(value / 100f);

            foreach (var playback in _active)
            {
                if (playback != null && playback.Source != null)
                {
                    playback.Source.volume = playback.BaseVolume * newVolume;
                }
            }
        }

        #endregion

        #region Internal

        /// <summary>
        /// Wrapper cho playback noi bo cua mot one-shot (tra ve Source).
        /// </summary>
        private AudioSource StartPlayback(AudioClip clip, Vector3 position, Transform parent, float spatialBlend, float volume, float pitch, bool loop)
        {
            SfxPlayback pb = StartPlayInternal(clip, position, parent, spatialBlend, volume, pitch, loop);
            return pb?.Source;
        }

        /// <summary>
        /// Lay source tu pool, cai dat player va them playback vao danh sach active (de cap nhat volume live).
        /// Source duoc SetActive(true) ngay trong GetFromPool, vi vay Play() luon lon tren source dang hoat dong.
        /// </summary>
        private SfxPlayback StartPlayInternal(AudioClip clip, Vector3 position, Transform parent, float spatialBlend, float volume, float pitch, bool loop)
        {
            if (clip == null) return null;

            EnsureSourcePrefab();

            GameObject go = GetFromPool(_sourcePrefab, position, Quaternion.identity);
            if (go == null) return null;

            AudioSource src = go.GetComponent<AudioSource>();
            src.spatialBlend = Mathf.Clamp01(spatialBlend);
            src.loop = loop;

            if (parent != null)
            {
                // Gan transform theo object di chuyen (bomb/player) de follow vi tri cua no.
                go.transform.SetParent(parent, false);
                go.transform.localPosition = Vector3.zero;
            }

            src.clip = clip;
            src.pitch = pitch;

            float baseVolume = Mathf.Clamp01(volume);
            src.volume = baseVolume * SfxVolume01;
            src.Play();

            SfxPlayback playback = new(src, baseVolume);
            _active.Add(playback);

            // One-shot: lich ra pool sau khi clip phat xong.
            if (!loop)
            {
                StartCoroutine(ReleaseAfter(playback, clip.length + 0.2f));
            }

            return playback;
        }

        /// <summary>
        /// Tao mot GameObject co AudioSource de lam "prefab nen" cho pool (chi la kuan, khong dung truc tiep).
        /// </summary>
        private void EnsureSourcePrefab()
        {
            if (_sourcePrefab != null) return;

            var go = new GameObject("SfxSource");
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            go.SetActive(false);
            _sourcePrefab = go;
        }

        /// <summary>
        /// Khoi tao san mot so nguon (inactive) trong pool de cac lan phat dau tien khong phai instantiate moi.
        /// </summary>
        private void PreWarmPool()
        {
            for (int i = 0; i < _initialPoolSize; i++)
            {
                GameObject go = GetFromPool(_sourcePrefab, Vector3.zero, Quaternion.identity);
                if (go != null)
                {
                    ReturnToPool(go);
                }
            }
        }

        /// <summary>
        /// Reset trang thai cua source moi khi duoc lay ra tu pool de chac chan ko dang phat (an toan cho Play()).
        /// </summary>
        /// <param name="instance">Instance vua duoc lay ra.</param>
        protected override void OnGetInstance(GameObject instance)
        {
            base.OnGetInstance(instance);
            if (instance != null && instance.TryGetComponent<AudioSource>(out var src))
            {
                src.playOnAwake = false;
                src.loop = false;
                src.Stop();
            }
        }

        /// <summary>
        /// Coroutine giai phong mot playback sau mot khoang thoi gian (one-shot da ket thuc).
        /// </summary>
        private System.Collections.IEnumerator ReleaseAfter(SfxPlayback playback, float delay)
        {
            yield return new WaitForSeconds(delay);
            Release(playback);
        }

        /// <summary>
        /// Tra source lai pool (stop, tach parent, inactive) de tai su dung.
        /// </summary>
        private void Release(SfxPlayback playback)
        {
            if (playback == null) return;
            if (!_active.Remove(playback)) return;

            AudioSource src = playback.Source;
            if (src != null)
            {
                src.Stop();
                ReturnToPool(src.gameObject);
            }
        }

        #endregion

        #region Handles & Inner Types

        /// <summary>
        /// Du lieu noi bo cua mot playback: source va volume base ban dau (de cap nhat live).
        /// </summary>
        internal class SfxPlayback
        {
            internal readonly AudioSource Source;
            internal readonly float BaseVolume;

            internal SfxPlayback(AudioSource source, float baseVolume)
            {
                Source = source;
                BaseVolume = baseVolume;
            }
        }

        /// <summary>
        /// Handle duoc tra ve tu PlaySfxLoop. Holder goi Stop() de dung lai va SetPitch() de chinh pitch.
        /// </summary>
        public class SfxLoopHandle : ISfxLoopHandle
        {
            private SfxManager _manager;
            private SfxPlayback _playback;

            internal SfxLoopHandle(SfxManager manager, SfxPlayback playback)
            {
                _manager = manager;
                _playback = playback;
            }

            /// <summary>
            /// Cap nhat pitch audio loop.
            /// </summary>
            /// <param name="pitch">Pitch de set.</param>
            public void SetPitch(float pitch)
            {
                if (_playback != null && _playback.Source != null)
                {
                    _playback.Source.pitch = pitch;
                }
            }

            /// <summary>
            /// Dung loop va tra lai source ve pool. Idempotent - an toan khi goi nhieu lan.
            /// </summary>
            public void Stop()
            {
                if (_manager != null && _playback != null)
                {
                    _manager.StopLoop(_playback);
                }
                _playback = null;
                _manager = null;
            }
        }

        #endregion
    }
}
