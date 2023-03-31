using System.Linq;
using Unity.ProjectAuditor.Editor.Diagnostic;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.Core
{
    public class IssueBuilder
    {
        ProjectIssue m_Issue;

        public static implicit operator ProjectIssue(IssueBuilder builder) => builder.m_Issue;

        public IssueBuilder(IssueCategory category, Descriptor descriptor, params object[] args)
        {
            m_Issue = new ProjectIssue(category, descriptor, args);
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
            m_Issue.customProperties = new string[numProperties];
            for (var i = 0; i < numProperties; i++)
                m_Issue.customProperties[i] = property.ToString();
            return this;
        }

        /// <summary>
        /// Initialize custom properties
        /// </summary>
        /// <param name="properties"> Issue-specific properties </param>
        public IssueBuilder WithCustomProperties(object[] properties)
        {
            if (properties != null)
                m_Issue.customProperties = properties.Select(p => p != null ? p.ToString() : string.Empty).ToArray();
            else
                m_Issue.customProperties = null;

            return this;
        }

        public IssueBuilder WithDescription(string description)
        {
            m_Issue.description = description;
            return this;
        }

        public IssueBuilder WithDependencies(DependencyNode dependencies)
        {
            m_Issue.dependencies = dependencies;
            return this;
        }

        public IssueBuilder WithDepth(int depth)
        {
            m_Issue.depth = depth;
            return this;
        }

        public IssueBuilder WithLocation(Location location)
        {
            m_Issue.location = location;
            return this;
        }

        public IssueBuilder WithLocation(string path, int line = 0)
        {
            m_Issue.location = new Location(path, line);
            return this;
        }

        public IssueBuilder WithLogLevel(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Error:
                    m_Issue.severity = Severity.Error;
                    break;
                case LogLevel.Warning:
                    m_Issue.severity = Severity.Warning;
                    break;
                case LogLevel.Info:
                    m_Issue.severity = Severity.Info;
                    break;
            }
            return this;
        }

        public IssueBuilder WithSeverity(Severity severity)
        {
            m_Issue.severity = severity;
            return this;
        }
    }
}
