using Unity.ProjectAuditor.Editor.Diagnostic;

namespace Unity.ProjectAuditor.Editor.Core
{
    // stephenm TODO: We probably need to make this public (and document it properly) as a class to inherit from for people making custom modules. And move it to API/not Core. Phase 2.
    internal class AnalysisContext
    {
        public AnalysisParams Params;

        /// <summary>
        /// Create an IssueBuilder for a diagnostic issue
        /// </summary>
        /// <param name="category">Issue category</param>
        /// <param name="id">Descriptor ID</param>
        /// <param name="messageArgs">Arguments to be used in the message formatting</param>
        /// <returns>The IssueBuilder, constructed with the specified category, descriptor ID and message arguments</returns>
        internal IssueBuilder CreateIssue(IssueCategory category, string id, params object[] messageArgs)
        {
            return new IssueBuilder(category, id, messageArgs);
        }

        /// <summary>
        /// Create an IssueBuilder for a non-diagnostic insight
        /// </summary>
        /// <param name="category">Issue category</param>
        /// <param name="description">User-friendly description</param>
        /// <returns>The IssueBuilder, constructed with the specified category and description string</returns>
        internal IssueBuilder CreateInsight(IssueCategory category, string description)
        {
            return new IssueBuilder(category, description);
        }

        public bool IsDescriptorEnabled(Descriptor descriptor)
        {
            if (!descriptor.IsApplicable(Params))
                return false;

            var rule = Params.Rules.GetRule(descriptor.Id);
            if (rule != null)
                return rule.Severity != Severity.None;

            return descriptor.IsEnabledByDefault;
        }
    }
}
