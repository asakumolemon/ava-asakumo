using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services.Providers;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of the simplified model service.
/// </summary>
public class ModelService : IModelService
{
    private const string DefaultTestModel = "gpt-3.5-turbo";
    private const int MaxRecentModels = 20;

    private readonly IDataService _dataService;
    private readonly AIProviderFactory _providerFactory;
    private readonly ILogger<ModelService>? _logger;

    /// <inheritdoc />
    public event EventHandler<ModelChangedEventArgs>? CurrentModelChanged;

    /// <inheritdoc />
    public ModelDescriptor? CurrentModel { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelService"/> class.
    /// </summary>
    public ModelService(
        IDataService dataService,
        AIProviderFactory providerFactory,
        ILogger<ModelService>? logger = null)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger;

        _ = InitializeAsync().ContinueWith(t =>
        {
            if (t.IsFaulted && t.Exception != null)
            {
                _logger?.LogError(t.Exception, "Failed to initialize ModelService");
            }
        }, TaskScheduler.Current);
    }

    private async Task InitializeAsync()
    {
        try
        {
            CurrentModel = await LoadCurrentModelAsync();
            _logger?.LogInformation("ModelService initialized with model: {Model}", CurrentModel?.Name ?? "none");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize ModelService");
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProviderInfo>> GetProvidersAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        var providers = _dataService.GetProviders();

        return providers.Select(p =>
        {
            var config = settings.ProviderConfigs.GetValueOrDefault(p.Id);
            return new ProviderInfo
            {
                Id = p.Id,
                Name = config?.DisplayName ?? p.Name,
                Icon = p.Icon,
                IsEnabled = config?.IsEnabled ?? false,
                IsConfigured = config?.IsValid ?? false,
                Color = GetProviderColor(p.Id)
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelDescriptor>> GetAllModelsAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        var providers = _dataService.GetProviders();
        var models = new List<ModelDescriptor>();

        foreach (var provider in providers)
        {
            if (!IsProviderEnabled(provider.Id, settings))
                continue;

            var providerModels = await GetModelsForProviderAsync(provider, settings);
            models.AddRange(providerModels.Where(m => m.IsEnabled));
        }

        return models.OrderBy(m => m.ProviderName).ThenBy(m => m.Name).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelDescriptor>> GetModelsByProviderAsync(string providerId)
    {
        var settings = await _dataService.GetSettingsAsync();
        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == providerId);

        if (provider is null || !IsProviderEnabled(providerId, settings))
            return Array.Empty<ModelDescriptor>();

        var models = await GetModelsForProviderAsync(provider, settings);
        return models.Where(m => m.IsEnabled).ToList();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ModelDescriptor>> GetRecentModelsAsync(int count = 5)
    {
        var settings = await _dataService.GetSettingsAsync();
        var providers = _dataService.GetProviders();

        var recentModels = settings.RecentModels
            .OrderByDescending(r => r.UsedAt)
            .Take(count)
            .Select(r => CreateModelFromRecent(r, providers, settings))
            .Where(m => m != null)
            .Cast<ModelDescriptor>()
            .ToList();

        return recentModels;
    }

    /// <inheritdoc />
    public async Task SwitchModelAsync(string providerId, string modelId)
    {
        var settings = await _dataService.GetSettingsAsync();
        var oldModel = CurrentModel;

        // Update current settings
        settings.CurrentProviderId = providerId;
        settings.CurrentModelId = modelId;

        // Update last used model for the provider
        if (settings.ProviderConfigs.TryGetValue(providerId, out var config))
        {
            config.LastUsedModelId = modelId;
        }

        // Add to recent models
        AddToRecentModels(settings, providerId, modelId);

        await _dataService.SaveSettingsAsync(settings);

        // Reload current model
        CurrentModel = await LoadCurrentModelAsync();

        _logger?.LogInformation("Switched model from {OldModel} to {NewModel}",
            oldModel?.FullName ?? "none", CurrentModel?.FullName ?? "none");

        // Notify subscribers
        CurrentModelChanged?.Invoke(this, new ModelChangedEventArgs
        {
            OldModel = oldModel,
            NewModel = CurrentModel
        });
    }

    /// <inheritdoc />
    public async Task<bool> ValidateCurrentModelAsync()
    {
        if (CurrentModel == null)
            return false;

        var settings = await _dataService.GetSettingsAsync();

        if (!settings.ProviderConfigs.TryGetValue(CurrentModel.ProviderId, out var config))
            return false;

        return config.IsValid && !string.IsNullOrEmpty(config.ApiKey);
    }

    private async Task<ModelDescriptor?> LoadCurrentModelAsync()
    {
        var settings = await _dataService.GetSettingsAsync();

        if (string.IsNullOrEmpty(settings.CurrentProviderId) ||
            string.IsNullOrEmpty(settings.CurrentModelId))
        {
            return null;
        }

        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == settings.CurrentProviderId);

        if (provider is null)
            return null;

        // Try to find model in static list
        var staticModel = provider.Models.FirstOrDefault(m => m.Id == settings.CurrentModelId);
        if (staticModel != null)
        {
            return CreateModelDescriptor(staticModel, provider, settings);
        }

        // Try to find in cached models
        if (settings.ProviderConfigs.TryGetValue(settings.CurrentProviderId, out var config))
        {
            var cachedModel = config.AvailableModels.FirstOrDefault(m => m.Id == settings.CurrentModelId);
            if (cachedModel != null)
            {
                return CreateModelDescriptor(cachedModel, provider, settings);
            }
        }

        // Fallback: create basic model descriptor
        return new ModelDescriptor
        {
            Id = settings.CurrentModelId,
            Name = settings.CurrentModelId,
            ProviderId = provider.Id,
            ProviderName = provider.Name,
            ProviderIcon = provider.Icon,
            IsEnabled = true
        };
    }

    private Task<IEnumerable<ModelDescriptor>> GetModelsForProviderAsync(AIProvider provider, AppSettings settings)
    {
        var models = new List<ModelDescriptor>();

        // Get enabled models from config, or use static list
        if (settings.ProviderConfigs.TryGetValue(provider.Id, out var config) && config.AvailableModels.Any())
        {
            models.AddRange(config.AvailableModels.Select(m => CreateModelDescriptor(m, provider, settings)));
        }
        else
        {
            models.AddRange(provider.Models.Select(m => CreateModelDescriptor(m, provider, settings)));
        }

        return Task.FromResult(models.AsEnumerable());
    }

    private ModelDescriptor CreateModelDescriptor(AIModel model, AIProvider provider, AppSettings settings)
    {
        return new ModelDescriptor
        {
            Id = model.Id,
            Name = model.Name,
            ProviderId = provider.Id,
            ProviderName = provider.Name,
            ProviderIcon = provider.Icon,
            Description = model.Description,
            IsEnabled = true,
            Capabilities = new ModelCapabilities
            {
                Streaming = true,
                Vision = model.Id.Contains("vision", StringComparison.OrdinalIgnoreCase) ||
                         model.Id.Contains("claude-3", StringComparison.OrdinalIgnoreCase) ||
                         model.Id.Contains("gemini-1.5", StringComparison.OrdinalIgnoreCase),
                FunctionCalling = model.Id.Contains("gpt-4", StringComparison.OrdinalIgnoreCase) ||
                                  model.Id.Contains("claude-3", StringComparison.OrdinalIgnoreCase)
            }
        };
    }

    private ModelDescriptor CreateModelDescriptor(Models.ModelInfo model, AIProvider provider, AppSettings settings)
    {
        return new ModelDescriptor
        {
            Id = model.Id,
            Name = model.Name,
            ProviderId = provider.Id,
            ProviderName = provider.Name,
            ProviderIcon = provider.Icon,
            Description = model.Description,
            IsEnabled = model.IsEnabled
        };
    }

    private ModelDescriptor? CreateModelFromRecent(RecentModel recent, List<AIProvider> providers, AppSettings settings)
    {
        var provider = providers.FirstOrDefault(p => p.Id == recent.ProviderId);
        if (provider is null)
            return null;

        var model = provider.Models.FirstOrDefault(m => m.Id == recent.ModelId);

        return new ModelDescriptor
        {
            Id = recent.ModelId,
            Name = model?.Name ?? recent.ModelId,
            ProviderId = recent.ProviderId,
            ProviderName = provider.Name,
            ProviderIcon = provider.Icon,
            LastUsedAt = recent.UsedAt
        };
    }

    private static bool IsProviderEnabled(string providerId, AppSettings settings)
    {
        if (!settings.ProviderConfigs.TryGetValue(providerId, out var config))
            return false;

        return config.IsEnabled && config.IsValid;
    }

    private static void AddToRecentModels(AppSettings settings, string providerId, string modelId)
    {
        // Remove existing entry if present
        settings.RecentModels.RemoveAll(r =>
            r.ProviderId == providerId && r.ModelId == modelId);

        // Add to front
        settings.RecentModels.Insert(0, new RecentModel
        {
            ProviderId = providerId,
            ModelId = modelId,
            UsedAt = DateTime.Now
        });

        // Keep only last N models
        if (settings.RecentModels.Count > MaxRecentModels)
        {
            settings.RecentModels = settings.RecentModels.Take(MaxRecentModels).ToList();
        }
    }

    /// <inheritdoc />
    public async Task<bool> TestProviderAsync(string providerId)
    {
        try
        {
            var settings = await _dataService.GetSettingsAsync();

            if (!settings.ProviderConfigs.TryGetValue(providerId, out var config) ||
                string.IsNullOrEmpty(config.ApiKey))
            {
                return false;
            }

            var providers = _dataService.GetProviders();
            var provider = providers.FirstOrDefault(p => p.Id == providerId);
            var testModel = config.AvailableModels.FirstOrDefault(m => m.IsEnabled)?.Id
                ?? provider?.Models.FirstOrDefault()?.Id
                ?? DefaultTestModel;

            var tempProvider = _providerFactory.CreateProvider(
                providerId,
                config.ApiKey,
                config.BaseUrl,
                testModel);

            return await tempProvider.ValidateAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to test provider {ProviderId}", providerId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<bool> RefreshProviderModelsAsync(string providerId)
    {
        try
        {
            var settings = await _dataService.GetSettingsAsync();

            if (!settings.ProviderConfigs.TryGetValue(providerId, out var config) ||
                string.IsNullOrEmpty(config.ApiKey))
            {
                return false;
            }

            var providers = _dataService.GetProviders();
            var provider = providers.FirstOrDefault(p => p.Id == providerId);
            var defaultModel = provider?.Models.FirstOrDefault()?.Id ?? DefaultTestModel;

            var providerInstance = _providerFactory.CreateProvider(
                providerId,
                config.ApiKey,
                config.BaseUrl,
                defaultModel);

            var models = await providerInstance.GetModelsAsync();

            // Update cached models
            config.AvailableModels = models.Select(m => new Models.ModelInfo
            {
                Id = m.Id,
                Name = m.Name,
                Description = m.Description,
                IsEnabled = true,
                ProviderId = providerId
            }).ToList();

            config.ModelsLastUpdated = DateTime.Now;
            await _dataService.SaveSettingsAsync(settings);

            _logger?.LogInformation("Refreshed {Count} models for provider {ProviderId}",
                config.AvailableModels.Count, providerId);

            return true;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to refresh models for provider {ProviderId}", providerId);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task SetProviderEnabledAsync(string providerId, bool enabled)
    {
        var settings = await _dataService.GetSettingsAsync();

        if (!settings.ProviderConfigs.TryGetValue(providerId, out var config))
        {
            config = new ProviderConfig();
            settings.ProviderConfigs[providerId] = config;
        }

        config.IsEnabled = enabled;
        await _dataService.SaveSettingsAsync(settings);

        _logger?.LogInformation("Provider {ProviderId} enabled state changed to {Enabled}", providerId, enabled);
    }

    /// <inheritdoc />
    public async Task SetProviderDisplayNameAsync(string providerId, string? displayName)
    {
        var settings = await _dataService.GetSettingsAsync();

        if (!settings.ProviderConfigs.TryGetValue(providerId, out var config))
        {
            config = new ProviderConfig();
            settings.ProviderConfigs[providerId] = config;
        }

        config.DisplayName = displayName;
        await _dataService.SaveSettingsAsync(settings);
    }

    private static string GetProviderColor(string providerId)
    {
        return providerId.ToLower() switch
        {
            "openai" => "#10A37F",
            "anthropic" => "#D97757",
            "google" => "#4285F4",
            "deepseek" => "#4F46E5",
            "ollama" => "#FF6B6B",
            _ => "#6B7280"
        };
    }
}
