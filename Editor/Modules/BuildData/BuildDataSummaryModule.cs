using System.Collections.Generic;
using Unity.ProjectAuditor.Editor.BuildData;
using Unity.ProjectAuditor.Editor.Core;
using Unity.ProjectAuditor.Editor.Diagnostic;
using SerializedObject = Unity.ProjectAuditor.Editor.BuildData.SerializedObjects.SerializedObject;

namespace Unity.ProjectAuditor.Editor.Modules
{
    internal enum BuildDataSummaryProperty
    {
        Type,
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
            Category = IssueCategory.BuildDataSummary,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Format = PropertyFormat.String, Name = "Name", LongName = "Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Type), Format = PropertyFormat.String, Name = "Type", LongName = "Asset Type", IsDefaultGroup = true },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size In Bytes" },
            },
        };

        internal static readonly IssueLayout k_DiagnosticIssueLayout = new IssueLayout
        {
            Category = IssueCategory.BuildDataDiagnostic,
            Properties = new[]
            {
                new PropertyDefinition { Type = PropertyType.Description, Name = "Issue", LongName = "Issue description"},
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Name), Format = PropertyFormat.String, Name = "Name", LongName = "Name" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Type), Format = PropertyFormat.String, Name = "Type", LongName = "Type" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Duplicates), Format = PropertyFormat.Integer, Name = "Instances", LongName = "Count Of Instances Of Object" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.Size), Format = PropertyFormat.Bytes, Name = "Size", LongName = "Size" },
                new PropertyDefinition { Type = PropertyTypeUtil.FromCustom(BuildDataDiagnosticsProperty.TotalSize), Format = PropertyFormat.Bytes, Name = "Total Size", LongName = "Total Size" },
                new PropertyDefinition { Type = PropertyType.Descriptor, Name = "Descriptor", IsDefaultGroup = true, IsHidden = true},
            }
        };

        internal const string PBD0000 = nameof(PBD0000);

        internal static readonly Descriptor k_DuplicateDiagnosticDescriptor = new Descriptor(
            PBD0000,
            "Duplicate Data",
            Areas.BuildSize,
            "This data is potentially duplicated.",
            "Investigate how the data was authored and/or built into the listed data files. If multiple files contain this data build size and loading times may be improved by moving the data into a shared data file."
        )
        {
            MessageFormat = "File '{0}' is a potential duplicate"
        };

        public override string Name => "BuildDataSummary";

        public override IReadOnlyCollection<IssueLayout> SupportedLayouts => new IssueLayout[]
        {
            k_SummaryLayout,
            k_DiagnosticIssueLayout
        };

        class SerializedObjectInfo
        {
            public int Count;
            public long Size;
        }

        struct SerializedObjectKey
        {
            public int NameHash;
            public int TypeHash;
            public long Size;
            public uint Crc;

            public SerializedObjectKey(string name, string type, long size, uint crc)
            {
                NameHash = name?.GetHashCode() ?? -1;
                TypeHash = type.GetHashCode();
                Size = size;
                Crc = crc;
            }
        }

        Dictionary<string, SerializedObjectInfo> m_SerializedObjectInfos = new Dictionary<string, SerializedObjectInfo>();
        Dictionary<SerializedObjectKey, List<SerializedObject>> m_PotentialDuplicates = new Dictionary<SerializedObjectKey, List<SerializedObject>>();

        SimpleDependencyNode k_CycleNode = new SimpleDependencyNode("(cycle)");

        public override void Initialize()
        {
            base.Initialize();

            RegisterDescriptor(k_DuplicateDiagnosticDescriptor);
        }

        public override AnalysisResult Audit(AnalysisParams projectAuditorParams, IProgress progress = null)
        {
            if (projectAuditorParams.BuildObjects != null)
            {
                var buildObjects = projectAuditorParams.BuildObjects;

                List<ProjectIssue> issues = new List<ProjectIssue>();

                // Count all SerializedObjects
                var objects = buildObjects.GetObjects<SerializedObject>();
                var objectDict = new Dictionary<int, SerializedObject>();

                List<List<SerializedObject>> duplicatesList = new List<List<SerializedObject>>();

                progress?.Start($"Summarizing {objects.Count} objects from Build Data", "Search in Progress...", objects.Count);

                foreach (var obj in objects)
                {
                    objectDict.Add(obj.Id, obj);

                    var size = obj.Size;

                    // Build a table with all objects, which also allows grouping and summarizing them
                    var name = obj.Name != null ? obj.Name : string.Empty;
                    var issue = new IssueBuilder(IssueCategory.BuildDataSummary, name)
                        .WithCustomProperties(new object[((int)BuildDataSummaryProperty.Num)]
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

                    SerializedObjectKey key = new SerializedObjectKey(obj.Name, obj.Type, obj.Size, obj.Crc32);

                    if (m_PotentialDuplicates.TryGetValue(key, out var objList))
                    {
                        objList.Add(obj);

                        if (objList.Count == 2)
                            duplicatesList.Add(objList);
                    }
                    else
                    {
                        var newList = new List<SerializedObject>(1);
                        newList.Add(obj);
                        m_PotentialDuplicates.Add(key, newList);
                    }

                    progress?.Advance();
                }

                var context = new AnalysisContext();

                progress?.Start($"Finding duplicate objects in Build Data ({duplicatesList.Count})", "Search in Progress...", duplicatesList.Count);

                var files = new HashSet<string>();

                foreach (var list in duplicatesList)
                {
                    var firstObj = list[0];

                    var mainDependencyNode = new SimpleDependencyNode("Information");

                    // Dependency view: Add tree with a list of bundles as child items
                    var bundleDependencyNode = new SimpleDependencyNode("Files containing this asset");

                    // Dependency view: Add tree with inverse references from this object to objects referencing it
                    var inverseDependencyNode = new LazyEvaluationDependencyNode("Objects referencing this asset");
                    mainDependencyNode.AddChild(inverseDependencyNode);

                    files.Clear();
                    foreach (var obj in list)
                    {
                        var filename = obj.BuildFile.DisplayName;
                        if (!files.Contains(filename))
                        {
                            files.Add(filename);

                            var childDependencyNode = new SimpleDependencyNode(filename);
                            bundleDependencyNode.AddChild(childDependencyNode);
                        }

                        inverseDependencyNode.OnEvaluate += () =>
                        {
                            var visitedObjects = new Stack<int>();
                            AddChildDependencyNodes(obj, inverseDependencyNode, buildObjects, objectDict,
                                visitedObjects, 10);
                        };
                    }

                    var name = firstObj.Name != null ? firstObj.Name : string.Empty;

                    mainDependencyNode.AddChild(bundleDependencyNode);

                    var issue = context.CreateIssue(IssueCategory.BuildDataDiagnostic,
                        k_DuplicateDiagnosticDescriptor.Id, name)
                        .WithCustomProperties(new object[]
                        {
                            name,
                            firstObj.Type,
                            list.Count,
                            firstObj.Size,
                            list.Count * firstObj.Size
                        }
                        ).WithDependencies(mainDependencyNode);

                    issues.Add(issue);

                    progress?.Advance();
                }

                projectAuditorParams.OnIncomingIssues(issues);
            }

            progress?.Clear();

            return AnalysisResult.Success;
        }

        private bool AddChildDependencyNodes(SerializedObject serializedObject,
            SimpleDependencyNode parentNode, BuildObjects buildObjects,
            Dictionary<int, SerializedObject> serializedObjects, Stack<int> visitedObjects, int depthLeft)
        {
            if (depthLeft == 0 || serializedObject == null)
                return false;

            var isObjectCycle = visitedObjects.Contains(serializedObject.Id);

            SimpleDependencyNode node;

            if (serializedObject.Type == "AssetBundle")
            {
                node = new SimpleDependencyNode(serializedObject.BuildFile.DisplayName);
            }
            else
            {
                if (visitedObjects.Count == 0)
                    node = new SimpleDependencyNode(
                        serializedObject.Name + " (" + serializedObject.Type + ") - " + serializedObject.BuildFile.DisplayName);
                else
                    node = new SimpleDependencyNode(serializedObject.Name + " (" + serializedObject.Type + ")");
            }

            parentNode.AddChild(node);

            if (isObjectCycle)
            {
                node.AddChild(k_CycleNode);
                return false;
            }

            visitedObjects.Push(serializedObject.Id);

            bool isLeafNode = true;

            if (buildObjects.ReferencesTo.TryGetValue(serializedObject.Id, out HashSet<BuildObjects.Reference> referencesTo))
            {
                bool allLeaves = true;

                foreach (var reference in referencesTo)
                {
                    var referencingObjId = reference.ObjectId;
                    if (serializedObjects.TryGetValue(referencingObjId, out var obj))
                    {
                        bool isLeaf = AddChildDependencyNodes(obj, node, buildObjects, serializedObjects,
                            visitedObjects, depthLeft - 1);

                        if (!isLeaf)
                            allLeaves = false;
                    }
                }

                isLeafNode = allLeaves;
            }

            visitedObjects.Pop();

            return isLeafNode;
        }
    }
}
