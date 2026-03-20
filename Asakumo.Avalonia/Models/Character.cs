using System;
using SQLite;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents an AI character for roleplay.
/// Compatible with SillyTavern/Character.AI Character Card V2 format.
/// </summary>
[Table("characters")]
public class Character
{
    /// <summary>
    /// Gets or sets the unique identifier for the character.
    /// </summary>
    [PrimaryKey]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    #region Basic Information

    /// <summary>
    /// Gets or sets the character name.
    /// This is sent with every message, so keep it short.
    /// </summary>
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character description.
    /// This is always included in the prompt - put core character info here.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the personality traits.
    /// Short tag-style description (e.g., "cheerful, cunning, teasing").
    /// </summary>
    [MaxLength(500)]
    public string? Personality { get; set; }

    /// <summary>
    /// Gets or sets the scenario/context for the roleplay.
    /// </summary>
    public string? Scenario { get; set; }

    #endregion

    #region Dialogue Settings

    /// <summary>
    /// Gets or sets the first message/greeting.
    /// Sets the tone and style for the character's responses.
    /// </summary>
    public string FirstMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets alternative greetings (JSON array).
    /// Randomly selected when starting a new chat.
    /// </summary>
    public string? AlternateGreetings { get; set; }

    /// <summary>
    /// Gets or sets example messages showing the character's speaking style.
    /// Format: &lt;START&gt;\n{{user}}: ...\n{{char}}: ...
    /// </summary>
    public string? ExampleMessages { get; set; }

    #endregion

    #region Advanced Settings

    /// <summary>
    /// Gets or sets the system prompt for the character.
    /// Overrides the default system prompt when set.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// Gets or sets post-history instructions.
    /// Special instructions for processing conversation history.
    /// </summary>
    public string? PostHistoryInstructions { get; set; }

    #endregion

    #region Metadata

    /// <summary>
    /// Gets or sets the creator name.
    /// </summary>
    [MaxLength(100)]
    public string? Creator { get; set; }

    /// <summary>
    /// Gets or sets creator notes/tips for using the character.
    /// </summary>
    public string? CreatorNotes { get; set; }

    /// <summary>
    /// Gets or sets tags for categorization (JSON array).
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Gets or sets the character version.
    /// </summary>
    [MaxLength(50)]
    public string? CharacterVersion { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    [Indexed]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the last updated timestamp.
    /// </summary>
    [Indexed]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    #endregion

    #region Avatar

    /// <summary>
    /// Gets or sets the avatar image file path.
    /// </summary>
    public string? AvatarPath { get; set; }

    #endregion

    #region Lorebook / Character Book

    /// <summary>
    /// Gets or sets the character book / lorebook data (JSON).
    /// Contains world info entries that activate based on keywords.
    /// </summary>
    public string? CharacterBook { get; set; }

    #endregion

    #region Extensions

    /// <summary>
    /// Gets or sets extension data (JSON).
    /// For custom fields and future format compatibility.
    /// </summary>
    public string? Extensions { get; set; }

    #endregion

    #region App-Specific Settings

    /// <summary>
    /// Gets or sets a value indicating whether the character is favourited.
    /// </summary>
    public bool IsFavourite { get; set; }

    /// <summary>
    /// Gets or sets the usage count.
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Gets or sets the last used timestamp.
    /// </summary>
    [Indexed]
    public DateTime? LastUsedAt { get; set; }

    #endregion

    /// <summary>
    /// Updates the last used timestamp and increments usage count.
    /// </summary>
    public void MarkAsUsed()
    {
        LastUsedAt = DateTime.UtcNow;
        UsageCount++;
    }

    /// <summary>
    /// Updates the timestamp before saving changes.
    /// </summary>
    public void MarkAsUpdated()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Represents the result of importing a character.
/// </summary>
public class CharacterImportResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the import was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the error message if import failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the imported character.
    /// </summary>
    public Character? Character { get; set; }

    /// <summary>
    /// Gets or sets the original format version ("V1", "V2", "V3", "PNG").
    /// </summary>
    public string OriginalFormat { get; set; } = string.Empty;

    /// <summary>
    /// Creates a successful import result.
    /// </summary>
    public static CharacterImportResult SuccessResult(Character character, string format)
    {
        return new CharacterImportResult
        {
            Success = true,
            Character = character,
            OriginalFormat = format
        };
    }

    /// <summary>
    /// Creates a failed import result.
    /// </summary>
    public static CharacterImportResult FailureResult(string errorMessage)
    {
        return new CharacterImportResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
