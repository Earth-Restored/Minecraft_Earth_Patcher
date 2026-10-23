using System;
using System.Collections.Generic;

namespace MCEPatcher.Core;

public static class StringExtensions
{
    // from: https://stackoverflow.com/a/24016130/15878562
    public static IEnumerable<int> AllIndexesOf(this string str, string searchString)
    {
        var minIndex = str.IndexOf(searchString);

        while (minIndex is not -1)
        {
            yield return minIndex;
            minIndex = str.IndexOf(searchString, minIndex + searchString.Length);
        }
    }

    public static string GetNewLine(string text)
    {
        int index = text.IndexOf('\n');

        if (index < 0)
        {
            if (text.Contains("\r"))
            {
                return "\r";
            }
            else
            {
                return Environment.NewLine;
            }
        }
        else if (index is 0)
        {
            return "\n";
        }

        return text[index - 1] is '\r' ? "\r\n" : "\n";
    }
}
