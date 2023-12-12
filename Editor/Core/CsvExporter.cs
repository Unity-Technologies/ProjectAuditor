using System;
using System.IO;
using System.Text;

namespace Unity.ProjectAuditor.Editor.Core
{
    class CsvExporter : Exporter
    {
        public CsvExporter(string path, IssueLayout layout) : base(path, layout) {}

        public override void WriteHeader()
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < m_Layout.Properties.Length; i++)
            {
                stringBuilder.Append(m_Layout.Properties[i].Name);
                if (i + 1 < m_Layout.Properties.Length)
                    stringBuilder.Append(",");
            }
            m_StreamWriter.WriteLine(stringBuilder);
        }

        protected override void WriteIssue(ProjectIssue issue)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < m_Layout.Properties.Length; i++)
            {
                var columnType = m_Layout.Properties[i].Type;
                var prop = issue.GetProperty(columnType);

                stringBuilder.Append('"');
                stringBuilder.Append(prop);
                stringBuilder.Append('"');

                if (i + 1 < m_Layout.Properties.Length)
                    stringBuilder.Append(",");
            }
            m_StreamWriter.WriteLine(stringBuilder);
        }
    }
}
