using System;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class ProjectIssueExtensions
    {
        internal const string k_NotAvailable = "N/A";

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
                    var filename = issue.filename;
                    if (string.IsNullOrEmpty(filename))
                        return k_NotAvailable;
                    if (filename.EndsWith(".cs"))
                        filename += string.Format(":{0}", issue.line);
                    return filename;
                case PropertyType.Path:
                    var path = string.Format("{0}", issue.relativePath);
                    if (string.IsNullOrEmpty(path))
                        return k_NotAvailable;
                    if (path.EndsWith(".cs"))
                        path += string.Format(":{0}", issue.line);
                    return path;
                case PropertyType.CriticalContext:
                    return issue.isPerfCriticalContext.ToString();
                default:
                    var propertyIndex = propertyType - PropertyType.Num;
                    return issue.GetCustomProperty(propertyIndex);
            }
        }

        public static int CompareTo(this ProjectIssue issueA, ProjectIssue issueB, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Severity:
                    return issueA.severity.CompareTo(issueB.severity);
                case PropertyType.Area:
                    var areasA = issueA.descriptor.areas;
                    var areasB = issueB.descriptor.areas;
                    var minLength = Math.Min(areasA.Length, areasB.Length);

                    for (int i = 0; i < minLength; i++)
                    {
                        var ca = string.CompareOrdinal(areasA[i], areasB[i]);
                        if (ca != 0)
                            return ca;
                    }

                    return areasA.Length.CompareTo(areasB.Length);
                case PropertyType.Description:
                    return string.CompareOrdinal(issueA.description, issueB.description);
                case PropertyType.FileType:

                    var pathA = issueA?.location?.Path ?? string.Empty;
                    var pathB = issueB?.location?.Path ?? string.Empty;

                    var extAIndex = GetExtensionIndexFromPath(pathA);
                    var extBIndex = GetExtensionIndexFromPath(pathB);

                    return string.CompareOrdinal(pathA, extAIndex, pathB, extBIndex, Math.Max(pathA.Length, pathB.Length));
                case PropertyType.Filename:
                    var filenameA = issueA?.location?.Path ?? string.Empty;
                    var filenameB = issueB?.location?.Path ?? string.Empty;

                    var filenameAIndex = GetFilenameIndexFromPath(filenameA);
                    var filenameBIndex = GetFilenameIndexFromPath(filenameB);

                    var cf = string.CompareOrdinal(filenameA, filenameAIndex, filenameB, filenameBIndex, Math.Max(filenameA.Length, filenameB.Length));
					
					// If it's the same filename, see if the lines are different
                    if (cf == 0)
                        return issueA.line.CompareTo(issueB.line);

                    return cf;
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
                    if (int.TryParse(propA, out var intA) && int.TryParse(propB, out var intB))
                        return intA.CompareTo(intB);

                    return string.CompareOrdinal(propA, propB);
            }
        }

        static char DirectorySeparatorChar = System.IO.Path.DirectorySeparatorChar;
        static char AltDirectorySeparatorChar = System.IO.Path.AltDirectorySeparatorChar;
        static char VolumeSeparatorChar = System.IO.Path.VolumeSeparatorChar;

        private static int GetExtensionIndexFromPath(string path)
        {
            int length = path.Length;
            int num = length;
            while (--num >= 0)
            {
                char c = path[num];
                if (c == '.')
                {
                    if (num != length - 1)
                    {
                        return num;
                    }

                    return length - 1;
                }

                if (c == System.IO.Path.DirectorySeparatorChar || c == System.IO.Path.AltDirectorySeparatorChar || c == System.IO.Path.VolumeSeparatorChar)
                {
                    return length - 1;
                }
            }
            return length - 1;
        }

        private static int GetFilenameIndexFromPath(string path)
        {
            int length = path.Length;
            int num = length;
            while (--num >= 0)
            {
                char c = path[num];
                if (c == DirectorySeparatorChar || c == AltDirectorySeparatorChar || c == VolumeSeparatorChar)
                {
                    return num + 1;
                }
            }
            return 0;
        }
    }
}
