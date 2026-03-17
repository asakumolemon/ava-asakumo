---
name: dotnet-avalonia-quality
description: Comprehensive code quality guidelines for Avalonia applications using CommunityToolkit.Mvvm. Enforces MVVM best practices, naming conventions, and Microsoft enterprise architecture patterns. Use when reviewing or writing C# Avalonia code with CommunityToolkit.Mvvm 8.4.0.
---

# Avalonia Code Quality Skill

This skill ensures high-quality Avalonia code using **CommunityToolkit.Mvvm 8.4.0** by enforcing MVVM best practices, Microsoft enterprise architecture patterns, and proper source generator usage.

## When to Use This Skill

Use this skill when:
1. Writing new C# Avalonia code
2. Reviewing existing Avalonia code for quality issues
3. Refactoring code to improve maintainability
4. Setting up code standards for an Avalonia project
5. Debugging MVVM data binding issues

## Core Principles

### 1. MVVM Pattern with CommunityToolkit.Mvvm 8.4.0

**Correct - 使用 Source Generators (推荐):**
```csharp
public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        // Implementation
    }

    public bool CanSend => !string.IsNullOrWhiteSpace(InputMessage) && !IsLoading;
}
```

**Note:** CommunityToolkit.Mvvm 8.4.0 generates the property automatically from the field.

**Incorrect:**
```csharp
// ViewModel directly references UI controls
public class ChatViewModel
{
    public void BadMethod()
    {
        var button = new Button(); // UI dependency in ViewModel
    }
}
```

### 2. Naming Conventions

| Element | Convention | Example |
|---------|-----------|---------|
| Classes/Interfaces | PascalCase | `ChatService`, `IChatProvider` |
| Methods | PascalCase | `SendMessageAsync()` |
| Properties | PascalCase | `public string UserName { get; set; }` |
| Private fields | _camelCase | `private readonly HttpClient _httpClient;` |
| Observable fields | _camelCase with underscore | `private string _inputMessage;` |
| Constants | UPPER_SNAKE_CASE | `const int MAX_RETRY = 3;` |
| Parameters | camelCase | `void Method(string userName)` |

### 3. ObservableProperty 最佳实践

**正确的属性通知链:**
```csharp
public partial class ChatViewModel : ViewModelBase
{
    // 当 InputMessage 变化时，通知 CanSend 和 SendMessageCommand
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]           // 通知 CanSend 变化
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]  // 刷新命令状态
    private string _inputMessage = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSend))]
    private bool _isAiResponding;

    // 计算属性 - 不需要 ObservableProperty
    public bool CanSend => !IsAiResponding && !string.IsNullOrWhiteSpace(InputMessage);

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputMessage))
            return;
        
        var message = InputMessage;
        InputMessage = string.Empty;  // 清空输入框会自动触发通知
        
        // ... send message
    }
}
```

### 4. Code Layout

**Correct:**
```csharp
namespace Asakumo.Services
{
    public class ChatService : IChatService
    {
        private readonly HttpClient _httpClient;

        public ChatService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task<string> SendMessageAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message cannot be empty", nameof(message));
            }

            // Implementation
            return await Task.FromResult(message);
        }
    }
}
```

### 5. Documentation

**Required Documentation:**
```csharp
/// <summary>
/// Provides chat functionality with AI providers.
/// </summary>
public interface IChatProvider
{
    /// <summary>
    /// Sends a message to the AI provider and returns the response.
    /// </summary>
    /// <param name="history">Previous conversation history.</param>
    /// <param name="message">The user message to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI response text.</returns>
    Task<string> SendMessageAsync(
        List<ChatMessage> history,
        string message,
        CancellationToken cancellationToken);
}
```

### 6. Async/Await Best Practices

**Correct:**
```csharp
public async Task LoadDataAsync(CancellationToken cancellationToken = default)
{
    try
    {
        var data = await _service.GetDataAsync(cancellationToken);
        Items = data;
    }
    catch (OperationCanceledException)
    {
        // Log cancellation
    }
    catch (Exception ex)
    {
        // Handle error
        await ShowErrorAsync(ex.Message);
    }
}

// Fire-and-forget in constructor (acceptable for desktop apps)
public SettingsViewModel(IDataService dataService)
{
    _ = LoadSettingsAsync();
}
```

**Incorrect:**
```csharp
// Avoid async void
public async void BadMethod() { }

// Avoid .Result or .Wait() - can cause deadlocks
var result = _service.GetDataAsync().Result;
```

### 7. Dependency Injection (Avalonia Specific)

**Correct:**
```csharp
// App.axaml.cs
public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        
        // Services
        services.AddSingleton<IDataService, DataService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<AIProviderFactory>();
        services.AddSingleton<IAIService, AIService>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<ChatViewModel>();

        var serviceProvider = services.BuildServiceProvider();
        
        // ... use serviceProvider
    }
}

// Constructor injection
public class ChatService : IChatService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ChatService> _logger;

    public ChatService(HttpClient httpClient, ILogger<ChatService> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

### 8. Project Structure

**Standard Avalonia Project Layout:**
```
Project/
├── Models/              # Data entities (ObservableObject for UI updates)
│   ├── ChatMessage.cs
│   └── Conversation.cs
├── Services/            # Business logic
│   ├── IChatService.cs
│   └── ChatService.cs
├── ViewModels/          # Presentation logic (CommunityToolkit.Mvvm)
│   ├── ViewModelBase.cs
│   └── ChatViewModel.cs
├── Views/               # AXAML pages
│   ├── ChatView.axaml
│   └── ChatView.axaml.cs
├── Converters/          # Value converters
│   └── BoolToColorConverter.cs
└── Assets/              # Resources
    └── Icons/
```

### 9. Data Binding (Avalonia AXAML)

**Correct:**
```xml
<!-- View with compiled bindings -->
<UserControl x:Class="MyApp.Views.ChatView"
             xmlns:vm="using:MyApp.ViewModels"
             x:DataType="vm:ChatViewModel">
    <TextBlock Text="{Binding UserName}" />
    <Button Command="{Binding SendMessageCommand}" 
            IsEnabled="{Binding CanSend}" />
</UserControl>
```

**ViewModel:**
```csharp
public partial class ChatViewModel : ViewModelBase
{
    [ObservableProperty]
    private string? _userName;
    
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        // Command automatically implements ICommand
    }
}
```

### 10. Avalonia-Specific Best Practices

#### Compiled Bindings
```xml
<!-- Enable compiled bindings for better performance -->
<Window x:Class="MyApp.Views.MainWindow"
        xmlns="https://github.com/avaloniaui"
        xmlns:vm="using:MyApp.ViewModels"
        x:DataType="vm:MainViewModel">
    <TextBlock Text="{Binding Title}" />
</Window>
```

#### Styles and Resources
```xml
<!-- App.axaml -->
<Application.Styles>
    <FluentTheme />
</Application.Styles>

<Application.Resources>
    <SolidColorBrush x:Key="AccentBrush" Color="{DynamicResource SystemAccentColor}" />
</Application.Resources>
```

## Code Review Checklist

When reviewing C# Avalonia code, check:

### CommunityToolkit.Mvvm Best Practices
- [ ] Using `[ObservableProperty]` for observable fields
- [ ] Using `[RelayCommand]` for commands
- [ ] `[NotifyPropertyChangedFor]` for computed properties
- [ ] `[NotifyCanExecuteChangedFor]` for command state updates
- [ ] ViewModel has no UI control references
- [ ] Proper use of data binding with `x:DataType`
- [ ] Async methods end with "Async" suffix
- [ ] CancellationToken passed to async methods
- [ ] Services registered in DI container
- [ ] Constructor injection used

### Naming Conventions
- [ ] Private fields use _camelCase
- [ ] Observable fields have underscore prefix
- [ ] Properties use PascalCase
- [ ] Methods use PascalCase

### Architecture
- [ ] Single responsibility per class
- [ ] Loose coupling between components
- [ ] Interface segregation
- [ ] Proper error handling
- [ ] No code duplication (DRY)

## Key Differences from ReactiveUI

| Feature | ReactiveUI | CommunityToolkit.Mvvm |
|---------|-----------|----------------------|
| Property Notification | `RaiseAndSetIfChanged` | `[ObservableProperty]` |
| Commands | `ReactiveCommand` | `[RelayCommand]` |
| Base Class | `ReactiveObject` | `ObservableObject` |
| Source Generation | No | Yes (compile-time) |
| Boilerplate | More | Less |

## Quality Metrics

High-quality Avalonia code should have:
- 100% public API documentation
- Zero compiler warnings
- No async void methods
- Proper null checking
- Clear separation of concerns
- Unit testable components
- Compiled bindings for performance

## References

- CommunityToolkit.Mvvm: https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
- Avalonia Documentation: https://docs.avaloniaui.net/