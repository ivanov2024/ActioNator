using ActioNator.Data;
using ActioNator.Data.Models;
using ActioNator.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ActioNator.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : Controller
    {
        private readonly ActioNatorDbContext _dbContext;

        public ReportsController(ActioNatorDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new ReportsViewModel
            {
                PostReports = await _dbContext.PostReports
                    .Include(pr => pr.Post)
                    .Include(pr => pr.ReportedByUser)
                    .OrderByDescending(pr => pr.CreatedAt)
                    .Select(pr => new PostReportViewModel
                    {
                        Id = pr.Id,
                        PostId = pr.PostId,
                        ReportedByUserId = pr.ReportedByUserId,
                        ReportedByUserName = pr.ReportedByUser.UserName,
                        Reason = pr.Reason,
                        CreatedAt = pr.CreatedAt,
                        Status = pr.Status
                    })
                    .ToListAsync(),

                CommentReports = await _dbContext.CommentReports
                    .Include(cr => cr.Comment)
                        .ThenInclude(c => c.Post)
                    .Include(cr => cr.ReportedByUser)
                    .OrderByDescending(cr => cr.CreatedAt)
                    .Select(cr => new CommentReportViewModel
                    {
                        Id = cr.Id,
                        CommentId = cr.CommentId,
                        PostId = cr.Comment.PostId!.Value,
                        CommentContent = cr.Comment.Content,
                        ReportedByUserId = cr.ReportedByUserId,
                        ReportedByUserName = cr.ReportedByUser.UserName!,
                        Reason = cr.Reason,
                        CreatedAt = cr.CreatedAt,
                        Status = cr.Status
                    })
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> PostReportDetails(Guid id)
        {
            var report = await _dbContext.PostReports
                .Include(pr => pr.Post)
                .Include(pr => pr.ReportedByUser)
                .Include(pr => pr.ReviewedByUser)
                .FirstOrDefaultAsync(pr => pr.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            var viewModel = new PostReportDetailsViewModel
            {
                Id = report.Id,
                PostId = report.PostId,
                PostContent = report.Post.Content,
                ReportedByUserId = report.ReportedByUserId,
                ReportedByUserName = report.ReportedByUser.UserName,
                Reason = report.Reason,
                Details = report.Details,
                CreatedAt = report.CreatedAt,
                Status = report.Status,
                ReviewedByUserId = report.ReviewedByUserId,
                ReviewedByUserName = report.ReviewedByUser?.UserName,
                ReviewedAt = report.ReviewedAt,
                ReviewNotes = report.ReviewNotes
            };

            return View(viewModel);
        }

        public async Task<IActionResult> CommentReportDetails(Guid id)
        {
            var report = await _dbContext.CommentReports
                .Include(cr => cr.Comment)
                    .ThenInclude(c => c.Post)
                .Include(cr => cr.ReportedByUser)
                .Include(cr => cr.ReviewedByUser)
                .FirstOrDefaultAsync(cr => cr.Id == id);

            if (report == null)
            {
                return NotFound();
            }

            var viewModel = new CommentReportDetailsViewModel
            {
                Id = report.Id,
                CommentId = report.CommentId,
                PostId = report.Comment.PostId!.Value,
                CommentContent = report.Comment.Content,
                ReportedByUserId = report.ReportedByUserId,
                ReportedByUserName = report.ReportedByUser.UserName!,
                Reason = report.Reason,
                Details = report.Details,
                CreatedAt = report.CreatedAt,
                Status = report.Status,
                ReviewedByUserId = report.ReviewedByUserId,
                ReviewedByUserName = report.ReviewedByUser?.UserName!,
                ReviewedAt = report.ReviewedAt,
                ReviewNotes = report.ReviewNotes
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePostReportStatus(Guid id, string status, string notes)
        {
            var report = await _dbContext.PostReports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            report.Status = status;
            report.ReviewNotes = notes;
            report.ReviewedByUserId = Guid.Parse(User.FindFirst("sub")?.Value);
            report.ReviewedAt = DateTime.UtcNow;

            _dbContext.Update(report);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(PostReportDetails), new { id });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCommentReportStatus(Guid id, string status, string notes)
        {
            var report = await _dbContext.CommentReports.FindAsync(id);
            if (report == null)
            {
                return NotFound();
            }

            report.Status = status;
            report.ReviewNotes = notes;
            report.ReviewedByUserId = Guid.Parse(User.FindFirst("sub")?.Value);
            report.ReviewedAt = DateTime.UtcNow;

            _dbContext.Update(report);
            await _dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(CommentReportDetails), new { id });
        }
    }
}
