using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi;
using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer
{
    public class AnalyzerTool
    {
        private Util.IdProvider<string> m_SerializedFileIdProvider = new Util.IdProvider<string>();
        private Util.ObjectIdProvider m_ObjectIdProvider = new Util.ObjectIdProvider();

        private Regex m_RegexSceneFile = new Regex(@"BuildPlayer-([^\.]+)(?:\.sharedAssets)?");

        // Used to map PPtr fileId to its corresponding serialized file id in the database.
        Dictionary<int, int> m_LocalToDbFileId = new Dictionary<int, int>();

        private string GetRelativePath(string folder, string path)
        {
            var stdFolder = folder.Replace("\\", "/");
            if (!stdFolder.EndsWith("/"))
                stdFolder += "/";
            var newPath = path.Replace("\\", "/").Replace(stdFolder, "");
            return newPath;
        }

        public List<SerializedObjects.Shader> Analyze(string path, string searchPattern)
        {
            var shaders = new List<SerializedObjects.Shader>();
            var files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
            int i = 1;
            int lastLength = 0;

            foreach (var file in files)
            {
                try
                {
                    using (var archive = UnityFileSystem.MountArchive(file, "archive:" + Path.DirectorySeparatorChar))
                    {
                        var assetBundleName = GetRelativePath(path, file);

                        foreach (var node in archive.Nodes)
                        {
                            if (node.Flags.HasFlag(ArchiveNodeFlags.SerializedFile))
                            {
                                AnalyzeSerializedFile(node.Path, "archive:" + Path.DirectorySeparatorChar, shaders);
                            }
                        }
                    }
                }
                catch (NotSupportedException)
                {
                    // It wasn't an AssetBundle, try to open the file as a SerializedFile.

                    var serializedFileName = GetRelativePath(path, file);

                    try
                    {
                        AnalyzeSerializedFile(serializedFileName, path, shaders);
                    }
                    catch (Exception e)
                    {}
                }
                catch (Exception e)
                {}

                ++i;
            }

            return shaders;
        }

        private void AnalyzeSerializedFile(string filename, string folder, List<SerializedObjects.Shader> shaders)
        {
            var fullPath = Path.Combine(folder, filename);
            using var sf = UnityFileSystem.OpenSerializedFile(fullPath);
            using var reader = new UnityFileReader(fullPath, 64 * 1024 * 1024);
            //using var pptrReader = new PPtrAndCrcProcessor(sf, reader, Path.GetDirectoryName(fullPath), AddReference);
            int serializedFileId = m_SerializedFileIdProvider.GetId(filename.ToLower());
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

            m_LocalToDbFileId.Clear();

            int localId = 0;
            m_LocalToDbFileId.Add(localId++, serializedFileId);
            foreach (var extRef in sf.ExternalReferences)
            {
                m_LocalToDbFileId.Add(localId++, m_SerializedFileIdProvider.GetId(extRef.Path.Substring(extRef.Path.LastIndexOf('/') + 1).ToLower()));
            }

            foreach (var obj in sf.Objects)
            {
                var currentObjectId = m_ObjectIdProvider.GetId((serializedFileId, obj.Id));

                var root = sf.GetTypeTreeRoot(obj.Id);
                var offset = obj.Offset;
                uint crc32 = 0;

                if (root.Type == "Shader")
                {
                    shaders.Add(new SerializedObjects.Shader(new RandomAccessReader(sf, root, reader, offset)));
                }
            }
        }
    }
}
