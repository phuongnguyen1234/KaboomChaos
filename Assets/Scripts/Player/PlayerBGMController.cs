using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Quản lý nhạc nền cho một người chơi cụ thể.
/// Hoạt động như một "tai nghe" riêng cho mỗi người chơi.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PlayerBGMController : MonoBehaviour
{
    [Tooltip("Database chứa tất cả các file nhạc.")]
    [SerializeField] private Core.Database.BGMDatabase _bgmDatabase;

    private AudioSource _audioSource;
    private Coroutine _musicCoroutine;
    private AudioClip _lastPlayedClip;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        // Cấu hình AudioSource cho nhạc nền 2D, không bị ảnh hưởng bởi vị trí.
        _audioSource.spatialBlend = 0; 
        _audioSource.playOnAwake = false;
    }

    public void StopMusic()
    {
        if (_musicCoroutine != null)
        {
            StopCoroutine(_musicCoroutine);
            _musicCoroutine = null;
        }
        if (_audioSource.isPlaying)
        {
            _audioSource.Stop();
        }
    }

    public void PlayLobbyMusic()
    {
        if (_bgmDatabase == null) return;
        PlayClip(_bgmDatabase.LobbyMusic, true);
    }

    public void PlayGameplayMusic(float intensity)
    {
        if (_bgmDatabase == null) return;
        StopMusic();
        List<AudioClip> playlist = intensity < 4 ? _bgmDatabase.NormalGameplayMusic : _bgmDatabase.IntenseGameplayMusic;
        
        if (playlist == null || playlist.Count == 0)
        {
            Debug.LogWarning($"[PlayerBGMController] No gameplay music playlist for intensity: {intensity}.", this);
            return;
        }

        _musicCoroutine = StartCoroutine(PlaylistCoroutine(playlist));
    }

    public void PlayLast30sMusic(float intensity)
    {
        if (_bgmDatabase == null) return;
        AudioClip clipToPlay = intensity < 4 ? _bgmDatabase.Last30sNormalMusic : _bgmDatabase.Last30sIntenseMusic;
        PlayClip(clipToPlay, true);
    }

    private IEnumerator PlaylistCoroutine(List<AudioClip> playlist)
    {
        _audioSource.loop = false;
        var playableClips = new List<AudioClip>(playlist);

        while (playableClips.Count > 0)
        {
            var availableClips = playableClips.Where(c => c != _lastPlayedClip).ToList();
            if (availableClips.Count == 0 && playableClips.Count > 0) availableClips = playableClips;

            AudioClip clipToPlay = availableClips[Random.Range(0, availableClips.Count)];
            
            _audioSource.clip = clipToPlay;
            _audioSource.Play();
            _lastPlayedClip = clipToPlay;

            yield return new WaitWhile(() => _audioSource.isPlaying);
            yield return new WaitForSeconds(0.2f);
        }
    }

    private void PlayClip(AudioClip clip, bool loop)
    {
        StopMusic();
        if (clip == null) return;
        if (_audioSource.clip == clip && _audioSource.isPlaying) return;

        _audioSource.clip = clip;
        _audioSource.loop = loop;
        _audioSource.Play();
    }
}