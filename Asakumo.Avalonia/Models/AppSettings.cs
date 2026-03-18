using System;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents application settings.
/// </summary>
public class AppSettings
{
    /// <summary>
    /// Gets or sets the current language.
    /// </summary>
    public string Language { get; set; } = "zh-CN";

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    public bool IsDarkMode { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the welcome screen has been shown.
    /// </summary>
    public bool HasSeenWelcome { get; set; }
}