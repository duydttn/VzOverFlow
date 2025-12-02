namespace VzOverFlow.Models
{
    public class EmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string SenderEmail { get; set; } = default!;
        public string SenderName { get; set; } = "VzOverFlow";
        public string AppPassword { get; set; } = default!;
    }
}

