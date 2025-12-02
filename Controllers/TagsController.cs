using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VzOverFlow.Data;

namespace VzOverFlow.Controllers
{
    public class TagsController : Controller
    {
        private readonly AppDbContext _context;

        public TagsController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string? search)
        {
            var query = _context.Tags
                .Include(t => t.Questions)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.Name.Contains(search));
            }

            var tags = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            return View(tags);
        }

        public async Task<IActionResult> Details(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return RedirectToAction(nameof(Index));
            }

            var normalized = name.Trim().ToLowerInvariant();

            var tag = await _context.Tags
                .Include(t => t.Questions)
                    .ThenInclude(q => q.User)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Tags)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Answers)
                .Include(t => t.Questions)
                    .ThenInclude(q => q.Votes)
                .FirstOrDefaultAsync(t => t.Name.ToLower() == normalized);

            if (tag == null)
            {
                return NotFound();
            }

            return View(tag);
        }
    }
}
