using System;
using System.Text.RegularExpressions;

namespace Phagocyte.Core;

public static class TextFormatter
{
    private static readonly Regex FormatRegex =
        new Regex(@"%(?:(\d+)\$)?([-+# 0]*)?(\d+)?(?:\.(\d+))?([%dfs])", RegexOptions.Compiled);

    public static string Format(string template, params object[] args)
    {
        if (string.IsNullOrEmpty(template) || args == null || args.Length == 0)
            return template ?? "";

        int argIndex = 0;
        return FormatRegex.Replace(template, match =>
        {
            string specifier = match.Groups[5].Value;
            if (specifier == "%") return "%";

            if (argIndex >= args.Length) return match.Value;
            object arg = args[argIndex++];

            if (specifier == "d")
            {
                return Convert.ToInt64(arg).ToString();
            }
            else if (specifier == "f")
            {
                string precisionStr = match.Groups[4].Value;
                if (int.TryParse(precisionStr, out int precision))
                {
                    return Convert.ToDouble(arg).ToString("F" + precision);
                }
                return Convert.ToDouble(arg).ToString();
            }
            else if (specifier == "s")
            {
                return arg?.ToString() ?? "";
            }
            return arg?.ToString() ?? match.Value;
        });
    }
}
