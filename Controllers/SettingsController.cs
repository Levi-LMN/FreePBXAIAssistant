// Controllers/SettingsController.cs
using FreePBXAIAssistant.Models;
using FreePBXAIAssistant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreePBXAIAssistant.Controllers
{
    [Authorize(Roles = "Administrator")]
    public class SettingsController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly SipService _sipService;
        private readonly AzureSpeechService _speechService;
        private readonly AzureOpenAIService _openAIService;

        public SettingsController(
            DatabaseService databaseService,
            SipService sipService,
            AzureSpeechService speechService,
            AzureOpenAIService openAIService)
        {
            _databaseService = databaseService;
            _sipService = sipService;
            _speechService = speechService;
            _openAIService = openAIService;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _databaseService.GetAISettingsAsync();

            var viewModel = new SettingsViewModel
            {
                AISettings = settings,
                FreePBXStatus = _sipService.IsConnected ? "Connected" : "Disconnected",
                AzureStatus = "Connected"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateAISettings(AISettings settings)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Index));
            }

            var success = await _databaseService.UpdateAISettingsAsync(settings);

            if (success)
            {
                TempData["Success"] = "AI settings updated successfully";
            }
            else
            {
                TempData["Error"] = "Error updating AI settings";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> TestConnections()
        {
            var results = new
            {
                FreePBX = _sipService.IsConnected,
                AzureSpeech = await _speechService.TestConnectionAsync(),
                AzureOpenAI = await _openAIService.TestConnectionAsync()
            };

            return Json(results);
        }
    }
}
