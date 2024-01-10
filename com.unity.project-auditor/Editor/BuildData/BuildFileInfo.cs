using System.Collections.Generic;
using System.IO;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class BuildFileInfo
    {
        public static string BaseFolder { get; set; }
        public string AbsolutePath { get; }
        public string Filename { get; }
        public string RelativePath { get; }

        public string DisplayName { get; private set; }

        public BuildFileInfo ArchiveFile { get; private set; }
        public long Size { get; }
        public bool IsArchive => m_ArchivedFiles != null;
        public bool IsInArchive => ArchiveFile != null;

        List<BuildFileInfo> m_ArchivedFiles;
        public IReadOnlyList<BuildFileInfo> ArchivedFiles => m_ArchivedFiles;

        public BuildFileInfo(string absolutePath, long size, bool isArchive)
        {
            AbsolutePath = absolutePath;
            Size = size;
            Filename = Path.GetFileName(absolutePath);
            RelativePath = GetRelativePath(BaseFolder, AbsolutePath);
            DisplayName = RelativePath;

            if (isArchive)
            {
                m_ArchivedFiles = new List<BuildFileInfo>();
            }
        }

        public BuildFileInfo AddArchivedFile(string path, long size)
        {
            var fileInfo = new BuildFileInfo(path, size, false);
            fileInfo.ArchiveFile = this;
            fileInfo.DisplayName = $"{RelativePath} ({fileInfo.Filename})";

            m_ArchivedFiles.Add(fileInfo);

            return fileInfo;
        }

        string GetRelativePath(string folder, string path)
        {
            var stdFolder = folder.Replace("\\", "/");
            if (!stdFolder.EndsWith("/"))
                stdFolder += "/";
            var stdPath = path.Replace("\\", "/");

            if (string.Compare(stdFolder, 0, stdPath, 0, stdFolder.Length) != 0)
                return path;

            return stdPath.Remove(0, stdFolder.Length);
        }
    }
}
