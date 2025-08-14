using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Infrastructure.Attributes;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.Services.Interfaces.JournalService;
using ActioNator.ViewModels.Journal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ActioNator.Areas.User.Controllers
{
    [Area("User")]
    public class JournalController : BaseController
    {
        private readonly IJournalService _journalService;
        private readonly IInputSanitizationService _sanitizationService;

        public JournalController(IJournalService journalService, IInputSanitizationService sanitizationService, UserManager<ApplicationUser> userManager)
            :base(userManager)
        {
            _journalService = journalService ?? throw new ArgumentNullException(nameof(journalService));
            _sanitizationService = sanitizationService ?? throw new ArgumentNullException(nameof(sanitizationService));
        }

        /// <summary>
        /// Display all journal entries
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? searchTerm = null, int page = 1, int pageSize = 3, CancellationToken cancellationToken = default)
        {
            // Sanitize and normalize search term to prevent injection and malformed input
            var sanitizedSearch = SanitizeSearchTerm(searchTerm);

            IEnumerable<JournalEntryViewModel>? entries
                = string.IsNullOrWhiteSpace(sanitizedSearch)
                ? await _journalService.GetAllEntriesAsync()
                : await _journalService.SearchEntriesAsync(sanitizedSearch);

            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            int totalCount = entries?.Count() ?? 0;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var pagedEntries = (entries ?? Enumerable.Empty<JournalEntryViewModel>())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new JournalEntriesListViewModel
            {
                Entries = pagedEntries,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = sanitizedSearch
            };

            return View(vm);
        }

        /// <summary>
        /// Return entries as JSON for AJAX consumers
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> List(string? searchTerm = null)
        {
            var sanitizedSearch = SanitizeSearchTerm(searchTerm);
            IEnumerable<JournalEntryViewModel> entries =
                string.IsNullOrWhiteSpace(sanitizedSearch)
                    ? await _journalService.GetAllEntriesAsync()
                    : await _journalService.SearchEntriesAsync(sanitizedSearch);

            return Json(entries);
        }

        /// <summary>
        /// Return paginated journal entries partial for AJAX pagination
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetJournalPartial(string? searchTerm = null, int page = 1, int pageSize = 3, CancellationToken cancellationToken = default)
        {
            var sanitizedSearch = SanitizeSearchTerm(searchTerm);
            IEnumerable<JournalEntryViewModel>? entries
                = string.IsNullOrWhiteSpace(sanitizedSearch)
                ? await _journalService.GetAllEntriesAsync()
                : await _journalService.SearchEntriesAsync(sanitizedSearch);

            page = Math.Max(1, page);
            pageSize = Math.Max(1, pageSize);

            int totalCount = entries?.Count() ?? 0;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            var pagedEntries = (entries ?? Enumerable.Empty<JournalEntryViewModel>())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var vm = new JournalEntriesListViewModel
            {
                Entries = pagedEntries,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = sanitizedSearch
            };

            return PartialView("_JournalEntriesPartial", vm);
        }

        /// <summary>
        /// Return the partial view for creating a new journal entry
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            // Deprecated: modal is embedded on Index; keep endpoint for backward-compatibility
            return RedirectToAction(nameof(Index));
        }

        // Centralized sanitization for search queries.
        // - Trims whitespace
        // - Uses input sanitization service to strip/normalize dangerous content
        // - Removes control characters
        // - Enforces a conservative max length to mitigate abuse
        private string? SanitizeSearchTerm(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            // Trim and sanitize via shared service
            var cleaned = _sanitizationService.SanitizeString(input.Trim());
            if (string.IsNullOrEmpty(cleaned)) return null;
            // Remove control chars
            cleaned = new string(cleaned.Where(c => !char.IsControl(c)).ToArray());
            if (string.IsNullOrWhiteSpace(cleaned)) return null;
            // Limit length (defensive)
            const int MaxLen = 100;
            if (cleaned.Length > MaxLen) cleaned = cleaned.Substring(0, MaxLen);
            return cleaned;
        }

        /// <summary>
        /// Return the partial view for editing an existing journal entry
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            JournalEntryViewModel? entry = await _journalService.GetEntryByIdAsync(id);

            if (entry == null)
            {
                return NotFound();
            }

            // Deprecated: modal is embedded on Index; keep endpoint for backward-compatibility
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Handle saving (create or update) a journal entry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> Save([FromBody] JournalEntryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return 
                    BadRequest
                    (new { success = false, errors 
                    = ModelState.Values
                    .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage) });
            }

            // Sanitize user inputs
            model.Title = _sanitizationService
                .SanitizeString(model.Title);

            model.Content = _sanitizationService
                .SanitizeHtml(model.Content);

            model.MoodTag = _sanitizationService
                .SanitizeString(model.MoodTag);

            Guid? userId = GetUserId();

            try
            {
                if (model.Id == Guid.Empty)
                {
                    // Create new entry
                    await _journalService
                        .CreateEntryAsync(model, userId);

                    TempData["SuccessMessage"] = "Journal entry created successfully.";
                }
                else
                {
                    // Update existing entry
                    await _journalService
                        .UpdateEntryAsync(model);

                    TempData["SuccessMessage"] = "Journal entry updated successfully.";
                }

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return 
                        Json(new { success = true, message = TempData["SuccessMessage"] });
                }

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState
                    .AddModelError("", ex.Message);

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { error = ex.Message });
                }

                return View("Index", await _journalService
                    .GetAllEntriesAsync());
            }
        }

        /// <summary>
        /// Delete a journal entry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> Delete([FromBody] DeleteJournalRequest request)
        {      
            if (!await _journalService
                .DeleteEntryAsync(request.Id))
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return NotFound(new { error = "Journal entry not found." });
                }
                
                TempData["ErrorMessage"] = "Journal entry not found.";
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Journal entry deleted successfully.";
            
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = TempData["SuccessMessage"] });
            }
            
            return RedirectToAction(nameof(Index));
        }
    }
}
