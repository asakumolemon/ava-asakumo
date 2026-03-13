using System.Collections.Generic;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Provides data access functionality.
/// </summary>
public interface IDataService
{
    /// <summary>
    /// Gets all conversations.
    /// </summary>
    /// <returns>A list of conversations.</returns>
    Task<List<Conversation>> GetConversationsAsync();

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
    /// Deletes a conversation.
    /// </summary>
    /// <param name="id">The conversation ID.</param>
    Task DeleteConversationAsync(string id);

    /// <summary>
    /// Gets all AI providers.
    /// </summary>
    /// <returns>A list of AI providers.</returns>
    List<AIProvider> GetProviders();

    /// <summary>
    /// Gets the application settings.
    /// </summary>
    /// <returns>The application settings.</returns>
    AppSettings GetSettings();

    /// <summary>
    /// Saves the application settings.
    /// </summary>
    /// <param name="settings">The settings to save.</param>
    Task SaveSettingsAsync(AppSettings settings);
}
