using System;
using SQLite;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// AI character for roleplay. Compatible with Character Card V2 format.
/// </summary>
[Table("characters")]
public class Character
{
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    // Basic Information
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Personality { get; set; }
    public string? Scenario { get; set; }

    // Dialogue Settings
    public string FirstMessage { get; set; } = string.Empty;
    public string? AlternateGreetings { get; set; }  // JSON array
    public string? ExampleMessages { get; set; }

    // Advanced Settings
    public string? SystemPrompt { get; set; }
    public string? PostHistoryInstructions { get; set; }

    // Metadata
    [MaxLength(100)]
    public string? Creator { get; set; }
    public string? CreatorNotes { get; set; }
    public string? Tags { get; set; }  // JSON array
    [MaxLength(50)]
    public string? CharacterVersion { get; set; }
    [Indexed]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Indexed]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Avatar
    public string? AvatarPath { get; set; }

    // Lorebook / Character Book (JSON)
    public string? CharacterBook { get; set; }

    // Extensions (JSON)
    public string? Extensions { get; set; }

    // App-Specific Settings
    public bool IsFavourite { get; set; }
    public int UsageCount { get; set; }
    [Indexed]
    public DateTime? LastUsedAt { get; set; }

    public void RecordUsage()
    {
        LastUsedAt = DateTime.UtcNow;
        UsageCount++;
    }

    public void Touch()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Result of importing a character.
/// </summary>
public class CharacterImportResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Character? Character { get; set; }
    public string OriginalFormat { get; set; } = string.Empty;

    public static CharacterImportResult SuccessResult(Character character, string format)
    {
        return new CharacterImportResult
        {
            Success = true,
            Character = character,
            OriginalFormat = format
        };
    }

    public static CharacterImportResult FailureResult(string errorMessage)
    {
        return new CharacterImportResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
