using System;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public static class ProjectIssueExtensions
    {
        const string k_NotAvailable = "N/A";

        public static string GetProperty(this ProjectIssue issue, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Severity:
                    return issue.severity.ToString();
                case PropertyType.Area:
                    return issue.descriptor.GetAreasSummary();
                case PropertyType.FileType:
                    var ext = issue.location.Extension;
                    if (ext.StartsWith("."))
                        ext = ext.Substring(1);
                    return ext;
                case PropertyType.Description:
                    return issue.description;
                case PropertyType.Filename:
                    var filename = string.Format("{0}", issue.filename);
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
    }
}
