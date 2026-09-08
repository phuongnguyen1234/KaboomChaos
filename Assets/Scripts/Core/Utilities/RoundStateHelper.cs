using Core.Enums;
using Core.Interfaces;

namespace Core.Utilities
{
    /// <summary>
    /// Tien ich kiem tra trang thai vong lap game de quyet dinh khi nao ap dung cac thay doi
    /// (vi du: Extreme Mode, AFK) chi trong do switch/giua cac round.
    /// </summary>
    public static class RoundStateHelper
    {
        /// <summary>
        /// Cho biet game dang O TRONG mot round dang dien ra hay khong (PreRound hoac RoundActive).
        /// Khi khong co IGameStateProvider (luc khoi dong) thi coi nhu khong trong round.
        /// Dung de tri hoan cac thay doi (Extreme Mode / AFK) cho toi khi round ket thuc.
        /// </summary>
        /// <returns>True neu dang trong round (PreRound hoac RoundActive), nguoc fu thuong false.</returns>
        public static bool IsInRound()
        {
            IGameStateProvider provider = IGameStateProvider.Instance;
            if (provider == null) return false;

            GameState state = provider.CurrentState;
            return state == GameState.PreRound || state == GameState.RoundActive;
        }
    }
}