using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;

namespace Asakumo.Avalonia.Models;

/// <summary>
/// A single entry in a character's lorebook / world info.
/// Entries activate based on keyword matching in the conversation.
/// </summary>
[Table("character_book_entries")]
public class CharacterBookEntry
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string CharacterId { get; set; } = string.Empty;

    // Activation keys (comma-separated)
    public string Keys { get; set; } = string.Empty;
    public string? SecondaryKeys { get; set; }
    public bool IsConstant { get; set; }
    public bool IsSelective { get; set; }

    // Content
    public string Content { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? EntryName { get; set; }

    // Priority
    public int Order { get; set; } = 100;
    public int Position { get; set; } = 0;

    // State
    public bool IsEnabled { get; set; } = true;

    public bool ShouldActivate(string contextText)
    {
        if (!IsEnabled)
            return false;

        if (IsConstant)
            return true;

        return MatchesKeys(contextText);
    }

    private bool MatchesKeys(string contextText)
    {
        var context = contextText.ToLowerInvariant();
        return HasPrimaryKeyMatch(context) && HasSecondaryKeyMatch(context);
    }

    private bool HasPrimaryKeyMatch(string context)
    {
        return SplitAndLowerKeys(Keys).Any(key => context.Contains(key));
    }

    private bool HasSecondaryKeyMatch(string context)
    {
        if (!IsSelective)
            return true;

        if (string.IsNullOrEmpty(SecondaryKeys))
            return false;

        return SplitAndLowerKeys(SecondaryKeys).Any(key => context.Contains(key));
    }

    private static IEnumerable<string> SplitAndLowerKeys(string? keys)
    {
        if (string.IsNullOrEmpty(keys))
            return Enumerable.Empty<string>();

        return keys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                   .Select(k => k.ToLowerInvariant());
    }
}

/// <summary>
/// Character book data structure for JSON serialization.
/// Stored in Character.CharacterBook field.
/// </summary>
public class CharacterBookData
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public System.Collections.Generic.List<CharacterBookEntryData>? Entries { get; set; }
    public string? Extensions { get; set; }
}

/// <summary>
/// Single entry in character book data for JSON serialization.
/// </summary>
public class CharacterBookEntryData
{
    public int Uid { get; set; }
    public System.Collections.Generic.List<string>? Keys { get; set; }
    public System.Collections.Generic.List<string>? KeySecondary { get; set; }
    public string? Content { get; set; }
    public bool Constant { get; set; }
    public bool Selective { get; set; }
    public int Order { get; set; } = 100;
    public int Position { get; set; } = 0;
    public bool Disable { get; set; }
    public string? Name { get; set; }
    public string? Comment { get; set; }
}
