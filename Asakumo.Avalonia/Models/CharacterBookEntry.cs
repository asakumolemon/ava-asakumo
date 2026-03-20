using SQLite;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// Represents a single entry in a character's lorebook / world info.
/// Entries are activated based on keyword matching in the conversation.
/// </summary>
[Table("character_book_entries")]
public class CharacterBookEntry
{
    /// <summary>
    /// Gets or sets the unique identifier for the entry.
    /// </summary>
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the character ID this entry belongs to.
    /// </summary>
    [Indexed]
    public string CharacterId { get; set; } = string.Empty;

    #region Activation Keys

    /// <summary>
    /// Gets or sets the primary activation keywords.
    /// Comma-separated list of keywords that trigger this entry.
    /// </summary>
    public string Keys { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the secondary activation keywords.
    /// Used when Selective mode is enabled.
    /// </summary>
    public string? SecondaryKeys { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry is always injected.
    /// When true, the entry is included regardless of keyword matching.
    /// </summary>
    public bool IsConstant { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether selective activation is enabled.
    /// When true, both primary and secondary keys must match.
    /// </summary>
    public bool IsSelective { get; set; }

    #endregion

    #region Content

    /// <summary>
    /// Gets or sets the content to inject when activated.
    /// This text is added to the prompt when the entry is triggered.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entry name/title (optional, for organization).
    /// </summary>
    [MaxLength(200)]
    public string? EntryName { get; set; }

    #endregion

    #region Priority

    /// <summary>
    /// Gets or sets the insertion order priority.
    /// Lower values are inserted first (default: 100).
    /// </summary>
    public int Order { get; set; } = 100;

    /// <summary>
    /// Gets or sets the insertion position.
    /// 0 = after character definition, 1 = before example messages, etc.
    /// </summary>
    public int Position { get; set; } = 0;

    #endregion

    #region State

    /// <summary>
    /// Gets or sets a value indicating whether this entry is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    #endregion

    /// <summary>
    /// Checks if this entry should be activated based on the given context.
    /// </summary>
    /// <param name="contextText">The conversation context to check against.</param>
    /// <returns>True if the entry should be activated.</returns>
    public bool ShouldActivate(string contextText)
    {
        if (!IsEnabled)
            return false;

        // Constant entries are always activated
        if (IsConstant)
            return true;

        var lowerContext = contextText.ToLowerInvariant();

        // Check primary keys
        var primaryKeys = Keys.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        bool primaryMatch = false;
        foreach (var key in primaryKeys)
        {
            if (lowerContext.Contains(key.ToLowerInvariant()))
            {
                primaryMatch = true;
                break;
            }
        }

        if (!primaryMatch)
            return false;

        // If selective mode is off, primary match is enough
        if (!IsSelective)
            return true;

        // In selective mode, also check secondary keys
        if (string.IsNullOrEmpty(SecondaryKeys))
            return false;

        var secondaryKeys = SecondaryKeys.Split(',', System.StringSplitOptions.RemoveEmptyEntries | System.StringSplitOptions.TrimEntries);
        foreach (var key in secondaryKeys)
        {
            if (lowerContext.Contains(key.ToLowerInvariant()))
                return true;
        }

        return false;
    }
}

/// <summary>
/// Represents the character book / lorebook data structure.
/// This is stored as JSON in the Character.CharacterBook field.
/// </summary>
public class CharacterBookData
{
    /// <summary>
    /// Gets or sets the name of the character book.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the character book.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the entries in the character book.
    /// </summary>
    public System.Collections.Generic.List<CharacterBookEntryData>? Entries { get; set; }

    /// <summary>
    /// Gets or sets extension data for future compatibility.
    /// </summary>
    public string? Extensions { get; set; }
}

/// <summary>
/// Represents a single entry in the character book data (for JSON serialization).
/// </summary>
public class CharacterBookEntryData
{
    /// <summary>
    /// Gets or sets the unique identifier.
    /// </summary>
    public int Uid { get; set; }

    /// <summary>
    /// Gets or sets the primary activation keys.
    /// </summary>
    public System.Collections.Generic.List<string>? Keys { get; set; }

    /// <summary>
    /// Gets or sets the secondary activation keys.
    /// </summary>
    public System.Collections.Generic.List<string>? KeySecondary { get; set; }

    /// <summary>
    /// Gets or sets the content to inject.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this entry is always injected.
    /// </summary>
    public bool Constant { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether selective activation is enabled.
    /// </summary>
    public bool Selective { get; set; }

    /// <summary>
    /// Gets or sets the insertion order priority.
    /// </summary>
    public int Order { get; set; } = 100;

    /// <summary>
    /// Gets or sets the insertion position.
    /// </summary>
    public int Position { get; set; } = 0;

    /// <summary>
    /// Gets or sets a value indicating whether this entry is disabled.
    /// </summary>
    public bool Disable { get; set; }

    /// <summary>
    /// Gets or sets the entry name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the comment/notes for this entry.
    /// </summary>
    public string? Comment { get; set; }
}
