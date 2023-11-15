using System.Collections.Generic;
using usingUnity.ProjectAuditor.Editor.UnityFileSystemApi.TypeTreeReaders;

namespace Unity.ProjectAuditor.Editor.Analyzer.SerializedObjects
{
    public class Shader
    {
        private string m_Name;
        public string Name => m_Name;
        private int m_DecompressedSize;
        public int DecompressedSize => m_DecompressedSize;
        private List<SubShader> m_SubShaders;
        public IReadOnlyList<SubShader> SubShaders => m_SubShaders;
        private IReadOnlyList<string> m_Keywords;
        public IReadOnlyList<string> Keywords => m_Keywords;

        public Shader(RandomAccessReader reader)
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
            m_SubShaders = new List<SubShader>(subShadersReader.GetArraySize());

            foreach (var subShader in subShadersReader)
            {
                m_SubShaders.Add(new SubShader(keywordSet, subShader, keywordNames));
            }

            m_DecompressedSize = 0;

            if (reader["decompressedLengths"].IsArrayOfObjects)
            {
                // The decompressed lengths are stored per graphics API.
                foreach (var apiLengths in reader["decompressedLengths"])
                {
                    foreach (var blockSize in apiLengths.GetValue<int[]>())
                    {
                        m_DecompressedSize += blockSize;
                    }
                }

                // Take the average (not ideal, but better than nothing).
                m_DecompressedSize /= reader["decompressedLengths"].GetArraySize();
            }
            else
            {
                foreach (var blockSize in reader["decompressedLengths"].GetValue<int[]>())
                {
                    m_DecompressedSize += blockSize;
                }
            }

            m_Name = parsedForm["m_Name"].GetValue<string>();
            m_Keywords = keywordSet.Keywords;
        }

        public class SubShader
        {
            private List<Pass> m_Passes;
            public IReadOnlyList<Pass> Passes => m_Passes;

            public SubShader(KeywordSet keywordSet, RandomAccessReader reader, Dictionary<int, string> keywordNames)
            {
                var passesReader = reader["m_Passes"];
                m_Passes = new List<Pass>(passesReader.GetArraySize());

                foreach (var pass in passesReader)
                {
                    m_Passes.Add(new Pass(keywordSet, pass, keywordNames));
                }
            }

            public class Pass
            {
                private string m_Name;
                public string Name => m_Name;

                // The key is the program type (vertex, fragment...)
                public Dictionary<string, IReadOnlyList<SubProgram>> m_Programs;
                public IReadOnlyDictionary<string, IReadOnlyList<SubProgram>> Programs => m_Programs;

                public Pass(KeywordSet keywordSet, RandomAccessReader reader, Dictionary<int, string> keywordNames)
                {
                    m_Programs = new Dictionary<string, IReadOnlyList<SubProgram>>();

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
                        m_Name = reader["m_State"]["m_Name"].GetValue<string>();
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
                            var programs = new List<SubProgram>(numSubPrograms);

                            for (int hwTier = 0; hwTier < subPrograms.GetArraySize(); ++hwTier)
                            {
                                foreach (var subProgram in subPrograms[hwTier])
                                {
                                    programs.Add(new SubProgram(keywordSet, subProgram, keywordNames, hwTier));
                                }
                            }

                            if (programs.Count > 0)
                            {
                                m_Programs[progType.typeName] = programs;
                            }
                        }
                        else
                        {
                            var subPrograms = program["m_SubPrograms"];

                            if (subPrograms.Count > 0)
                            {
                                var programs = new List<SubProgram>(subPrograms.GetArraySize());

                                foreach (var subProgram in subPrograms)
                                {
                                    programs.Add(new SubProgram(keywordSet, subProgram, keywordNames));
                                }

                                m_Programs[progType.typeName] = programs;
                            }
                        }
                    }
                }

                public class SubProgram
                {
                    private int m_HwTier;
                    public int HwTier => m_HwTier;
                    private int m_Api;
                    public int Api => m_Api;
                    private uint m_BlobIndex;
                    public uint BlobIndex => m_BlobIndex;

                    // Keyword index in ShaderData.Keywords
                    private List<int> m_Keywords;
                    public IReadOnlyList<int> Keywords => m_Keywords;

                    public SubProgram(KeywordSet keywordSet, RandomAccessReader reader, Dictionary<int, string> keywordNames, int hwTier = -1)
                    {
                        m_Api = reader["m_GpuProgramType"].GetValue<sbyte>();
                        m_BlobIndex = reader["m_BlobIndex"].GetValue<uint>();
                        m_Keywords = new List<int>();
                        m_HwTier = hwTier != -1 ? hwTier : reader["m_ShaderHardwareTier"].GetValue<sbyte>();

                        if (reader.HasChild("m_KeywordIndices"))
                        {
                            var indices = reader["m_KeywordIndices"].GetValue<ushort[]>();

                            foreach (var index in indices)
                            {
                                if (keywordNames.TryGetValue(index, out var name))
                                {
                                    m_Keywords.Add(keywordSet.GetKeywordIndex(name));
                                }
                            }
                        }
                        else
                        {
                            foreach (var index in reader["m_GlobalKeywordIndices"].GetValue<ushort[]>())
                            {
                                if (keywordNames.TryGetValue(index, out var name))
                                {
                                    m_Keywords.Add(keywordSet.GetKeywordIndex(name));
                                }
                            }

                            foreach (var index in reader["m_LocalKeywordIndices"].GetValue<ushort[]>())
                            {
                                if (keywordNames.TryGetValue(index, out var name))
                                {
                                    m_Keywords.Add(keywordSet.GetKeywordIndex(name));
                                }
                            }
                        }
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
