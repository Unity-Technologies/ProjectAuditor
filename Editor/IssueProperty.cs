using System;

namespace Unity.ProjectAuditor.Editor
{
    public enum PropertyType
    {
        Description = 0,
        Severity,
        Area,
        Path,
        Filename,
        FileType,
        Custom
    }

    public static class IssueProperty
    {
        public static string GetProperty(this ProjectIssue issue, PropertyType propertyType)
        {
            switch (propertyType)
            {
                case PropertyType.Severity:
                    return issue.severity.ToString();
                case PropertyType.Area:
                    return issue.descriptor.area;
                case PropertyType.FileType:
                    var ext = issue.location.Extension;
                    if (ext.StartsWith("."))
                        ext = ext.Substring(1);
                    return ext;
                case PropertyType.Description:
                    return issue.description;
                case PropertyType.Filename:
                    var filename = string.Format("{0}", issue.filename);
                    if (issue.category == IssueCategory.Code)
                        filename += string.Format(":{0}", issue.line);
                    return filename;
                case PropertyType.Path:
                    var path = string.Format("{0}", issue.location.Path);
                    if (issue.category == IssueCategory.Code)
                        path += string.Format(":{0}", issue.line);
                    return path;
                default:
                    var propertyIndex = propertyType - PropertyType.Custom;
                    return issue.GetCustomProperty(propertyIndex);
            }
        }
    }
}
