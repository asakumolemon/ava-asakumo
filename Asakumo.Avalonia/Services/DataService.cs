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
            await _database.CreateTableAsync<ProviderConfig>();
            await _database.CreateTableAsync<Character>();
            await _database.CreateTableAsync<CharacterBookEntry>();

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

    #region Providers

    /// <inheritdoc/>
    public IReadOnlyList<AIProvider> GetProviders()
    {
        return AIProvider.GetAllProviders();
    }

    /// <inheritdoc/>
    public AIProvider? GetProvider(string providerId)
    {
        return AIProvider.GetAllProviders().FirstOrDefault(p => p.Id == providerId);
    }

    /// <inheritdoc/>
    public async Task<ProviderConfig?> GetProviderConfigAsync(string providerId)
    {
        await EnsureInitializedAsync();

        var config = await _database!.Table<ProviderConfig>()
            .Where(c => c.ProviderId == providerId)
            .FirstOrDefaultAsync();

        return config;
    }

    /// <inheritdoc/>
    public async Task SaveProviderConfigAsync(ProviderConfig config)
    {
        await EnsureInitializedAsync();

        config.UpdatedAt = DateTime.UtcNow;

        if (config.CreatedAt == default)
        {
            config.CreatedAt = DateTime.UtcNow;
        }

        await _database!.InsertOrReplaceAsync(config);

        _logger.LogDebug("Saved provider config for {ProviderId}", config.ProviderId);
    }

    /// <inheritdoc/>
    public async Task DeleteProviderConfigAsync(string providerId)
    {
        await EnsureInitializedAsync();

        await _database!.ExecuteAsync(
            "DELETE FROM provider_configs WHERE ProviderId = ?",
            providerId);

        _logger.LogDebug("Deleted provider config for {ProviderId}", providerId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProviderConfig>> GetAllProviderConfigsAsync()
    {
        await EnsureInitializedAsync();

        var configs = await _database!.Table<ProviderConfig>()
            .ToListAsync();

        return configs;
    }

    #endregion

    #region Conversations

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Conversation>> GetConversationsAsync()
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
    public async Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId)
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

    #region Characters

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetCharactersAsync()
    {
        await EnsureInitializedAsync();

        var characters = await _database!.Table<Character>()
            .OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt)
            .ToListAsync();

        return characters;
    }

    /// <inheritdoc/>
    public async Task<Character?> GetCharacterAsync(string id)
    {
        await EnsureInitializedAsync();

        var character = await _database!.Table<Character>()
            .Where(c => c.Id == id)
            .FirstOrDefaultAsync();

        return character;
    }

    /// <inheritdoc/>
    public async Task SaveCharacterAsync(Character character)
    {
        await EnsureInitializedAsync();

        character.MarkAsUpdated();
        await _database!.InsertOrReplaceAsync(character);

        _logger.LogDebug("Saved character {Id} - {Name}", character.Id, character.Name);
    }

    /// <inheritdoc/>
    public async Task DeleteCharacterAsync(string id)
    {
        await EnsureInitializedAsync();

        // Delete lorebook entries first, then character
        await _database!.ExecuteAsync("DELETE FROM character_book_entries WHERE CharacterId = ?", id);
        await _database.ExecuteAsync("DELETE FROM characters WHERE Id = ?", id);

        _logger.LogDebug("Deleted character {Id} and its lorebook entries", id);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<CharacterBookEntry>> GetCharacterBookEntriesAsync(string characterId)
    {
        await EnsureInitializedAsync();

        var entries = await _database!.Table<CharacterBookEntry>()
            .Where(e => e.CharacterId == characterId)
            .OrderBy(e => e.Order)
            .ToListAsync();

        return entries;
    }

    /// <inheritdoc/>
    public async Task SaveCharacterBookEntryAsync(CharacterBookEntry entry)
    {
        await EnsureInitializedAsync();

        if (entry.Id == 0)
        {
            await _database!.InsertAsync(entry);
        }
        else
        {
            await _database!.UpdateAsync(entry);
        }

        _logger.LogDebug("Saved lorebook entry {Id} for character {CharacterId}", entry.Id, entry.CharacterId);
    }

    /// <inheritdoc/>
    public async Task DeleteCharacterBookEntryAsync(int entryId)
    {
        await EnsureInitializedAsync();

        await _database!.ExecuteAsync(
            "DELETE FROM character_book_entries WHERE Id = ?",
            entryId);

        _logger.LogDebug("Deleted lorebook entry {Id}", entryId);
    }

    /// <inheritdoc/>
    public async Task DeleteCharacterBookEntriesAsync(string characterId)
    {
        await EnsureInitializedAsync();

        await _database!.ExecuteAsync(
            "DELETE FROM character_book_entries WHERE CharacterId = ?",
            characterId);

        _logger.LogDebug("Deleted all lorebook entries for character {CharacterId}", characterId);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> SearchCharactersAsync(string query)
    {
        await EnsureInitializedAsync();

        var lowerQuery = $"%{query.ToLowerInvariant()}%";

        var characters = await _database!.Table<Character>()
            .Where(c => c.Name.ToLower().Contains(lowerQuery) ||
                       (c.Tags != null && c.Tags.ToLower().Contains(lowerQuery)) ||
                       (c.Description != null && c.Description.ToLower().Contains(lowerQuery)))
            .OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt)
            .ToListAsync();

        return characters;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetFavouriteCharactersAsync()
    {
        await EnsureInitializedAsync();

        var characters = await _database!.Table<Character>()
            .Where(c => c.IsFavourite)
            .OrderByDescending(c => c.LastUsedAt ?? c.CreatedAt)
            .ToListAsync();

        return characters;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Character>> GetRecentlyUsedCharactersAsync(int count = 10)
    {
        await EnsureInitializedAsync();

        var characters = await _database!.Table<Character>()
            .Where(c => c.LastUsedAt != null)
            .OrderByDescending(c => c.LastUsedAt)
            .Take(count)
            .ToListAsync();

        return characters;
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
                Conversations = (await GetConversationsAsync()).ToList()
            };

            // Get all messages for each conversation
            var allMessages = new List<ChatMessage>();
            foreach (var conv in backup.Conversations)
            {
                var messages = await GetMessagesAsync(conv.Id);
                allMessages.AddRange(messages);
            }
            backup.Messages = allMessages;

            // Get all characters and their lorebook entries
            backup.Characters = (await GetCharactersAsync()).ToList();
            var allEntries = new List<CharacterBookEntry>();
            foreach (var character in backup.Characters)
            {
                var entries = await GetCharacterBookEntriesAsync(character.Id);
                allEntries.AddRange(entries);
            }
            backup.CharacterBookEntries = allEntries;

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
    public List<Character> Characters { get; set; } = new();
    public List<CharacterBookEntry> CharacterBookEntries { get; set; } = new();
}