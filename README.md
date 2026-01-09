# VzOverFlow

VzOverFlow là một nền tảng hỏi đáp theo mô hình Stack Overflow, xây dựng bằng ngôn ngữ C#, ASP.NET Core, và sử dụng Entity Framework Core với SQLite để lưu trữ dữ liệu. Mục tiêu của dự án là tạo ra một cộng đồng học hỏi, nơi người dùng có thể đặt câu hỏi, trả lời, bình luận, bầu chọn và tương tác để giải quyết các vấn đề về lập trình.

## Tính năng chính

- Đăng ký, đăng nhập người dùng
- Đặt câu hỏi, trả lời, bình luận
- Bầu chọn cho câu trả lời và câu hỏi
- Chấp nhận câu trả lời đúng
- Quản lý tag, bộ lọc nội dung
- Gửi email xác nhận, phục hồi mật khẩu (sử dụng Gmail)
- Gamification (tăng reputation, badge) cho người dùng hoạt động tích cực
- Giao diện hiện đại, tối ưu trải nghiệm với Tailwind CSS

## Cài đặt nhanh

```bash
git clone https://github.com/duydttn/VzOverFlow.git
cd VzOverFlow
dotnet restore
dotnet ef database update
dotnet run
```
Ứng dụng mặc định chạy tại `https://localhost:5142`.

## Yêu cầu hệ thống

- [.NET 9 SDK](https://dotnet.microsoft.com/)
- SQLite (phiên bản tối thiểu 3.x)
- Visual Studio 2022/2023 hoặc VS Code

## Cấu trúc dự án

- `Models/` — Các lớp dữ liệu (User, Question, Answer, Tag ...)
- `Services/` — Các service logic (GmailEmailSender, UserService, QuestionService,...)
- `Views/` — Razor views cho giao diện web
- `wwwroot/css/` — Tài nguyên CSS, sử dụng Tailwind và custom styles
- `backup_css/` — Backup các file CSS cũ (xem chi tiết trong `backup_css/README.md`)

## Một số đoạn mã tiêu biểu

### Định nghĩa model Question

```csharp
public class Question
{
    public int Id { get; set; }
    [Required(ErrorMessage = "Tiêu đề là bắt buộc")]
    [StringLength(200, MinimumLength = 15, ErrorMessage = "Tiêu đề phải dài 15-200 ký tự")]
    public string Title { get; set; }
    public string Body { get; set; }
    public int ViewCount { get; set; }
    public bool IsClosed { get; set; }
    public DateTime CreatedAt { get; set; }
    // ... các thuộc tính liên quan
}
```

### Gửi email xác nhận qua Gmail

```csharp
public async Task SendAsync(string toEmail, string subject, string htmlBody)
{
    using var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
    {
        Credentials = new NetworkCredential(_settings.SenderEmail, _settings.AppPassword),
        EnableSsl = true
    };
    var mail = new MailMessage
    {
        From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
        Subject = subject,
        Body = htmlBody,
        IsBodyHtml = true
    };
    mail.To.Add(toEmail);
    await client.SendMailAsync(mail);
}
```

## Giao diện

- Responsive, tối ưu dark/light mode
- Phản hồi giao diện tốt nhờ Tailwind CSS
- UI thân thiện với người mới sử dụng

## Đóng góp

Chào đón mọi hình thức đóng góp! Hãy fork repo và tạo pull request hoặc mở Issue để góp ý, báo bug.

## Giấy phép

Hiện tại dự án chưa công khai license. Bạn vui lòng liên hệ với tác giả nếu cần sử dụng cho mục đích thương mại.

---

> **Tác giả:** [duydttn](https://github.com/duydttn)
