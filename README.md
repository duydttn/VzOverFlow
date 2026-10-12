# 🚀 VzOverFlow - Premium Q&A Platform

![License](https://img.shields.io/badge/license-Private-blue?style=for-the-badge)
![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-9.0-512bd4?style=for-the-badge&logo=dotnet)
![Tailwind CSS](https://img.shields.io/badge/Tailwind_CSS-3.0-38b2ac?style=for-the-badge&logo=tailwind-css)

**VzOverFlow** là một nền tảng hỏi đáp hiện đại theo mô hình Stack Overflow, được thiết kế với trải nghiệm người dùng cao cấp (Premium UI), hệ thống Gamification mạnh mẽ và khả năng bảo mật nội dung tự động.

---

## ✨ Điểm nổi bật (New Features)

### 🎨 Giao diện Premium & Dark Mode
- **Hall of Fame**: Bảng xếp hạng người dùng với thiết kế Podium (Bục vinh quang) 3D, hiệu ứng Glassmorphism và gradient rực rỡ.
- **Dark Mode**: Hỗ trợ chế độ tối hoàn hảo, bảo vệ mắt và tăng tính thẩm mỹ.
- **EasyMDE Editor**: Soạn thảo câu hỏi và câu trả lời bằng Markdown chuyên nghiệp với bản xem trước thời gian thực.

### 🛡️ Hệ thống Bảo mật & Chống Spam
- **SpamChecker**: Tự động lọc nội dung rác, cờ bạc và các liên kết độc hại bằng AI pattern matching. [Xem chi tiết tại đây](./Docs/spamchecker.md).
- **OAuth 2.0**: Đăng nhập nhanh chóng qua Google, Facebook, và GitHub.
- **Two-Factor Authentication (2FA)**: Bảo mật tài khoản cấp độ cao qua Email/Authenticator app.

### 🎮 Gamification & Tương tác
- **Hệ thống Uy tín (Reputation)**: Tăng điểm khi được upvote, giảm khi bị downvote hoặc vi phạm.
- **Huy hiệu (Badges)**: Vinh danh người dùng dựa trên đóng góp (Gold, Silver, Bronze).
- **Daily Missions**: Hệ thống nhiệm vụ hàng ngày giúp người dùng duy trì thói quen đóng góp.
- **Real-time Notifications**: Thông báo tức thời qua SignalR khi có câu trả lời hoặc bình luận mới.

---

## 🛠️ Công nghệ sử dụng

- **Backend**: C# 13, ASP.NET Core 9.0 (MVC)
- **Database**: Entity Framework Core với SQLite
- **Frontend**: Tailwind CSS, Vanilla JavaScript, Razor Pages
- **Editor**: EasyMDE (Markdown)
- **Real-time**: SignalR
- **Validation**: Google reCAPTCHA v3 & Custom SpamChecker

---

## 🚀 Cài đặt nhanh

### 1. Clone Project
```bash
git clone https://github.com/duydttn/VzOverFlow.git
cd VzOverFlow
```

### 2. Cài đặt Dependencies
```bash
dotnet restore
```

### 3. Cấu hình Email (Optional)
Cập nhật file `appsettings.json` để sử dụng tính năng gửi email qua Gmail:
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "Port": 587,
  "SenderEmail": "your-email@gmail.com",
  "AppPassword": "your-app-password"
}
```

### 4. Khởi tạo Database & Chạy
```bash
dotnet ef database update
dotnet run
```
Truy cập: `https://localhost:5001`

---

## 📂 Cấu trúc dự án

- `Controllers/` — Điều hướng và xử lý logic yêu cầu.
- `Services/` — Chứa Business Logic (Reputation, Gamification, SpamChecker, Email...).
- `Models/` — Entity data và ViewModels.
- `Views/` — Giao diện Razor (HTML/CSS).
- `Docs/` — Tài liệu hướng dẫn chi tiết dành cho nhà phát triển.
- `wwwroot/` — Static assets (Images, CSS, JS).

---

## 🤝 Đóng góp

Mọi đóng góp nhằm cải thiện VzOverFlow đều được trân trọng. Vui lòng tạo **Issue** hoặc gửi **Pull Request**.

> **Tác giả:** [duydttn](https://github.com/duydttn)  
> **Phiên bản:** 2.0 (Premium Build)

