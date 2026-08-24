# ═══════════════════════════════════════════════════════════════

# Rules for AI Coding Assistant — KaboomChaos Project

# ═══════════════════════════════════════════════════════════════

## 1. Unity Version & API Conventions

- **Unity Version**: 6000.5.0f1 (Unity 6)
- Sử dụng các API mới nhất của Unity 6000.5, bao gồm:
  - `InputSystem` package (UnityEngine.InputSystem)
  - `UIToolkit` (UnityEngine.UIElements) cho UI system
  - Modern `MonoBehaviour` patterns (Awake/Start/Update, không dùng SendMessage)
  - Entity Component System (nếu cần hiệu suất cao)
  - Addressables system cho việc load tài nguyên (nếu cần)
- Không sử dụng các API legacy/obsolete như:
  - `UnityEngine.Network` (dùng Unity Gaming Services hoặc Netcode for GameObjects)
  - `WWW` (dùng `UnityWebRequest`)
  - Old Input System (dùng InputSystem Package)

---

## 2. Documentation & Commenting Rules

### 2.1 XML Documentation (///)

**BẮT BUỘC** cho tất cả các thành phần public/protected:

```csharp
/// <summary>
/// [Mô tả ngắn gọn về class/interface/struct]
/// </summary>
public class ExampleClass : MonoBehaviour
{ ... }
```

- Comment bằng tiếng việt không dấu, không comment bằng tiếng anh. Kệ các comment tiếng việt có dấu đã có sẵn

### 2.2 Regions & Code Organization

- Sử dụng `#region` để nhóm các thành phần liên quan
- Các region phổ biến:
  - `#region Fields`
  - `#region Properties`
  - `#region Unity Lifecycle`
  - `#region Public Methods`
  - `#region Private Methods`

### 2.3 Inline Comments

- Comment rõ ràng cho các logic phức tạp
- Sử dụng `//` comments giải thích **tại sao** làm như vậy, không chỉ **làm gì**

## 3. Interface Usage & Circular Dependency Prevention

- **Luôn ưu tiên dùng Interface** khi có sự phụ thuộc giữa các assembly khác nhau
- Interface giúp **tránh circular dependency** bằng cách tách biệt contract khỏi implementation
- Đặt interface trong assembly **Core** → namespace `Core.Interfaces`
- Mỗi interface chỉ nên có một responsibility (SRP - Single Responsibility Principle)

```csharp
// ✅ Đúng: Interface in Core -> Implementation in other assemblies
// --- Core/Interfaces/IDamageable.cs ---
namespace Core.Interfaces
{
    /// <summary>
    /// Interface cho các đối tượng có thể nhận sát thương.
    /// </summary>
    public interface IDamageable
    {
        void TakeDamage(float amount);
        bool IsAlive { get; }
    }
}

// --- Bombs/Bomb.cs ---
namespace Bombs
{
    public class Bomb : MonoBehaviour, IDamageable
    {
        public void TakeDamage(float amount) { /* ... */ }
        public bool IsAlive => true;
    }
}

// --- Player/PlayerStats.cs ---
namespace Player
{
    public class PlayerStats
    {
        // Chỉ phụ thuộc vào Interface -> không circular dependency
        public void ApplyExplosion(IDamageable damageable, float damage)
        {
            damageable.TakeDamage(damage);
        }
    }
}
```

- **Nguyên tắc Dependency Inversion (DIP)**:
  - Module cấp cao không phụ thuộc vào module cấp thấp
  - Cả hai đều phụ thuộc vào abstraction (Interface)

## 4. Assembly Architecture — Core as Center

### 4.1 Assembly Dependency Graph

```
                    ┌─────────────────────┐
                    │      Core           │
                    │  - Interfaces/      │
                    │  - Utilities/       │
                    └────────┬────────────┘
                             │
              ┌──────────────┼──────────────┐
              │              │              │
              ▼              ▼              ▼
        ┌──────────┐  ┌──────────┐  ┌──────────┐
        │ Managers  │  │  Player  │  │    UI    │
        └──────────┘  └──────────┘  └──────────┘
              │              │              │
              ▼              ▼              ▼
        ┌──────────┐  ┌──────────┐  ┌──────────┐
        │  Bombs   │  │  Enemies │  │   ...    │
        └──────────┘  └──────────┘  └──────────┘
```

### 4.2 Assembly Rules

| Assembly     | Namespace Root | References                  | Mô tả                                        |
| ------------ | -------------- | --------------------------- | -------------------------------------------- |
| **Core**     | `Core`         | None (chỉ tham chiếu Unity) | Chứa Interfaces, Utilities, Enums, Constants |
| **Managers** | `Managers`     | Core                        | Game managers (GameManager, PoolManager...)  |
| **Player**   | `Player`       | Core                        | Player logic, stats, movement                |
| **UI**       | `UI`           | Core                        | UI screens, HUD, menus                       |
| **Bombs**    | `Bombs`        | Core                        | Bomb logic, explosions                       |
| **Editor**   | `Editor`       | Core, UnityEditor           | Editor tools, custom inspectors              |

**Quan trọng**:

- ✅ **Core KHÔNG được tham chiếu** assembly nào khác (tránh circular dependency)
- ✅ Các assembly khác **CHỈ tham chiếu Core**
- ✅ Nếu Assembly A cần gọi Assembly B, hãy tạo interface trong Core và inject dependency
- ❌ Không bao giờ tạo reference vòng tròn giữa các assembly

### 4.3 Core Namespace Structure

````
KaboomChaos
├── Interfaces/           # Tất cả interface định nghĩa ở đây
│   ├── IDamageable.cs
│   ├── IInputHandler.cs
│   ├── IPoolable.cs
│   └── ...
├── Enums/                # Enum dùng chung
│   ├── GameState.cs
│   └── ...
├── Constants/            # Hằng số dùng chung
│   └── ...
├── Utilities/            # Các utility class thuần

### 4.4 Dependency Injection Pattern

Khi cần giao tiếp giữa các assembly:

```csharp
// ✅ DÙNG: Interface + DI (Dependency Injection)
// Core/Interfaces/IInputHandler.cs
namespace Core.Interfaces
{
    public interface IInputHandler
    {
        Vector2 MoveInput { get; }
        bool IsJumpPressed { get; }
    }
}

// Player/PlayerMovement.cs
namespace Player
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour inputBehaviour; // Inject via Inspector
        private IInputHandler inputHandler;

        private void Awake()
        {
            inputHandler = inputBehaviour as IInputHandler;
            // Hoặc: inputHandler = FindAnyObjectByType<MonoBehaviour>() as IInputHandler;
        }
    }
}

// Managers/PlayerInputHandler.cs
namespace Managers
{
    public class PlayerInputHandler : MonoBehaviour, IInputHandler
    {
        // Implementation...
    }
}
````

---

## 5. Naming Conventions

| Loại             | Convention                        | Ví dụ              |
| ---------------- | --------------------------------- | ------------------ |
| Namespace        | `<SubSystem>`                     | `Player`           |
| Interface        | `I` + PascalCase                  | `IDamageable`      |
| Class            | PascalCase                        | `PlayerMovement`   |
| Method           | PascalCase                        | `MoveToPosition()` |
| Property         | PascalCase                        | `MoveSpeed`        |
| Public Field     | PascalCase                        | `HealthPoints`     |
| Private Field    | `_camelCase`                      | `_currentHealth`   |
| Serialized Field | `_camelCase` + `[SerializeField]` | `_moveSpeed`       |
| Local Variable   | camelCase                         | `playerPosition`   |
| Constant         | PascalCase                        | `MaxHealth`        |
| Event            | PascalCase                        | `OnPlayerDied`     |
| Unity Event      | `On` + EventName                  | `OnCollisionEnter` |

---

## 6. Code Style & Best Practices

- **Sử dụng** `var` khi kiểu dữ liệu đã rõ ràng từ vế phải
- **Tránh** magic numbers/strings — dùng const hoặc enum
- **Luôn** kiểm tra null trước khi gọi method trên reference
- **Sử dụng** `[SerializeField]` thay vì `public` field để expose trong Inspector
- **Cache** component references trong Awake() thay vì GetComponent() mỗi frame
- **Sử dụng** `ObjectPool` pattern cho objects được spawn/destroy thường xuyên
- **Tránh** dùng `FindObjectOfType`, `GameObject.Find` trong Update/LateUpdate
- **Sử dụng** bất đồng bộ (async/await, UniTask, Coroutines) cho loading operations

### 6.1 MonoBehaviour Lifecycle Pattern

```csharp
public class ExampleBehaviour : MonoBehaviour
{
    #region Fields
    private Rigidbody2D _rb;
    private Collider2D _collider;
    #endregion

    #region Properties
    public float Speed { get; set; }
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
    }

    private void Start() { /* Initialize */ }
    private void Update() { /* Per-frame logic */ }
    private void OnDestroy() { /* Cleanup */ }
    #endregion

    #region Public Methods
    public void MoveTo(Vector2 target) { /* ... */ }
    #endregion

    #region Private Methods
    private void HandleCollision() { /* ... */ }
    #endregion
}
```

---

## 7. File Organization

- **Mỗi file chỉ chứa MỘT type** (class/interface/struct/enum)
- Ngoại lệ: Các class nhỏ phục vụ cho class chính (ví dụ: custom EventArgs)
- **Đặt tên file trùng với tên type** (ví dụ: `IDamageable.cs`, `PlayerMovement.cs`)
- Group các file theo tính năng, không theo kiểu (ví dụ: Bombs/Explosion.cs, Bombs/Bomb.cs)

---

## 8. Git & Version Control

- Commit message format: `[<Type>] <Short description>`
  - Ví dụ: `[Feat] Add bomb explosion system`
  - Ví dụ: `[Fix] Fix null reference when player dies`
  - Ví dụ: `[Refactor] Extract input handling to interface`
- Không commit file meta không cần thiết
- Sử dụng `.gitignore` chuẩn của Unity

---

## 9. Performance Considerations

- **Pooling**: Dùng ObjectPool cho bom, collectibles, particle effects
- **Batching**: Ưu tiên GPU instancing, sprite atlas
- **Static analysis**: Mark các object không di chuyển là `Static` trong Inspector
- **UI**: Sử dụng UI Toolkit (UIElements) thay vì uGUI cũ nếu được
