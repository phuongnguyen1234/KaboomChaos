using Core.Enums;

namespace Core.Interfaces
{
    /// <summary>
    /// Cung cap trang thai hien tai cua vong lap game (GameState) qua singleton.
    /// Duoc implement boi GameloopManager (assembly Managers) va gan Instance trong Awake.
    /// UI (assembly UI) khong the reference assembly Managers nen doc trang thai qua interface nay.
    /// </summary>
    public interface IGameStateProvider
    {
        /// <summary>
        /// The hien Singleton toan cuc cua IGameStateProvider.
        /// Duoc gan boi lop cu the (GameloopManager) trong Awake(); cac assembly khac
        /// chi doc gia tri nay qua interface de tranh tham chieu truc tiep toi assembly Managers.
        /// </summary>
        static IGameStateProvider Instance { get; set; }

        /// <summary>
        /// Trang thai hien tai cua vong lap game
        /// (None, Intermission, Building, PreRound, RoundActive, PostRound).
        /// </summary>
        GameState CurrentState { get; }
    }
}