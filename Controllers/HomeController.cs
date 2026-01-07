// Controllers/HomeController.cs
using FreePBXAIAssistant.Models;
using FreePBXAIAssistant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreePBXAIAssistant.Controllers
{
    public class HomeController : Controller
    {
        private readonly DatabaseService _databaseService;
        private readonly CallProcessor _callProcessor;
        private readonly SipService _sipService;

        public HomeController(
            DatabaseService databaseService,
            CallProcessor callProcessor,
            SipService sipService)
        {
            _databaseService = databaseService;
            _callProcessor = callProcessor;
            _sipService = sipService;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.UtcNow.Date;
            var recentCalls = await _databaseService.GetCallRecordsAsync(1, 10);
            var intentDistribution = await _databaseService.GetIntentDistributionAsync(today);

            var viewModel = new DashboardViewModel
            {
                TotalCallsToday = await _databaseService.GetTotalCallCountAsync(),
                ActiveCalls = _callProcessor.GetActiveCallCount(),
                RecentCalls = recentCalls,
                IntentDistribution = intentDistribution,
                SipConnected = _sipService.IsConnected,
                SystemStatus = _sipService.IsConnected ? "Connected" : "Disconnected"
            };

            viewModel.TransferredCalls = recentCalls.Count(c => c.TransferredToAgent);
            viewModel.ResolvedCalls = recentCalls.Count(c => c.Outcome == "Completed");
            viewModel.AverageCallDuration = recentCalls.Any()
                ? (float)recentCalls.Average(c => c.DurationSeconds)
                : 0;

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}








