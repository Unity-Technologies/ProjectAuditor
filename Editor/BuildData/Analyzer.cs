using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Unity.ProjectAuditor.Editor.BuildData.SerializedObjects;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData
{
    public class Analyzer
    {
        private Util.IdProvider<string> m_SerializedFileIdProvider = new Util.IdProvider<string>();
        private Util.ObjectIdProvider m_ObjectIdProvider = new Util.ObjectIdProvider();

        private Regex m_RegexSceneFile = new Regex(@"BuildPlayer-([^\.]+)(?:\.sharedAssets)?");

        // Used to map PPtr fileId to its corresponding serialized file id in the database.
        Dictionary<int, int> m_LocalToDbFileId = new Dictionary<int, int>();

        private readonly Dictionary<string, Type> m_SerializedObjectTypes = new Dictionary<string, Type>()
        {
            { "AnimationClip", typeof(AnimationClip)},
            { "AssetBundle", typeof(AssetBundle)},
            { "AudioClip", typeof(AudioClip)},
            { "Mesh", typeof(Mesh)},
            { "PreloadData", typeof(PreloadData)},
            { "Shader", typeof(Shader)},
            { "Texture2D", typeof(Texture2D)},
        };

        private Dictionary<Type, List<SerializedObject>> m_SerializedObjects =
            new Dictionary<Type, List<SerializedObject>>();

        public Analyzer()
        {
            foreach (var serializedObjectType in m_SerializedObjectTypes)
            {
                m_SerializedObjects[serializedObjectType.Value] = new List<SerializedObject>();
            }

            m_SerializedObjects[typeof(SerializedObject)] = new List<SerializedObject>();
        }

        public IEnumerable<T> GetSerializedObjects<T>() where T : SerializedObject
        {
            foreach (var l in m_SerializedObjects)
            {
                if (typeof(T).IsAssignableFrom(l.Key))
                {
                    foreach (var o in m_SerializedObjects[l.Key])
                        yield return (T)o;
                }
            }
        }

        private string GetRelativePath(string folder, string path)
        {
            var stdFolder = folder.Replace("\\", "/");
            if (!stdFolder.EndsWith("/"))
                stdFolder += "/";
            var newPath = path.Replace("\\", "/").Replace(stdFolder, "");
            return newPath;
        }

        public void Analyze(string path, string searchPattern)
        {
            var files = Directory.GetFiles(path, searchPattern, SearchOption.AllDirectories);
            int i = 1;
            int lastLength = 0;

            foreach (var file in files)
            {
                try
                {
                    using (var archive = UnityFileSystem.MountArchive(file, "archive:" + Path.DirectorySeparatorChar))
                    {
                        var buildFileInfo =
                            new BuildFileInfo(GetRelativePath(path, file), new FileInfo(file).Length, true);

                        foreach (var node in archive.Nodes)
                        {
                            if (node.Flags.HasFlag(ArchiveNodeFlags.SerializedFile))
                            {
                                AnalyzeSerializedFile(node.Path, "archive:" + Path.DirectorySeparatorChar, buildFileInfo);
                            }
                        }
                    }
                }
                catch (NotSupportedException)
                {
                    // It wasn't an AssetBundle, try to open the file as a SerializedFile.

                    var serializedFileName = GetRelativePath(path, file);
                    var buildFileInfo = new BuildFileInfo(GetRelativePath(serializedFileName, file),
                        new FileInfo(file).Length, false);

                    try
                    {
                        AnalyzeSerializedFile(serializedFileName, path, buildFileInfo);
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

                ++i;
            }
        }

        private void AnalyzeSerializedFile(string filename, string folder, BuildFileInfo fileInfo)
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

                SerializedObject serializedObject;

                if (m_SerializedObjectTypes.TryGetValue(root.Type, out var serializedType))
                {
                    serializedObject = Activator.CreateInstance(serializedType, new object[] {new RandomAccessReader(sf, root, reader, offset), obj.Size, fileInfo}) as SerializedObject;
                }
                else
                {
                    serializedType = typeof(SerializedObject);
                    serializedObject = new SerializedObject(new RandomAccessReader(sf, root, reader, offset), obj.Size, root.Type, fileInfo);
                }

                m_SerializedObjects[serializedType].Add(serializedObject);
            }
        }
    }
}
