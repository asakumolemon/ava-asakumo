---
name: dotnet-avalonia-quality
description: Comprehensive code quality guidelines for Avalonia applications. Enforces StyleCop rules, MVVM best practices, naming conventions, and Microsoft enterprise architecture patterns. Use when reviewing or writing C# Avalonia code to ensure high quality, maintainable, and testable code.
---

# Avalonia Code Quality Skill

This skill ensures high-quality Avalonia code by enforcing StyleCop rules, Microsoft enterprise architecture patterns, and MVVM best practices.

## When to Use This Skill

Use this skill when:
1. Writing new C# Avalonia code
2. Reviewing existing Avalonia code for quality issues
3. Refactoring code to improve maintainability
4. Setting up code standards for an Avalonia project
5. Debugging MVVM data binding issues

## Core Principles

### 1. MVVM Pattern Compliance

**Correct:**
```csharp
// ViewModel has no UI dependencies
public class ChatViewModel : ViewModelBase
{
    private string? _userName;
    public string? UserName
    {
        get => _userName;
        set => this.RaiseAndSetIfChanged(ref _userName, value);
    }
    
    public ICommand SendCommand { get; }
}
```

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

### 2. Naming Conventions (StyleCop SA1300-SA1313)

| Element | Convention | Example |
|---------|-----------|---------|
| Classes/Interfaces | PascalCase | `ChatService`, `IChatProvider` |
| Methods | PascalCase | `SendMessageAsync()` |
| Properties | PascalCase | `public string UserName { get; set; }` |
| Private fields | _camelCase | `private readonly HttpClient _httpClient;` |
| Constants | UPPER_SNAKE_CASE | `const int MAX_RETRY = 3;` |
| Parameters | camelCase | `void Method(string userName)` |

### 3. Code Layout (StyleCop SA1500-SA1518)

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

### 4. Documentation (StyleCop SA1600-SA1650)

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

### 5. Async/Await Best Practices

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
```

**Incorrect:**
```csharp
// Avoid async void
public async void BadMethod() { }

// Avoid .Result or .Wait()
var result = _service.GetDataAsync().Result;
```

### 6. Dependency Injection (Avalonia Specific)

**Correct:**
```csharp
// App.axaml.cs or Program.cs
builder.Services.AddSingleton<IChatService, ChatService>();
builder.Services.AddTransient<ChatViewModel>();
builder.Services.AddTransient<ChatView>();

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

### 7. Project Structure

**Standard Avalonia Project Layout:**
```
Project/
├── Models/              # Data entities
│   ├── ChatMessage.cs
│   └── Conversation.cs
├── Services/            # Business logic
│   ├── IChatService.cs
│   └── ChatService.cs
├── ViewModels/          # Presentation logic (ReactiveUI or CommunityToolkit.Mvvm)
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

### 8. Data Binding (Avalonia AXAML)

**Correct:**
```xml
<!-- View -->
<Label Text="{Binding UserName}" />
<Button Command="{Binding SendCommand}" 
        IsEnabled="{Binding !IsLoading}" />
```

```csharp
// ViewModel with ReactiveUI
public class ChatViewModel : ViewModelBase
{
    public string? UserName
    {
        get => _userName;
        set => this.RaiseAndSetIfChanged(ref _userName, value);
    }
    
    public ReactiveCommand<Unit, Unit> SendCommand { get; }
}
```

### 9. Avalonia-Specific Best Practices

#### Compiled Bindings
```xml
<!-- Enable compiled bindings for better performance -->
<Window x:Class="MyApp.Views.MainWindow"
        xmlns="https://github.com/avaloniaui"
        x:DataType="vm:MainViewModel">
    <TextBlock Text="{Binding Title}" />
</Window>
```

#### Styles and Resources
```xml
<!-- App.axaml -->
<Application.Styles>
    <FluentTheme />
    <SimpleTheme />
</Application.Styles>

<Application.Resources>
    <SolidColorBrush x:Key="AccentBrush">#10A37F</SolidColorBrush>
</Application.Resources>
```

#### Control Templates
```xml
<Styles.Resources>
    <ControlTheme x:Key="CustomButton" TargetType="Button">
        <Setter Property="Background" Value="{DynamicResource AccentBrush}" />
        <Setter Property="CornerRadius" Value="8" />
    </ControlTheme>
</Styles.Resources>
```

## Code Review Checklist

When reviewing C# Avalonia code, check:

### StyleCop Rules
- [ ] SA1600: All public types documented
- [ ] SA1604: All public methods documented  
- [ ] SA1300: PascalCase for types/methods/properties
- [ ] SA1309: _camelCase for private fields
- [ ] SA1500: Braces on new lines
- [ ] SA1516: Blank line between methods
- [ ] SA1210: Using statements sorted alphabetically

### Avalonia Best Practices
- [ ] ViewModel has no UI control references
- [ ] Proper use of data binding
- [ ] ReactiveCommand or ICommand used
- [ ] Compiled bindings enabled (x:DataType)
- [ ] Async methods end with "Async" suffix
- [ ] CancellationToken passed to async methods
- [ ] Services registered in DI container
- [ ] Constructor injection used

### Architecture
- [ ] Single responsibility per class
- [ ] Loose coupling between components
- [ ] Interface segregation
- [ ] Proper error handling
- [ ] No code duplication (DRY)

## Quality Metrics

High-quality Avalonia code should have:
- 100% public API documentation
- Zero StyleCop warnings (treat as errors)
- No async void methods
- Proper null checking
- Clear separation of concerns
- Unit testable components
- Compiled bindings for performance

## References

- StyleCop Analyzers: https://github.com/DotNetAnalyzers/StyleCopAnalyzers
- Avalonia Documentation: https://docs.avaloniaui.net/
- ReactiveUI: https://www.reactiveui.net/
- CommunityToolkit.Mvvm: https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/
