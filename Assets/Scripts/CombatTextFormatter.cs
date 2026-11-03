using System.Text.RegularExpressions;

public static class CombatTextFormatter
{
    public static string Colorize(string message)
    {
        if (string.IsNullOrEmpty(message))
            return message;

        string pattern =
            @"\b(?:removing \d+ Shield|" +
            @"dealing (?:up to )?\d+ (?:(?:HP|Piercing) )?damage|" +
            @"(?:apply|applying) (?:up to )?\d+ Bleed|" +
            @"\d+ Bleed Damage|" +
            @"Remaining Bleed: \d+|" +
            @"restoring (?:up to )?\d+ HP|" +
            @"granting \d+ Shield|" +
            @"Correct answer|" +
            @"Incorrect answer|" +
            @"failed to activate|" +
            @"attacked|healed|healing|heal|" +
            @"damage|shield|buff|debuff|" +
            @"increasing|increase|reducing|reduce)\b";

        return Regex.Replace(message, pattern, match =>
        {
                string word = match.Value;
                string lower = word.ToLowerInvariant();

                string color;

                if (lower.Contains("bleed"))
                {
                    color = "#B4283C"; // Bleed - Dark Crimson
                }

                else if (lower.Contains("incorrect") ||
                    lower.Contains("failed") ||
                    lower.Contains("damage") ||
                    lower.Contains("removing") ||
                    lower == "attacked")
                {
                    color = "#C94B45"; // Damage - Red
                }

                else if (lower.Contains("correct") ||
                         lower.Contains("restoring") ||
                         lower.Contains("heal"))
                {
                    color = "#348A45"; // Healing - Green
                }

                else if (lower.Contains("shield") ||
                         lower.Contains("granting"))
                {
                    color = "#387FBC"; // Shield - Blue
                }

                else if ((lower.Contains("buff") &&
                          !lower.Contains("debuff")) ||
                         lower.Contains("increasing") ||
                         lower.Contains("increase"))
                {
                    color = "#A47A21"; // Buff - Gold
                }

                else
                {
                    color = "#8653B2"; // Debuff - Purple
                }

                return $"<color={color}>{word}</color>";
            },
            RegexOptions.IgnoreCase
        );
    }
}
