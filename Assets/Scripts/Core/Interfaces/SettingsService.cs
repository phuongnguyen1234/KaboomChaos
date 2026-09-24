using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Service locator don gian de truy cap ISettingsManager tu Core ma khong bi circular dependency.
    /// SettingsManager (o assembly Managers) gan Instance nay vao Awake.
    /// </summary>
    public static class SettingsService
    {
        public static ISettingsManager Instance { get; set; }
    }
}