using System;
using System.IO;
using System.Text;
using Unity.ProjectAuditor.Editor;

namespace Editor.Utils
{
    public class Exporter : IDisposable
    {
        readonly PropertyType[] m_PropertyTypes;
        readonly StreamWriter m_StreamWriter;

        public Exporter(string path, PropertyType[] propertyTypes)
        {
            m_PropertyTypes = propertyTypes;
            m_StreamWriter = new StreamWriter(path);
        }

        public void Dispose()
        {
            m_StreamWriter.Flush();
            m_StreamWriter.Close();
        }

        public string ColumnIndexToName(int i, string[] customPropertyNames)
        {
            var columnType = m_PropertyTypes[i];
            if (columnType < PropertyType.Custom)
                return columnType.ToString();
            return customPropertyNames[columnType - PropertyType.Custom];
        }

        public void WriteHeader(string[] customPropertyNames)
        {
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < m_PropertyTypes.Length; i++)
            {
                stringBuilder.Append(ColumnIndexToName(i, customPropertyNames));
                if (i+1 < m_PropertyTypes.Length)
                    stringBuilder.Append(",");
            }
            m_StreamWriter.WriteLine(stringBuilder);
        }

        public void WriteIssue(ProjectIssue issue)
        {
            var stringBuilder = new StringBuilder();
            for (int i = 0; i < m_PropertyTypes.Length; i++)
            {
                var columnType = m_PropertyTypes[i];
                var prop = issue.GetProperty(columnType);
                stringBuilder.Append(prop);
                if (i+1 < m_PropertyTypes.Length)
                    stringBuilder.Append(",");
            }

            m_StreamWriter.WriteLine(stringBuilder);
        }
    }
}
