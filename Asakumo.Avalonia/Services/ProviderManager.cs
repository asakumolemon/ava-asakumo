using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services.Providers;
using Microsoft.Extensions.Logging;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Implementation of the provider manager service.
/// </summary>
public class ProviderManager : IProviderManager
{
    private readonly IDataService _dataService;
    private readonly AIProviderFactory _providerFactory;
    private readonly ILogger<ProviderManager>? _logger;

    /// <inheritdoc />
    public event EventHandler<ModelChangedEventArgs>? ModelChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderManager"/> class.
    /// </summary>
    public ProviderManager(
        IDataService dataService,
        AIProviderFactory providerFactory,
        ILogger<ProviderManager>? logger = null)
    {
        _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
        _providerFactory = providerFactory ?? throw new ArgumentNullException(nameof(providerFactory));
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderViewModel>> GetEnabledProvidersAsync()
    {
        var providers = _dataService.GetProviders();
        var settings = await _dataService.GetSettingsAsync();

        return providers
            .Where(p => IsProviderEnabled(p.Id, settings))
            .Select(p => CreateProviderViewModel(p, settings))
            .Where(p => p.IsEnabled);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ProviderViewModel>> GetConfiguredProvidersAsync()
    {
        var providers = _dataService.GetProviders();
        var settings = await _dataService.GetSettingsAsync();

        return providers
            .Select(p => CreateProviderViewModel(p, settings))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ModelViewModel>> GetProviderModelsAsync(string providerId, bool forceRefresh = false)
    {
        var settings = await _dataService.GetSettingsAsync();
        
        if (!settings.ProviderConfigs.TryGetValue(providerId, out var config))
        {
            return Enumerable.Empty<ModelViewModel>();
        }

        // Check if we need to refresh from API
        var shouldRefresh = forceRefresh || 
                           !config.AvailableModels.Any() ||
                           !config.ModelsLastUpdated.HasValue ||
                           (DateTime.Now - config.ModelsLastUpdated.Value).TotalHours > 24;

        if (shouldRefresh)
        {
            var refreshed = await RefreshProviderModelsInternalAsync(providerId, settings);
            if (!refreshed)
            {
                // Fall back to static models if API refresh fails
                return GetStaticModels(providerId, settings);
            }
        }

        return config.AvailableModels
            .Select(m => CreateModelViewModel(m, providerId, settings))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ModelViewModel>> GetAllEnabledModelsAsync()
    {
        var settings = await _dataService.GetSettingsAsync();
        var providers = _dataService.GetProviders();
        var models = new List<ModelViewModel>();

        foreach (var provider in providers)
        {
            if (!IsProviderEnabled(provider.Id, settings))
                continue;

            var providerModels = await GetProviderModelsAsync(provider.Id);
            models.AddRange(providerModels.Where(m => m.IsEnabled));
        }

        return models;
    }

    /// <inheritdoc />
    public async Task SwitchModelAsync(string providerId, string modelId)
    {
        var settings = await _dataService.GetSettingsAsync();
        
        var oldProviderId = settings.CurrentProviderId;
        var oldModelId = settings.CurrentModelId;

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

        _logger?.LogInformation("Switched model from {OldProvider}/{OldModel} to {NewProvider}/{NewModel}",
            oldProviderId, oldModelId, providerId, modelId);

        // Notify subscribers
        ModelChanged?.Invoke(this, new ModelChangedEventArgs
        {
            OldProviderId = oldProviderId,
            OldModelId = oldModelId,
            NewProviderId = providerId,
            NewModelId = modelId
        });
    }

    /// <inheritdoc />
    public async Task<ModelViewModel?> GetCurrentModelAsync()
    {
        var settings = await _dataService.GetSettingsAsync();

        if (string.IsNullOrEmpty(settings.CurrentProviderId) ||
            string.IsNullOrEmpty(settings.CurrentModelId))
        {
            return null;
        }

        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == settings.CurrentProviderId);
        
        if (provider == null)
            return null;

        // Try to get model from cached available models
        if (settings.ProviderConfigs.TryGetValue(settings.CurrentProviderId, out var config))
        {
            var cachedModel = config.AvailableModels.FirstOrDefault(m => m.Id == settings.CurrentModelId);
            if (cachedModel != null)
            {
                return CreateModelViewModel(cachedModel, settings.CurrentProviderId, settings);
            }
        }

        // Fall back to static model list
        var staticModel = provider.Models.FirstOrDefault(m => m.Id == settings.CurrentModelId);
        if (staticModel != null)
        {
            return new ModelViewModel
            {
                Id = staticModel.Id,
                Name = staticModel.Name,
                Description = staticModel.Description,
                ProviderId = provider.Id,
                ProviderName = provider.Name,
                IsEnabled = true,
                IsCurrent = true,
                Category = staticModel.Category
            };
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<RecentModelViewModel>> GetRecentModelsAsync(int count = 5)
    {
        var settings = await _dataService.GetSettingsAsync();
        var providers = _dataService.GetProviders();

        var recentModels = settings.RecentModels
            .OrderByDescending(r => r.UsedAt)
            .Take(count)
            .Select(r =>
            {
                var provider = providers.FirstOrDefault(p => p.Id == r.ProviderId);
                var model = provider?.Models.FirstOrDefault(m => m.Id == r.ModelId);
                
                return new RecentModelViewModel
                {
                    ProviderId = r.ProviderId,
                    ProviderName = provider?.Name ?? r.ProviderId,
                    ModelId = r.ModelId,
                    ModelName = model?.Name ?? r.ModelId,
                    CustomName = r.CustomName,
                    UsedAt = r.UsedAt
                };
            })
            .Where(r => !string.IsNullOrEmpty(r.ModelName))
            .ToList();

        return recentModels;
    }

    /// <inheritdoc />
    public async Task SetProviderEnabledAsync(string providerId, bool enabled)
    {
        var settings = await _dataService.GetSettingsAsync();

        if (!settings.ProviderConfigs.TryGetValue(providerId, out var config))
        {
            // Create new config if it doesn't exist
            config = new ProviderConfig();
            settings.ProviderConfigs[providerId] = config;
        }

        config.IsEnabled = enabled;
        await _dataService.SaveSettingsAsync(settings);

        _logger?.LogInformation("Provider {ProviderId} enabled state changed to {Enabled}", providerId, enabled);
    }

    /// <inheritdoc />
    public async Task SetModelEnabledAsync(string providerId, string modelId, bool enabled)
    {
        var settings = await _dataService.GetSettingsAsync();

        if (!settings.ProviderConfigs.TryGetValue(providerId, out var config))
            return;

        var model = config.AvailableModels.FirstOrDefault(m => m.Id == modelId);
        if (model != null)
        {
            model.IsEnabled = enabled;
        }
        else
        {
            // Add model if it doesn't exist in the list
            var providers = _dataService.GetProviders();
            var provider = providers.FirstOrDefault(p => p.Id == providerId);
            var staticModel = provider?.Models.FirstOrDefault(m => m.Id == modelId);
            
            if (staticModel != null)
            {
                config.AvailableModels.Add(new ModelInfo
                {
                    Id = staticModel.Id,
                    Name = staticModel.Name,
                    Description = staticModel.Description,
                    IsEnabled = enabled,
                    ProviderId = providerId
                });
            }
        }

        await _dataService.SaveSettingsAsync(settings);
        _logger?.LogInformation("Model {ModelId} for provider {ProviderId} enabled state changed to {Enabled}", 
            modelId, providerId, enabled);
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

            // Get a default model for testing
            var providers = _dataService.GetProviders();
            var provider = providers.FirstOrDefault(p => p.Id == providerId);
            var testModel = config.AvailableModels.FirstOrDefault(m => m.IsEnabled)?.Id
                ?? provider?.Models.FirstOrDefault()?.Id
                ?? "gpt-3.5-turbo";

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

    /// <inheritdoc />
    public async Task<bool> RefreshProviderModelsAsync(string providerId)
    {
        var settings = await _dataService.GetSettingsAsync();
        return await RefreshProviderModelsInternalAsync(providerId, settings);
    }

    private async Task<bool> RefreshProviderModelsInternalAsync(string providerId, AppSettings settings)
    {
        try
        {
            if (!settings.ProviderConfigs.TryGetValue(providerId, out var config) ||
                string.IsNullOrEmpty(config.ApiKey))
            {
                return false;
            }

            // Get a default model to create provider instance
            var providers = _dataService.GetProviders();
            var provider = providers.FirstOrDefault(p => p.Id == providerId);
            var defaultModel = provider?.Models.FirstOrDefault()?.Id ?? "gpt-3.5-turbo";

            var providerInstance = _providerFactory.CreateProvider(
                providerId,
                config.ApiKey,
                config.BaseUrl,
                defaultModel);

            var models = await providerInstance.GetModelsAsync();

            // Update cached models
            config.AvailableModels = models.Select(m => new ModelInfo
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

    private ProviderViewModel CreateProviderViewModel(AIProvider provider, AppSettings settings)
    {
        var isConfigured = settings.ProviderConfigs.TryGetValue(provider.Id, out var config);
        var enabledModelCount = isConfigured 
            ? config!.AvailableModels.Count(m => m.IsEnabled)
            : 0;

        return new ProviderViewModel
        {
            Id = provider.Id,
            Name = config?.DisplayName ?? provider.Name,
            Category = provider.Category,
            IsEnabled = isConfigured && config!.IsEnabled,
            IsConfigured = isConfigured && config!.IsValid,
            IsValid = isConfigured && config!.IsValid,
            EnabledModelCount = enabledModelCount,
            Icon = provider.Icon
        };
    }

    private ModelViewModel CreateModelViewModel(ModelInfo model, string providerId, AppSettings settings)
    {
        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == providerId);

        return new ModelViewModel
        {
            Id = model.Id,
            Name = model.Name,
            Description = model.Description,
            ProviderId = providerId,
            ProviderName = provider?.Name ?? providerId,
            IsEnabled = model.IsEnabled,
            IsCurrent = settings.CurrentProviderId == providerId && settings.CurrentModelId == model.Id,
            Category = ModelCategory.Chat // Default category, could be enhanced
        };
    }

    private IEnumerable<ModelViewModel> GetStaticModels(string providerId, AppSettings settings)
    {
        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == providerId);

        if (provider == null)
            return Enumerable.Empty<ModelViewModel>();

        return provider.Models.Select(m => new ModelViewModel
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            ProviderId = providerId,
            ProviderName = provider.Name,
            IsEnabled = true,
            IsCurrent = settings.CurrentProviderId == providerId && settings.CurrentModelId == m.Id,
            Category = m.Category
        });
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

        // Keep only last 20
        if (settings.RecentModels.Count > 20)
        {
            settings.RecentModels = settings.RecentModels.Take(20).ToList();
        }
    }
}
