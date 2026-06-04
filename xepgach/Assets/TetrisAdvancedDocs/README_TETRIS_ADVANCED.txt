HƯỚNG DẪN GAME "XẾP GẠCH TETRIS NÂNG CAO"
================================================

Em đã thêm script:
Assets/Scripts/TetrisAdvanced/TetrisAdvancedGame.cs

Cách chạy:
1. Mở project bằng Unity.
2. Mở scene: Assets/Scenes/SampleScene.unity.
3. Bấm Play.
4. Game tự tạo bảng 10x20, UI, nút mobile, Hold, Next Queue, Ghost Piece.

Điều khiển trong Unity Editor:
- A / Mũi tên trái: sang trái
- D / Mũi tên phải: sang phải
- W / Mũi tên lên: xoay
- S / Mũi tên xuống: soft drop
- Space: hard drop
- C hoặc Left Shift: Hold piece
- R: chơi lại khi Game Over

Điều khiển mobile:
- Tap bên trái màn hình: sang trái
- Tap giữa màn hình: xoay
- Tap bên phải màn hình: sang phải
- Vuốt xuống: rơi chậm
- Vuốt lên: hard drop
- Nút HOLD: giữ khối
- Nút DROP: rơi nhanh

Tính năng đã có:
- Bảng chơi 10x20
- 7 khối I, O, T, S, Z, J, L theo random 7-bag
- Xoay 90 độ có wall kick cơ bản
- Di chuyển trái/phải, soft drop, hard drop
- Hold piece
- Next queue 3 khối
- Ghost piece
- Xóa hàng
- Tính điểm theo số dòng
- Combo
- Level tăng mỗi 10 dòng
- Soft drop / hard drop có điểm riêng
- T-spin detection cơ bản
- UI dọc cân đối cho mobile

Build Android:
1. File > Build Settings > Android > Switch Platform.
2. Player Settings > Resolution and Presentation > Orientation: Portrait.
3. Build APK.

Ghi chú:
- Project gốc gần như chưa có gameplay, nên em dùng cách Auto Bootstrap: không cần kéo GameObject/script vào scene.
- Nếu muốn tự thiết kế sprite đẹp hơn, chỉ cần thay hàm CreateSquareSprite hoặc dùng SpriteRenderer prefab riêng.
