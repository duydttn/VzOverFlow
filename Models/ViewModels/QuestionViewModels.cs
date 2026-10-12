using System.ComponentModel.DataAnnotations;

namespace VzOverFlow.Models.ViewModels
{
    public class QuestionCreateViewModel
    {
        [Display(Name = "Tiêu đề")]
        [Required(ErrorMessage = "{0} là bắt buộc")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "{0} phải từ {2} đến {1} ký tự")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Nội dung")]
        [Required(ErrorMessage = "{0} là bắt buộc")]
        [MinLength(20, ErrorMessage = "{0} phải có ít nhất {1} ký tự")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Thẻ (Tags)")]
        public string? Tags { get; set; }
    }

    public class QuestionEditViewModel
    {
        public int Id { get; set; }

        [Display(Name = "Tiêu đề")]
        [Required(ErrorMessage = "{0} là bắt buộc")]
        [StringLength(200, MinimumLength = 10, ErrorMessage = "{0} phải từ {2} đến {1} ký tự")]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "Nội dung")]
        [Required(ErrorMessage = "{0} là bắt buộc")]
        [MinLength(20, ErrorMessage = "{0} phải có ít nhất {1} ký tự")]
        public string Body { get; set; } = string.Empty;

        [Display(Name = "Thẻ (Tags)")]
        public string? Tags { get; set; }
    }
}