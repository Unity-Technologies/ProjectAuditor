using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.BuildData.SerializedObjects
{
    public class Shader : SerializedObject
    {
        public int DecompressedSize { get; }
        public IReadOnlyList<SubShader> SubShaders { get; }
        public IReadOnlyList<string> Keywords { get; }

        public Shader(RandomAccessReader reader, long size, BuildFileInfo buildFile)
            : base(reader, size, "Shader", buildFile)
        {
            Dictionary<int, string> keywordNames = null;
            KeywordSet keywordSet = new KeywordSet();
            var parsedForm = reader["m_ParsedForm"];

            // Starting in some Unity 2021 version, keyword names are stored in m_KeywordNames.
            if (parsedForm.HasChild("m_KeywordNames"))
            {
                keywordNames = new Dictionary<int, string>();

                int i = 0;
                foreach (var keyword in parsedForm["m_KeywordNames"])
                {
                    keywordNames[i++] = keyword.GetValue<string>();
                }
            }

            var subShadersReader = parsedForm["m_SubShaders"];
            var subShaders = new List<SubShader>(subShadersReader.GetArraySize());

            foreach (var subShader in subShadersReader)
            {
                subShaders.Add(new SubShader(keywordSet, subShader, keywordNames));
            }

            DecompressedSize = 0;

            if (reader["decompressedLengths"].IsArrayOfObjects)
            {
                // The decompressed lengths are stored per graphics API.
                foreach (var apiLengths in reader["decompressedLengths"])
                {
                    foreach (var blockSize in apiLengths.GetValue<int[]>())
                    {
                        DecompressedSize += blockSize;
                    }
                }

                // Take the average (not ideal, but better than nothing).
                DecompressedSize /= reader["decompressedLengths"].GetArraySize();
            }
            else
            {
                foreach (var blockSize in reader["decompressedLengths"].GetValue<int[]>())
                {
                    DecompressedSize += blockSize;
                }
            }

            Name = parsedForm["m_Name"].GetValue<string>();
            Keywords = keywordSet.Keywords;
            SubShaders = subShaders;
        }

        public class SubShader
        {
            public IReadOnlyList<Pass> Passes { get; }

            public SubShader(KeywordSet keywordSet, RandomAccessReader reader, Dictionary<int, string> keywordNames)
            {
                var passesReader = reader["m_Passes"];
                var passes = new List<Pass>(passesReader.GetArraySize());

                foreach (var pass in passesReader)
                {
                    passes.Add(new Pass(keywordSet, pass, keywordNames));
                }

                Passes = passes;
            }

            public class Pass
            {
                public string Name { get; }
                // The key is the program type (vertex, fragment...)
                public IReadOnlyDictionary<string, IReadOnlyList<SubProgram>> Programs { get; }

                public Pass(KeywordSet keywordSet, RandomAccessReader reader, Dictionary<int, string> keywordNames)
                {
                    var programs = new Dictionary<string, IReadOnlyList<SubProgram>>();

                    if (keywordNames == null)
                    {
                        keywordNames = new Dictionary<int, string>();

                        var nameIndices = reader["m_NameIndices"];

                        foreach (var nameIndex in nameIndices)
                        {
                            keywordNames[nameIndex["second"].GetValue<int>()] =
                                nameIndex["first"].GetValue<string>();
                        }
                    }

                    if (reader.HasChild("m_State"))
                    {
                        Name = reader["m_State"]["m_Name"].GetValue<string>();
                    }

                    foreach (var progType in s_progTypes)
                    {
                        if (!reader.HasChild(progType.fieldName))
                        {
                            continue;
                        }

                        var program = reader[progType.fieldName];

                        // Starting in some Unity 2021.3 version, programs are stored in m_PlayerSubPrograms instead of m_SubPrograms.
                        if (program.HasChild("m_PlayerSubPrograms"))
                        {
                            int numSubPrograms = 0;
                            var subPrograms = program["m_PlayerSubPrograms"];

                            // And they are stored per hardware tiers.
                            foreach (var tierProgram in subPrograms)
                            {
                                // Count total number of programs.
                                numSubPrograms += tierProgram.GetArraySize();
                            }

                            // Preallocate enough elements to avoid allocations.
                            var programList = new List<SubProgram>(numSubPrograms);

                            for (int hwTier = 0; hwTier < subPrograms.GetArraySize(); ++hwTier)
                            {
                                foreach (var subProgram in subPrograms[hwTier])
                                {
                                    programList.Add(new SubProgram(keywordSet, subProgram, keywordNames, hwTier));
                                }
                            }

                            if (programs.Count > 0)
                            {
                                programs[progType.typeName] = programList;
                            }
                        }
                        else
                        {
                            var subPrograms = program["m_SubPrograms"];

                            if (subPrograms.Count > 0)
                            {
                                var programList = new List<SubProgram>(subPrograms.GetArraySize());

                                foreach (var subProgram in subPrograms)
                                {
                                    programList.Add(new SubProgram(keywordSet, subProgram, keywordNames));
                                }

                                programs[progType.typeName] = programList;
                            }
                        }
                    }

                    Programs = programs;
                }

                public class SubProgram
                {
                    public int HwTier { get; }
                    public int Api { get; }
                    public uint BlobIndex { get; }

                    // Keyword index in Shader.Keywords
                    public IReadOnlyList<int> Keywords { get; }

                    public SubProgram(KeywordSet keywordSet, RandomAccessReader reader, Dictionary<int, string> keywordNames, int hwTier = -1)
                    {
                        Api = reader["m_GpuProgramType"].GetValue<sbyte>();
                        BlobIndex = reader["m_BlobIndex"].GetValue<uint>();
                        var keywords = new List<int>();
                        HwTier = hwTier != -1 ? hwTier : reader["m_ShaderHardwareTier"].GetValue<sbyte>();

                        if (reader.HasChild("m_KeywordIndices"))
                        {
                            var indices = reader["m_KeywordIndices"].GetValue<ushort[]>();

                            foreach (var index in indices)
                            {
                                if (keywordNames.TryGetValue(index, out var name))
                                {
                                    keywords.Add(keywordSet.GetKeywordIndex(name));
                                }
                            }
                        }
                        else
                        {
                            foreach (var index in reader["m_GlobalKeywordIndices"].GetValue<ushort[]>())
                            {
                                if (keywordNames.TryGetValue(index, out var name))
                                {
                                    keywords.Add(keywordSet.GetKeywordIndex(name));
                                }
                            }

                            foreach (var index in reader["m_LocalKeywordIndices"].GetValue<ushort[]>())
                            {
                                if (keywordNames.TryGetValue(index, out var name))
                                {
                                    keywords.Add(keywordSet.GetKeywordIndex(name));
                                }
                            }
                        }

                        Keywords = keywords;
                    }
                }
            }
        }

        private static readonly IReadOnlyList<(string fieldName, string typeName)> s_progTypes = new List<(string fieldName, string typeName)>()
        {
            ("progVertex", "vertex"),
            ("progFragment", "fragment"),
            ("progGeometry", "geometry"),
            ("progHull", "hull"),
            ("progDomain", "domain"),
            ("progRayTracing", "ray tracing"),
        };

        public class KeywordSet
        {
            public IReadOnlyList<string> Keywords => m_Keywords;

            private List<string> m_Keywords = new List<string>();
            private Dictionary<string, int> m_KeywordToIndex = new Dictionary<string, int>();

            public int GetKeywordIndex(string name)
            {
                int index;

                if (m_KeywordToIndex.TryGetValue(name, out index))
                {
                    return index;
                }

                index = Keywords.Count;
                m_Keywords.Add(name);
                m_KeywordToIndex[name] = index;

                return index;
            }
        }
    }
}
