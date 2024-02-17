using System;
using System.Globalization;
using System.Text.RegularExpressions;
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
            return $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        }

        /// <summary>
        /// Formats a given TimeSpan object as a string in the format "HH:mm:ss".
        /// </summary>
        /// <param name="timeSpan">The TimeSpan object to format.</param>
        /// <returns>A string representation of the input value.</returns>
        public static string FormatDurationWithMs(TimeSpan timeSpan)
        {
            return $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds:000}";
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
        /// Formats a decimal number as a percentage with a specified number of decimal places.
        /// </summary>
        /// <param name="number">The decimal number to format.</param>
        /// <param name="numDecimalPlaces">Number of decimals.</param>
        /// <returns>A string representation of the decimal number as a percentage.</returns>
        public static string FormatPercentage(float number, int numDecimalPlaces = 0)
        {
            var formatString = $"{{0:F{numDecimalPlaces}}}";
            return string.Format(CultureInfo.InvariantCulture.NumberFormat, formatString, (100.0f * number)) + "%";
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

        /// <summary>
        /// Formats a given frequency as a string in the format "X Hz" or "X kHz".
        /// </summary>
        /// <param name="size">Frequency value to format.</param>
        /// <returns>A string representation of the input value as a frequency in Hz or kHz.</returns>
        public static string FormatHz(int frequency)
        {
            return (frequency < 1000) ? $"{frequency} Hz" : $"{((float)frequency / 1000.0f):G0} kHz";
        }

        /// <summary>
        /// Formats a given float duration as a string in the format "X.XXX s".
        /// </summary>
        /// <param name="length">Length value to format.</param>
        /// <returns>A string representation of the input value as a duration in seconds.</returns>
        public static string FormatLengthInSeconds(float length)
        {
            return length.ToString("F3") + " s";
        }

        /// <summary>
        /// Formats a given float framerate as a string in the format "X fps".
        /// </summary>
        /// <param name="framerate">Framerate value to format.</param>
        /// <returns>A string representation of the input value as a framerate.</returns>
        public static string FormatFramerate(float framerate)
        {
            return framerate + " fps";
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

        public static string StripRichTextTags(string text)
        {
            text = RemoveRichTextTag(text, "b", string.Empty);
            text = RemoveRichTextTag(text, "i", string.Empty);
            text = RemoveRichTextTag(text, "u", string.Empty);
            text = RemoveRichTextTag(text, "color", string.Empty);

            return text;
        }

        static string RemoveRichTextTag(string input, string tagName, string replaceWith)
        {
            const string k_RichTextTagRegExp = "</?{0}[^<]*?>";

            var reg = new Regex(String.Format(k_RichTextTagRegExp, tagName), RegexOptions.IgnoreCase);
            return reg.Replace(input, replaceWith);
        }
    }
}
