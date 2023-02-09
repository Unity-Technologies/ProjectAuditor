using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class Formatting
    {
        public static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd HH:mm");
        }

        public static string FormatDuration(TimeSpan t)
        {
            return t.Hours + ":" + t.Minutes.ToString("D2") + ":" + t.Seconds.ToString("D2");
        }

        public static string FormatTime(TimeSpan timeSpan)
        {
            var timeMs = timeSpan.TotalMilliseconds;
            if (timeMs < 1000)
                return timeMs.ToString("F1") + " ms";
            if (timeMs < 60000)
                return timeSpan.TotalSeconds.ToString("F2") + " s";
            return timeSpan.TotalMinutes.ToString("F2") + " min";
        }

        public static string FormatTime(float timeMs)
        {
            if (float.IsNaN(timeMs))
                return "NaN";
            return FormatTime(TimeSpan.FromMilliseconds(timeMs));
        }

        /// <summary>
        /// Formats a decimal number as a percentage with one decimal place.
        /// </summary>
        /// <param name="number">The decimal number to format.</param>
        /// <returns>A string representation of the decimal number as a percentage.</returns>
        public static string FormatPercentage(float number)
        {
            return $"{number:P1}";
        }

        public static string FormatSize(ulong size)
        {
            return EditorUtility.FormatBytes((long)size);
        }

        static readonly string k_StringSeparator = ", ";

        public static string CombineStrings(string[] strings, string separator = null)
        {
            return string.Join(separator ?? k_StringSeparator, strings);
        }

        public static string[] SplitStrings(string combinedString, string separator = null)
        {
            return combinedString.Split(new[] {separator ?? k_StringSeparator}, StringSplitOptions.None);
        }

        public static string ReplaceStringSeparators(string combinedString, string separator)
        {
            return combinedString.Replace(k_StringSeparator, separator);
        }
    }
}
