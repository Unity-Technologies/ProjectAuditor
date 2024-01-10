using System;
using System.IO;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;
using UnityEngine;
using AnimationClip = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.AnimationClip;
using AudioClip = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.AudioClip;
using Mesh = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.Mesh;
using Shader = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.Shader;
using Texture2D = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.Texture2D;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class Analyzer
    {
        PPtrResolver m_PPtrResolver = new PPtrResolver();
        Regex m_RegexSceneFile = new Regex(@"BuildPlayer-([^\.]+)(?:\.sharedAssets)?");
        bool m_AnyDuplicateObjects;

        public BuildObjects Analyze(string path, string searchPattern, BuildObjects buildObjects)
        {
            var files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
            m_AnyDuplicateObjects = false;

            BuildFileInfo.BaseFolder = path;

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
                                try
                                {
                                    AnalyzeSerializedFile(fileInfo, buildObjects);
                                }
                                catch (Exception e)
                                {
                                    if (e.Message != "Unknown error.")
                                        throw;
                                }
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
                    catch (NotSupportedException)
                    {
                    }
                    catch (Exception e)
                    {
                        if (e.Message != "Unknown error.")
                            throw;
                    }
                }
            }

            return buildObjects;
        }

        public void Cleanup()
        {
            m_PPtrResolver.Reset();

            UnityFileReader.ClearBufferPool();
        }

        void AnalyzeSerializedFile(BuildFileInfo fileInfo, BuildObjects buildObjects)
        {
            using var sf = UnityFileSystem.OpenSerializedFile(fileInfo.AbsolutePath);
            using var reader = new UnityFileReader(fileInfo.AbsolutePath, 64 * 1024 * 1024);
            var folder = fileInfo.IsInArchive ? "archive:" : Path.GetDirectoryName(fileInfo.AbsolutePath);
            using var processor = new PPtrAndCrcProcessor(sf, reader, folder, m_PPtrResolver, buildObjects);

            //int sceneId = -1;
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

                try
                {
                    buildObjects.AddObject(serializedObject);
                }
                catch (ArgumentException e)
                {
                    // TODO: report once per Analyze call what specific path probably contains duplicate data
                    if (e.Message.Contains("An item with the same key has already been added."))
                    {
                        if (m_AnyDuplicateObjects == false)
                        {
                            Debug.LogWarning("BuildData.Analyzer.Analyze: At least one built object was scanned twice. There are potential duplicate folders/asset bundles in provided path(s).");
                            m_AnyDuplicateObjects = true;
                        }
                    }
                    else
                        throw;
                }
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
