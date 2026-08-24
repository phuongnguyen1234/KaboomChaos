using UnityEngine;
using Core.Interfaces;
using Collectibles.Behaviors;
using System.Collections.Generic;
using System.Linq;
using System;
using Core;

namespace Player
{
    /// <summary>
    /// Quản lý khiên đang hoạt động trên người chơi. Xử lý vòng đời của nó
    /// và ủy thác hành vi cho strategy khiên đang được kích hoạt.
    /// </summary>
    public class PlayerShieldController : MonoBehaviour, IPlayerShieldController
    {
        /// <summary>
        /// Lớp nội bộ để quản lý trạng thái của một khiên đang hoạt động.
        /// </summary>
        private class ActiveShield
        {
            public IShieldBehavior Behavior { get; }
            public IBaseShieldData Data { get; }
            public float Timer { get; set; }
            public GameObject VfxInstance { get; set; }
            public int Stacks { get; set; }
            public AudioSource ShieldAudioSource { get; set; } // Dành cho các âm thanh khiên đặc biệt (ví dụ: Magic Shield)

            public ActiveShield(IShieldBehavior behavior, IBaseShieldData data, GameObject vfx)
            {
                Behavior = behavior;
                Data = data;
                Timer = data.Duration;
                VfxInstance = vfx;
                Stacks = 1;
            }
        }

        private IPlayer _player;
        private readonly List<ActiveShield> _activeShields = new();

        public bool IsShieldActive => _activeShields.Count > 0;
        public bool HasFireShield => IsShieldTypeActive(typeof(Collectibles.Data.FireShieldData));

        private void Awake()
        {
            _player = GetComponentInParent<IPlayer>();
        }

        private void OnEnable()
        {
            GameEvents.OnRoundEndPlayerReset += RemoveAllShields;
        }

        private void OnDisable()
        {
            GameEvents.OnRoundEndPlayerReset -= RemoveAllShields;

            // Dọn dẹp tất cả khiên khi controller bị disable để tránh rò rỉ
            // Tạo một bản sao của danh sách để tránh lỗi khi sửa đổi trong lúc duyệt
            // Gọi phương thức RemoveAllShields để tái sử dụng logic dọn dẹp.
            RemoveAllShields();
        }

        private void FixedUpdate()
        {
            if (!IsShieldActive) return;

            // Duyệt ngược để có thể xóa item khỏi danh sách một cách an toàn
            for (int i = _activeShields.Count - 1; i >= 0; i--)
            {
                var shield = _activeShields[i];

                // Ủy thác cho strategy
                shield.Behavior.OnFixedUpdate(_player, shield.Data);

                // Xử lý bộ đếm thời gian cho các khiên có thời hạn
                if (shield.Data.Duration > 0 && !shield.Data.UnlimitedDuration)
                {
                    shield.Timer -= Time.fixedDeltaTime;
                    if (shield.Timer <= 0)
                    {
                        RemoveShield(shield);
                    }
                }
            }
        }

        /// <summary>
        /// Áp dụng một khiên mới cho người chơi. Hỗ trợ cộng dồn và các quy tắc đặc biệt.
        /// </summary>
        public void ApplyShield(IBaseShieldData shieldData)
        {
            // SỬA LỖI: Chặn tuyệt đối việc nhặt Crystal Shield lần thứ 2 ngay tại controller,
            // không phụ thuộc vào nơi caller (ShieldBehavior hay bất kỳ path nào khác).
            if (shieldData != null && shieldData is Collectibles.Data.CrystalShieldData
                && IsShieldTypeActive(typeof(Collectibles.Data.CrystalShieldData)))
            {
                Debug.Log("[PlayerShieldController] Đã sở hữu Crystal Shield, không thể cộng dồn. Bỏ qua vật phẩm.");
                return;
            }

            // Kiểm tra xem khiên cùng loại đã tồn tại chưa
            var existingShield = _activeShields.FirstOrDefault(s => s.Data.GetType() == shieldData.GetType());

            if (existingShield != null)
            {
                // Nếu là Crystal Shield: Không làm gì cả (chặn không cho nhặt thêm)
                if (shieldData is Collectibles.Data.CrystalShieldData)
                {
                    Debug.Log("Crystal Shield đã tồn tại, không thể nhặt thêm.");
                    return;
                }

                // Nếu là Magic Shield: Thực hiện logic stack
                if (shieldData is Collectibles.Data.MagicShieldData magicData)
                {
                    existingShield.Timer = magicData.Duration; // Làm mới thời gian
                    existingShield.Stacks++;

                    int pitchLevel = ((existingShield.Stacks - 1) % 3) + 1;

                    if (existingShield.ShieldAudioSource != null)
                    {
                        // Dừng rồi phát lại để "restart" bản nhạc với pitch mới từ đầu.
                        // Nhạc khiên KHÔNG loop nên Stop + Play luôn đảm bảo nghe lại từ đầu ở pitch mới.
                        existingShield.ShieldAudioSource.pitch = magicData.BasePitch + (pitchLevel - 1) * magicData.PitchPerStack;
                        existingShield.ShieldAudioSource.Stop();
                        existingShield.ShieldAudioSource.Play(); 
                    }
                    // KHÔNG gọi lại OnApply ở đây: tốc độ/nhảy chỉ nên áp dụng MỘT LẦN
                    // khi tạo khiên magic mới (đã chuyển vào SetupAndAddShield).
                    return; 
                }

                // Nếu là Fire Shield (hoặc các loại có duration): Refresh duration
                if (shieldData.Duration > 0)
                {
                    existingShield.Timer = shieldData.Duration;
                    Debug.Log($"{shieldData.GetType().Name} đã được refresh duration.");
                }
                return;
            }

            // Nếu không tồn tại khiên cùng loại -> Tạo khiên mới
            var behavior = ShieldBehaviorFactory.CreateBehavior(shieldData);
            if (behavior == null) return;

            if (shieldData.ShieldVFX != null)
            {
                var vfxInstance = Instantiate(shieldData.ShieldVFX, _player.GameObject.transform);
                vfxInstance.transform.localPosition = Vector3.zero;
                var newShield = new ActiveShield(behavior, shieldData, vfxInstance);
                SetupAndAddShield(newShield);
            }
            else
            {
                var newShield = new ActiveShield(behavior, shieldData, null);
                SetupAndAddShield(newShield);
            }
        }

        private void SetupAndAddShield(ActiveShield shield)
        {
            // Logic đặc biệt khi thêm Magic Shield lần đầu
            if (shield.Data is Collectibles.Data.MagicShieldData magicData)
            {
                                // Yêu cầu BGM toàn cục (của toàn trò chơi) tạm dừng để chỉ có nhạc
                // riêng của khiên Magic Shield đang phát — tránh phát đôi thực cảm.
                GameEvents.TriggerMusicPauseRequested();

                // SỬA LỖI: Áp dụng tốc độ/nhảy CHỈ MỘT LẦN khi khiên magic được tạo mới.
                // Trước đây logic này nằm trong MagicShieldBehavior.OnApply nên mỗi lần stack
                // (OnApply được gọi lại) đều cộng dồn -> "tăng cường độ" (tốc độ/nhảy) thay vì tăng pitch nhạc.
                _player.ApplySpeedMultiplier(magicData.SpeedMultiplier);
                _player.ApplyJumpMultiplier(magicData.JumpMultiplier);

                var audioGO = new GameObject("MagicShieldAudio");
                audioGO.transform.SetParent(_player.GameObject.transform);
                var audioSource = audioGO.AddComponent<AudioSource>();
                audioSource.clip = magicData.ShieldMusic;
                // Nhạc Magic Shield KHÔNG loop: chỉ phát đúng MỘT lượt mỗi lần kích hoạt/stack.
                // Hết bài mà khiên còn hiệu lực thì giữ im lặng cho đến khi khiên hết hạn
                // (BGM toàn cục vẫn bị chặn cho đến lúc đó).
                audioSource.loop = false;
                audioSource.spatialBlend = 0; // 2D sound
                audioSource.pitch = magicData.BasePitch;
                audioSource.Play();
                shield.ShieldAudioSource = audioSource;
            }

            _activeShields.Add(shield);
            shield.Behavior.OnApply(_player, this, shield.Data);
        }

        /// <summary>
        /// Gỡ bỏ một khiên cụ thể khỏi người chơi.
        /// </summary>
        private void RemoveShield(ActiveShield shield)
        {
            if (shield == null || !_activeShields.Contains(shield)) return;

            // SỬA LỖI: Xóa khiên khỏi danh sách TRƯỚC KHI gọi OnRemove.
            // Điều này đảm bảo logic trong MagicShieldBehavior.OnRemove (kiểm tra IsShieldTypeActive) hoạt động chính xác.
            _activeShields.Remove(shield);

            shield.Behavior.OnRemove(_player, this, shield.Data);

            if (shield.VfxInstance != null)
            {
                Destroy(shield.VfxInstance);
            }

                        // Dọn dẹp đặc biệt cho Magic Shield
            if (shield.Data is Collectibles.Data.MagicShieldData)
            {
                // Chỉ tiếp tục BGM toàn cục nếu không còn Magic Shield nào khác đang hoạt động.
                if (!IsShieldTypeActive(typeof(Collectibles.Data.MagicShieldData)))
                {
                    GameEvents.TriggerMusicResumeRequested();
                    // Chỉ reset modifier khi không còn bất kỳ Magic Shield nào (vì toàn bộ stack
                    // đang nằm trong cùng một ActiveShield, nên khi xóa là hết stack).
                    _player.ResetModifiers();
                }
                if (shield.ShieldAudioSource != null)
                {
                    Destroy(shield.ShieldAudioSource.gameObject);
                }
            }
        }

        /// <summary>
        /// Gỡ bỏ tất cả các khiên đang hoạt động. Được gọi khi người chơi chết hoặc round kết thúc.
        /// </summary>
        public void RemoveAllShields()
        {
            // Sử dụng ToList() để tạo bản sao vì RemoveShield sẽ thay đổi danh sách gốc.
            foreach (var shield in _activeShields.ToList())
            {
                RemoveShield(shield);
            }
            _activeShields.Clear();
        }
        /// <summary>
        /// Xử lý sát thương đến. Được gọi bởi component máu chính của player.
        /// Sát thương sẽ được xử lý tuần tự qua tất cả các khiên đang hoạt động.
        /// </summary>
        /// <returns>Lượng sát thương còn lại sau khi khiên đã xử lý.</returns>
        public float ProcessDamage(float amount, DamageSourceType sourceType, StatusEffectType effectContext)
        {
            if (!IsShieldActive) return amount;

            float damageToProcess = amount;
            List<ActiveShield> shieldsToRemove = new();

            // Duyệt qua một bản sao của danh sách để an toàn khi sửa đổi
            foreach (var shield in _activeShields.ToList())
            {
                float result = shield.Behavior.OnDamageTaken(damageToProcess, sourceType, effectContext, shield.Data);

                if (result < 0) // Giá trị đặc biệt: hấp thụ sát thương và phá vỡ khiên
                {
                    shieldsToRemove.Add(shield);
                    damageToProcess = 0; // Sát thương được hấp thụ hoàn toàn bởi khiên này
                }
                else
                {
                    damageToProcess = result;
                }

                // Nếu tất cả sát thương đã được xử lý, không cần kiểm tra các khiên khác
                if (damageToProcess <= 0) break;
            }

            foreach (var shield in shieldsToRemove)
            {
                RemoveShield(shield);
            }

            return Mathf.Max(0, damageToProcess);
        }

        /// <summary>
        /// Xử lý một hiệu ứng trạng thái sắp được áp dụng.
        /// </summary>
        /// <returns>True nếu khiên chặn hiệu ứng, False nếu không.</returns>
        public bool ProcessStatusEffect(StatusEffectType effect)
        {
            if (!IsShieldActive) return false; 

            // Nếu BẤT KỲ khiên nào chặn được hiệu ứng, thì hiệu ứng sẽ bị chặn.
            return _activeShields.Any(shield => shield.Behavior.OnStatusEffectApplied(effect, shield.Data));
        }

        /// <summary>
        /// Kiểm tra xem một loại khiên cụ thể có đang hoạt động không.
        /// </summary>
        public bool IsShieldTypeActive(Type shieldDataType)
        {
            return _activeShields.Any(s => s.Data.GetType() == shieldDataType || s.Data.GetType().IsSubclassOf(shieldDataType));
        }
    
        /// <summary>
        /// Đếm số lượng khiên đang hoạt động dựa trên loại dữ liệu khiên.
        /// </summary>
        public int GetActiveShieldCount(Type shieldDataType)
        {
            return _activeShields.Count(s => s.Data.GetType() == shieldDataType || s.Data.GetType().IsSubclassOf(shieldDataType));
        }
    }
}