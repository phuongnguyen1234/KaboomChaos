using UnityEngine;
using System.Collections.Generic;

namespace Core.Database
{
    [CreateAssetMenu(fileName = "BGMDatabase", menuName = "Kaboom Chaos/Database/BGM Database")]
    public class BGMDatabase : ScriptableObject
    {
        [Header("Music Clips")]
        [Tooltip("Nhạc phát ở màn hình chính (chưa dùng).")]
        public AudioClip HomeMusic;
        [Tooltip("Nhạc phát ở sảnh chờ (trong giai đoạn chọn map).")]
        public AudioClip LobbyMusic;

        [Header("Gameplay Music - Normal")]
        [Tooltip("Danh sách nhạc nền cho gameplay ở độ khó thường (Intensity < 4).")]
        public List<AudioClip> NormalGameplayMusic;
        [Tooltip("Nhạc nền cho 30 giây cuối ở độ khó thường.")]
        public AudioClip Last30sNormalMusic;

        [Header("Gameplay Music - Intense")]
        [Tooltip("Danh sách nhạc nền cho gameplay ở độ khó cao (Intensity >= 4).")]
        public List<AudioClip> IntenseGameplayMusic;
        [Tooltip("Nhạc nền cho 30 giây cuối ở độ khó cao.")]
        public AudioClip Last30sIntenseMusic;
    }
}
