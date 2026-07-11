using UnityEngine;

/// <summary>
/// ScriptableObject chứa metadata cho một map, bao gồm ID, tên, ảnh bìa và prefab của map.
/// </summary>
[CreateAssetMenu(fileName = "NewMapData", menuName = "Kaboom Chaos/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Thông tin Map")]
    [Tooltip("ID định danh duy nhất cho map.")]
    [SerializeField] private string _id;

    [Tooltip("Tên hiển thị của map.")]
    [SerializeField] private string _mapName;

    [Tooltip("Ảnh bìa của map, dùng để hiển thị trong UI.")]
    [SerializeField] private Sprite _coverImage;

    [Tooltip("Prefab chính chứa toàn bộ nội dung của map.")]
    [SerializeField] private GameObject _mapPrefab;

    // --- Public Accessors ---

    /// <summary>
    /// ID định danh duy nhất cho map.
    /// </summary>
    public string ID => _id;

    /// <summary>
    /// Tên hiển thị của map.
    /// </summary>
    public string MapName => _mapName;

    /// <summary>
    /// Ảnh bìa của map, dùng để hiển thị trong UI.
    /// </summary>
    public Sprite CoverImage => _coverImage;

    /// <summary>
    /// Prefab chính chứa toàn bộ nội dung của map.
    /// </summary>
    public GameObject MapPrefab => _mapPrefab;
}