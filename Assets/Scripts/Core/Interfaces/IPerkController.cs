namespace Core.Interfaces
{
    /// <summary>
    /// Giao dien cho bo dieu khien Perk tren Player.
    /// Quan ly mot perk dang duoc trang bi tren nguoi choi (1 slot, tuong tu ISkillController).
    /// </summary>
    public interface IPerkController
    {
        /// <summary>
        /// Perk hien tai dang duoc trang bi (null neu chua trang bi perk nao).
        /// </summary>
        IPerkData CurrentPerk { get; }

        /// <summary>
        /// Trang bi mot perk moi cho Player.
        /// Neu truyen null hoac trung voi perk hien tai, se go bo trang bi.
        /// Perk cu (neu co) se tu dong bi go bo hieu ung truoc khi perk moi duoc ap dung.
        /// </summary>
        /// <param name="perkData">Du lieu cua perk can trang bi, hoac null de go trang bi.</param>
        void EquipPerk(IPerkData perkData);
    }
}