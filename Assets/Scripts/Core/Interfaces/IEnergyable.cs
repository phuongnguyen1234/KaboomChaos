namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho cac doi tuong co nang luong (energy).
    /// Energy day 100% la dieu kien de su dung Skill; sau khi dung, energy bi rut can
    /// va tu sac lai trong mot khoang thoi gian.
    /// </summary>
    public interface IEnergyable
    {
        /// <summary>
        /// Luong energy hien tai cua doi tuong.
        /// </summary>
        float CurrentEnergy { get; }

        /// <summary>
        /// Luong energy toi da, tuong ung voi gia tri 100%.
        /// </summary>
        float MaxEnergy { get; }

        /// <summary>
        /// True khi energy da day 100% va khong dang trong qua trinh sac.
        /// Chi khi gia tri nay la true thi Skill moi duoc phep su dung.
        /// </summary>
        bool IsFullyCharged { get; }

        /// <summary>
        /// True khi energy dang trong qua trinh tu sac.
        /// </summary>
        bool IsRecharging { get; }

        /// <summary>
        /// Thoi gian sac con lai (giay).
        /// </summary>
        float RechargeRemaining { get; }

        /// <summary>
        /// Tieu toan bo energy de su dung Skill (chi thanh cong khi energy dang day 100%).
        /// Sau khi tieu, energy se tu sac day lai trong thoi gian rechargeDuration giay,
        /// thuong bang voi thoi gian hoi chieu (cooldown) cua Skill.
        /// </summary>
        /// <param name="rechargeDuration">Thoi gian (giay) de sac day lai energy. Neu <= 0 thi dung thoi gian sac mac dinh.</param>
        /// <returns>True neu tieu thanh cong, False neu energy chua day hoac dang trong qua trinh sac.</returns>
        bool TryConsumeFullEnergy(float rechargeDuration);

        /// <summary>
        /// Bat dau rut can energy tu tu tu day (100%) ve 0 trong `drainDuration` giay,
        /// dung de dong bo voi thoi gian duy tri (duration) cua Skill. Sau khi rut het,
        /// energy se tu sac day lai trong `rechargeDuration` giay.
        /// Chi thanh cong khi energy dang day 100% va khong dang sac/rut.
        /// </summary>
        /// <param name="drainDuration">Thoi gian (giay) de rut energy tu day ve 0.</param>
        /// <param name="rechargeDuration">Thoi gian (giay) sac lai sau khi rut het. Neu <= 0 thi dung thoi gian sac mac dinh.</param>
        /// <returns>True neu bat dau rut thanh cong.</returns>
        bool TryStartDrain(float drainDuration, float rechargeDuration);

        /// <summary>
        /// Lam day energy ngay lap tuc va huy qua trinh sac dang chay (neu co).
        /// Danh cho collectible Energy hoac cac hieu ung hoi phuc nang luong.
        /// </summary>
        /// <returns>Luong energy thuc te da duoc hoi phuc.</returns>
        float RestoreFullEnergy();
    }
}