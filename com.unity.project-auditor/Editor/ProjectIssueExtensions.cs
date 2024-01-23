using System;
using System.Runtime.CompilerServices;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    // Extension methods for ProjectIssues which don't form part of the API: Used in UI, Tests, and HTML/CSV exporters
    internal static class ProjectIssueExtensions
    {
        internal const string k_NotAvailable = "N/A";

        // -2 because we're not interested in "None" or "All"
        static readonly int s_NumAreaEnumValues = Enum.GetNames(typeof(Areas)).Length - 2;

        public static string GetContext(this ReportItem issue)
        {
            if (issue.Dependencies == null)
                return issue.RelativePath;

            var root = issue.Dependencies;
            return root.Name;
        }

        public static string GetProperty(this ReportItem issue, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.LogLevel:
                    return issue.LogLevel.ToString();
                case PropertyType.Severity:
                    return issue.Severity.ToString();
                case PropertyType.Areas:
                    return issue.Id.GetDescriptor().GetAreasSummary();
                case PropertyType.FileType:
                    if (issue.Location == null)
                        return k_NotAvailable;
                    return issue.Location.Extension;
                case PropertyType.Description:
                    return issue.Description;
                case PropertyType.Descriptor:
                    return issue.Id.GetDescriptor().Title;
                case PropertyType.Filename:
                    if (string.IsNullOrEmpty(issue.Filename))
                        return k_NotAvailable;
                    return issue.Location.FormattedFilename;
                case PropertyType.Path:
                    if (string.IsNullOrEmpty(issue.RelativePath))
                        return k_NotAvailable;
                    return issue.Location.FormattedPath;
                case PropertyType.Directory:
                    if (string.IsNullOrEmpty(issue.RelativePath))
                        return k_NotAvailable;
                    return PathUtils.GetDirectoryName(issue.Location.Path);
                case PropertyType.Platform:
                    return issue.Id.GetDescriptor().GetPlatformsSummary();
                default:
                    var propertyIndex = propertyType - PropertyType.Num;
                    return issue.GetCustomProperty(propertyIndex);
            }
        }

        public static string GetPropertyGroup(this ReportItem issue, PropertyDefinition propertyDefinition)
        {
            switch (propertyDefinition.Type)
            {
                case PropertyType.Filename:
                    if (string.IsNullOrEmpty(issue.Filename))
                        return k_NotAvailable;
                    return issue.Location.Filename;
                case PropertyType.Path:
                    if (string.IsNullOrEmpty(issue.RelativePath))
                        return k_NotAvailable;
                    return issue.Location.Path;
                default:
                    if (propertyDefinition.Format != PropertyFormat.String)
                        return string.Format("{0}: {1}", propertyDefinition.Name, issue.GetProperty(propertyDefinition.Type));
                    return issue.GetProperty(propertyDefinition.Type);
            }
        }

        internal static int CompareTo(this ReportItem issueA, ReportItem issueB, PropertyType propertyType)
        {
            if (issueA == null && issueB == null)
                return 0;
            if (issueA == null)
                return -1;
            if (issueB == null)
                return 1;

            switch (propertyType)
            {
                case PropertyType.LogLevel:
                    return issueA.LogLevel.CompareTo(issueB.LogLevel);
                case PropertyType.Severity:
                    return issueA.Severity.CompareTo(issueB.Severity);
                case PropertyType.Areas:
                    var areasA = (int)issueA.Id.GetDescriptor().Areas;
                    var areasB = (int)issueB.Id.GetDescriptor().Areas;

                    if (areasA == areasB)
                        return 0;

                    // Sort according to differences in the least significant bit
                    // (i.e. the smallest enum value, which is the one that comes first alphabetically)
                    for (int i = 0; i < s_NumAreaEnumValues; ++i)
                    {
                        var mask = 1 << i;
                        var c = (areasB & mask) - (areasA & mask);
                        if (c != 0)
                            return c;
                    }
                    return 0;
                case PropertyType.Description:
                    return string.CompareOrdinal(issueA.Description, issueB.Description);
                case PropertyType.FileType:
                {
                    var pathA = issueA.RelativePath;
                    var pathB = issueB.RelativePath;

                    var extAIndex = PathUtils.GetExtensionIndexFromPath(pathA);
                    var extBIndex = PathUtils.GetExtensionIndexFromPath(pathB);

                    return string.CompareOrdinal(pathA, extAIndex, pathB, extBIndex, Math.Max(pathA.Length, pathB.Length));
                }
                case PropertyType.Filename:
                {
                    var pathA = issueA.RelativePath;
                    var pathB = issueB.RelativePath;

                    var filenameAIndex = PathUtils.GetFilenameIndexFromPath(pathA);
                    var filenameBIndex = PathUtils.GetFilenameIndexFromPath(pathB);

                    var cf = string.CompareOrdinal(pathA, filenameAIndex, pathB, filenameBIndex, Math.Max(pathA.Length, pathB.Length));

                    // If it's the same filename, see if the lines are different
                    if (cf == 0)
                        return issueA.Line.CompareTo(issueB.Line);

                    return cf;
                }
                case PropertyType.Path:
                    var cp = string.CompareOrdinal(issueA.RelativePath ?? string.Empty, issueB.RelativePath ?? string.Empty);

                    // If it's the same path, see if the lines are different
                    if (cp == 0)
                        return issueA.Line.CompareTo(issueB.Line);

                    return cp;
                default:
                    var propA = issueA.GetProperty(propertyType);
                    var propB = issueB.GetProperty(propertyType);

                    // Maybe instead of parsing, just assume the values are numbers and do it inplace?
                    //if (int.TryParse(propA, out var intA) && int.TryParse(propB, out var intB))
                    //    return intA.CompareTo(intB);

                    //return string.CompareOrdinal(propA, propB);
                    return UnsafeIntStringComparison(propA, propB);
            }
        }

#if NET_4_6
        /// <summary>
        /// Compares two strings, assuming they are both containing numerical characters only, skipping a lot of safety checks
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        static int UnsafeIntStringComparison(string stringA, string stringB)
        {
            // Compare string length (simple and fast comparison)
            var c = stringA.Length.CompareTo(stringB.Length);

            // If length is same, compare char-by-char, else use result of previous comparison
            return c == 0 ? string.CompareOrdinal(stringA, stringB) : c;
        }
    }
}
