using UnityEngine;

namespace Bombs.Data
{
    /// <summary>
    /// Du lieu cau hinh cho Laser Drone - mot doi tuong bay giong "bom" nhung khong no tai cho.
    /// No ha xuong mot doan -Y, nham theo một player ngau nhien (xoay Head nhin theo truc Z+),
    /// giu duong ngam 3 giay (co audio), roi ban mot chum vụ nổ chay tu Eye den muc tieu.
    /// </summary>
    [CreateAssetMenu(fileName = "NewLaserDroneData", menuName = "Kaboom Chaos/Bomb Types/Laser Drone")]
    public class LaserDroneData : BombData
    {
        [Header("Duong bay cua Laser Drone")]
        [Tooltip("Khoang cach -Y toi thieu ma drone bay xuong sau khi spawn.")]
        [Min(0f)]
        public float descentDistanceMin = 3f;

        [Tooltip("Khoang cach -Y toi da ma drone bay xuong sau khi spawn.")]
        [Min(0f)]
        public float descentDistanceMax = 6f;

        [Tooltip("Toc do bay xuong (don vi/giay).")]
        [Min(0.01f)]
        public float descentSpeed = 4f;

        [Tooltip("Khoang cach +Y ma drone bay len truoc khi despawn sau khi khai hoa.")]
        [Min(0f)]
        public float ascentDistance = 6f;

        [Tooltip("Toc do bay len khi roi di (don vi/giay).")]
        [Min(0.01f)]
        public float ascentSpeed = 4f;

        [Header("Trang Tri Copter")]
        [Tooltip("Toc do quay cua object con 'Copter' quanh truc Y (do/giay). 0 = khong quay. Cac object con khac nhung muon quay cung tuc nay co the dat ten 'Copter' hoac 'Rotor'.")]
        [Min(0f)]
        public float copterSpinSpeed = 360f;

        [Header("Ngam & Muc Tieu")]
        [Tooltip("Thoi gian drone nghi (khong di chuyen) sau khi ha xuong truoc khi toan quay Head nhin player.")]
        [Min(0f)]
        public float preAimRestDuration = 3f;

        [Tooltip("Thoi gian ngam (giay). Trong khoang thoi gian nay duong ngam tu Eye theo dau player va co audio.")]
        [Min(0f)]
        public float aimDuration = 3f;

        [Tooltip("Thoi gian nghi (giay) sau khi ket thuc ngam truoc khi khai hoa. Duong ngam duoc co dinh trong lúc nay de player co the roi di khoi vung ban.")]
        [Min(0f)]
        public float postAimDelay = 0.5f;

        [Header("Duong Ngam (Sight Line)")]
        [Tooltip("Prefab duong ngam tia laser tu Eye den player. Neu prefab co LineRenderer, behavior se cap nhat 2 diem dau-cuoi; nguoc lai prefab duoc dat o giua va scale theo chieu dai truc Z+ (1 don vi = 1 world unit).")]
        public GameObject sightLineVFX;

        [Tooltip("Do day duong ngam (khi prefab KHONG dung LineRenderer). Duoc dung cho phan scale ngang cua duong ngam.")]
        [Min(0.001f)]
        public float sightLineWidth = 0.15f;

        [Tooltip("Prefab VFX tai CUOI duong ngam (diem muc tieu). Hien thi o dau duong ngam, di chuyen theo player khi ngam. An di sau vụ nổ cuoi cung cua chum ban.")]
        public GameObject sightEndVFX;

        [Header("Audio Ngam")]
        [Tooltip("Audio phat mot lan (one-shot) khi bat dau ngam.")]
        public AudioClip aimAudioClip;

        [Tooltip("Gia tri pitch ban dau de tuong hop du lieu (khong con dung cho loop/scale pitch).")]
        [Range(0.1f, 3f)]
        public float aimAudioPitchStart = 0.5f;

        [Tooltip("Gia tri pitch ket thuc de tuong hop du lieu (khong con dung cho loop/scale pitch).")]
        [Range(0.1f, 3f)]
        public float aimAudioPitchEnd = 2f;

        [Header("Audio Beep (Ket thuc Ngam)")]
        [Tooltip("Audio beep lien tac rie. Play tai thoi diem ket thuc ngam (ngay sau audio aim dung) va LOOP chim trong mot khoang thoi gian ngau.")]
        public AudioClip beepAudioClip;

        [Tooltip("Cao do (pitch) cua audio beep.")]
        [Range(0.1f, 3f)]
        public float beepAudioPitch = 1f;

        [Tooltip("Thoi gian ma audio beep loop tai ket thuc ngam (giay). Khong u thoi gian nay thi beep dung truoc khi khai hoa.")]
        [Min(0.01f)]
        public float beepLoopDuration = 0.6f;

        [Header("Chum Vụ Nổ (Laser Beam)")]
        [Tooltip("Toc do 'dau dan' cua chum vụ nổ di tu Eye den muc tieu (don vi/giay).")]
        [Min(0.01f)]
        public float beamTravelSpeed = 12f;

        [Tooltip("Thoi gian giua 2 vụ nổ lien tiep trong chum (giay). Khoang cach giua cac vụ nổ = beamTravelSpeed * explosionInterval, do do muc tieu cang xa thi cang nhieu vụ nổ.")]
        [Min(0.01f)]
        public float explosionInterval = 0.12f;
    }
}