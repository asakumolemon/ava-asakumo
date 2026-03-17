using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Microsoft.Extensions.Logging;
using SQLite;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of the data service with SQLite and JSON persistence.
/// </summary>
public class DataService : IDataService
{
    private readonly ILogger<DataService> _logger;
    private SQLiteAsyncConnection? _database;
    private readonly string _dbPath;
    private readonly string _settingsPath;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings? _cachedSettings;
    private bool _isInitialized;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DataService(ILogger<DataService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Determine platform-specific data directory
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(appDataPath, "Asakumo");

        // Ensure directory exists
        if (!Directory.Exists(appDir))
        {
            Directory.CreateDirectory(appDir);
        }

        _dbPath = Path.Combine(appDir, "asakumo.db");
        _settingsPath = Path.Combine(appDir, "settings.json");

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    #region Initialization

    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        try
        {
            _database = new SQLiteAsyncConnection(_dbPath);

            // Create tables
            await _database.CreateTableAsync<Conversation>();
            await _database.CreateTableAsync<ChatMessage>();

            _isInitialized = true;
            _logger.LogInformation("Database initialized at {Path}", _dbPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize database");
            throw;
        }
    }

    #endregion

    #region Conversations

    /// <inheritdoc/>
    public async Task<List<Conversation>> GetConversationsAsync()
    {
        await EnsureInitializedAsync();

        var conversations = await _database!.Table<Conversation>()
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();

        return conversations;
    }

    /// <inheritdoc/>
    public async Task<Conversation?> GetConversationAsync(string id)
    {
        await EnsureInitializedAsync();

        var conversation = await _database!.Table<Conversation>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();

        return conversation;
    }

    /// <inheritdoc/>
    public async Task SaveConversationAsync(Conversation conversation)
    {
        await EnsureInitializedAsync();

        conversation.UpdatedAt = DateTime.Now;
        await _database!.InsertOrReplaceAsync(conversation);

        _logger.LogDebug("Saved conversation {Id}", conversation.Id);
    }

    /// <inheritdoc/>
    public async Task DeleteConversationAsync(string id)
    {
        await EnsureInitializedAsync();

        // Delete messages first, then conversation
        // Note: sqlite-net-pcl doesn't support async operations inside RunInTransactionAsync
        await _database!.ExecuteAsync("DELETE FROM chat_messages WHERE ConversationId = ?", id);
        await _database.ExecuteAsync("DELETE FROM conversations WHERE Id = ?", id);

        _logger.LogDebug("Deleted conversation {Id} and its messages", id);
    }

    #endregion

    #region Messages

    /// <inheritdoc/>
    public async Task<List<ChatMessage>> GetMessagesAsync(string conversationId)
    {
        await EnsureInitializedAsync();

        var messages = await _database!.Table<ChatMessage>()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.Timestamp)
            .ToListAsync();

        return messages;
    }

    /// <inheritdoc/>
    public async Task SaveMessageAsync(ChatMessage message)
    {
        await EnsureInitializedAsync();

        await _database!.InsertOrReplaceAsync(message);

        _logger.LogDebug("Saved message {Id} for conversation {ConversationId}",
            message.Id, message.ConversationId);
    }

    /// <inheritdoc/>
    public async Task DeleteMessageAsync(string messageId)
    {
        await EnsureInitializedAsync();

        await _database!.ExecuteAsync(
            "DELETE FROM chat_messages WHERE Id = ?",
            messageId);

        _logger.LogDebug("Deleted message {Id}", messageId);
    }

    /// <inheritdoc/>
    public async Task DeleteMessagesAsync(string conversationId)
    {
        await EnsureInitializedAsync();

        await _database!.ExecuteAsync(
            "DELETE FROM chat_messages WHERE ConversationId = ?",
            conversationId);

        _logger.LogDebug("Deleted all messages for conversation {ConversationId}", conversationId);
    }

    #endregion

    #region Settings

    /// <inheritdoc/>
    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        try
        {
            if (File.Exists(_settingsPath))
            {
                var json = await File.ReadAllTextAsync(_settingsPath);
                _cachedSettings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions)
                    ?? new AppSettings();
            }
            else
            {
                _cachedSettings = new AppSettings();
            }

            return _cachedSettings;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Settings file corrupted, using defaults");
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            _cachedSettings = new AppSettings();
            return _cachedSettings;
        }
    }

    /// <inheritdoc/>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            var tempPath = _settingsPath + ".tmp";

            // Write to temp file first (atomic write pattern)
            await File.WriteAllTextAsync(tempPath, json);

            // Replace the original file
            File.Move(tempPath, _settingsPath, overwrite: true);

            _cachedSettings = settings;
            _logger.LogDebug("Saved settings to {Path}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings");
            throw;
        }
    }

    #endregion

    #region Provider Config

    /// <inheritdoc/>
    public async Task<ProviderConfig?> GetProviderConfigAsync(string providerId)
    {
        var settings = await GetSettingsAsync();
        return settings.ProviderConfigs.TryGetValue(providerId, out var config) ? config : null;
    }

    /// <inheritdoc/>
    public async Task SaveProviderConfigAsync(string providerId, ProviderConfig config)
    {
        var settings = await GetSettingsAsync();
        settings.ProviderConfigs[providerId] = config;
        await SaveSettingsAsync(settings);
    }

    #endregion

    #region Providers (Static Data)

    /// <inheritdoc/>
    public List<AIProvider> GetProviders()
    {
        return new List<AIProvider>
        {
            new AIProvider
            {
                Id = "quickstart",
                Name = "无需配置，立即体验",
                Category = ProviderCategory.QuickStart,
                Description = "使用内置免费模型",
                RequiresApiKey = false,
                Icon = "🌟"
            },
            new AIProvider
            {
                Id = "openai",
                Name = "OpenAI (GPT)",
                Category = ProviderCategory.Popular,
                Description = "GPT-4, GPT-3.5 等模型",
                DefaultBaseUrl = "https://api.openai.com/v1",
                RequiresApiKey = true,
                Icon = "●",
                Models = new List<AIModel>
                {
                    new AIModel
                    {
                        Id = "gpt-4o",
                        Name = "GPT-4o",
                        Description = "最强大的多模态模型",
                        Category = ModelCategory.Recommended,
                        IsRecommended = true,
                        Tags = new List<string> { "Chat", "Vision" },
                        ProviderId = "openai"
                    },
                    new AIModel
                    {
                        Id = "gpt-4o-mini",
                        Name = "GPT-4o-mini",
                        Description = "快速且经济的选择",
                        Category = ModelCategory.Recommended,
                        IsRecommended = true,
                        Tags = new List<string> { "Fast", "Economic" },
                        ProviderId = "openai"
                    },
                    new AIModel
                    {
                        Id = "o3-mini",
                        Name = "o3-mini",
                        Description = "适合复杂任务和数学问题",
                        Category = ModelCategory.Reasoning,
                        Tags = new List<string> { "Reasoning" },
                        ProviderId = "openai"
                    },
                    new AIModel
                    {
                        Id = "gpt-3.5-turbo",
                        Name = "GPT-3.5-turbo",
                        Description = "性价比高，适合日常对话",
                        Category = ModelCategory.Chat,
                        Tags = new List<string> { "Chat", "Fast" },
                        ProviderId = "openai"
                    }
                }
            },
            new AIProvider
            {
                Id = "anthropic",
                Name = "Anthropic (Claude)",
                Category = ProviderCategory.Popular,
                Description = "Claude 系列模型",
                DefaultBaseUrl = "https://api.anthropic.com",
                RequiresApiKey = true,
                Icon = "○",
                Models = new List<AIModel>
                {
                    new AIModel
                    {
                        Id = "claude-3-5-sonnet",
                        Name = "Claude 3.5 Sonnet",
                        Description = "最新一代模型，性能出色",
                        Category = ModelCategory.Recommended,
                        IsRecommended = true,
                        Tags = new List<string> { "Chat", "Fast" },
                        ProviderId = "anthropic"
                    },
                    new AIModel
                    {
                        Id = "claude-3-opus",
                        Name = "Claude 3 Opus",
                        Description = "最强推理能力",
                        Category = ModelCategory.Reasoning,
                        Tags = new List<string> { "Reasoning" },
                        ProviderId = "anthropic"
                    }
                }
            },
            new AIProvider
            {
                Id = "google",
                Name = "Google (Gemini)",
                Category = ProviderCategory.Popular,
                Description = "Gemini 系列模型",
                DefaultBaseUrl = "https://generativelanguage.googleapis.com",
                RequiresApiKey = true,
                Icon = "○",
                Models = new List<AIModel>
                {
                    new AIModel
                    {
                        Id = "gemini-2.0-flash",
                        Name = "Gemini 2.0 Flash",
                        Description = "最新一代，快速高效",
                        Category = ModelCategory.Recommended,
                        IsRecommended = true,
                        Tags = new List<string> { "Chat", "Fast" },
                        ProviderId = "google"
                    },
                    new AIModel
                    {
                        Id = "gemini-1.5-pro",
                        Name = "Gemini 1.5 Pro",
                        Description = "强大的推理能力",
                        Category = ModelCategory.Reasoning,
                        Tags = new List<string> { "Reasoning" },
                        ProviderId = "google"
                    },
                    new AIModel
                    {
                        Id = "gemini-1.5-flash",
                        Name = "Gemini 1.5 Flash",
                        Description = "经济高效的选择",
                        Category = ModelCategory.Chat,
                        Tags = new List<string> { "Fast", "Economic" },
                        ProviderId = "google"
                    }
                }
            },
            new AIProvider
            {
                Id = "deepseek",
                Name = "DeepSeek",
                Category = ProviderCategory.Popular,
                Description = "国产大模型，性价比高",
                DefaultBaseUrl = "https://api.deepseek.com",
                RequiresApiKey = true,
                Icon = "○",
                Models = new List<AIModel>
                {
                    new AIModel
                    {
                        Id = "deepseek-chat",
                        Name = "DeepSeek Chat",
                        Description = "通用对话模型",
                        Category = ModelCategory.Recommended,
                        IsRecommended = true,
                        Tags = new List<string> { "Chat", "Economic" },
                        ProviderId = "deepseek"
                    },
                    new AIModel
                    {
                        Id = "deepseek-reasoner",
                        Name = "DeepSeek Reasoner",
                        Description = "深度推理模型",
                        Category = ModelCategory.Reasoning,
                        Tags = new List<string> { "Reasoning" },
                        ProviderId = "deepseek"
                    }
                }
            },
            new AIProvider
            {
                Id = "ollama",
                Name = "Ollama (本地部署)",
                Category = ProviderCategory.Local,
                Description = "本地运行开源模型",
                DefaultBaseUrl = "http://localhost:11434",
                RequiresApiKey = false,
                Icon = "🖥️",
                Models = new List<AIModel>
                {
                    new AIModel
                    {
                        Id = "llama3.2",
                        Name = "Llama 3.2",
                        Description = "Meta 最新开源模型",
                        Category = ModelCategory.Recommended,
                        IsRecommended = true,
                        Tags = new List<string> { "Local", "Open Source" },
                        ProviderId = "ollama"
                    },
                    new AIModel
                    {
                        Id = "qwen2.5",
                        Name = "Qwen 2.5",
                        Description = "阿里通义千问",
                        Category = ModelCategory.Chat,
                        Tags = new List<string> { "Local", "Chinese" },
                        ProviderId = "ollama"
                    },
                    new AIModel
                    {
                        Id = "deepseek-r1",
                        Name = "DeepSeek R1",
                        Description = "深度推理本地版",
                        Category = ModelCategory.Reasoning,
                        Tags = new List<string> { "Reasoning", "Local" },
                        ProviderId = "ollama"
                    },
                    new AIModel
                    {
                        Id = "codellama",
                        Name = "Code Llama",
                        Description = "代码生成专用",
                        Category = ModelCategory.Chat,
                        Tags = new List<string> { "Code", "Local" },
                        ProviderId = "ollama"
                    }
                }
            }
        };
    }

    #endregion

    #region Backup & Maintenance

    /// <summary>
    /// Clears all conversations and messages from the database.
    /// </summary>
    public async Task ClearAllConversationsAsync()
    {
        await EnsureInitializedAsync();

        await _database!.ExecuteAsync("DELETE FROM chat_messages");
        await _database.ExecuteAsync("DELETE FROM conversations");

        _logger.LogInformation("Cleared all conversations and messages");
    }

    /// <summary>
    /// Exports all data to a backup file.
    /// </summary>
    /// <param name="backupPath">The path to save the backup.</param>
    public async Task BackupDataAsync(string backupPath)
    {
        await EnsureInitializedAsync();

        try
        {
            var backup = new BackupData
            {
                ExportedAt = DateTime.Now,
                Settings = await GetSettingsAsync(),
                Conversations = await GetConversationsAsync()
            };

            // Get all messages for each conversation
            var allMessages = new List<ChatMessage>();
            foreach (var conv in backup.Conversations)
            {
                var messages = await GetMessagesAsync(conv.Id);
                allMessages.AddRange(messages);
            }
            backup.Messages = allMessages;

            var json = JsonSerializer.Serialize(backup, _jsonOptions);
            await File.WriteAllTextAsync(backupPath, json);

            _logger.LogInformation("Backup created at {Path}", backupPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create backup");
            throw;
        }
    }

    /// <summary>
    /// Gets the database file path.
    /// </summary>
    public string GetDatabasePath() => _dbPath;

    private const string TempFileExtension = ".tmp";

    /// <summary>
    /// Clears all application data including settings and database.
    /// This will delete the database file and settings file.
    /// </summary>
    /// <returns>Detailed result of the clear operation.</returns>
    public async Task<ClearDataResult> ClearAllDataAsync()
    {
        var result = new ClearDataResult();

        try
        {
            if (_database != null)
            {
                await _database.CloseAsync();
                _database = null;
                _isInitialized = false;
            }

            _cachedSettings = null;

            if (File.Exists(_dbPath))
            {
                try
                {
                    File.Delete(_dbPath);
                    result.DatabaseDeleted = true;
                    _logger.LogInformation("Deleted database file: {Path}", _dbPath);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to delete database: {ex.Message}");
                    _logger.LogError(ex, "Failed to delete database file: {Path}", _dbPath);
                }
            }
            else
            {
                result.DatabaseDeleted = true;
            }

            if (File.Exists(_settingsPath))
            {
                try
                {
                    File.Delete(_settingsPath);
                    result.SettingsDeleted = true;
                    _logger.LogInformation("Deleted settings file: {Path}", _settingsPath);
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to delete settings: {ex.Message}");
                    _logger.LogError(ex, "Failed to delete settings file: {Path}", _settingsPath);
                }
            }
            else
            {
                result.SettingsDeleted = true;
            }

            DeleteTempFiles();

            if (result.AllCleared)
            {
                _logger.LogInformation("All application data has been cleared");
            }
            else
            {
                _logger.LogWarning("Partially cleared application data. Errors: {Errors}", result.Errors);
            }

            return result;
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Unexpected error: {ex.Message}");
            _logger.LogError(ex, "Failed to clear all application data");
            return result;
        }
    }

    private void DeleteTempFiles()
    {
        var tempDbPath = _dbPath + TempFileExtension;
        var tempSettingsPath = _settingsPath + TempFileExtension;

        TryDeleteFile(tempDbPath);
        TryDeleteFile(tempSettingsPath);
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                _logger.LogDebug("Deleted temp file: {Path}", path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete temp file: {Path}", path);
        }
    }

    #endregion
}

/// <summary>
/// Represents a backup of all application data.
/// </summary>
internal class BackupData
{
    public DateTime ExportedAt { get; set; }
    public AppSettings? Settings { get; set; }
    public List<Conversation> Conversations { get; set; } = new();
    public List<ChatMessage> Messages { get; set; } = new();
}
