using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of the data service.
/// </summary>
public class DataService : IDataService
{
    private List<Conversation> _conversations = new();
    private AppSettings _settings = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DataService"/> class.
    /// </summary>
    public DataService()
    {
        InitializeSampleData();
    }

    /// <inheritdoc/>
    public Task<List<Conversation>> GetConversationsAsync()
    {
        return Task.FromResult(_conversations.OrderByDescending(c => c.UpdatedAt).ToList());
    }

    /// <inheritdoc/>
    public Task<Conversation?> GetConversationAsync(string id)
    {
        return Task.FromResult(_conversations.FirstOrDefault(c => c.Id == id));
    }

    /// <inheritdoc/>
    public Task SaveConversationAsync(Conversation conversation)
    {
        var existingIndex = _conversations.FindIndex(c => c.Id == conversation.Id);
        if (existingIndex >= 0)
        {
            _conversations[existingIndex] = conversation;
        }
        else
        {
            _conversations.Add(conversation);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteConversationAsync(string id)
    {
        _conversations.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }

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
                Icon = "○"
            },
            new AIProvider
            {
                Id = "deepseek",
                Name = "DeepSeek",
                Category = ProviderCategory.Popular,
                Description = "国产大模型，性价比高",
                DefaultBaseUrl = "https://api.deepseek.com",
                RequiresApiKey = true,
                Icon = "○"
            },
            new AIProvider
            {
                Id = "ollama",
                Name = "Ollama (本地部署)",
                Category = ProviderCategory.Local,
                Description = "本地运行开源模型",
                DefaultBaseUrl = "http://localhost:11434",
                RequiresApiKey = false,
                Icon = "🖥️"
            }
        };
    }

    /// <inheritdoc/>
    public AppSettings GetSettings()
    {
        return _settings;
    }

    /// <inheritdoc/>
    public Task SaveSettingsAsync(AppSettings settings)
    {
        _settings = settings;
        return Task.CompletedTask;
    }

    private void InitializeSampleData()
    {
        // Add sample conversations for testing
        _conversations = new List<Conversation>
        {
            new Conversation
            {
                Id = "1",
                Title = "你好，请介绍一下你自己",
                ModelName = "GPT-4o-mini",
                ProviderId = "openai",
                CreatedAt = DateTime.Now.AddHours(-2),
                UpdatedAt = DateTime.Now.AddHours(-2),
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "你好，请介绍一下你自己",
                        IsUser = true,
                        Timestamp = DateTime.Now.AddHours(-2)
                    },
                    new ChatMessage
                    {
                        Content = "你好！我是一个AI助手，可以帮助你回答问题、写作、编程、翻译等各种任务。",
                        IsUser = false,
                        Timestamp = DateTime.Now.AddHours(-2).AddSeconds(5)
                    }
                }
            },
            new Conversation
            {
                Id = "2",
                Title = "解释量子力学",
                ModelName = "GPT-4o",
                ProviderId = "openai",
                CreatedAt = DateTime.Now.AddDays(-1),
                UpdatedAt = DateTime.Now.AddDays(-1),
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "解释量子力学",
                        IsUser = true,
                        Timestamp = DateTime.Now.AddDays(-1)
                    }
                }
            },
            new Conversation
            {
                Id = "3",
                Title = "写一段Python代码",
                ModelName = "Claude 3.5 Sonnet",
                ProviderId = "anthropic",
                CreatedAt = DateTime.Now.AddDays(-3),
                UpdatedAt = DateTime.Now.AddDays(-3),
                Messages = new List<ChatMessage>
                {
                    new ChatMessage
                    {
                        Content = "写一段Python代码",
                        IsUser = true,
                        Timestamp = DateTime.Now.AddDays(-3)
                    }
                }
            }
        };
    }
}
