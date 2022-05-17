using System;
using System.Runtime.CompilerServices;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class ProjectIssueExtensions
    {
        internal const string k_NotAvailable = "N/A";

        public static string GetContext(this ProjectIssue issue)
        {
            if (issue.dependencies == null)
                return string.Empty;

            var root = issue.dependencies;
            return root.name;
        }

        public static string GetProperty(this ProjectIssue issue, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Severity:
                    return issue.severity.ToString();
                case PropertyType.Area:
                    return issue.descriptor.GetAreasSummary();
                case PropertyType.FileType:
                    if (issue.location == null)
                        return k_NotAvailable;
                    var ext = issue.location.Extension;
                    if (ext.StartsWith("."))
                        ext = ext.Substring(1);
                    return ext;
                case PropertyType.Description:
                    return issue.description;
                case PropertyType.Filename:
                    if (string.IsNullOrEmpty(issue.filename))
                        return k_NotAvailable;
                    return issue.location.FormattedFilename;
                case PropertyType.Path:
                    if (string.IsNullOrEmpty(issue.relativePath))
                        return k_NotAvailable;
                    return issue.location.FormattedPath;
                case PropertyType.CriticalContext:
                    return issue.isPerfCriticalContext.ToString();
                default:
                    var propertyIndex = propertyType - PropertyType.Num;
                    return issue.GetCustomProperty(propertyIndex);
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
                case PropertyType.Severity:
                    return issueA.severity.CompareTo(issueB.severity);
                case PropertyType.Area:
                    var areasA = issueA.descriptor.areas;
                    var areasB = issueB.descriptor.areas;
                    var minLength = Math.Min(areasA.Length, areasB.Length);

                    for (var i = 0; i < minLength; i++)
                    {
                        var ca = string.CompareOrdinal(areasA[i], areasB[i]);
                        if (ca != 0)
                            return ca;
                    }

                    return areasA.Length.CompareTo(areasB.Length);
                case PropertyType.Description:
                    return string.CompareOrdinal(issueA.description, issueB.description);
                case PropertyType.FileType:
                {
                    var pathA = issueA.relativePath;
                    var pathB = issueB.relativePath;

                    var extAIndex = PathUtils.GetExtensionIndexFromPath(pathA);
                    var extBIndex = PathUtils.GetExtensionIndexFromPath(pathB);

                    return string.CompareOrdinal(pathA, extAIndex, pathB, extBIndex, Math.Max(pathA.Length, pathB.Length));
                }
                case PropertyType.Filename:
                {
                    var pathA = issueA.relativePath;
                    var pathB = issueB.relativePath;

                    var filenameAIndex = PathUtils.GetFilenameIndexFromPath(pathA);
                    var filenameBIndex = PathUtils.GetFilenameIndexFromPath(pathB);

                    var cf = string.CompareOrdinal(pathA, filenameAIndex, pathB, filenameBIndex, Math.Max(pathA.Length, pathB.Length));

                    // If it's the same filename, see if the lines are different
                    if (cf == 0)
                        return issueA.line.CompareTo(issueB.line);

                    return cf;
                }
                case PropertyType.Path:
                    var cp = string.CompareOrdinal(issueA.relativePath ?? string.Empty, issueB.relativePath ?? string.Empty);

                    // If it's the same path, see if the lines are different
                    if (cp == 0)
                        return issueA.line.CompareTo(issueB.line);

                    return cp;
                case PropertyType.CriticalContext:
                    return issueA.isPerfCriticalContext.CompareTo(issueB.isPerfCriticalContext);
                default:
                    var propertyIndex = propertyType - PropertyType.Num;

                    var propA = issueA.GetCustomProperty(propertyIndex);
                    var propB = issueB.GetCustomProperty(propertyIndex);

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
