using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;
using VzOverFlow.Models.ViewModels;

namespace VzOverFlow.ViewComponents
{
    public class RightSidebarViewComponent : ViewComponent
    {
        private readonly AppDbContext _context;

        public RightSidebarViewComponent(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var popularTags = await _context.Tags
                .AsNoTracking()
                .OrderByDescending(t => t.Questions.Count)
                .Select(t => new PopularTagViewModel
                {
                    Name = t.Name,
                    QuestionCount = t.Questions.Count
                })
                .Take(5)
                .ToListAsync();

            var model = new RightSidebarViewModel
            {
                PopularTags = popularTags,
                TotalQuestions = await _context.Questions.CountAsync(),
                TotalAnswers = await _context.Answers.CountAsync()
            };

            return View(model);
        }
    }
}

