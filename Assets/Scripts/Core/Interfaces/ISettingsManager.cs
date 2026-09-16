namespace Core.Interfaces
{
    /// <summary>
    /// Contract dla configuracion global (Settings) cua game.
    /// Lua gia tri trong PlayerPrefs de khoi phuc sau restart game.
    /// </summary>
    /// <remarks>
    /// UseSkillKey se luu cao sam nume cua Key (vi du "E", "Space", "K").
    /// Core KHONG phu thuoc de UnityEngine.InputSystem, deci cac assembly si
    /// (UI, Skills) face conversie Key → nume tren partea lor.
    /// </remarks>
    public interface ISettingsManager
    {
        /// <summary>
        /// Music volume trong khoang [0, 100] (0 = mute).
        /// </summary>
        float MusicVolume { get; }

        /// <summary>
        /// Dan moi gia tri music volume.
        /// </summary>
        /// <param name="value">Gia tri moi trong khoang [0, 100].</param>
        void SetMusicVolume(float value);

        /// <summary>
        /// SFX volume trong khoang [0, 100] (0 = mute).
        /// </summary>
        float SfxVolume { get; }

        /// <summary>
        /// Dan moi gia tri SFX volume.
        /// </summary>
        /// <param name="value">Gia tri moi trong khoang [0, 100].</param>
        void SetSfxVolume(float value);

        /// <summary>
        /// Key dung de kich hoat Skill, luu cao sam nume Key (vi du "E", "Space").
        /// Mac dinh = "E".
        /// </summary>
        string UseSkillKey { get; }

        /// <summary>
        /// Dan moi key dung de kich hoat Skill.
        /// </summary>
        /// <param name="keyName">Sam nume Key (vi du "E").</param>
        void SetUseSkillKey(string keyName);

        /// <summary>
        /// Trang thai bat/tat hieu ung lac man hinh va overlay khi co vu no.
        /// </summary>
        bool ScreenShakeEnabled { get; }

        /// <summary>
        /// Dat trang thai bat/tat hieu ung lac man hinh.
        /// </summary>
        /// <param name="enabled">True neu bat, false neu tat.</param>
        void SetScreenShakeEnabled(bool enabled);
    }
}