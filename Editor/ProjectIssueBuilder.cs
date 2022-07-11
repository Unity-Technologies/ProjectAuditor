using System.Linq;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor
{
    public class ProjectIssueBuilder
    {
        ProjectIssue m_Issue;

        public static implicit operator ProjectIssue(ProjectIssueBuilder builder) => builder.m_Issue;

        internal ProjectIssueBuilder(IssueCategory category, ProblemDescriptor descriptor, params object[] args)
        {
            m_Issue = new ProjectIssue(category, descriptor, args);
        }

        internal ProjectIssueBuilder(IssueCategory category, string description)
        {
            m_Issue = new ProjectIssue(category, description);
        }

        /// <summary>
        /// Initialize all custom properties to the same value
        /// </summary>
        /// <param name="numProperties"> total number of custom properties </param>
        /// <param name="property"> value the properties will be set to </param>
        public ProjectIssueBuilder WithCustomProperties(int numProperties, object property)
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
        public ProjectIssueBuilder WithCustomProperties(object[] properties)
        {
            if (properties != null)
                m_Issue.customProperties = properties.Select(p => p != null ? p.ToString() : string.Empty).ToArray();
            else
                m_Issue.customProperties = null;

            return this;
        }

        public ProjectIssueBuilder WithDescription(string description)
        {
            m_Issue.description = description;
            return this;
        }

        public ProjectIssueBuilder WithDependencies(DependencyNode dependencies)
        {
            m_Issue.dependencies = dependencies;
            return this;
        }

        internal ProjectIssueBuilder WithDepth(int depth)
        {
            m_Issue.depth = depth;
            return this;
        }

        public ProjectIssueBuilder WithLocation(Location location)
        {
            m_Issue.location = location;
            return this;
        }

        public ProjectIssueBuilder WithSeverity(Rule.Severity severity)
        {
            m_Issue.severity = severity;
            return this;
        }
    }
}
