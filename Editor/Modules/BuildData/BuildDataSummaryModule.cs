using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using SerializedObject = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.SerializedObject;

namespace Unity.ProjectAuditor.Editor.Modules
{
    enum BuildDataListProperty
    {
        Type,
        Size,
        Num,
    }

    enum BuildDataSummaryProperty
    {
        Type,
        Count,
        Size,
        Num,
    }

    enum BuildDataDiagnosticsProperty
    {
        Name,
        Type,
        Duplicates,
        Size,
        TotalSize,
        Num,
    }

    class BuildDataSummaryModule : Module
    {
        static readonly IssueLayout k_SummaryLayout = new IssueLayout
        {
            category = IssueCategory.BuildDataSummary,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Type), format = PropertyFormat.String, name = "Type", longName = "Asset Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Count), format = PropertyFormat.Integer, name = "Count", longName = "Number Of Assets Of This Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size In Bytes" },
            }
        };

        static readonly IssueLayout k_ListLayout = new IssueLayout
        {
            category = IssueCategory.BuildDataList,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataListProperty.Type), format = PropertyFormat.String, name = "Type", longName = "Asset Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataListProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size In Bytes" },
            }
        };

        internal static readonly IssueLayout k_DiagnosticIssueLayout = new IssueLayout
        {
            category = IssueCategory.BuildDataDiagnostic,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, name = "Issue", longName = "Issue description"},
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Name), format = PropertyFormat.String, name = "Name", longName = "Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Type), format = PropertyFormat.String, name = "Type", longName = "Type" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Duplicates), format = PropertyFormat.Integer, name = "Instances", longName = "Count Of Instances Of Object" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.TotalSize), format = PropertyFormat.Bytes, name = "Total Size", longName = "Total Size" },
                new PropertyDefinition { type = PropertyType.Descriptor, name = "Descriptor", defaultGroup = true, hidden = true},
            }
        };

        internal const string PBD0000 = nameof(PBD0000);

        internal static readonly Descriptor k_DuplicateDiagnosticDescriptor = new Descriptor(
            PBD0000,
            "Duplicate Data",
            new[] {Area.GPU, Area.Quality},
            "This data is potentially duplicated.",
            "Investigate how the data was authored and/or built into the listed data files. If multiple files contain this data build size and loading times may be improved by moving the data into a shared data file."
        )
        {
            MessageFormat = "File '{0}' is a potential duplicate"
        };

        public override string Name => "Summary";

        public override bool IsEnabledByDefault => false;

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_SummaryLayout,
            k_ListLayout,
            k_DiagnosticIssueLayout
        };

        class SerializedObjectInfo
        {
            public int Count;
            public long Size;
        }

        struct SerliazedObjectKey
        {
            public int NameHash;
            public int TypeHash;
            public long Size;

            public SerliazedObjectKey(string name, string type, long size)
            {
                NameHash = name?.GetHashCode() ?? -1;
                TypeHash = type.GetHashCode();
                Size = size;
            }
        }

        Dictionary<string, SerializedObjectInfo> m_SerializedObjectInfos = new Dictionary<string, SerializedObjectInfo>();
        Dictionary<SerliazedObjectKey, List<SerializedObject>> m_PotentialDuplicates = new Dictionary<SerliazedObjectKey, List<SerializedObject>>();

        public override void Initialize()
        {
            base.Initialize();

            RegisterDescriptor(k_DuplicateDiagnosticDescriptor);
        }

        ProjectIssue CreateIssueForObjectType(string type, int count, long size)
        {
            return new IssueBuilder(IssueCategory.BuildDataSummary, type)
                .WithCustomProperties(
                new object[((int)BuildDataSummaryProperty.Num)]
                {
                    type,
                    count,
                    size
                });
        }

        public override void Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            if (projectAuditorParams.BuildObjects != null)
            {
                var buildObjects = projectAuditorParams.BuildObjects;

                progress?.Start("Parsing all assets from Build Data", "Search in Progress...", 1);

                List<ProjectIssue> issues = new List<ProjectIssue>();

                // Count all SerializedObjects
                var objects = buildObjects.GetObjects<SerializedObject>();
                foreach (var obj in objects)
                {
                    var size = obj.Size;

                    var name = obj.Name != null ? obj.Name : string.Empty;
                    var issue = new IssueBuilder(IssueCategory.BuildDataList, name)
                        .WithCustomProperties(new object[((int)BuildDataListProperty.Num)]
                    {
                        obj.Type,
                        obj.Size
                    });
                    issues.Add(issue);

                    if (m_SerializedObjectInfos.TryGetValue(obj.Type, out var info))
                    {
                        info.Count++;
                        info.Size += size;
                    }
                    else
                    {
                        m_SerializedObjectInfos.Add(obj.Type, new SerializedObjectInfo { Count = 1, Size = size });
                    }

                    SerliazedObjectKey key = new SerliazedObjectKey(obj.Name, obj.Type, obj.Size);

                    if (m_PotentialDuplicates.TryGetValue(key, out var objList))
                    {
                        objList.Add(obj);
                    }
                    else
                    {
                        var newList = new List<SerializedObject>(1);
                        newList.Add(obj);
                        m_PotentialDuplicates.Add(key, newList);
                    }
                }

                // Create one issue per type of SerializedObjects
                foreach (var key in m_SerializedObjectInfos.Keys)
                {
                    var issue = CreateIssueForObjectType(key, m_SerializedObjectInfos[key].Count,
                        m_SerializedObjectInfos[key].Size);
                    issues.Add(issue);
                }

                // Diagnostic issues
                var context = new AnalysisContext();

                foreach (var duplicate in m_PotentialDuplicates)
                {
                    if (duplicate.Value.Count > 1)
                    {
                        var firstObj = duplicate.Value[0];

                        var dependencyNode = new SimpleDependencyNode("Files containing this asset");

                        var files = new HashSet<string>();
                        foreach (var obj in duplicate.Value)
                        {
                            var filename = obj.BuildFile.DisplayName;
                            if (!files.Contains(filename))
                            {
                                files.Add(filename);

                                var childDependencyNode = new SimpleDependencyNode(filename);
                                dependencyNode.AddChild(childDependencyNode);
                            }
                        }

                        var name = firstObj.Name != null ? firstObj.Name : string.Empty;

                        var issue = context.Create(IssueCategory.BuildDataDiagnostic,
                            k_DuplicateDiagnosticDescriptor.Id, name)
                            .WithCustomProperties(new object[]
                            {
                                name,
                                firstObj.Type,
                                duplicate.Value.Count,
                                firstObj.Size,
                                duplicate.Value.Count * firstObj.Size
                            }
                            ).WithDependencies(dependencyNode);

                        issues.Add(issue);
                    }
                }

                projectAuditorParams.OnIncomingIssues(issues);
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }
    }
}
