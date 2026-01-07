// Controllers/KnowledgeBaseController.cs
using FreePBXAIAssistant.Models;
using FreePBXAIAssistant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreePBXAIAssistant.Controllers
{
    [Authorize]
    public class KnowledgeBaseController : Controller
    {
        private readonly DatabaseService _databaseService;

        public KnowledgeBaseController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<IActionResult> Index(string? category = null, string? search = null)
        {
            var items = await _databaseService.GetKnowledgeItemsAsync(category, search);
            var categories = await _databaseService.GetKnowledgeCategoriesAsync();

            var viewModel = new KnowledgeBaseViewModel
            {
                Items = items,
                Categories = categories,
                SelectedCategory = category ?? string.Empty,
                SearchQuery = search ?? string.Empty
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new KnowledgeItem());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(KnowledgeItem item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            await _databaseService.CreateKnowledgeItemAsync(item);
            TempData["Success"] = "Knowledge item created successfully";

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var item = await _databaseService.GetKnowledgeItemAsync(id);

            if (item == null)
            {
                return NotFound();
            }

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(KnowledgeItem item)
        {
            if (!ModelState.IsValid)
            {
                return View(item);
            }

            var success = await _databaseService.UpdateKnowledgeItemAsync(item);

            if (success)
            {
                TempData["Success"] = "Knowledge item updated successfully";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError(string.Empty, "Error updating knowledge item");
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _databaseService.DeleteKnowledgeItemAsync(id);

            if (success)
            {
                TempData["Success"] = "Knowledge item deleted successfully";
            }
            else
            {
                TempData["Error"] = "Error deleting knowledge item";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
