using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal class IssueBuilder
    {
        ProjectIssue m_Issue;

        public static implicit operator ProjectIssue(IssueBuilder builder) => builder.m_Issue;

        public IssueBuilder(IssueCategory category, DescriptorId id, params object[] args)
        {
            m_Issue = new ProjectIssue(category, id, args);
        }

        public IssueBuilder(IssueCategory category, string description)
        {
            m_Issue = new ProjectIssue(category, description);
        }

        /// <summary>
        /// Initialize all custom properties to the same value
        /// </summary>
        /// <param name="numProperties"> total number of custom properties </param>
        /// <param name="property"> value the properties will be set to </param>
        public IssueBuilder WithCustomProperties(int numProperties, object property)
        {
            m_Issue.CustomProperties = new string[numProperties];
            for (var i = 0; i < numProperties; i++)
                m_Issue.CustomProperties[i] = property.ToString();
            return this;
        }

        /// <summary>
        /// Initialize custom properties
        /// </summary>
        /// <param name="properties"> Issue-specific properties </param>
        public IssueBuilder WithCustomProperties(object[] properties)
        {
            if (properties != null)
                m_Issue.CustomProperties = properties.Select(p => p != null ? p.ToString() : string.Empty).ToArray();
            else
                m_Issue.CustomProperties = null;

            return this;
        }

        public IssueBuilder WithDescription(string description)
        {
            m_Issue.Description = description;
            return this;
        }

        public IssueBuilder WithDependencies(DependencyNode dependencies)
        {
            m_Issue.Dependencies = dependencies;
            return this;
        }

        public IssueBuilder WithLocation(Location location)
        {
            m_Issue.Location = location;
            return this;
        }

        public IssueBuilder WithLocation(string path, int line = 0)
        {
            m_Issue.Location = new Location(path, line);
            return this;
        }

        public IssueBuilder WithLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Error:
                    m_Issue.Severity = Severity.Error;
                    break;
                case LogLevel.Warning:
                    m_Issue.Severity = Severity.Warning;
                    break;
                case LogLevel.Info:
                    m_Issue.Severity = Severity.Info;
                    break;
            }
            return this;
        }

        public IssueBuilder WithSeverity(Severity severity)
        {
            m_Issue.Severity = severity;
            return this;
        }
    }
}
