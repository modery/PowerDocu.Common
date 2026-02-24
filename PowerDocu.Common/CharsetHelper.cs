using System;
using System.Globalization;
using System.Text;

namespace PowerDocu.Common
{
    public static class CharsetHelper
    {
        private static readonly char[] UnsafeChars =
        {
            ':',
            '?',
            '<',
            '>',
            '/',
            '|',
            ',',
            '*',
            '&',
            '"',
            '#'
        };

        // this function cleans up names a lot. Replaces Umlauts and similar with "safe letters" (e.g. ä to a), and strips most other characters that would cause errors (e.g. Chinese chracters)
        // Problems are mostly happening in the graphviz library. Not sure how much control we have and what other options there are, considering this a temporary fix for the moment
        public static string GetSafeName(string s)
        {
            if (String.IsNullOrEmpty(s))
            {
                return "NameNotDefined";
            }

            String normalizedString = s.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder(normalizedString.Length);

            for (int i = 0; i < normalizedString.Length; i++)
            {
                Char c = normalizedString[i];
                // Strip combining diacritical marks (e.g. ä → a)
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                    continue;

                char normalized = c;
                // Re-normalize to FormC char-by-char isn't possible, so we handle
                // ASCII safety and unsafe-char replacement in a single pass below.
                // Non-ASCII characters that survived diacritical stripping will be
                // replaced with '-' (same as the previous ASCII-encoding approach).
                if (normalized > 127)
                {
                    sb.Append('-');
                    continue;
                }

                // Replace all unsafe characters with '-'
                bool isUnsafe = false;
                for (int j = 0; j < UnsafeChars.Length; j++)
                {
                    if (normalized == UnsafeChars[j])
                    {
                        isUnsafe = true;
                        break;
                    }
                }
                sb.Append(isUnsafe ? '-' : normalized);
            }

            return sb.ToString();
        }
    }
}
