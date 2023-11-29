using System;
using System.IO;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class Analyzer
    {
        PPtrResolver m_PPtrResolver = new PPtrResolver();
        Regex m_RegexSceneFile = new Regex(@"BuildPlayer-([^\.]+)(?:\.sharedAssets)?");

        public BuildObjects Analyze(string path, string searchPattern)
        {
            var buildObjects = new BuildObjects();
            var files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
            int lastLength = 0;

            BuildFileInfo.BaseFolder = path;

            m_PPtrResolver.Reset();

            foreach (var file in files)
            {
                try
                {
                    using (var archive = UnityFileSystem.MountArchive(file, "archive:" + Path.DirectorySeparatorChar))
                    {
                        var archiveFileInfo = new BuildFileInfo(file, new FileInfo(file).Length, true);

                        foreach (var node in archive.Nodes)
                        {
                            if (node.Flags.HasFlag(ArchiveNodeFlags.SerializedFile))
                            {
                                var fileInfo = archiveFileInfo.AddArchivedFile(Path.Combine("archive:", node.Path), node.Size);
                                AnalyzeSerializedFile(fileInfo, buildObjects);
                            }
                        }
                    }
                }
                catch (NotSupportedException)
                {
                    // It wasn't an AssetBundle, try to open the file as a SerializedFile.

                    var fileInfo = new BuildFileInfo(file, new FileInfo(file).Length, false);

                    try
                    {
                        AnalyzeSerializedFile(fileInfo, buildObjects);
                    }
                    catch (NotSupportedException e)
                    {
                    }
                }
            }

            UnityFileReader.ClearBufferPool();

            return buildObjects;
        }

        void AnalyzeSerializedFile(BuildFileInfo fileInfo, BuildObjects buildObjects)
        {
            using var sf = UnityFileSystem.OpenSerializedFile(fileInfo.AbsolutePath);
            using var reader = new UnityFileReader(fileInfo.AbsolutePath, 64 * 1024 * 1024);
            var folder = fileInfo.IsInArchive ? "archive:" : Path.GetDirectoryName(fileInfo.AbsolutePath);
            using var processor = new PPtrAndCrcProcessor(sf, reader, folder, m_PPtrResolver, buildObjects);
            int sceneId = -1;

            /*var match = m_RegexSceneFile.Match(filename);

            if (match.Success)
            {
                var sceneName = match.Groups[1].Value;

                // There is no Scene object in Unity (a Scene is the full content of a
                // SerializedFile). We generate an object id using the name of the Scene
                // as SerializedFile name, and the object id 0.
                sceneId = m_ObjectIdProvider.GetId((m_SerializedFileIdProvider.GetId(sceneName), 0));

                // There are 2 SerializedFiles per Scene, one ends with .sharedAssets. This is a
                // dirty trick to avoid inserting the scene object a second time.
                if (filename.EndsWith(".sharedAssets"))
                {
                   // TODO: handle scenes
                }
            }*/

            m_PPtrResolver.BeginSerializedFile(fileInfo.Filename, sf.ExternalReferences);

            foreach (var obj in sf.Objects)
            {
                var root = sf.GetTypeTreeRoot(obj.Id);
                var offset = obj.Offset;
                uint crc32 = processor.Process(obj, root);

                var typeTreeReader = new TypeTreeReader(sf, root, reader, offset);

                var serializedObject = ReadSerializedObject(obj, fileInfo, typeTreeReader, crc32);

                buildObjects.AddObject(serializedObject);
            }

            m_PPtrResolver.EndSerializedFile();
        }

        SerializedObject ReadSerializedObject(ObjectInfo obj, BuildFileInfo fileInfo, TypeTreeReader reader, uint crc32)
        {
            switch (reader.Node.Type)
            {
                case "AnimationClip": return new AnimationClip(obj, fileInfo, m_PPtrResolver, reader, crc32);
                //case "AssetBundle": return new AssetBundle(obj, fileInfo, m_PPtrResolver, reader, crc32);
                case "AudioClip": return new AudioClip(obj, fileInfo, m_PPtrResolver, reader, crc32);
                case "Mesh": return new Mesh(obj, fileInfo, m_PPtrResolver, reader, crc32);
                //case "PreloadData": return new PreloadData(obj, fileInfo, m_PPtrResolver, reader, crc32);
                case "Shader": return new Shader(obj, fileInfo, m_PPtrResolver, reader, crc32);
                case "Texture2D": return new Texture2D(obj, fileInfo, m_PPtrResolver, reader, crc32);
                default: return new SerializedObject(obj, fileInfo, m_PPtrResolver, reader, crc32);
            }
        }
    }
}
