using UnityEngine;

namespace Core.Interfaces
{
    /// <summary>
    /// Service locator don gian de truy cap ISfxManager tu Core ma khong bi circular dependency.
    /// </summary>
    public static class SfxService
    {
        public static ISfxManager Instance { get; set; }
    }
}

