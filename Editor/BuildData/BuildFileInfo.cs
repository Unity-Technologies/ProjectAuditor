namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class BuildFileInfo
    {
        public string Path { get; }
        public long Size { get; }
        public bool IsAssetBundle { get; }

        public BuildFileInfo(string path, long size, bool isAssetBundle)
        {
            Path = path;
            Size = size;
            IsAssetBundle = isAssetBundle;
        }
    }
}
