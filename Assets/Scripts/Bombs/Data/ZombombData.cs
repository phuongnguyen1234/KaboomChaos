using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// Du lieu cau hinh cho bom Zombomb - mot loai bom hẹn giờ (fuse bomb)
    /// co kha nang lan tung dot (dash) de duoi theo nguoi choi gan nhat
    /// khi nguoi choi do nam trong ban kinh theo đuổi.
    /// </summary>
    [CreateAssetMenu(fileName = "NewZombombData", menuName = "Kaboom Chaos/Bomb Types/Zombomb")]
    public class ZombombData : BombData
    {
        [Header("Hanh vi Zombomb Theo đuổi")]
        [Tooltip("Ban kinh theo đuổi: bom chi bat dau lan duoi neu nguoi choi gan nhat nam trong khoang cach nay.")]
        [Min(0f)]
        public float chaseRadius = 9f;

        [Tooltip("Khoang cach dung lao: neu nguoi choi gan hon khoang nay, bom se dung lao va dung yen.")]
        [Min(0f)]
        public float stopRange = 1.3f;

        [Tooltip("Toc do lao toi (lan) cua bom khi o trang thai Dash (don vi/giay).")]
        [Min(0f)]
        public float dashSpeed = 10f;

        [Tooltip("Thoi gian lao toi trong moi chu ky (giay).")]
        [Min(0.01f)]
        public float dashDuration = 0.35f;

        [Tooltip("Thoi gian nghi giua hai lan lao (giay).")]
        [Min(0f)]
        public float restDuration = 0.9f;

        [Tooltip("He so nhan toc do quay/lan. 1 = lan khong truot chuan (omega = toc do tinh tien / ban kinh sphere collider). Tang len de lan nhanh hon binh thuong, giam xuong (<1) de lan cham hon. 0 = khong xoay.")]
        [Min(0f)]
        public float rollMultiplier = 1f;

        [Tooltip("Gia toc ham khi bom dang nghi (don vi/giay binh phuong) de bom dung lai muot hon.")]
        [Min(0f)]
        public float restDeceleration = 8f;
    }
}