using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models.ViewModels
{
    public class AiConfigViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn nhà cung cấp.")]
        public string Provider { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập API Key.")]
        public string ApiKey { get; set; } = string.Empty;

        public string? ModelName { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Note { get; set; }
    }
}