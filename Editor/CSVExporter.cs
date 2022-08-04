using System;
using System.IO;
using System.Text;

namespace Unity.ProjectAuditor.Editor.Utils
{
    public class CSVExporter : IDisposable
    {
        readonly IssueLayout m_Layout;
        readonly StreamWriter m_StreamWriter;

        public CSVExporter(string path, IssueLayout layout)
        {
            m_Layout = layout;
            m_StreamWriter = new StreamWriter(path);
        }

        public void Dispose()
        {
            m_StreamWriter.Flush();
            m_StreamWriter.Close();
        }

        public void WriteHeader()
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < m_Layout.properties.Length; i++)
            {
                stringBuilder.Append(m_Layout.properties[i].name);
                if (i + 1 < m_Layout.properties.Length)
                    stringBuilder.Append(",");
            }
            m_StreamWriter.WriteLine(stringBuilder);
        }

        public void WriteIssues(ProjectIssue[] issues)
        {
            foreach (var issue in issues)
                WriteIssue(issue);
        }

        void WriteIssue(ProjectIssue issue)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < m_Layout.properties.Length; i++)
            {
                var columnType = m_Layout.properties[i].type;
                var prop = issue.GetProperty(columnType);

                stringBuilder.Append('"');
                stringBuilder.Append(prop);
                stringBuilder.Append('"');

                if (i + 1 < m_Layout.properties.Length)
                    stringBuilder.Append(",");
            }

            m_StreamWriter.WriteLine(stringBuilder);
        }
    }
}
