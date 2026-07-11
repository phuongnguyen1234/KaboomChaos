using UnityEngine;

[ExecuteAlways]
[SelectionBase] // Giúp click vào Object trong Scene dễ hơn
public class ColorTint : MonoBehaviour
{
    [Header("Chế độ màu")]
    [SerializeField] 
    [Tooltip("Tích chọn nếu muốn đổi màu riêng cho Object này. Bỏ tích để dùng màu mặc định của Material.")]
    private bool overrideColor = false;

    [SerializeField] 
    private Color customColor = Color.white;

    private const string PROPERTY_NAME = "_Tint";
    private MaterialPropertyBlock propBlock;
    private Renderer myRenderer;

    private void OnValidate()
    {
        UpdateColor();
    }

    private void Start()
    {
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (myRenderer == null) myRenderer = GetComponent<Renderer>();
        if (myRenderer == null) return;

        propBlock ??= new MaterialPropertyBlock();

        // Lấy block hiện tại của Renderer
        myRenderer.GetPropertyBlock(propBlock);

        if (overrideColor)
        {
            // Nếu muốn tự chỉnh: Ghi đè màu customColor vào block
            propBlock.SetColor(PROPERTY_NAME, customColor);
            myRenderer.SetPropertyBlock(propBlock);
        }
        else
        {
            // Nếu KHÔNG muốn tự chỉnh: Xóa thuộc tính biến màu cũ trong Block 
            // để Object tự động quay về dùng màu gốc của Material Variant đang gắn
            propBlock.Clear();
            myRenderer.SetPropertyBlock(propBlock);
        }
    }
}