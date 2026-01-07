// Controllers/CallsController.cs
using FreePBXAIAssistant.Models;
using FreePBXAIAssistant.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FreePBXAIAssistant.Controllers
{
    [Authorize]
    public class CallsController : Controller
    {
        private readonly DatabaseService _databaseService;

        public CallsController(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<IActionResult> Index(int page = 1, string? search = null, string? intent = null)
        {
            var pageSize = 20;
            var calls = await _databaseService.GetCallRecordsAsync(page, pageSize, search, intent);
            var totalCalls = await _databaseService.GetTotalCallCountAsync(intent);

            var viewModel = new CallsViewModel
            {
                Calls = calls,
                TotalCalls = totalCalls,
                CurrentPage = page,
                PageSize = pageSize,
                SearchQuery = search ?? string.Empty,
                IntentFilter = intent ?? string.Empty
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Details(int id)
        {
            var call = await _databaseService.GetCallRecordAsync(id);

            if (call == null)
            {
                return NotFound();
            }

            return View(call);
        }

        [HttpGet]
        public async Task<IActionResult> Export(string? intent = null)
        {
            var calls = await _databaseService.GetCallRecordsAsync(1, 10000, null, intent);

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Call ID,Caller Number,Start Time,Duration (s),Intent,Outcome,Transferred,Sentiment");

            foreach (var call in calls)
            {
                csv.AppendLine($"{call.CallId},{call.CallerNumber},{call.StartTime:yyyy-MM-dd HH:mm:ss},{call.DurationSeconds},{call.Intent},{call.Outcome},{call.TransferredToAgent},{call.SentimentScore:F2}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"calls_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
        }
    }
}
