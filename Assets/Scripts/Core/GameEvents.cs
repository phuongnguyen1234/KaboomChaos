using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Interfaces;

namespace Core
{
    /// <summary>
    /// Định nghĩa tất cả các event toàn cục trong game.
    /// Sử dụng static events để các hệ thống khác có thể đăng ký và phản hồi.
    /// </summary>
    public static class GameEvents
    {
        #region Event Declarations

        #region Player Events
        /// <summary>
        /// Được gọi khi một người chơi yêu cầu tự reset (tự sát).
        /// </summary>
        public static event Action<IPlayer> OnPlayerResetRequested;
        /// <summary>
        /// Được gọi khi người chơi yêu cầu reset character (style Roblox).
        /// Không tham chiển chứa người chơi; PlayerManager sẽ quản người chơi hiện tại.
        /// </summary>
        public static event Action OnResetPlayerRequested;
        /// <summary>
        /// Được gọi khi một người chơi chết.

        /// </summary>
        public static event Action<IPlayer> OnPlayerDied;
        /// <summary>
        /// Được gọi khi máu của người chơi thay đổi.
        /// </summary>
        public static event Action<IPlayer, float, float> OnPlayerHealthChanged;
        /// <summary>
        /// Được gọi khi kết thúc một round, yêu cầu các component của người chơi tự reset lại trạng thái.
        /// Ví dụ: PlayerHealth sẽ hồi đầy máu, StatusEffectReceiver sẽ xóa hiệu ứng.
        /// </summary>
        public static event Action OnRoundEndPlayerReset;
        /// <summary>
        /// Được gọi khi một hiệu ứng trạng thái được áp dụng lên người chơi.
        /// </summary>
        public static event Action<IPlayer, StatusEffectType> OnPlayerStatusEffectApplied;
        /// <summary>
        /// Được gọi khi một hiệu ứng trạng thái trên người chơi được hoàn tác.
        /// </summary>
        public static event Action<IPlayer, StatusEffectType> OnPlayerStatusEffectReverted;
        /// <summary>
        /// Duoc goi khi nang luong (energy) cua nguoi choi thay doi.
        /// Tham so: nguoi choi, energy hien tai, energy toi da.
        /// </summary>
        public static event Action<IPlayer, float, float> OnPlayerEnergyChanged;
        /// <summary>
        /// Duoc goi khi nguoi choi tieu toan bo energy de su dung Skill.
        /// Luc nay energy ve 0 va bat dau qua trinh tu sac.
        /// </summary>
        public static event Action<IPlayer> OnPlayerSkillEnergyConsumed;
        /// <summary>
        /// Duoc goi khi energy cua nguoi choi da sac day tro lai 100%,
        /// nghia la Skill da san sang duoc su dung lan tiep theo.
        /// </summary>
        public static event Action<IPlayer> OnPlayerEnergyFullyCharged;

        /// <summary>
        /// Được gọi khi skill hoặc perk của người chơi thay đổi trạng thái trang bi
        /// (trang bi hoặc gỡ bỏ), dùng để UI (HUD) cập nhật icon trang bi hiển thị.
        /// </summary>
        public static event Action OnPlayerEquipmentChanged;

        /// <summary>
        /// Yeu cau kiem tra xem hieu ung trang thai dinh huong vao player co bi chan hay khong.
        /// Dung cho perk thay doi rule (vi du Anti-Freeze chan dong bang).
        /// Tra ve: true neu hieu ung bi chan (khong ap dung), false neu tiep tuc ap dung binh thuong.
        /// </summary>
        public static event Func<IPlayer, StatusEffectType, bool> OnQueryPlayerStatusEffectBlocked;

        /// <summary>
        /// Duoc goi khi player thuc su nhan sat thuong (da qua duoc khien va kiem tra mien nhiem).
        /// Dung cho perk theo doi luong sat thuong nhan duoc (Regeneration, Anti-Freeze).
        /// Tham so: player, luong sat thuong thuc te, nguon sat thuong, loai hieu ung (context).
        /// </summary>
        public static event Action<IPlayer, float, DamageSourceType, StatusEffectType> OnPlayerDamageTaken;

        /// <summary>
        /// Duoc goi khi player trung dan vao mot vu no (explosion hit), bat ke co khien hay dang bat tu hay khong.
        /// Dung cho perk/rule can phan ung voi viec "trung vu no" ma khong phu thuoc vao viec co that su nhan sat thuong
        /// sau khi qua khien/mien nhiem hay khong (vi du: Anti-Freeze tang Max HP moi lan trung vu no bang).
        /// Tham so: player, loai hieu ung (context) cua vu no.
        /// </summary>
        public static event Action<IPlayer, StatusEffectType> OnPlayerExplosionHit;

        /// <summary>
        /// Duoc goi khi player nhat mot dong coin. Tham so thu hai la luong BonusHP
        /// da cau hinh san trong CoinData (0 neu coin khong co bonus HP).
        /// Dung cho perk Big Saver de hoi HP khi nhat coin.
        /// </summary>
        public static event Action<IPlayer, float> OnPlayerCoinCollected;
        #endregion

        #region Extreme Mode Events
        /// <summary>
        /// Yeu cau thay doi trang thai Extreme Mode (goi tu Option Menu).
        /// PlayerDataManager lang nghe de luu tru va phat lai trang thai thuc te qua OnExtremeModeStateChanged.
        /// </summary>
        public static event Action<bool> OnExtremeModeChanged;

        /// <summary>
        /// Bao hieu trang thai Extreme Mode thuc te da thay doi (sau khi da luu tru).
        /// Cac thanh phan (PlayerHealth, PlayerPerkController, HUD, UIManager...) lang nghe de phan ung.
        /// </summary>
        public static event Action<bool> OnExtremeModeStateChanged;

        /// <summary>
        /// Yeu cau tra ve trang thai Extreme Mode hien tai (da luu tru) de khoi phuc logic/UI khi vao game.
        /// </summary>
        public static Func<bool> OnRequestExtremeModeEnabled;
        #endregion

        #region AFK Mode Events
        /// <summary>
        /// Yeu cau thay doi trang thai AFK (goi tu Option Menu).
        /// PlayerDataManager lang nghe de luu tru va phat lai trang thai thuc te qua OnAfkStateChanged.
        /// </summary>
        public static event Action<bool> OnAfkEnabledChanged;

        /// <summary>
        /// Bao hieu trang thai AFK thuc te da thay doi (sau khi da luu tru).
        /// Cac thanh phan (PlayerManager, PlayerAfkIndicator...) lang nghe de phan ung.
        /// </summary>
        public static event Action<bool> OnAfkStateChanged;

        /// <summary>
        /// Yeu cau tra ve trang thai AFK hien tai (da luu tru) de khoi phuc logic/UI khi vao game.
        /// </summary>
        public static Func<bool> OnRequestAfkEnabled;
        #endregion

        #region Bomb Events
        /// <summary>
        /// Được gọi khi một quả bom muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnBombDespawnRequest;

        /// <summary>
        /// Yeu cau lay danh sach cac instance bom dang hoat dong (da duoc sinh tu pool).
        /// Duoc dung boi skill Disarm de go vo cac bom trong khu vuc.
        /// </summary>
        public static event Func<IReadOnlyList<GameObject>> OnRequestActiveBombInstances;

        /// <summary>
        /// Duoc goi khi mot vu no xay ra trong game.
        /// Tham so: vi tri vu no (Vector3) va ban kinh vu no (float).
        /// </summary>
        public static event Action<Vector3, float> OnExplosionOccurred;
        #endregion

        #region VFX Events
        /// <summary>
        /// Yêu cầu sinh ra một hiệu ứng hình ảnh (VFX).
        /// Trả về instance của VFX đã được sinh ra.
        /// </summary>
        public static event Func<GameObject, Vector3, Quaternion, GameObject> OnVFXSpawnRequest;
        /// <summary>
        /// Được gọi khi một hiệu ứng hình ảnh (VFX) muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnVFXDespawnRequest;

        /// <summary>
        /// Duoc goi khi player co gang nhap Battery nhung energy dang day (khong the nhap).
        /// HUD lang nghe de hien thi thong bao 'Charge Full' tren UI.
        /// </summary>
        public static event Action<IPlayer, AudioClip> OnBatteryCollectibleRefused;
        #endregion

        #region UI & Floating Text Events
        /// <summary>
        /// Yêu cầu hiển thị một text nổi.
        /// Tham số: Transform (đối tượng cha để đi theo), Vector3 (vị trí offset cục bộ), string (nội dung), Color (màu sắc).
        /// </summary>        
        public static event Action<Transform, Vector3, string, Color, Transform, bool> OnFloatingTextRequested;
        public static event Action<GameObject> OnFloatingTextDespawnRequest;
        #endregion

        #region Block & Terrain Events
        /// <summary>
        /// Được gọi khi một khối địa hình (DestructibleBlock) muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnBlockDespawnRequest;
        /// <summary>
        /// Yêu cầu sinh ra một khối địa hình (DestructibleBlock) từ pool.
        /// Trả về instance của khối đã được sinh ra.
        /// </summary>
        public static event Func<GameObject, Vector3, Quaternion, GameObject> OnBlockSpawnRequest;
        #endregion

                #region Game Loop Events
        /// <summary>
        /// Được gọi khi kết thúc một round, yêu cầu các hiệu ứng tạm thời (khí độc, điện...) tự dọn dẹp.
        /// </summary>
        public static event Action OnRoundEndCleanup;

        /// <summary>
        /// Yêu cầu bắt đầu phiên chơi game (thường từ màn hình chính).
        /// </summary>
        public static event Action OnStartGameRequest;

        /// <summary>
        /// Yêu cầu spawn player trước khi bắt đầu game (trong lúc transition).
        /// </summary>
        public static event Action OnSpawnInitialPlayerRequest;

        /// <summary>
        /// Yêu cầu trở về màn hình chính (từ trong game).
        /// </summary>
        public static event Action OnReturnToHomeRequest;

        /// <summary>
        /// Cờ toàn cục cho biết Pause Menu đang hiển thị hay không.
        /// Khi true, các hệ thống nhận input (camera, nhân vật) phải ngừng hoạt động.
        /// </summary>
        public static bool IsPauseMenuVisible = false;

        /// <summary>
        /// Yêu cầu tạm dừng toàn bộ nhạc nền (dùng khi khiên Magic đang phát nhạc riêng).
        /// </summary>
        public static event Action OnMusicPauseRequested;

        /// <summary>
        /// Yêu cầu tiếp tục nhạc nền đã bị tạm dừng.
        /// </summary>
        public static event Action OnMusicResumeRequested;

        /// <summary>
        /// Yeu cau fade out nhac nen hien tai ve 0 trong mot khoang thoi gian (giay).
        /// </summary>
        public static event Action<float> OnMusicFadeOutRequested;

        /// <summary>
        /// Duoc goi khi do kho du kien cho round tiep theo (NextRoundIntensity) thay doi.
        /// Tham so: NextRoundIntensity (float), MinIntensity (float), MaxIntensity (float).
        /// </summary>
        public static event Action<float, float, float> OnNextRoundIntensityChanged;
        #endregion

        #region Player Data Events
        /// <summary>
        /// Yêu cầu lưu tất cả dữ liệu người chơi.
        /// </summary>
        public static event Action OnSavePlayerDataRequest;

        /// <summary>
        /// Yêu cầu thêm (hoặc bớt) một lượng credits.
        /// Tham số: int (số lượng credits cần thêm).
        /// </summary>
        public static event Action<int> OnAddCreditsRequest;

        /// <summary>
        /// Được gọi khi số credits của người chơi thay đổi.
        /// Tham số: int (tổng số credits mới).
        /// </summary>
        public static event Action<int> OnCreditsChanged;

        /// <summary>
        /// Yêu cầu lấy số credits hiện tại. Trả về: int (số credits hiện tại).
        /// </summary>
        public static event Func<int> OnRequestCurrentCredits;

        /// <summary>
        /// Được gọi khi danh sách skill mà người chơi sở hữu thay đổi.
        /// Tham số: IReadOnlyList<string> (danh sách ID skill hiện tại).
        /// </summary>
        public static event Action<IReadOnlyList<string>> OnOwnedSkillsChanged;

        /// <summary>
        /// Yêu cầu lấy danh sách ID các skill mà người chơi đang sở hữu.
        /// Trả về: IReadOnlyList<string> (danh sách ID).
        /// </summary>
        public static event Func<IReadOnlyList<string>> OnRequestOwnedSkillIds;

        /// <summary>
        /// Yêu cầu thêm một skill vào danh sách sở hữu của người chơi
        /// (thường được gọi khi mua skill trong Shop).
        /// Tham số: string (ID của skill).
        /// </summary>
        public static event Action<string> OnAddOwnedSkill;

        /// <summary>
        /// Được gọi khi danh sách perk mà người chơi sở hữu thay đổi.
        /// Tham số: IReadOnlyList<string> (danh sách ID perk hiện tại).
        /// </summary>
        public static event Action<IReadOnlyList<string>> OnOwnedPerksChanged;

        /// <summary>
        /// Yêu cầu lấy danh sách ID các perk mà người chơi đang sở hữu.
        /// Trả về: IReadOnlyList<string> (danh sách ID).
        /// </summary>
        public static event Func<IReadOnlyList<string>> OnRequestOwnedPerkIds;

        /// <summary>
        /// Yêu cầu thêm một perk vào danh sách sở hữu của người chơi
        /// (thường được gọi khi mua perk trong Shop).
        /// Tham số: string (ID của perk).
        /// </summary>
        public static event Action<string> OnAddOwnedPerk;

        /// <summary>
        /// Yêu cầu lấy tổng số lần quay thưởng (mua skill) đã thực hiện ở Shop.
        /// Trả về: int (số lần quay).
        /// </summary>
        public static event Func<int> OnRequestSkillSpinCount;

        /// <summary>
        /// Yêu cầu tăng tổng số lần quay thưởng skill lên 1 và lưu lại.
        /// Dùng để tăng giá các nhóm skill (mỗi nhóm tăng theo hệ số riêng).
        /// </summary>
        public static event Action OnIncreaseSkillSpinCount;

        /// <summary>
        /// Duoc goi khi skill dang trang bi cua nguoi choi thay doi (null/rong = go trang bi).
        /// PlayerDataManager lang nghe de luu lai trang thai trang bi.
        /// </summary>
        public static event Action<string> OnEquippedSkillIdChanged;

        /// <summary>
        /// Yeu cau lay ID skill dang duoc trang bi (da luu).
        /// </summary>
        public static event Func<string> OnRequestEquippedSkillId;

        /// <summary>
        /// Duoc goi khi perk dang trang bi cua nguoi choi thay doi.
        /// PlayerDataManager lang nghe de luu lai trang thai trang bi.
        /// </summary>
        public static event Action<string> OnEquippedPerkIdChanged;

        /// <summary>
        /// Yeu cau lay ID perk dang duoc trang bi (da luu).
        /// </summary>
        public static event Func<string> OnRequestEquippedPerkId;
        #endregion

        #region Collectible Events
        /// <summary>
        /// Yêu cầu sinh ra một vật phẩm (collectible) từ pool.
        /// Trả về instance của vật phẩm đã được sinh ra.
        /// </summary>
        public static event Func<GameObject, Vector3, Quaternion, GameObject> OnCollectibleSpawnRequest;

        /// <summary>
        /// Được gọi khi một vật phẩm muốn được trả về pool.
        /// </summary>
        public static event Action<GameObject> OnCollectibleDespawnRequest;
        #endregion

        #endregion

        #region Trigger Methods

                #region Player Triggers
        public static void TriggerPlayerResetRequested(IPlayer player) => OnPlayerResetRequested?.Invoke(player);
        public static void TriggerResetPlayerRequested() => OnResetPlayerRequested?.Invoke();
        public static void TriggerPlayerDied(IPlayer player) => OnPlayerDied?.Invoke(player);

        public static void TriggerPlayerHealthChanged(IPlayer player, float currentHealth, float maxHealth) => OnPlayerHealthChanged?.Invoke(player, currentHealth, maxHealth);
        public static void TriggerRoundEndPlayerReset() => OnRoundEndPlayerReset?.Invoke();
        public static void TriggerPlayerStatusEffectApplied(IPlayer player, StatusEffectType effect) => OnPlayerStatusEffectApplied?.Invoke(player, effect);
        public static void TriggerPlayerStatusEffectReverted(IPlayer player, StatusEffectType effect) => OnPlayerStatusEffectReverted?.Invoke(player, effect);
        public static void TriggerPlayerEnergyChanged(IPlayer player, float currentEnergy, float maxEnergy) => OnPlayerEnergyChanged?.Invoke(player, currentEnergy, maxEnergy);
        public static void TriggerPlayerSkillEnergyConsumed(IPlayer player) => OnPlayerSkillEnergyConsumed?.Invoke(player);
        public static void TriggerPlayerEnergyFullyCharged(IPlayer player) => OnPlayerEnergyFullyCharged?.Invoke(player);

        public static bool TriggerQueryPlayerStatusEffectBlocked(IPlayer player, StatusEffectType effect)
            => OnQueryPlayerStatusEffectBlocked != null ? OnQueryPlayerStatusEffectBlocked(player, effect) : false;

        public static void TriggerPlayerDamageTaken(IPlayer player, float amount, DamageSourceType sourceType, StatusEffectType effectContext)
            => OnPlayerDamageTaken?.Invoke(player, amount, sourceType, effectContext);

        public static void TriggerPlayerExplosionHit(IPlayer player, StatusEffectType effectContext)
            => OnPlayerExplosionHit?.Invoke(player, effectContext);

        public static void TriggerPlayerCoinCollected(IPlayer player, float bonusHp)
            => OnPlayerCoinCollected?.Invoke(player, bonusHp);
        #endregion

        #region Bomb Triggers
        public static void TriggerBombDespawnRequest(GameObject bombInstance) => OnBombDespawnRequest?.Invoke(bombInstance);

        public static IReadOnlyList<GameObject> TriggerRequestActiveBombInstances() => OnRequestActiveBombInstances?.Invoke() ?? new List<GameObject>();

        public static void TriggerExplosionOccurred(Vector3 position, float radius) => OnExplosionOccurred?.Invoke(position, radius);
        #endregion

        #region VFX Triggers
        public static GameObject TriggerVFXSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnVFXSpawnRequest?.Invoke(prefab, position, rotation);
        public static void TriggerVFXDespawnRequest(GameObject vfxInstance) => OnVFXDespawnRequest?.Invoke(vfxInstance);

        public static void TriggerBatteryCollectibleRefused(IPlayer player, AudioClip chargeFullSfx) => OnBatteryCollectibleRefused?.Invoke(player, chargeFullSfx);
        #endregion
        
        #region UI & Floating Text Triggers
        public static void TriggerFloatingTextRequested(Transform parent, Vector3 offset, string text, Color color, Transform containerOverride = null, bool showIcon = false) => OnFloatingTextRequested?.Invoke(parent, offset, text, color, containerOverride, showIcon);
        public static void TriggerFloatingTextDespawnRequest(GameObject textObject) => OnFloatingTextDespawnRequest?.Invoke(textObject);
        #endregion
        
        #region Block & Terrain Triggers
        public static void TriggerBlockDespawnRequest(GameObject blockInstance) => OnBlockDespawnRequest?.Invoke(blockInstance);
        public static GameObject TriggerBlockSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnBlockSpawnRequest?.Invoke(prefab, position, rotation);
        #endregion

        #region Game Loop Triggers
        public static void TriggerRoundEndCleanup() => OnRoundEndCleanup?.Invoke();
        public static void TriggerStartGameRequest() => OnStartGameRequest?.Invoke();
        public static void TriggerSpawnInitialPlayerRequest() => OnSpawnInitialPlayerRequest?.Invoke();
        public static void TriggerReturnToHomeRequest() => OnReturnToHomeRequest?.Invoke();

        public static void TriggerMusicPauseRequested() => OnMusicPauseRequested?.Invoke();
        public static void TriggerMusicResumeRequested() => OnMusicResumeRequested?.Invoke();
        public static void TriggerMusicFadeOutRequested(float duration = 1.0f) => OnMusicFadeOutRequested?.Invoke(duration);
        public static void TriggerNextRoundIntensityChanged(float nextIntensity, float minIntensity, float maxIntensity)
            => OnNextRoundIntensityChanged?.Invoke(nextIntensity, minIntensity, maxIntensity);
        #endregion

        #region Player Data Triggers
        public static void TriggerSavePlayerDataRequest() => OnSavePlayerDataRequest?.Invoke();
        public static void TriggerAddCreditsRequest(int amount) => OnAddCreditsRequest?.Invoke(amount);
        public static void TriggerCreditsChanged(int newTotal) => OnCreditsChanged?.Invoke(newTotal);
        public static int TriggerRequestCurrentCredits() => OnRequestCurrentCredits?.Invoke() ?? 0;
        public static IReadOnlyList<string> TriggerRequestOwnedSkillIds() => OnRequestOwnedSkillIds?.Invoke() ?? new List<string>();
        public static void TriggerAddOwnedSkill(string skillId) => OnAddOwnedSkill?.Invoke(skillId);
        public static void TriggerOwnedSkillsChanged(IReadOnlyList<string> ownedSkillIds) => OnOwnedSkillsChanged?.Invoke(ownedSkillIds);
        public static IReadOnlyList<string> TriggerRequestOwnedPerkIds() => OnRequestOwnedPerkIds?.Invoke() ?? new List<string>();
        public static void TriggerAddOwnedPerk(string perkId) => OnAddOwnedPerk?.Invoke(perkId);
        public static void TriggerOwnedPerksChanged(IReadOnlyList<string> ownedPerkIds) => OnOwnedPerksChanged?.Invoke(ownedPerkIds);
        public static int TriggerRequestSkillSpinCount() => OnRequestSkillSpinCount?.Invoke() ?? 0;
        public static void TriggerIncreaseSkillSpinCount() => OnIncreaseSkillSpinCount?.Invoke();

        /// <summary>
        /// Bao hieu rang skill dang trang bi da thay doi de PlayerDataManager luu lai.
        /// </summary>
        /// <param name="skillId">ID skill moi dang trang bi, hoac null/rong neu go trang bi.</param>
        public static void TriggerEquippedSkillIdChanged(string skillId) => OnEquippedSkillIdChanged?.Invoke(skillId);

        /// <summary>
        /// Yeu cau lay ID skill dang duoc trang bi tu PlayerDataManager.
        /// </summary>
        public static string TriggerRequestEquippedSkillId() => OnRequestEquippedSkillId?.Invoke();

        /// <summary>
        /// Bao hieu rang perk trang bi da thay doi de PlayerDataManager luu lai.
        /// </summary>
        /// <param name="perkId">ID perk moi dang duoc trang bi, hoac null/rong neu go trang bi.</param>
        public static void TriggerEquippedPerkIdChanged(string perkId) => OnEquippedPerkIdChanged?.Invoke(perkId);

        /// <summary>
        /// Yeu cau lay ID perk dang duoc trang bi tu PlayerDataManager.
        /// </summary>
        public static string TriggerRequestEquippedPerkId() => OnRequestEquippedPerkId?.Invoke();

        /// <summary>
        /// Thông báo rằng trạng thái trang bi (skill/perk) của người chơi đã thay đổi để UI đồng bộ lại.
        /// </summary>
        public static void TriggerPlayerEquipmentChanged() => OnPlayerEquipmentChanged?.Invoke();
        #endregion

        #region Extreme Mode Triggers
        /// <summary>
        /// Yeu cau thay doi trang thai Extreme Mode (tu Option Menu).
        /// </summary>
        /// <param name="enabled">True neu bat Extreme Mode, false neu tat.</param>
        public static void TriggerExtremeModeChanged(bool enabled) => OnExtremeModeChanged?.Invoke(enabled);

        /// <summary>
        /// Bao hieu trang thai Extreme Mode thuc te da thay doi de cac thanh phan khac phan ung.
        /// </summary>
        /// <param name="enabled">True neu Extreme Mode dang bat.</param>
        public static void TriggerExtremeModeStateChanged(bool enabled) => OnExtremeModeStateChanged?.Invoke(enabled);

        /// <summary>
        /// Yeu cau tra ve trang thai Extreme Mode hien tai (da luu tru).
        /// </summary>
        /// <returns>True neu Extreme Mode dang bat.</returns>
        public static bool TriggerRequestExtremeModeEnabled() => OnRequestExtremeModeEnabled?.Invoke() ?? false;
        #endregion

        #region AFK Mode Triggers
        /// <summary>
        /// Yeu cau thay doi trang thai AFK (tu Option Menu).
        /// </summary>
        /// <param name="enabled">True neu bat AFK, false neu tat.</param>
        public static void TriggerAfkEnabledChanged(bool enabled) => OnAfkEnabledChanged?.Invoke(enabled);

        /// <summary>
        /// Bao hieu trang thai AFK thuc te da thay doi de cac thanh phan khac phan ung.
        /// </summary>
        /// <param name="enabled">True neu AFK dang bat.</param>
        public static void TriggerAfkStateChanged(bool enabled) => OnAfkStateChanged?.Invoke(enabled);

        /// <summary>
        /// Yeu cau tra ve trang thai AFK hien tai (da luu tru).
        /// </summary>
        /// <returns>True neu AFK dang bat.</returns>
        public static bool TriggerRequestAfkEnabled() => OnRequestAfkEnabled?.Invoke() ?? false;
        #endregion

        #region Settings Triggers
        /// <summary>
        /// Bao phio hieu khi gia tri Music volume doi (Settings), de BGMController ap dung live.
        /// </summary>
        public static event Action<float> OnSettingsMusicVolumeChanged;
        /// <summary>
        /// Bao phio hieu khi gia tri SFX volume doi (Settings), de cac sistem SFX ap dung live.
        /// </summary>
        public static event Action<float> OnSettingsSfxVolumeChanged;
        /// <summary>
        /// Bao hieu khi cai dat Screen Shake & Overlay thay doi (Settings).
        /// </summary>
        public static event Action<bool> OnSettingsScreenShakeChanged;
        /// <summary>
        /// Bao hieu khi phim tat kich hoat skill thay doi (Settings).
        /// </summary>
        public static event Action<string> OnSettingsUseSkillKeyChanged;

        /// <summary>
        /// Yeu cau lam mo (muffle) hoac khoi phuc am thanh nhac nen BGM (dung khi player bi dong bang).
        /// </summary>
        public static event Action<bool> OnBgmAudioMuffleRequested;

        /// <summary>
        /// Trigger cho OnSettingsMusicVolumeChanged.
        /// </summary>
        /// <param name="value">Gia tri music volume moi.</param>
        public static void TriggerSettingsMusicVolumeChanged(float value) => OnSettingsMusicVolumeChanged?.Invoke(value);

        /// <summary>
        /// Trigger cho OnSettingsSfxVolumeChanged.
        /// </summary>
        /// <param name="value">Gia tri SFX volume moi.</param>
        public static void TriggerSettingsSfxVolumeChanged(float value) => OnSettingsSfxVolumeChanged?.Invoke(value);

        /// <summary>
        /// Trigger cho OnSettingsScreenShakeChanged.
        /// </summary>
        /// <param name="enabled">True neu bat hieu ung lac man hinh.</param>
        public static void TriggerSettingsScreenShakeChanged(bool enabled) => OnSettingsScreenShakeChanged?.Invoke(enabled);

        /// <summary>
        /// Trigger cho OnSettingsUseSkillKeyChanged.
        /// </summary>
        /// <param name="keyName">Ten phim moi duoc cai dat (vi du: "E", "Space").</param>
        public static void TriggerSettingsUseSkillKeyChanged(string keyName) => OnSettingsUseSkillKeyChanged?.Invoke(keyName);

        /// <summary>
        /// Trigger cho OnBgmAudioMuffleRequested.
        /// </summary>
        /// <param name="muffle">True de lam mo BGM, false de khoi phuc binh thuong.</param>
        public static void TriggerBgmAudioMuffleRequested(bool muffle) => OnBgmAudioMuffleRequested?.Invoke(muffle);
        #endregion

        #region Collectible Triggers
        public static GameObject TriggerCollectibleSpawnRequest(GameObject prefab, Vector3 position, Quaternion rotation) => OnCollectibleSpawnRequest?.Invoke(prefab, position, rotation);
        public static void TriggerCollectibleDespawnRequest(GameObject collectibleInstance) => OnCollectibleDespawnRequest?.Invoke(collectibleInstance);
        #endregion
        
        #region Listener Checks
        public static bool IsVFXPoolListening() => OnVFXDespawnRequest != null;
        public static bool IsBlockPoolListening() => OnBlockDespawnRequest != null;
        #endregion
        
        #endregion
    }
}
