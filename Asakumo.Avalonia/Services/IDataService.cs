using System.Collections.Generic;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Provides data access functionality.
/// </summary>
public interface IDataService
{
    #region Conversations

    /// <summary>
    /// Gets all conversations ordered by last updated time.
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
    /// <returns>A list of messages.</returns>
    Task<List<ChatMessage>> GetMessagesAsync(string conversationId);

    /// <summary>
    /// Saves a message.
    /// </summary>
    /// <param name="message">The message to save.</param>
    Task SaveMessageAsync(ChatMessage message);

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

    #region Provider Config

    /// <summary>
    /// Gets the provider configuration.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <returns>The provider configuration, or null if not found.</returns>
    Task<ProviderConfig?> GetProviderConfigAsync(string providerId);

    /// <summary>
    /// Saves the provider configuration.
    /// </summary>
    /// <param name="providerId">The provider ID.</param>
    /// <param name="config">The configuration to save.</param>
    Task SaveProviderConfigAsync(string providerId, ProviderConfig config);

    #endregion

    #region Providers (Static Data)

    /// <summary>
    /// Gets all AI providers (static definition).
    /// </summary>
    /// <returns>A list of AI providers.</returns>
    List<AIProvider> GetProviders();

    #endregion
}
