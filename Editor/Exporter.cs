using System;
using System.IO;

namespace Unity.ProjectAuditor.Editor
{
    public abstract class Exporter : IDisposable
    {
        protected readonly IssueLayout m_Layout;
        protected readonly StreamWriter m_StreamWriter;

        protected Exporter(string path, IssueLayout layout)
        {
            m_Layout = layout;
            m_StreamWriter = new StreamWriter(path);
        }

        public void Dispose()
        {
            m_StreamWriter.Flush();
            m_StreamWriter.Close();
        }

        public abstract void WriteHeader();

        public void WriteIssues(ProjectIssue[] issues)
        {
            foreach (var issue in issues)
                WriteIssue(issue);
        }

        protected abstract void WriteIssue(ProjectIssue issue);

        public virtual void WriteFooter() {}
    }
}
