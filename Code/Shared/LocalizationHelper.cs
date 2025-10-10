using System;

namespace Mountain;

public static class LocalizationHelper
{
    /// <summary>
    /// Resolves a string - either returns it directly in the case that it's not a localization key or localizes and formats it.
    /// </summary>
    /// <param name="key">A localization key (e.g., "#status.burn.name")</param>
    /// <param name="formatArgs">Optional format arguments</param>
    public static string Resolve(string key, params object[] formatArgs)
    {
        if (!IsLocalizationKey(key))
        {
            return key;
        }

        var keyWithoutHash = key[1..];
        var phrase = Language.GetPhrase(keyWithoutHash);

        if (phrase is null)
        {
            return string.Empty;
        }

        // If there are no format keys.
        if (!phrase.Contains('{'))
        {
            return phrase;
        }

        try
        {
            return string.Format(phrase, formatArgs);
        }
        catch (FormatException)
        {
            return phrase;
        }
    }

    private static bool IsLocalizationKey(string text)
    {
        return text.StartsWith('#');
    }
}