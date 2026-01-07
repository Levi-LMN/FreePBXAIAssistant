using FreePBXAIAssistant.Data;
using FreePBXAIAssistant.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FreePBXAIAssistant.Services
{
    public class DatabaseService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseService> _logger;

        public DatabaseService(ApplicationDbContext context, ILogger<DatabaseService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region User Management

        public async Task EnsureAdminUserExists()
        {
            try
            {
                var adminExists = await _context.Users.AnyAsync(u => u.Username == "admin");

                if (!adminExists)
                {
                    var admin = new User
                    {
                        Username = "admin",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                        Email = "admin@example.com",
                        Role = "Administrator",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Users.Add(admin);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Default admin user created");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring admin user exists");
            }
        }

        public async Task<User?> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user != null && BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                {
                    user.LastLoginAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                    return user;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error authenticating user");
                return null;
            }
        }

        #endregion

        #region Call Records

        public async Task<int> CreateCallRecordAsync(string callId, string callerNumber)
        {
            try
            {
                var record = new CallRecord
                {
                    CallId = callId,
                    CallerNumber = callerNumber,
                    StartTime = DateTime.UtcNow,
                    Intent = "Unknown",
                    Outcome = "In Progress"
                };

                _context.CallRecords.Add(record);
                await _context.SaveChangesAsync();

                return record.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating call record for {callId}");
                return 0;
            }
        }

        public async Task UpdateCallRecordAsync(
            string callId,
            List<ConversationMessage> transcription,
            string intent,
            float sentimentScore)
        {
            try
            {
                var record = await _context.CallRecords.FirstOrDefaultAsync(c => c.CallId == callId);

                if (record != null)
                {
                    record.ConversationLog = JsonSerializer.Serialize(transcription);
                    record.Intent = intent;
                    record.SentimentScore = sentimentScore;

                    // Extract full transcription
                    record.Transcription = string.Join("\n",
                        transcription.Select(m => $"{m.Role.ToUpper()}: {m.Content}"));

                    // Extract AI responses
                    record.AIResponses = string.Join("\n",
                        transcription.Where(m => m.Role == "assistant").Select(m => m.Content));

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating call record {callId}");
            }
        }

        public async Task UpdateCallTransferAsync(
            string callId,
            bool transferred,
            string agentExtension,
            string transferReason)
        {
            try
            {
                var record = await _context.CallRecords.FirstOrDefaultAsync(c => c.CallId == callId);

                if (record != null)
                {
                    record.TransferredToAgent = transferred;
                    record.AgentExtension = agentExtension;
                    record.TransferReason = transferReason;

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating call transfer {callId}");
            }
        }

        public async Task FinalizeCallRecordAsync(
            string callId,
            DateTime endTime,
            int durationSeconds,
            string outcome)
        {
            try
            {
                var record = await _context.CallRecords.FirstOrDefaultAsync(c => c.CallId == callId);

                if (record != null)
                {
                    record.EndTime = endTime;
                    record.DurationSeconds = durationSeconds;
                    record.Outcome = outcome;

                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error finalizing call record {callId}");
            }
        }

        public async Task<List<CallRecord>> GetCallRecordsAsync(
            int page = 1,
            int pageSize = 20,
            string? searchQuery = null,
            string? intentFilter = null)
        {
            try
            {
                var query = _context.CallRecords.AsQueryable();

                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    query = query.Where(c =>
                        c.CallerNumber.Contains(searchQuery) ||
                        c.Transcription.Contains(searchQuery) ||
                        c.Intent.Contains(searchQuery));
                }

                if (!string.IsNullOrWhiteSpace(intentFilter))
                {
                    query = query.Where(c => c.Intent == intentFilter);
                }

                return await query
                    .OrderByDescending(c => c.StartTime)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting call records");
                return new List<CallRecord>();
            }
        }

        public async Task<CallRecord?> GetCallRecordAsync(int id)
        {
            try
            {
                return await _context.CallRecords.FindAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting call record {id}");
                return null;
            }
        }

        public async Task<int> GetTotalCallCountAsync(string? intentFilter = null)
        {
            try
            {
                var query = _context.CallRecords.AsQueryable();

                if (!string.IsNullOrWhiteSpace(intentFilter))
                {
                    query = query.Where(c => c.Intent == intentFilter);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting total call count");
                return 0;
            }
        }

        public async Task<Dictionary<string, int>> GetIntentDistributionAsync(DateTime? startDate = null)
        {
            try
            {
                var query = _context.CallRecords.AsQueryable();

                if (startDate.HasValue)
                {
                    query = query.Where(c => c.StartTime >= startDate.Value);
                }

                return await query
                    .GroupBy(c => c.Intent)
                    .Select(g => new { Intent = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Intent, x => x.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting intent distribution");
                return new Dictionary<string, int>();
            }
        }

        #endregion

        #region Knowledge Base

        public async Task<string> GetRelevantKnowledgeAsync(string query)
        {
            try
            {
                // Simple keyword-based knowledge retrieval
                var keywords = query.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var relevantItems = await _context.KnowledgeItems
                    .Where(k => k.IsActive)
                    .OrderByDescending(k => k.Priority)
                    .ToListAsync();

                var matchedItems = relevantItems
                    .Where(k => keywords.Any(keyword =>
                        k.Title.ToLower().Contains(keyword) ||
                        k.Content.ToLower().Contains(keyword) ||
                        k.Tags.ToLower().Contains(keyword)))
                    .Take(5)
                    .ToList();

                if (matchedItems.Any())
                {
                    return string.Join("\n\n", matchedItems.Select(k =>
                        $"[{k.Category}] {k.Title}:\n{k.Content}"));
                }

                // Return top priority items if no matches
                var topItems = relevantItems.Take(3).ToList();
                return string.Join("\n\n", topItems.Select(k =>
                    $"[{k.Category}] {k.Title}:\n{k.Content}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting relevant knowledge");
                return string.Empty;
            }
        }

        public async Task<List<KnowledgeItem>> GetKnowledgeItemsAsync(string? category = null, string? search = null)
        {
            try
            {
                var query = _context.KnowledgeItems.Where(k => k.IsActive);

                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(k => k.Category == category);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(k =>
                        k.Title.Contains(search) ||
                        k.Content.Contains(search) ||
                        k.Tags.Contains(search));
                }

                return await query
                    .OrderByDescending(k => k.Priority)
                    .ThenBy(k => k.Category)
                    .ThenBy(k => k.Title)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge items");
                return new List<KnowledgeItem>();
            }
        }

        public async Task<KnowledgeItem?> GetKnowledgeItemAsync(int id)
        {
            return await _context.KnowledgeItems.FindAsync(id);
        }

        public async Task<int> CreateKnowledgeItemAsync(KnowledgeItem item)
        {
            try
            {
                item.CreatedAt = DateTime.UtcNow;
                item.UpdatedAt = DateTime.UtcNow;

                _context.KnowledgeItems.Add(item);
                await _context.SaveChangesAsync();

                return item.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating knowledge item");
                return 0;
            }
        }

        public async Task<bool> UpdateKnowledgeItemAsync(KnowledgeItem item)
        {
            try
            {
                item.UpdatedAt = DateTime.UtcNow;

                _context.KnowledgeItems.Update(item);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating knowledge item {item.Id}");
                return false;
            }
        }

        public async Task<bool> DeleteKnowledgeItemAsync(int id)
        {
            try
            {
                var item = await _context.KnowledgeItems.FindAsync(id);

                if (item != null)
                {
                    item.IsActive = false;
                    await _context.SaveChangesAsync();
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting knowledge item {id}");
                return false;
            }
        }

        public async Task<List<string>> GetKnowledgeCategoriesAsync()
        {
            try
            {
                return await _context.KnowledgeItems
                    .Where(k => k.IsActive)
                    .Select(k => k.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting knowledge categories");
                return new List<string>();
            }
        }

        #endregion

        #region AI Settings

        public async Task EnsureDefaultSettingsExist()
        {
            try
            {
                var settingsExist = await _context.AISettings.AnyAsync();

                if (!settingsExist)
                {
                    var defaultSettings = new AISettings
                    {
                        SystemPrompt = @"You are a professional AI insurance assistant for an insurance company. Your role is to:
- Answer questions about insurance policies, coverage, and claims
- Guide customers through common insurance processes
- Identify customer needs and intent accurately
- Provide clear, concise, and empathetic responses
- Transfer calls to human agents when appropriate",
                        Temperature = 0.7f,
                        MaxTokens = 800,
                        ConfidenceThreshold = 0.7f,
                        EnableTransferToAgent = true,
                        TransferKeywords = JsonSerializer.Serialize(new[] { "agent", "representative", "person", "human", "supervisor" }),
                        EscalationRules = JsonSerializer.Serialize(new[]
                        {
                            "Customer expresses frustration or anger",
                            "Complex account-specific issues",
                            "Complaints or disputes",
                            "Policy cancellations"
                        }),
                        MaxConversationTurns = 20,
                        EnableSentimentAnalysis = true,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.AISettings.Add(defaultSettings);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Default AI settings created");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring default settings exist");
            }
        }

        public async Task<AISettings> GetAISettingsAsync()
        {
            try
            {
                var settings = await _context.AISettings.FirstOrDefaultAsync();

                if (settings == null)
                {
                    await EnsureDefaultSettingsExist();
                    settings = await _context.AISettings.FirstAsync();
                }

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting AI settings");
                throw;
            }
        }

        public async Task<bool> UpdateAISettingsAsync(AISettings settings)
        {
            try
            {
                settings.UpdatedAt = DateTime.UtcNow;

                _context.AISettings.Update(settings);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating AI settings");
                return false;
            }
        }

        #endregion
    }
}