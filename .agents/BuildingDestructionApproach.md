Dưới đây là bản tóm tắt toàn diện về cách tiếp cận **Phá hủy Modular dựa trên Đồ thị Liên kết kết hợp Đóng băng/Kích hoạt Vật lý động (Graph-based Modular Destruction with Dynamic Sleep/Wake)** để bạn có cái nhìn tổng quan nhất khi đưa vào dự án.

---

## 1. Ý tưởng cốt lõi

Thay vì phó mặc việc phá hủy và tính toán áp lực gãy công trình cho Engine vật lý (PhysX) vốn rất nặng, hệ thống này chuyển bài toán về **Logic Đồ thị (Graph Logic)** bằng CPU:

- Công trình được cấu thành từ các mảnh ghép tĩnh (`isKinematic = true`). Mối quan hệ giữa các mảnh kề nhau được lưu trữ dưới dạng một **Đồ thị Kết cấu (Structural Graph)**.
- Khi bom nổ, game dùng thuật toán duyệt đồ thị (BFS/DFS) từ các điểm tựa cố định (Anchor/Nền đất) để tìm các mảng bị "mất gốc".
- Những mảng mất gốc này mới được kích hoạt vật lý (`isKinematic = false`) để rơi xuống, va chạm chéo với nhau tạo thành đống đổ nát, rồi tự động **"Ngủ đông chủ động" (Active Sleep)** khi nằm im để giải phóng CPU.

---

## 2. Các Đánh đổi (Tradeoffs)

- **Ưu điểm (Pros):**
- **Hiệu năng cực cao:** Lúc bình thường tốn 0% CPU vật lý. Khi nổ, CPU chỉ tính toán thuật toán duyệt đồ thị rất nhẹ thay vì giải các phương trình khớp nối (`FixedJoint`) phức tạp.
- **Visual sập đổ chân thực:** Nhà sập theo mảng, cầu gãy đôi, công trình đổ đổ sụp theo đúng logic kết cấu (không bị lỗi "nhà treo lơ lửng" giữa trời).
- **Đống đổ nát có tương tác:** Mảnh vỡ xếp chồng lên nhau thành đống, người chơi có thể giẫm/nhảy lên được.

- **Nhược điểm (Cons):**
- **Tăng độ phức tạp code:** Cần tự viết hệ thống quản lý đồ thị, thuật toán kiểm tra kết nối và cơ chế quản lý trạng thái mảnh vỡ.
- **Chi phí Bake Map ban đầu:** Cần tốn một chút thời gian lúc load game hoặc dựng map để thiết lập danh sách hàng xóm (`neighbors`) cho từng mảnh.

---

## 3. Chuẩn bị những gì? (Prerequisites)

Để chuẩn bị triển khai, bạn cần thiết lập cấu trúc cơ bản trong Unity bao gồm:

- **Dữ liệu Mảnh vỡ (`DestructionPiece`):** Mỗi object cấu thành map cần có Box/Sphere Collider (tránh Mesh Collider), Rigidbody (mặc định bật `isKinematic = true`) và script lưu trạng thái (`isAnchor`, `neighbors`, `isSleeping`).
- **Hệ thống Layer vật lý:** Tạo Layer riêng cho mảnh vỡ (ví dụ: `Debris`). Trong **Project Settings > Physics**, cấu hình cho phép `Debris` va chạm với chính nó (để tạo đống đổ nát), với `Player` và với `Terrain`.
- **Bộ quản lý trung tâm (`DestructionManager`):** Một script Singleton điều phối việc chạy thuật toán duyệt đồ thị và quản lý danh sách các mảnh vỡ cần xử lý.

---

## 4. Cách triển khai (Implementation Steps)

### Bước 1: Thiết lập Đồ thị (Bake Graph)

Khi bắt đầu map, hệ thống tự động quét toàn bộ các mảnh. Mảnh nào nằm chạm mặt đất được đánh dấu là `isAnchor = true`. Các mảnh chạm nhau sẽ tự động add nhau vào danh sách `neighbors` của đối phương.

### Bước 2: Xử lý Vụ nổ

Khi bom nổ, dùng `Physics.OverlapSphere` để tìm các mảnh trong bán kính:

1. Đánh dấu các mảnh trúng bom trực tiếp là `isDestroyed = true` và ẩn/xóa chúng đi.
2. Từ các mảnh _hàng xóm trực tiếp_ của những mảnh vừa mất, chạy thuật toán BFS ngược về các node `isAnchor`.

### Bước 3: Kích hoạt Sụp đổ

Mảnh nào không tìm được đường về bất kỳ `Anchor` nào sẽ được gom vào danh sách sụp đổ. Chuyển `isKinematic = false` cho cả cụm này (có thể kích hoạt rải rác qua từng frame để tránh spike FPS) và áp một lực nổ (`AddExplosionForce`) cho sinh động.

### Bước 4: Đóng băng tối ưu (Active Sleep)

Mỗi mảnh vỡ khi rơi xuống đất sẽ liên tục check vận tốc. Nếu vận tốc $\approx 0$ trong khoảng 1.5 giây, chuyển mảnh đó về `isKinematic = true` (Hóa đá). CPU hoàn toàn được giải phóng, nhưng Collider tĩnh vẫn giữ nguyên để người chơi tương tác.

---

## 5. Các Edge Case (Trường hợp biên) và Cách xử lý

- **Mảnh vỡ đang "Hóa đá" lại bị quả bom khác nổ trúng:**
- _Xử lý:_ Vòng quét của quả bom tiếp theo khi quét trúng mảnh vỡ đang ngủ phải gọi hàm `WakeUp()` để trả lại `isKinematic = false` trước khi áp lực nổ văng nó đi.

- **Mặt đất/Block đất nâng đỡ đống đổ nát bị nổ mất:**
- _Xử lý:_ Khi một block nền bị mất, bắn một tia `Physics.BoxCast` hoặc `OverlapBox` thẳng lên trên trời. Bất kỳ mảnh vỡ đang ngủ nào nằm trong vùng quét đó sẽ bị gọi `WakeUp()` để rơi tự do xuống tiếp.

- **Mảnh A đang nằm đè lên mảnh B, nhưng mảnh B bị biến mất hoặc bay đi:**
- _Xử lý:_ Đừng hủy hoàn toàn Rigidbody của mảnh vỡ mà hãy dùng hàm `rb.Sleep()` tự nhiên của Unity hoặc bắt sự kiện `OnCollisionEnter/Exit`. Khi mảnh B di chuyển, hệ thống vật lý mặc định của Unity hoặc code check khoảng cách sẽ tự động "đánh thức" mảnh A dậy để nó rơi theo.

- **Số lượng mảnh vỡ quá lớn đổ xuống cùng một lúc:**
- _Xử lý (Cap số lượng):_ Giới hạn một số lượng Rigidbody động tối đa đồng thời (ví dụ: tối đa 50-70 mảnh lớn được bật vật lý). Các mảnh siêu nhỏ bên trong sẽ biến thành Particle Mesh không vật lý và tự hủy sau vài giây để giảm tải cho PhysX.
