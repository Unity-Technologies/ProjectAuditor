using System;
using System.IO;


namespace Unity.ProjectAuditor.Editor
{
    public class HTMLExporter : Exporter
    {
        public HTMLExporter(string path, IssueLayout layout) : base(path, layout) { }

        public override void WriteHeader()
        {
            m_StreamWriter.Write(@"<html>" + m_StreamWriter.NewLine + @"<body>" + m_StreamWriter.NewLine);
            m_StreamWriter.Write(@"<table width='50%' cellpadding='10' style='margin-top:10px' cellspacing='3' border='1' rules='all'>" + m_StreamWriter.NewLine + @"<tr>" + m_StreamWriter.NewLine);
            for (var i = 0; i < m_Layout.properties.Length; i++)
            {
                m_StreamWriter.WriteLine(@"<th>" + m_Layout.properties[i].name + @"</th>");
            }
            m_StreamWriter.WriteLine(@"</tr>");
        }

        protected override void WriteIssue(ProjectIssue issue)
        {
            m_StreamWriter.WriteLine(@"<tr>");
            for (var i = 0; i < m_Layout.properties.Length; i++)
            {
                var columnType = m_Layout.properties[i].type;
                var prop = issue.GetProperty(columnType);
                m_StreamWriter.WriteLine(@"<td>" + prop + @"</td>");
            }
            m_StreamWriter.WriteLine(@"</tr>");
            //m_StreamWriter.Write(@"</body>" + m_StreamWriter.NewLine + @"</html>" + m_StreamWriter.NewLine);
        }

        public override void WriteFooter() {
            m_StreamWriter.Write(@"</body>" + m_StreamWriter.NewLine + @"</html>" + m_StreamWriter.NewLine);

        }
    }

}
