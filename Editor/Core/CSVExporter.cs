using System;
using System.IO;
using System.Text;

namespace Unity.ProjectAuditor.Editor.Core
{
    public class CSVExporter : Exporter
    {
        public CSVExporter(string path, IssueLayout layout) : base(path, layout) {}

        public override void WriteHeader()
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

        protected override void WriteIssue(ProjectIssue issue)
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
