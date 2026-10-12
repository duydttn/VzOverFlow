# 🛡️ SpamChecker Documentation

`SpamChecker` là một công cụ hỗ trợ lọc nội dung rác (spam) được tích hợp trong VzOverFlow để đảm bảo chất lượng bài viết và môi trường cộng đồng trong sạch.

## 🚀 Cách hoạt động

Lớp `SpamChecker` nằm tại `Helpers/SpamChecker.cs`. Nó sử dụng một quy trình kiểm tra đa lớp bao gồm phân tích từ khóa, cấu trúc văn bản và uy tín của người dùng.

### 1. Phân tích từ khóa (Regex Keywords)

Hệ thống sử dụng danh sách `SpamPatterns` (biểu thức chính quy) để phát hiện các nội dung nhạy cảm:
- **Cờ bạc & Cá cược**: Phát hiện các từ khóa tiếng Việt như `nhà cái`, `cá độ`, `lô đề`, `tài xỉu`, `kubet`,...
- **Nội dung người lớn**: Chặn các từ khóa liên quan đến `s*e*x`, `18+`, `drug`,...
- **Link lạ**: Kiểm tra các URL không thuộc danh sách domain tin cậy (`.vn`, `.com`, `.org`, `.net`,...).

### 2. Phân tích cấu trúc (Structural Analysis)

Áp dụng cho người dùng mới (chưa có uy tín cao):
- **Tỷ lệ Caps Lock**: Nếu văn bản có hơn 70% ký tự in hoa (đối với nội dung dài hơn 20 ký tự), nó sẽ bị coi là spam (hét vào mặt người đọc).
- **Số lượng liên kết**: Giới hạn tối đa 3 liên kết trong một nội dung duy nhất.
- **Lặp ký tự**: Phát hiện các chuỗi ký tự lặp lại vô nghĩa (ví dụ: `aaaaa`).

### 3. Cơ chế Uy tín (Trusted User Bypass)

Hệ thống tự động tin tưởng những người đóng góp tích cực:
- **Ngưỡng uy tín (Reputation) > 50**: Người dùng được coi là "Tin cậy" (Trusted).
- Người dùng tin cậy sẽ **bỏ qua các bước kiểm tra cấu trúc**, giúp họ thoải mái trình bày mã nguồn hoặc trích dẫn nhiều link tài liệu chuyên môn. Tuy nhiên, các từ khóa spam "cứng" vẫn được kiểm tra.

## 🛠️ Sử dụng trong Code

Để kiểm tra một nội dung có phải là spam hay không, bạn chỉ cần gọi phương thức tĩnh:

```csharp
bool isSpam = SpamChecker.ContainsSpam(content, userReputation);
```

**Tham số:**
- `content`: Nội dung chuỗi cần kiểm tra.
- `userReputation`: Điểm uy tín của người dùng đang thực hiện hành động (mặc định = 0).

## 📄 Ví dụ tích hợp trong Controller

```csharp
if (SpamChecker.ContainsSpam(model.Body, user.Reputation))
{
    ModelState.AddModelError("Body", "Nội dung của bạn bị hệ thống đánh dấu là spam. Vui lòng kiểm tra lại.");
    return View(model);
}
```

---
> [!TIP]
> Danh sách từ khóa spam có thể được mở rộng linh hoạt trong file `Helpers/SpamChecker.cs` để đối phó với các hình thức spam mới.