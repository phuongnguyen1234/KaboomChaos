namespace Core.Interfaces
{
    /// <summary>
    /// Interface quan ly vong lap game va thong so do kho intensity.
    /// </summary>
    public interface IGameloopManager
    {
        /// <summary>
        /// Instance singleton toan cuc cua IGameloopManager.
        /// </summary>
        static IGameloopManager Instance { get; set; }

        float CurrentIntensity { get; }
        float NextRoundIntensity { get; }
        float MinIntensity { get; }
        float MaxIntensity { get; }
        void StartGame();
    }
}