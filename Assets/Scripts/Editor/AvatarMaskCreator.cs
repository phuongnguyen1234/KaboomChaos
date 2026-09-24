#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Editor
{
    /// <summary>
    /// Cong cu Editor tao AvatarMask cho phan than tren (Upper Body) cua Player.
    /// </summary>
    public class AvatarMaskCreator
    {
        #region Public Methods
        [MenuItem("Tools/Player/Create Upper Body Avatar Mask")]
        public static void CreateUpperBodyMask()
        {
            GameObject player = Selection.activeGameObject;

            if (player == null)
            {
                Debug.LogError("Vui long chon Player prefab.");
                return;
            }

            Transform root = player.transform;

            AvatarMask mask = new();

            // Than tren (Upper body)
            AddTransform(mask, root, "Root/PivotTorso/Torso");
            AddTransform(mask, root, "Root/PivotTorso/PivotHead");
            AddTransform(mask, root, "Root/PivotTorso/PivotL_Arm");
            AddTransform(mask, root, "Root/PivotTorso/PivotR_Arm");

            string path = "Assets/PlayerUpperBody.mask";

            AssetDatabase.CreateAsset(mask, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Da tao Avatar Mask: {path}");
        }
        #endregion

        #region Private Methods
        private static void AddTransform(AvatarMask mask, Transform root, string path)
        {
            Transform target = root.Find(path);

            if (target == null)
            {
                Debug.LogWarning($"Khong tim thay Transform: {path}");
                return;
            }

            mask.AddTransformPath(target, true);
        }
        #endregion
    }
}
#endif
