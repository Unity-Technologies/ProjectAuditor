using System;
using UnityEditor;

namespace Unity.ProjectAuditor.Editor.Utils
{
    internal static class Formatting
    {
        /// <summary>
        /// Formats a given DateTime object as a string in the format "yyyy/MM/dd HH:mm".
        /// </summary>
        /// <param name="dateTime">The DateTime object to format.</param>
        /// <returns>A string representation of the input DateTime object in the specified format.</returns>
        public static string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString("yyyy/MM/dd HH:mm");
        }

        /// <summary>
        /// Formats a given TimeSpan object as a string in the format "HH:mm:ss".
        /// </summary>
        /// <param name="timeSpan">The TimeSpan object to format.</param>
        /// <returns>A string representation of the input value.</returns>
        public static string FormatDuration(TimeSpan timeSpan)
        {
            return timeSpan.Hours + ":" + timeSpan.Minutes.ToString("D2") + ":" + timeSpan.Seconds.ToString("D2");
        }

        /// <summary>
        /// Formats a given TimeSpan object as a string in the format "X ms", "X s", or "X min", depending on the length of the time span.
        /// </summary>
        /// <param name="timeSpan">The TimeSpan object to format.</param>
        /// <returns>A string representation of the input TimeSpan object.</returns>
        public static string FormatTime(TimeSpan timeSpan)
        {
            var timeMs = timeSpan.TotalMilliseconds;
            if (timeMs < 1000)
                return timeMs.ToString("F1") + " ms";
            if (timeMs < 60000)
                return timeSpan.TotalSeconds.ToString("F2") + " s";
            return timeSpan.TotalMinutes.ToString("F2") + " min";
        }

        /// <summary>
        /// Formats a given time value as a string in the format "X ms", "X s", or "X min"
        /// </summary>
        /// <param name="timeMs">The time value to format, in milliseconds.</param>
        /// <returns>A string representation of the input float value.</returns>
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

        /// <summary>
        /// Formats a given size in bytes as a string in the format "X bytes".
        /// </summary>
        /// <param name="size">Size value to format.</param>
        /// <returns>A string representation of the input value as a size.</returns>
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
