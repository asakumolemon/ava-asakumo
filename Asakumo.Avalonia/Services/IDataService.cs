using System.Collections.Generic;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Provides data access functionality.
/// </summary>
public interface IDataService
{
    #region Providers

    /// <summary>
    /// Gets all statically defined AI providers with their models.
    /// </summary>
    /// <returns>A read-only list of AI providers.</returns>
    IReadOnlyList<AIProvider> GetProviders();

    /// <summary>
    /// Gets an AI provider by ID.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The provider, or null if not found.</returns>
    AIProvider? GetProvider(string providerId);

    /// <summary>
    /// Gets the configuration for a specific provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>
    /// The provider configuration if it exists in the database;
    /// null if the provider has never been configured.
    /// </returns>
    Task<ProviderConfig?> GetProviderConfigAsync(string providerId);

    /// <summary>
    /// Saves the configuration for a provider.
    /// </summary>
    /// <param name="config">The provider configuration to save.</param>
    Task SaveProviderConfigAsync(ProviderConfig config);

    /// <summary>
    /// Deletes the configuration for a provider.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    Task DeleteProviderConfigAsync(string providerId);

    /// <summary>
    /// Gets all configured providers.
    /// </summary>
    /// <returns>A read-only list of provider configurations.</returns>
    Task<IReadOnlyList<ProviderConfig>> GetAllProviderConfigsAsync();

    #endregion

    #region Conversations

    /// <summary>
    /// Gets all conversations ordered by last updated time.
    /// </summary>
    /// <returns>A read-only list of conversations.</returns>
    Task<IReadOnlyList<Conversation>> GetConversationsAsync();

    /// <summary>
    /// Gets a conversation by ID.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    /// <returns>The conversation, or null if not found.</returns>
    Task<Conversation?> GetConversationAsync(string id);

    /// <summary>
    /// Saves a conversation.
    /// </summary>
    /// <param name="conversation">The conversation to save.</param>
    Task SaveConversationAsync(Conversation conversation);

    /// <summary>
    /// Deletes a conversation and all its messages.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    Task DeleteConversationAsync(string id);

    #endregion

    #region Messages

    /// <summary>
    /// Gets all messages for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    /// <returns>A read-only list of messages.</returns>
    Task<IReadOnlyList<ChatMessage>> GetMessagesAsync(string conversationId);

    /// <summary>
    /// Saves a message.
    /// </summary>
    /// <param name="message">The message to save.</param>
    Task SaveMessageAsync(ChatMessage message);

    /// <summary>
    /// Deletes a message by ID.
    /// </summary>
    /// <param name="messageId">The message ID.</param>
    Task DeleteMessageAsync(string messageId);

    /// <summary>
    /// Deletes all messages for a conversation.
    /// </summary>
    /// <param name="conversationId">The conversation ID.</param>
    Task DeleteMessagesAsync(string conversationId);

    #endregion

    #region Settings

    /// <summary>
    /// Gets the application settings.
    /// </summary>
    /// <returns>The application settings.</returns>
    Task<AppSettings> GetSettingsAsync();

    /// <summary>
    /// Saves the application settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveSettingsAsync(AppSettings settings);

    #endregion

    #region Characters

    /// <summary>
    /// Gets all characters ordered by last used time.
    /// </summary>
    /// <returns>A read-only list of characters.</returns>
    Task<IReadOnlyList<Character>> GetCharactersAsync();

    /// <summary>
    /// Gets a character by ID.
    /// </summary>
    /// <param name="id">The character ID.</param>
    /// <returns>The character, or null if not found.</returns>
    Task<Character?> GetCharacterAsync(string id);

    /// <summary>
    /// Saves a character.
    /// </summary>
    /// <param name="character">The character to save.</param>
    Task SaveCharacterAsync(Character character);

    /// <summary>
    /// Deletes a character by ID.
    /// </summary>
    /// <param name="id">The character ID.</param>
    Task DeleteCharacterAsync(string id);

    /// <summary>
    /// Gets all lorebook entries for a character.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    /// <returns>A read-only list of lorebook entries.</returns>
    Task<IReadOnlyList<CharacterBookEntry>> GetCharacterBookEntriesAsync(string characterId);

    /// <summary>
    /// Saves a lorebook entry.
    /// </summary>
    /// <param name="entry">The entry to save.</param>
    Task SaveCharacterBookEntryAsync(CharacterBookEntry entry);

    /// <summary>
    /// Deletes a lorebook entry by ID.
    /// </summary>
    /// <param name="entryId">The entry ID.</param>
    Task DeleteCharacterBookEntryAsync(int entryId);

    /// <summary>
    /// Deletes all lorebook entries for a character.
    /// </summary>
    /// <param name="characterId">The character ID.</param>
    Task DeleteCharacterBookEntriesAsync(string characterId);

    /// <summary>
    /// Searches characters by name or tags.
    /// </summary>
    /// <param name="query">The search query.</param>
    /// <returns>A read-only list of matching characters.</returns>
    Task<IReadOnlyList<Character>> SearchCharactersAsync(string query);

    /// <summary>
    /// Gets favourite characters.
    /// </summary>
    /// <returns>A read-only list of favourite characters.</returns>
    Task<IReadOnlyList<Character>> GetFavouriteCharactersAsync();

    /// <summary>
    /// Gets recently used characters.
    /// </summary>
    /// <param name="count">The maximum number of characters to return.</param>
    /// <returns>A read-only list of recently used characters.</returns>
    Task<IReadOnlyList<Character>> GetRecentlyUsedCharactersAsync(int count = 10);

    #endregion

    #region Backup & Maintenance

    /// <summary>
    /// Clears all conversations and messages.
    /// </summary>
    Task ClearAllConversationsAsync();

    /// <summary>
    /// Exports all data to a backup file.
    /// </summary>
    /// <param name="backupPath">The path to save the backup.</param>
    Task BackupDataAsync(string backupPath);

    /// <summary>
    /// Gets the database file path.
    /// </summary>
    /// <returns>The database file path.</returns>
    string GetDatabasePath();

    /// <summary>
    /// Clears all application data including settings and database.
    /// This will delete the database file and settings file.
    /// </summary>
    /// <returns>Detailed result of the clear operation.</returns>
    Task<ClearDataResult> ClearAllDataAsync();

    #endregion
}

/// <summary>
/// Represents the result of clearing application data.
/// </summary>
public class ClearDataResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the database file was deleted.
    /// </summary>
    public bool DatabaseDeleted { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the settings file was deleted.
    /// </summary>
    public bool SettingsDeleted { get; set; }

    /// <summary>
    /// Gets or sets the list of errors that occurred during cleanup.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Gets a value indicating whether all data was cleared successfully.
    /// </summary>
    public bool AllCleared => DatabaseDeleted && SettingsDeleted && Errors.Count == 0;
}
