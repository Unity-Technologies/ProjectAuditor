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
                    catch (Exception e)
                    {
                        // TODO: better error handling
                        // This is tricky because Unity doesn't return coherent error codes when trying to open a file that is not a SerializedFile
                    }
                }
                catch (Exception e)
                {
                    // TODO: better error handling
                    // We may want to report files that couldn't be analyzed.
                }
            }

            return buildObjects;
        }

        void AnalyzeSerializedFile(BuildFileInfo fileInfo, BuildObjects buildObjects)
        {
            using var sf = UnityFileSystem.OpenSerializedFile(fileInfo.AbsolutePath);
            using var reader = new UnityFileReader(fileInfo.AbsolutePath, 64 * 1024 * 1024);
            using var processor = new PPtrAndCrcProcessor(sf, reader, Path.GetDirectoryName(fileInfo.AbsolutePath), m_PPtrResolver, buildObjects);
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
                var currentObjectId = m_PPtrResolver.GetObjectId(0, obj.Id);

                var root = sf.GetTypeTreeRoot(obj.Id);
                var offset = obj.Offset;
                uint crc32 = 0;//processor.Process(currentObjectId, offset, root);
                var serializedObject = ReadSerializedObject(fileInfo, new TypeTreeReader(sf, root, reader, offset), currentObjectId, obj.Size, crc32, root.Type);

                buildObjects.AddObject(serializedObject);
            }

            m_PPtrResolver.EndSerializedFile();
        }

        SerializedObject ReadSerializedObject(BuildFileInfo fileInfo, TypeTreeReader reader, int id, long size, uint crc32, string type)
        {
            switch (type)
            {
                case "AnimationClip": return new AnimationClip(fileInfo, m_PPtrResolver, reader, id, size, crc32);
                case "AssetBundle": return new AssetBundle(fileInfo, m_PPtrResolver, reader, id, size, crc32);
                case "AudioClip": return new AudioClip(fileInfo, m_PPtrResolver, reader, id, size, crc32);
                case "Mesh": return new Mesh(fileInfo, m_PPtrResolver, reader, id, size, crc32);
                case "PreloadData": return new PreloadData(fileInfo, m_PPtrResolver, reader, id, size, crc32);
                case "Shader": return new Shader(fileInfo, m_PPtrResolver, reader, id, size, crc32);
                case "Texture2D": return new Texture2D(fileInfo, m_PPtrResolver, reader, id, size, crc32);
                default: return new SerializedObject(fileInfo, m_PPtrResolver, reader, id, size, crc32, type);
            }
        }
    }
}
