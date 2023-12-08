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
            category = IssueCategory.BuildDataSummary,
            properties = new[]
            {
                new PropertyDefinition { type = PropertyType.Description, format = PropertyFormat.String, name = "Name", longName = "Name" },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Type), format = PropertyFormat.String, name = "Type", longName = "Asset Type", defaultGroup = true },
                new PropertyDefinition { type = PropertyTypeUtil.FromCustom(BuildDataSummaryProperty.Size), format = PropertyFormat.Bytes, name = "Size", longName = "Size In Bytes" },
            },
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
            Areas.BuildSize,
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
            public uint Crc;

            public SerliazedObjectKey(string name, string type, long size, uint crc)
            {
                NameHash = name?.GetHashCode() ?? -1;
                TypeHash = type.GetHashCode();
                Size = size;
                Crc = crc;
            }
        }

        Dictionary<string, SerializedObjectInfo> m_SerializedObjectInfos = new Dictionary<string, SerializedObjectInfo>();
        Dictionary<SerliazedObjectKey, List<SerializedObject>> m_PotentialDuplicates = new Dictionary<SerliazedObjectKey, List<SerializedObject>>();

        public override void Initialize()
        {
            base.Initialize();

            RegisterDescriptor(k_DuplicateDiagnosticDescriptor);
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
                var objectDict = new Dictionary<int, SerializedObject>();

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

                    SerliazedObjectKey key = new SerliazedObjectKey(obj.Name, obj.Type, obj.Size, obj.Crc32);

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

                // Diagnostic issues
                var context = new AnalysisContext();

                foreach (var duplicate in m_PotentialDuplicates)
                {
                    if (duplicate.Value.Count > 1)
                    {
                        var firstObj = duplicate.Value[0];

                        var mainDependencyNode = new SimpleDependencyNode("Information");

                        // Dependency view: Add tree with a list of bundles as child items
                        var bundleDependencyNode = new SimpleDependencyNode("Files containing this asset");

                        // Dependency view: Add tree with inverse references from this object to objects referencing it
                        var inverseDependencyNode = new SimpleDependencyNode("Objects referencing this asset");
                        mainDependencyNode.AddChild(inverseDependencyNode);

                        var files = new HashSet<string>();
                        foreach (var obj in duplicate.Value)
                        {
                            var filename = obj.BuildFile.DisplayName;
                            if (!files.Contains(filename))
                            {
                                files.Add(filename);

                                var childDependencyNode = new SimpleDependencyNode(filename);
                                bundleDependencyNode.AddChild(childDependencyNode);
                            }

                            HashSet<int> visitedObjects = new HashSet<int>();
                            AddChildDependencyNodes(obj, inverseDependencyNode, buildObjects, objectDict,
                                visitedObjects, 10);
                        }

                        var name = firstObj.Name != null ? firstObj.Name : string.Empty;

                        mainDependencyNode.AddChild(bundleDependencyNode);

                        var issue = context.CreateIssue(IssueCategory.BuildDataDiagnostic,
                            k_DuplicateDiagnosticDescriptor.Id, name)
                            .WithCustomProperties(new object[]
                            {
                                name,
                                firstObj.Type,
                                duplicate.Value.Count,
                                firstObj.Size,
                                duplicate.Value.Count * firstObj.Size
                            }
                            ).WithDependencies(mainDependencyNode);

                        issues.Add(issue);
                    }
                }

                projectAuditorParams.OnIncomingIssues(issues);
            }

            progress?.Clear();

            projectAuditorParams.OnModuleCompleted?.Invoke();
        }

        private bool AddChildDependencyNodes(SerializedObject serializedObject,
            SimpleDependencyNode parentNode, BuildObjects buildObjects,
            Dictionary<int, SerializedObject> serializedObjects, HashSet<int> visitedObjects, int depthLeft)
        {
            if (depthLeft == 0 || serializedObject == null)
                return false;

            var isObjectCycle = visitedObjects.Contains(serializedObject.Id);
            string info = string.Empty;

            if (isObjectCycle)
            {
                info += "Cycle: ";
            }

            if (serializedObject.Type == "AssetBundle")
            {
                info += serializedObject.BuildFile.DisplayName;
            }
            else
            {
                info += string.IsNullOrEmpty(serializedObject.Name) ? "---" : serializedObject.Name;
                info += " (" + serializedObject.Type + ")";
            }

            if (isObjectCycle)
                return false;

            var node = new SimpleDependencyNode(info);
            parentNode.AddChild(node);

            visitedObjects.Add(serializedObject.Id);

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

            return isLeafNode;
        }
    }
}
