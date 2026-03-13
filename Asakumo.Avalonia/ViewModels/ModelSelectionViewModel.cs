using System.Collections.ObjectModel;
using System.Linq;
using Asakumo.Avalonia.Models;
using Asakumo.Avalonia.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Asakumo.Avalonia.ViewModels;

/// <summary>
/// View model for the model selection view.
/// </summary>
public partial class ModelSelectionViewModel : ViewModelBase
{
    private readonly IDataService _dataService;
    private readonly INavigationService _navigationService;

    /// <summary>
    /// Gets or sets the recommended models.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AIModel> _recommendedModels = new();

    /// <summary>
    /// Gets or sets the reasoning models.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AIModel> _reasoningModels = new();

    /// <summary>
    /// Gets or sets the chat models.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<AIModel> _chatModels = new();

    /// <summary>
    /// Gets or sets the selected model.
    /// </summary>
    [ObservableProperty]
    private AIModel? _selectedModel;

    /// <summary>
    /// Gets or sets the provider name.
    /// </summary>
    [ObservableProperty]
    private string _providerName = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether connected successfully.
    /// </summary>
    [ObservableProperty]
    private bool _isConnected;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelSelectionViewModel"/> class.
    /// </summary>
    /// <param name="dataService">The data service.</param>
    /// <param name="navigationService">The navigation service.</param>
    public ModelSelectionViewModel(IDataService dataService, INavigationService navigationService)
    {
        _dataService = dataService;
        _navigationService = navigationService;
        LoadModels();
    }

    /// <summary>
    /// Command to go back.
    /// </summary>
    [RelayCommand]
    private void GoBack()
    {
        _navigationService.GoBack();
    }

    /// <summary>
    /// Command to select a model.
    /// </summary>
    /// <param name="model">The selected model.</param>
    [RelayCommand]
    private void SelectModel(AIModel model)
    {
        SelectedModel = model;
    }

    /// <summary>
    /// Command to confirm the selection.
    /// </summary>
    [RelayCommand]
    private void ConfirmSelection()
    {
        if (SelectedModel == null)
            return;

        var settings = _dataService.GetSettings();
        settings.CurrentModelId = SelectedModel.Id;
        _dataService.SaveSettingsAsync(settings);

        // Navigate back to chat
        _navigationService.NavigateTo<ChatViewModel>();
    }

    private void LoadModels()
    {
        var settings = _dataService.GetSettings();
        if (string.IsNullOrEmpty(settings.CurrentProviderId))
            return;

        var providers = _dataService.GetProviders();
        var provider = providers.FirstOrDefault(p => p.Id == settings.CurrentProviderId);

        if (provider == null)
            return;

        ProviderName = provider.Name;
        IsConnected = settings.ProviderConfigs.TryGetValue(provider.Id, out var config) && config.IsValid;

        RecommendedModels.Clear();
        ReasoningModels.Clear();
        ChatModels.Clear();

        foreach (var model in provider.Models)
        {
            switch (model.Category)
            {
                case ModelCategory.Recommended:
                    RecommendedModels.Add(model);
                    break;
                case ModelCategory.Reasoning:
                    ReasoningModels.Add(model);
                    break;
                case ModelCategory.Chat:
                    ChatModels.Add(model);
                    break;
            }
        }

        // Select current model or first recommended
        if (!string.IsNullOrEmpty(settings.CurrentModelId))
        {
            SelectedModel = provider.Models.FirstOrDefault(m => m.Id == settings.CurrentModelId);
        }

        SelectedModel ??= RecommendedModels.FirstOrDefault();
    }
}
