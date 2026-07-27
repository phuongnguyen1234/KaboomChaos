namespace Core.Interfaces
{
    /// <summary>
    /// Interface for any controllable bomb object in the game world.
    /// Provides a contract for external systems to interact with bombs
    /// without needing to know their concrete implementation.
    /// </summary>
    public interface IBombController
    {
        /// <summary>
        /// Activates the bomb's primary function (e.g., starts the fuse, begins falling).
        /// </summary>
        void Activate();

        /// <summary>
        /// Gets the current activation state of the bomb.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Resets the bomb's internal state so it can be reused by an object pool.
        /// </summary>
        void ResetState();
    }
}