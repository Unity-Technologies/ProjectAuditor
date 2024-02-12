using System;
using NUnit.Framework;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.UI.Framework;

namespace Unity.ProjectAuditor.EditorTests
{
    class UtilityTests
    {
        [Test]
        public void UI_Utility_GeneralIconsAreAvailable()
        {
            foreach (Utility.IconType iconType in Enum.GetValues(typeof(Utility.IconType)))
            {
                var iconContent = Utility.GetIcon(iconType);
                Assert.IsNotNull(iconContent, $"Icon for {iconType} is not available.");
            }
        }

        [Test]
        public void UI_Utility_LogLevelIconsAreAvailable()
        {
            foreach (LogLevel logLevel in Enum.GetValues(typeof(LogLevel)))
            {
                var iconContent = Utility.GetLogLevelIcon(logLevel);
                Assert.IsNotNull(iconContent, $"Icon for LogLevel {logLevel} is not available.");
            }
        }

        [Test]
        public void UI_Utility_SeverityIconsAreAvailable()
        {
            foreach (var severity in Enum.GetValues(typeof(Severity)))
            {
                var iconContent = Utility.GetSeverityIconWithText((Severity)severity);
                Assert.IsNotNull(iconContent, $"Icon with text for Severity {severity} is not available.");
            }
        }
    }
}
