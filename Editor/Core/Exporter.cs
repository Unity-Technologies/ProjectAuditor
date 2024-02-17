using System;
using System.IO;
using System.Linq;

namespace Unity.ProjectAuditor.Editor.Core
{
    internal abstract class Exporter : IDisposable
    {
        readonly Report m_Report;

        protected StreamWriter m_StreamWriter;

        protected Exporter(Report report)
        {
            m_Report = report;
        }

        public void Export(string path, IssueCategory category, Func<ReportItem, bool> predicate = null)
        {
            m_StreamWriter = new StreamWriter(path);
            var issues = m_Report.FindByCategory(category);
            if (predicate != null)
                issues = issues.Where(predicate).ToList();
            var layout = m_Report.GetLayout(category);
            WriteHeader(layout);
            foreach (var issue in issues)
                WriteIssue(layout, issue);
            WriteFooter(layout);
        }

        public void Dispose()
        {
            if (m_StreamWriter == null)
                return;

            m_StreamWriter.Flush();
            m_StreamWriter.Close();
        }

        public virtual void WriteFooter(IssueLayout layout) {}

        public abstract void WriteHeader(IssueLayout layout);

        protected abstract void WriteIssue(IssueLayout layout, ReportItem issue);
    }
}
