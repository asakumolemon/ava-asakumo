using System;
using Avalonia.Styling;

namespace Asakumo.Avalonia.Services;

/// <summary>
/// Service for managing application theme.
/// </summary>
public interface IThemeService
{
    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    bool IsDarkMode { get; set; }

    /// <summary>
    /// Initializes the theme from saved settings.
    /// </summary>
    /// <param name="isDarkMode">Whether dark mode should be enabled.</param>
    void Initialize(bool isDarkMode);

    /// <summary>
    /// Occurs when the theme is changed.
    /// </summary>
    event Action<bool>? ThemeChanged;
}

/// <summary>
/// Implementation of the theme service.
/// </summary>
public class ThemeService : IThemeService
{
    private bool _isDarkMode = true;

    /// <inheritdoc/>
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                ApplyTheme();
                ThemeChanged?.Invoke(value);
            }
        }
    }

    /// <inheritdoc/>
    public event Action<bool>? ThemeChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    public ThemeService()
    {
        // Default to system theme on startup
        // Will be overridden by saved settings
    }

    /// <inheritdoc/>
    public void Initialize(bool isDarkMode)
    {
        _isDarkMode = isDarkMode;
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (global::Avalonia.Application.Current != null)
        {
            global::Avalonia.Application.Current.RequestedThemeVariant = _isDarkMode
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
    }
}