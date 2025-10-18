using System;
using System.Text.RegularExpressions;

public static class MessageCleaner
{
    public static string turnToCommand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        input = input.ToLowerInvariant();

        if (!input.Contains("felkiált"))
            return input;
        var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        string result = "";
        foreach (var rawPart in parts)
        {
            string part = rawPart;
            if (part.Contains("felkiált"))
                part = "!";

            result += part;
        }
        result = Regex.Replace(result, @"[^\w!]+", "");

        return result;
    }
}
