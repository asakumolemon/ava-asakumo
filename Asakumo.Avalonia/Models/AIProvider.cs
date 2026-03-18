using System;
using System.Collections.Generic;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents an AI service provider with its available models.
/// </summary>
public class AIProvider
{
    /// <summary>
    /// Gets or sets the unique identifier for the provider (e.g., "openai").
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the provider (e.g., "OpenAI").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the provider type (e.g., "openai-compatible", "anthropic", "google").
    /// </summary>
    public AIProviderType Type { get; set; }

    /// <summary>
    /// Gets or sets the icon identifier or emoji for the provider.
    /// </summary>
    public string Icon { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default base URL for API requests.
    /// </summary>
    public string DefaultBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the provider requires a base URL configuration.
    /// </summary>
    public bool RequiresBaseUrl { get; set; }

    /// <summary>
    /// Gets or sets the list of available models for this provider.
    /// </summary>
    public List<AIModel> Models { get; set; } = new();

    /// <summary>
    /// Gets or sets optional description of the provider.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the website URL for the provider.
    /// </summary>
    public string? WebsiteUrl { get; set; }

    /// <summary>
    /// Gets or sets the URL to get an API key for the provider.
    /// </summary>
    public string? ApiKeyHelpUrl { get; set; }

    #region Static Provider Definitions

    /// <summary>
    /// Gets all statically defined providers.
    /// </summary>
    public static List<AIProvider> GetAllProviders()
    {
        return new List<AIProvider>
        {
            CreateOpenAIProvider(),
            CreateAnthropicProvider(),
            CreateGoogleProvider(),
            CreateDeepSeekProvider(),
            CreateOllamaProvider()
        };
    }

    private static AIProvider CreateOpenAIProvider()
    {
        return new AIProvider
        {
            Id = "openai",
            Name = "OpenAI",
            Type = AIProviderType.OpenAICompatible,
            Icon = "🤖",
            DefaultBaseUrl = "https://api.openai.com/v1",
            RequiresBaseUrl = false,
            Description = "OpenAI GPT models including GPT-4 and GPT-3.5",
            WebsiteUrl = "https://platform.openai.com",
            ApiKeyHelpUrl = "https://platform.openai.com/api-keys",
            Models = new List<AIModel>
            {
                new AIModel
                {
                    Id = "gpt-4o",
                    Name = "GPT-4o",
                    MaxTokens = 128000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "openai",
                    Description = "Most capable GPT-4 model, optimized for speed and intelligence"
                },
                new AIModel
                {
                    Id = "gpt-4o-mini",
                    Name = "GPT-4o Mini",
                    MaxTokens = 128000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "openai",
                    Description = "Affordable and intelligent small model"
                },
                new AIModel
                {
                    Id = "o3-mini",
                    Name = "o3-mini",
                    MaxTokens = 200000,
                    SupportsVision = false,
                    SupportsFunctionCalling = true,
                    ProviderId = "openai",
                    Description = "Latest reasoning model with improved capabilities"
                },
                new AIModel
                {
                    Id = "gpt-4-turbo",
                    Name = "GPT-4 Turbo",
                    MaxTokens = 128000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "openai",
                    Description = "Previous generation GPT-4 with vision support"
                },
                new AIModel
                {
                    Id = "gpt-3.5-turbo",
                    Name = "GPT-3.5 Turbo",
                    MaxTokens = 16384,
                    SupportsVision = false,
                    SupportsFunctionCalling = true,
                    ProviderId = "openai",
                    Description = "Fast and affordable model for simple tasks"
                }
            }
        };
    }

    private static AIProvider CreateAnthropicProvider()
    {
        return new AIProvider
        {
            Id = "anthropic",
            Name = "Anthropic",
            Type = AIProviderType.Anthropic,
            Icon = "🧠",
            DefaultBaseUrl = "https://api.anthropic.com",
            RequiresBaseUrl = false,
            Description = "Claude AI models by Anthropic",
            WebsiteUrl = "https://console.anthropic.com",
            ApiKeyHelpUrl = "https://console.anthropic.com/settings/keys",
            Models = new List<AIModel>
            {
                new AIModel
                {
                    Id = "claude-sonnet-4-20250514",
                    Name = "Claude Sonnet 4",
                    MaxTokens = 200000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "anthropic",
                    Description = "Latest Claude model with excellent reasoning"
                },
                new AIModel
                {
                    Id = "claude-3-5-sonnet-20241022",
                    Name = "Claude 3.5 Sonnet",
                    MaxTokens = 200000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "anthropic",
                    Description = "High-performance model with extended capabilities"
                },
                new AIModel
                {
                    Id = "claude-3-5-haiku-20241022",
                    Name = "Claude 3.5 Haiku",
                    MaxTokens = 200000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "anthropic",
                    Description = "Fast and efficient model for everyday tasks"
                },
                new AIModel
                {
                    Id = "claude-3-opus-20240229",
                    Name = "Claude 3 Opus",
                    MaxTokens = 200000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "anthropic",
                    Description = "Most powerful Claude 3 model for complex tasks"
                }
            }
        };
    }

    private static AIProvider CreateGoogleProvider()
    {
        return new AIProvider
        {
            Id = "google",
            Name = "Google AI",
            Type = AIProviderType.Google,
            Icon = "✨",
            DefaultBaseUrl = "https://generativelanguage.googleapis.com",
            RequiresBaseUrl = false,
            Description = "Google Gemini models",
            WebsiteUrl = "https://aistudio.google.com",
            ApiKeyHelpUrl = "https://aistudio.google.com/app/apikey",            Models = new List<AIModel>
            {
                new AIModel
                {
                    Id = "gemini-2.0-flash",
                    Name = "Gemini 2.0 Flash",
                    MaxTokens = 1048576,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "google",
                    Description = "Latest Gemini model with multimodal capabilities"
                },
                new AIModel
                {
                    Id = "gemini-1.5-pro",
                    Name = "Gemini 1.5 Pro",
                    MaxTokens = 2097152,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "google",
                    Description = "Advanced reasoning with very long context"
                },
                new AIModel
                {
                    Id = "gemini-1.5-flash",
                    Name = "Gemini 1.5 Flash",
                    MaxTokens = 1048576,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "google",
                    Description = "Fast and efficient for high-volume tasks"
                }
            }
        };
    }

    private static AIProvider CreateDeepSeekProvider()
    {
        return new AIProvider
        {
            Id = "deepseek",
            Name = "DeepSeek",
            Type = AIProviderType.OpenAICompatible,
            Icon = "🔮",
            DefaultBaseUrl = "https://api.deepseek.com",
            RequiresBaseUrl = false,
            Description = "DeepSeek AI models with excellent reasoning capabilities",
            WebsiteUrl = "https://platform.deepseek.com",
            ApiKeyHelpUrl = "https://platform.deepseek.com/api_keys",            Models = new List<AIModel>
            {
                new AIModel
                {
                    Id = "deepseek-chat",
                    Name = "DeepSeek Chat",
                    MaxTokens = 64000,
                    SupportsVision = false,
                    SupportsFunctionCalling = true,
                    ProviderId = "deepseek",
                    Description = "General-purpose conversational model"
                },
                new AIModel
                {
                    Id = "deepseek-reasoner",
                    Name = "DeepSeek Reasoner",
                    MaxTokens = 64000,
                    SupportsVision = false,
                    SupportsFunctionCalling = false,
                    ProviderId = "deepseek",
                    Description = "Advanced reasoning model (R1)"
                }
            }
        };
    }

    private static AIProvider CreateOllamaProvider()
    {
        return new AIProvider
        {
            Id = "ollama",
            Name = "Ollama",
            Type = AIProviderType.OpenAICompatible,
            Icon = "🦙",
            DefaultBaseUrl = "http://localhost:11434/v1",
            RequiresBaseUrl = true,
            Description = "Run open-source models locally with Ollama",
            WebsiteUrl = "https://ollama.ai",
            Models = new List<AIModel>
            {
                new AIModel
                {
                    Id = "llama3.2",
                    Name = "Llama 3.2",
                    MaxTokens = 128000,
                    SupportsVision = true,
                    SupportsFunctionCalling = true,
                    ProviderId = "ollama",
                    Description = "Meta's Llama 3.2 model"
                },
                new AIModel
                {
                    Id = "qwen2.5",
                    Name = "Qwen 2.5",
                    MaxTokens = 128000,
                    SupportsVision = false,
                    SupportsFunctionCalling = true,
                    ProviderId = "ollama",
                    Description = "Alibaba's Qwen 2.5 model"
                },
                new AIModel
                {
                    Id = "deepseek-r1",
                    Name = "DeepSeek R1",
                    MaxTokens = 128000,
                    SupportsVision = false,
                    SupportsFunctionCalling = false,
                    ProviderId = "ollama",
                    Description = "DeepSeek reasoning model"
                },
                new AIModel
                {
                    Id = "codellama",
                    Name = "Code Llama",
                    MaxTokens = 16384,
                    SupportsVision = false,
                    SupportsFunctionCalling = false,
                    ProviderId = "ollama",
                    Description = "Specialized for code generation"
                }
            }
        };
    }

    #endregion
}

/// <summary>
/// Defines the type of AI provider.
/// </summary>
public enum AIProviderType
{
    /// <summary>
    /// OpenAI-compatible API (OpenAI, DeepSeek, Ollama, etc.)
    /// </summary>
    OpenAICompatible,

    /// <summary>
    /// Anthropic Claude API
    /// </summary>
    Anthropic,

    /// <summary>
    /// Google Gemini API
    /// </summary>
    Google
}
