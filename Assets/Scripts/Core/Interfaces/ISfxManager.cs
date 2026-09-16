using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho he thong quan ly am thanh (SFX).
    /// </summary>
    public interface ISfxManager
    {
        /// <summary>
        /// Phat mot SFX one-shot 3D positional.
        /// </summary>
        /// <param name="spatialBlend">Muc do spatial cua audio: 0 = hoan toan 2D, 1 = hoan toan 3D. Mac dinh 0.5 (trung gian).</param>
        AudioSource PlaySfx(AudioClip clip, Vector3 position, float volume = 1f, float pitch = 1f, float spatialBlend = 0.5f);

        /// <summary>
        /// Phat mot SFX one-shot 2D (khong chieu, cho UI/HUD/menu canh bao).
        /// </summary>
        AudioSource PlaySfx(AudioClip clip, float volume = 1f, float pitch = 1f);

        /// <summary>
        /// Phat mot SFX loop (tic-tac, hum, beep) va tra ve handle de stop/pitch.
        /// </summary>
        ISfxLoopHandle PlaySfxLoop(AudioClip clip, Transform parent, float volume = 1f, float pitch = 1f, bool spatial = true);
    }

    /// <summary>
    /// Handle de dieu khien mot SFX dang loop (chuyen pitch hoac stop).
    /// </summary>
    public interface ISfxLoopHandle
    {
        void SetPitch(float pitch);
        void Stop();
    }
}

