using ActioNator.Controllers;
using ActioNator.Data.Models;
using ActioNator.Infrastructure.Attributes;
using ActioNator.Infrastructure.Extensions;
using ActioNator.Services.Interfaces.InputSanitizationService;
using ActioNator.Services.Interfaces.JournalService;
using ActioNator.ViewModels.Journal;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

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
        public async Task<IActionResult> Index(string searchTerm = null)
        {
            IEnumerable<JournalEntryViewModel>? entries 
                = string.IsNullOrWhiteSpace(searchTerm)
                ? await _journalService
                    .GetAllEntriesAsync()
                : await _journalService
                    .SearchEntriesAsync(searchTerm);

            return View(entries);
        }

        /// <summary>
        /// Return the partial view for creating a new journal entry
        /// </summary>
        [HttpGet]
        public IActionResult Create()
        {
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // If AJAX request, return partial view
                return PartialView("_JournalEntryModal", new JournalEntryViewModel
                {
                    CreatedAt = DateTime.Now
                });
            }

            // Otherwise redirect to index
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Return the partial view for editing an existing journal entry
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            JournalEntryViewModel? entry 
                = await _journalService
                .GetEntryByIdAsync(id);

            if (entry == null)
            {
                return NotFound();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                // If AJAX request, return JSON data for the entry
                return Json(entry);
            }

            // Otherwise redirect to index
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Handle saving (create only) a journal entry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(JournalEntryViewModel model)
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

            try
            {
                if (model.Id == Guid.Empty)
                {
                    Guid? userId = GetUserId();

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
                    // Get the newly created/updated entry to return with the response
                    var entry = await _journalService.GetEntryByIdAsync(model.Id);
                    
                    // Return both JSON success data and the entry HTML
                    return Json(new { 
                        success = true, 
                        message = TempData["SuccessMessage"],
                        entryHtml = await this.RenderViewToStringAsync("_JournalEntryCard", entry),
                        entry = entry
                    });
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
        /// Update an existing journal entry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> Update([FromBody] JournalEntryViewModel model)
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

            if (model.Id == Guid.Empty)
            {
                return BadRequest(new { error = "Invalid entry ID" });
            }

            // Sanitize user inputs
            model.Title = _sanitizationService
                .SanitizeString(model.Title);

            model.Content = _sanitizationService
                .SanitizeHtml(model.Content);

            model.MoodTag = _sanitizationService
                .SanitizeString(model.MoodTag);

            try
            {
                // Update existing entry
                await _journalService
                    .UpdateEntryAsync(model);

                TempData["SuccessMessage"] = "Journal entry updated successfully.";

                // Get the updated entry to return with the response
                var entry = await _journalService.GetEntryByIdAsync(model.Id);
                
                // Return both JSON success data and the entry HTML
                return Json(new { 
                    success = true, 
                    message = TempData["SuccessMessage"],
                    entryHtml = await this.RenderViewToStringAsync("_JournalEntryCard", entry),
                    entry = entry
                });
            }
            catch (Exception ex)
            {
                ModelState
                    .AddModelError("", ex.Message);

                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a journal entry
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryTokenFromJson]
        public async Task<IActionResult> Delete([FromBody] DeleteEntryRequest request)
        {      
            // Log the received ID for debugging
            Console.WriteLine($"Received delete request for ID: {request?.Id}");
            
            // Validate the ID
            if (request == null || request.Id == Guid.Empty)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return BadRequest(new { error = "Invalid journal entry ID." });
                }
                
                TempData["ErrorMessage"] = "Invalid journal entry ID.";
                return RedirectToAction(nameof(Index));
            }
            
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
