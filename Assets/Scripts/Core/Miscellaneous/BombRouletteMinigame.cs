using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Master controller quan ly minigame Bomb Roulette trong Lobby.
    /// Gom 1 qua bom va 5 nut bam: 1 nut la Nut That (Bomb/Bay no), cac nut con lai la Nut Gia (Safe).
    /// Khi Player dam len Nut That: Phat SFX bao hieu sai, doi 1 giay roi phat no (kill player thay vi teleport), reset minigame.
    /// Khi Player dam het tat ca Nut Gia: Chuc mung, phat VFX/SFX, trao credits kem Floating Text, reset minigame.
    /// </summary>
    public class BombRouletteMinigame : MonoBehaviour
    {
        #region Fields

        [Header("Buttons & Bomb Reference")]
        [Tooltip("Danh sach cac nut bam BombRouletteButton trong minigame.")]
        [SerializeField] private List<BombRouletteButton> _buttons = new();

        [Tooltip("Transform vi tri cua qua bom (noi phat ra vu no). Neu de trong, lay vi tri cua object nay.")]
        [SerializeField] private Transform _bombTransform;

        [Header("Explosion Settings")]
        [Tooltip("Ban kinh vu no (world space) khi qua bom phat no.")]
        [SerializeField] private float _explosionRadius = 5f;

        [Tooltip("Thoi gian hoan lai (giay) tu khi dap phai nut that den khi bom phat no.")]
        [SerializeField] private float _fuseDelay = 1f;

        [Tooltip("SFX phat ngay khi nguoi choi dam vao nut that de bao hieu sai/nguy hiem.")]
        [SerializeField] private AudioClip _wrongSfx;

        [Tooltip("VFX prefab vu no (co ExplosionEffectController hoac VFX thuong).")]
        [SerializeField] private GameObject _explosionVfxPrefab;

        [Tooltip("SFX phat khi qua bom phat no.")]
        [SerializeField] private AudioClip _explosionSfx;

        [Header("Reward & Congratulations Settings")]
        [Tooltip("So luong credits thuong khi nguoi choi vuot qua tat ca nut gia.")]
        [SerializeField] private int _rewardCredits = 15;

        [Tooltip("Mau chu floating text thuong khi thang (mac dinh mau vang coin #FFCD2A).")]
        [SerializeField] private Color _rewardTextColor = new Color(1f, 205f / 255f, 42f / 255f, 1f);

        [Tooltip("Danh sach cac GameObject VFX chuc mung khi thang minigame (co the la Prefab hoac Scene Object).")]
        [SerializeField] private List<GameObject> _congratulationsVfxObjects = new();

        [Tooltip("Danh sach ParticleSystem chuc mung trong Scene can Play khi thang minigame.")]
        [SerializeField] private List<ParticleSystem> _congratulationsParticleSystems = new();

        [Tooltip("VFX prefab phat khi nguoi choi thang minigame (dam het cac nut gia). Ho tro tuong thich cu.")]
        [SerializeField] private GameObject _congratulationsVfxPrefab;

        [Tooltip("SFX phat khi nguoi choi thang minigame.")]
        [SerializeField] private AudioClip _congratulationsSfx;

        [Header("Minigame Settings")]
        [Tooltip("Thoi gian (giay) hoan lai truoc khi reset minigame sau khi no hoac thang.")]
        [SerializeField] private float _resetDelay = 1.5f;

        // Trang thai runtime
        private BombRouletteButton _realBombButton;
        private int _fakeButtonsPressedCount;
        private bool _isResetting;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Tu dong tim cac nut con neu danh sach de trong
            if (_buttons == null || _buttons.Count == 0)
            {
                _buttons = new List<BombRouletteButton>(GetComponentsInChildren<BombRouletteButton>());
            }

            foreach (var btn in _buttons)
            {
                if (btn != null)
                {
                    btn.Initialize(this);
                }
            }

            ResetMinigame();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Xu ly logic khi Player giam len mot nut trong minigame.
        /// </summary>
        /// <param name="button">Nut va cham.</param>
        /// <param name="player">Player giam len nut.</param>
        public void OnButtonStepped(BombRouletteButton button, IPlayer player)
        {
            if (_isResetting || button == null) return;

            Vector3 bombPos = _bombTransform != null ? _bombTransform.position : transform.position;

            // 1. KIEM TRA NUT THAT (BOM NO)
            if (button == _realBombButton)
            {
                _isResetting = true;

                // 1a. Phat SFX bao hieu sai / canh bao ngay lap tuc
                if (_wrongSfx != null && SfxService.Instance != null)
                {
                    SfxService.Instance.PlaySfx(_wrongSfx, bombPos);
                }

                // 1b. Bat dau trinh tu dem nguoc roi phat no va kill player
                StartCoroutine(ExplosionSequenceRoutine(bombPos, player));
                return;
            }

            // 2. KIEM TRA NUT GIA (SAFE)
            _fakeButtonsPressedCount++;
            int totalFakeButtons = Mathf.Max(1, _buttons.Count - 1);

            // Neu da dam het tat ca nut gia
            if (_fakeButtonsPressedCount >= totalFakeButtons)
            {
                _isResetting = true;

                // 2a. Phat tat ca VFX & SFX chuc mung
                PlayCongratulationsVfx(bombPos);

                if (_congratulationsSfx != null && SfxService.Instance != null)
                {
                    SfxService.Instance.PlaySfx(_congratulationsSfx, bombPos);
                }

                // 2b. Trao credits va hien thi Floating Text tren Player
                GameEvents.TriggerAddCreditsRequest(_rewardCredits);

                if (player != null && player.GameObject != null)
                {
                    GameEvents.TriggerFloatingTextRequested(
                        player.GameObject.transform,
                        Vector3.up * 2f,
                        $"+{_rewardCredits}",
                        _rewardTextColor,
                        player.CoinTextContainer,
                        true
                    );
                }

                // 2c. Reset minigame sau delay
                StartCoroutine(ResetRoutine(_resetDelay));
            }
        }

        /// <summary>
        /// Reset minigame: nang tat ca nut len, chon ngau nhien 1 nut thiet va reset dem.
        /// </summary>
        public void ResetMinigame()
        {
            _isResetting = false;
            _fakeButtonsPressedCount = 0;

            if (_buttons == null || _buttons.Count == 0) return;

            // Reset tat ca nut
            foreach (var btn in _buttons)
            {
                if (btn != null)
                {
                    btn.ResetButton();
                }
            }

            // Chon ngau nhien 1 nut lam Nut That (Bomb/Bay no)
            int realIndex = Random.Range(0, _buttons.Count);
            _realBombButton = _buttons[realIndex];
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Coroutine cho delay truoc khi phat no, kill player va reset minigame.
        /// </summary>
        /// <param name="bombPos">Vi tri qua bom phat no.</param>
        /// <param name="steppingPlayer">Player da giam len nut that.</param>
        private IEnumerator ExplosionSequenceRoutine(Vector3 bombPos, IPlayer steppingPlayer)
        {
            // 1. Cho thoi gian fuse (mac dinh 1 giay) truoc khi bom phat no
            if (_fuseDelay > 0f)
            {
                yield return new WaitForSeconds(_fuseDelay);
            }

            // 2. Tao hieu ung no VFX
            if (_explosionVfxPrefab != null)
            {
                GameObject vfxObj = GameEvents.TriggerVFXSpawnRequest(_explosionVfxPrefab, bombPos, Quaternion.identity);
                if (vfxObj != null && vfxObj.TryGetComponent<ExplosionEffectController>(out var expController))
                {
                    expController.Trigger(_explosionRadius);
                }
            }

            // 3. Phat SFX vu no
            if (_explosionSfx != null && SfxService.Instance != null)
            {
                SfxService.Instance.PlaySfx(_explosionSfx, bombPos);
            }

            // 4. Kill tat ca Player trong ban kinh vu no (va stepping player) giong nhu reset player
            KillPlayersInRadius(bombPos, _explosionRadius, steppingPlayer);

            // 5. Cho delay roi reset minigame
            yield return new WaitForSeconds(_resetDelay);
            ResetMinigame();
        }

        /// <summary>
        /// Phat tat ca hieu ung VFX chuc mung khi nguoi choi thang minigame.
        /// Ho tro danh sach ParticleSystem trong Scene, danh sach GameObject VFX, va Prefab don le.
        /// </summary>
        /// <param name="bombPos">Vi tri trung tam phat VFX.</param>
        private void PlayCongratulationsVfx(Vector3 bombPos)
        {
            // 1. Phat danh sach ParticleSystem (trong Scene hoac duoc gan truc tiep)
            if (_congratulationsParticleSystems != null)
            {
                foreach (var ps in _congratulationsParticleSystems)
                {
                    if (ps != null)
                    {
                        ps.gameObject.SetActive(true);
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        ps.Play(true);
                    }
                }
            }

            // 2. Phat danh sach GameObject VFX (co the la Prefab hoac Scene Object)
            if (_congratulationsVfxObjects != null)
            {
                foreach (var vfxObj in _congratulationsVfxObjects)
                {
                    if (vfxObj == null) continue;

                    // Neu la object dang ton tai trong Scene
                    if (vfxObj.scene.IsValid())
                    {
                        vfxObj.SetActive(true);
                        var particleSystems = vfxObj.GetComponentsInChildren<ParticleSystem>(true);
                        foreach (var ps in particleSystems)
                        {
                            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                            ps.Play(true);
                        }
                    }
                    else
                    {
                        // Neu la Prefab Asset
                        GameEvents.TriggerVFXSpawnRequest(vfxObj, bombPos, Quaternion.identity);
                    }
                }
            }

            // 3. Backward compatibility cho prefab don le da gan tu truoc
            if (_congratulationsVfxPrefab != null)
            {
                GameEvents.TriggerVFXSpawnRequest(_congratulationsVfxPrefab, bombPos, Quaternion.identity);
            }
        }

        /// <summary>
        /// Kill tat ca Player nam trong ban kinh vu no giong nhu reset player.
        /// </summary>
        /// <param name="center">Vi tri tam vu no.</param>
        /// <param name="radius">Ban kinh vu no.</param>
        /// <param name="steppingPlayer">Player da giam len nut.</param>
        private void KillPlayersInRadius(Vector3 center, float radius, IPlayer steppingPlayer)
        {
            Collider[] hitColliders = Physics.OverlapSphere(center, radius, ~0, QueryTriggerInteraction.Collide);
            HashSet<IPlayer> affectedPlayers = new();

            foreach (var hit in hitColliders)
            {
                if (hit == null) continue;

                IPlayer player = hit.GetComponentInParent<IPlayer>();
                if (player == null || player.GameObject == null) continue;

                if (!affectedPlayers.Contains(player))
                {
                    affectedPlayers.Add(player);
                    GameEvents.TriggerPlayerResetRequested(player);
                }
            }

            // Dam bao player giam vao nut luon bi kill ke ca khi o ngoai mep collider ban kinh
            if (steppingPlayer != null && steppingPlayer.GameObject != null && !affectedPlayers.Contains(steppingPlayer))
            {
                GameEvents.TriggerPlayerResetRequested(steppingPlayer);
            }
        }

        /// <summary>
        /// Coroutine doi mot khoang thoi gian roi reset minigame.
        /// </summary>
        private IEnumerator ResetRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            ResetMinigame();
        }

        #endregion
    }
}
