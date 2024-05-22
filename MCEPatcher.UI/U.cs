using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MCEPatcher.UI
{
    internal static class U
    {
        public static string? LimitLengthMiddle(string? str, int maxLength, string ender = "...")
        {
            if (str is null || str.Length <= maxLength) return str;

            int strLen = (maxLength - ender.Length) / 2;

            if (strLen <= 0) return ender;

            return str.Substring(0, strLen) + ender + str.Substring(str.Length - strLen, strLen);
        }
    }
}
