// using UnityEngine;
// using System.Collections.Generic;

// namespace Core
// {
//     /// <summary>
//     /// Manager centralizat pentru SFX-uri cua game. Tuate effectel sonore se jocată prin el
//     /// applicand volume la SFX din SettingsManager (slider SFX), actualizat live. Unici
//     /// AudioSource-ul se basează pe un pool reutilizable, deci totul san coherent și dispers.
//     /// </summary>
//     /// <remarks>
//     /// AudioSource-ul soane creată de la manager (pool reutilizable) și volume applicata lor =
//     /// SettingsManager.SfxVolume * volume base. Neu user schimba slider SFX (OnSettingsSfxVolumeChanged),
//     /// tot AudioSource-ul player current se sub aktualizeaza.
//     /// </remarks>
//     public class SfxManager : MonoBehaviour
//     {
//         #region Singleton

//         /// <summary>
//         /// Instance singleton al SfxManager.
//         /// </summary>
//         public static SfxManager Instance { get; private set; }

//         #endregion

//         #region Constants

//         // Taglia initială / maximală a poolului de players SFX.
//         private const int PoolInitialSize = 12;
//         private const int PoolMaxSize = 32;

//         #endregion

//         #region Fields

//         // Poolul de GameObject inactive (de reutiliza source AudioSource).
//         private readonly List<GameObject> _pool = new();

//         // AudioSource-ul active (one-shot sau loop) se trackuiă pentru live-update de volume.
//         private readonly List<SfxPlayback> _active = new();

//         #endregion

//         #region Unity Lifecycle

//         /// <summary>
//         /// Autocratează SfxManager neu nu existent (same pattern cu SettingsManager/BGMController).
//         /// </summary>
//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//         private static void AutoBootstrap()
//         {
//             if (Instance == null)
//             {
//                 var o = new GameObject("[SfxManager]");
//                 o.AddComponent<SfxManager>();
//             }
//         }

//         private void Awake()
//         {
//             if (Instance != null && Instance != this)
//             {
//                 Destroy(gameObject);
//                 return;
//             }

//             Instance = this;
//             DontDestroyOnLoad(gameObject);

//             // Pre-dimensionezza poolul de AudioSource reutilizable.
//             for (int i = 0; i < PoolInitialSize; ++i)
//             {
//                 _pool.Add(CreatePooledSource());
//             }
//         }

//         private void OnEnable()
//         {
//             // Neu user schimba slider SFX, actualizeaza tot AudioSource-ul active.
//             GameEvents.OnSettingsSfxVolumeChanged += HandleSfxVolumeChanged;
//         }

//         private void OnDisable()
//         {
//             GameEvents.OnSettingsSfxVolumeChanged -= HandleSfxVolumeChanged;
//         }

//         #endregion
// #region One-Shot API

//         /// <summary>
//         /// Jocă o one-shot SFX 3D positional (explosion, pickup, hit...).
//         /// Source ei se recicleaza automatic după final clip.
//         /// </summary>
//         /// <param name="clip">AudioClip de cai cui.</param>
//         /// <param name="position">Pozition world pentru audio 3D.</param>
//         /// <param name="volume">Volume base (0-1) al clip (inmultim cu SFX volume din Settings).</param>
//         /// <param name="pitch">Pitch al clip.</param>
//         /// <returns>AudioSource player, sau null neu clip==null.</returns>
//         public AudioSource PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f)
//         {
//             return StartPlayback(clip, position, null, true, volume, pitch, false);
//         }

//         /// <summary>
//         /// Jocă o one-shot SFX 2D (a-spatial, pentru UI/HUD/menu alert).
//         /// </summary>
//         /// <param name="clip">AudioClip de cai cui.</param>
//         /// <param name="volume">Volume base (0-1).</param>
//         /// <param name="pitch">Pitch al clip.</param>
//         /// <returns>AudioSource player, sau null neu clip==null.</returns>
//         public AudioSource PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f)
//         {
//             return StartPlayback(clip, default, null, false, volume, pitch, false);
//         }

//         #endregion

//         #region Loop API

//         /// <summary>
//         /// Jocă o SFX loop (ticking, hum, beep) ca o handle de oprire/stop/pitch.
//         /// Source ei grava de la obiect parent, deci audio-le sană cu el și pozition se actualizeaza.
//         /// </summary>
//         /// <param name="clip">AudioClip de cai cui.</param>
//         /// <param name="parent">Transform parent (obiect carre se mișchă) pentru pozition spatial local.</param>
//         /// <param name="volume">Volume base (0-1).</param>
//         /// <param name="pitch">Pitch base al loop.</param>
//         /// <param name="spatial">True = 3D spatial, false = 2D (de ecudo magic shield).</param>
//         /// <returns>Handle pentru ce.Stop(), sau null neu clip==null.</returns>
//         public SfxLoopHandle PlaySfxLoop(AudioClip clip, Transform parent, float volume = 1f, float pitch = 1f, bool spatial = true)
//         {
//             if (clip == null) return null;

//             SfxPlayback pb = StartPlaybackInternal(clip, default, parent, spatial, volume, pitch, true);
//             return pb != null ? new SfxLoopHandle(this, pb) : null;
//         }

//         /// <summary>
//         /// Releaseara source al pool, chamat de handle.Stop().
//         /// </summary>
//         /// <param name="playback">Playback de a que nit.</param>
//         private void StopLoop(SfxPlayback playback)
//         {
//             Release(playback);
//         }

//         #endregion

//         #region Volume

//         /// <summary>
//         /// SFX volume current (0-1) luat din SettingsManager (source of truth persistent).
//         /// </summary>
//         public float SfxVolume01 => SettingsManager.Instance != null ? SettingsManager.Instance.SfxVolume / 100f : 1f;

//         /// <summary>
//         /// Actualizeaza volume AudioSource-ului active current neu user schimba slider SFX.
//         /// </summary>
//         /// <param name="value">Nou SFX volume (0-100 din Settings).</param>
//         private void HandleSfxVolumeChanged(float value)
//         {
//             float newVolume = Mathf.Clamp01(value / 100f);

//             foreach (var playback in _active)
//             {
//                 if (playback != null && playback.Source != null)
//                 {
//                     playback.Source.volume = playback.BaseVolume * newVolume;
//                 }
//             }
//         }

//         #endregion
// #region Internal

//         /// <summary>
//         /// Wrapper pentru playback intern de un un ci shot (returaneaza Source).
//         /// </summary>
//         private AudioSource StartPlayback(AudioClip clip, Vector3 position, Transform parent, bool spatial, float volume, float pitch, bool loop)
//         {
//             SfxPlayback pb = StartPlaybackInternal(clip, position, parent, spatial, volume, pitch, loop);
//             return pb != null ? pb.Source : null;
//         }

//         /// <summary>
//         /// Configureaza source player si inregistreaza playback in lista active (de live-update volume).
//         /// </summary>
//         private SfxPlayback StartPlaybackInternal(AudioClip clip, Vector3 position, Transform parent, bool spatial, float volume, float pitch, bool loop)
//         {
//             if (clip == null) return null;

//             GameObject go = Acquire();
//             AudioSource src = go.GetComponent<AudioSource>();
//             src.spatialBlend = spatial ? 1f : 0f;

//             if (parent != null)
//             {
//                 // Parent la transform al object carre se mișchă (bomb/player) de co sandu si urmi.
//                 go.transform.SetParent(parent, false);
//                 go.transform.localPosition = Vector3.zero;
//             }
//             else if (spatial)
//             {
//                 go.transform.position = position;
//             }

//             src.clip = clip;
//             src.loop = loop;
//             src.pitch = pitch;

//             float baseVolume = Mathf.Clamp01(volume);
//             src.volume = baseVolume * SfxVolume01;
//             src.Play();

//             SfxPlayback playback = new(src, baseVolume);
//             _active.Add(playback);

//             // One-shot: programma release din pool dupa final al clip.
//             if (!loop)
//             {
//                 StartCoroutine(ReleaseAfter(playback, clip.length + 0.2f));
//             }

//             return playback;
//         }

//         /// <summary>
//         /// Crateaza o GameObject cu AudioSource, gă de popup in pool (inactive).
//         /// </summary>
//         private GameObject CreatePooledSource()
//         {
//             var go = new GameObject("SfxSource");
//             go.AddComponent<AudioSource>();
//             go.SetActive(false);
//             return go;
//         }

//         /// <summary>
//         /// Draç din pool o GameObject (sau creaza nou), le activeaza.
//         /// </summary>
//         private GameObject Acquire()
//         {
//             if (!_pool.isEmpty())
//             {
//                 GameObject go = _pool.RemoveAt(_pool.size() - 1);
//                 go.SetActive(true);
//                 return go;
//             }
//             return CreatePooledSource();
//         }

//         /// <summary>
//         /// Coroutine care release o playback dupa un delay (one-shot terminalson).
//         /// </summary>
//         private System.Collections.IEnumerator ReleaseAfter(SfxPlayback playback, float delay)
//         {
//             yield return new WaitForSeconds(delay);
//             Release(playback);
//         }

//         /// <summary>
//         /// Recicleaza source al pool (stop, detach parent, inactive).
//         /// </summary>
//         private void Release(SfxPlayback playback)
//         {
//             if (playback == null) return;
//             if (!_active.Remove(playback)) return;

//             AudioSource src = playback.Source;
//             if (src != null)
//             {
//                 src.Stop();
//                 GameObject go = src.gameObject;
//                 go.transform.SetParent(null, false);
//                 go.transform.position = Vector3.zero;

//                 if (_pool.size() < PoolMaxSize)
//                 {
//                     go.SetActive(false);
//                     _pool.Add(go);
//                 }
//                 else
//                 {
//                     Destroy(go);
//                 }
//             }
//         }

//         #endregion
// #region Handles & Inner Types

//         /// <summary>
//         /// Date interne de un playback: source si volume base original (de live-update).
//         /// </summary>
//         private static class SfxPlayback
//         {
//             public readonly AudioSource Source;
//             public readonly float BaseVolume;

//             public SfxPlayback(AudioSource source, float baseVolume)
//             {
//                 Source = source;
//                 BaseVolume = baseVolume;
//             }
//         }

//         /// <summary>
//         /// Handle retornat de PlaySfxLoop. Holderul apela Stop() pentru iznire si SetPitch() pentru tune.
//         /// </summary>
//         public static class SfxLoopHandle
//         {
//             private SfxManager _manager;
//             private SfxPlayback _playback;

//             internal SfxLoopHandle(SfxManager manager, SfxPlayback playback)
//             {
//                 _manager = manager;
//                 _playback = playback;
//             }

//             /// <summary>
//             /// Actualizeaza pitch super audio loop (de laser drone aim ramp, magic shield stack...).
//             /// </summary>
//             /// <param name="pitch">Nou pitch.</param>
//             public void SetPitch(float pitch)
//             {
//                 if (_playback != null && _playback.Source != null)
//                 {
//                     _playback.Source.pitch = pitch;
//                 }
//             }

//             /// <summary>
//             /// Hi stop loop si recicleeaza source in pool. Idempotent - safe de invocate mult de or.
//             /// </summary>
//             public void Stop()
//             {
//                 if (_manager != null && _playback != null)
//                 {
//                     _manager.StopLoop(_playback);
//                 }
//                 _playback = null;
//                 _manager = null;
//             }
//         }

//         #endregion
//     }
// }