using System;
using System.Runtime.CompilerServices;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    // Extension methods for ProjectIssues which don't form part of the API: Used in UI, Tests, and HTML/CSV exporters
    internal static class ProjectIssueExtensions
    {
        internal const string k_NotAvailable = "N/A";

        public static string GetContext(this ProjectIssue issue)
        {
            if (issue.Dependencies == null)
                return issue.RelativePath;

            var root = issue.Dependencies;
            return root.name;
        }

        public static string GetProperty(this ProjectIssue issue, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.LogLevel:
                    return issue.LogLevel.ToString();
                case PropertyType.Severity:
                    return issue.Severity.ToString();
                case PropertyType.Area:
                    return issue.Id.GetDescriptor().GetAreasSummary();
                case PropertyType.FileType:
                    if (issue.Location == null)
                        return k_NotAvailable;
                    var ext = issue.Location.Extension;
                    if (ext.StartsWith("."))
                        ext = ext.Substring(1);
                    return ext;
                case PropertyType.Description:
                    return issue.Description;
                case PropertyType.Descriptor:
                    return issue.Id.GetDescriptor().title;
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

        public static string GetPropertyGroup(this ProjectIssue issue, PropertyDefinition propertyDefinition)
        {
            switch (propertyDefinition.type)
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
                    if (propertyDefinition.format != PropertyFormat.String)
                        return string.Format("{0}: {1}", propertyDefinition.name, issue.GetProperty(propertyDefinition.type));
                    return issue.GetProperty(propertyDefinition.type);
            }
        }

        internal static int CompareTo(this ProjectIssue issueA, ProjectIssue issueB, PropertyType propertyType)
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
                case PropertyType.Area:
                    var areasA = issueA.Id.GetDescriptor().areas;
                    var areasB = issueB.Id.GetDescriptor().areas;

                    var minLength = Math.Min(areasA.Length, areasB.Length);

                    for (var i = 0; i < minLength; i++)
                    {
                        var ca = string.CompareOrdinal(areasA[i], areasB[i]);
                        if (ca != 0)
                            return ca;
                    }

                    return areasA.Length.CompareTo(areasB.Length);
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
