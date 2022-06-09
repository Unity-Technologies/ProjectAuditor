using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;
using Unity.ProjectAuditor.Editor.Utils;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyCompilationTask : ITask
    {
        public AssemblyBuilder builder;
        public CompilerMessage[] messages;
        public Stopwatch stopWatch;

        public string assemblyName
        {
            get
            {
                return Path.GetFileNameWithoutExtension(builder.assemblyPath);
            }
        }

        public string assemblyPath
        {
            get
            {
                return builder.assemblyPath;
            }
        }

        public long durationInMs
        {
            get
            {
                return stopWatch != null ? stopWatch.ElapsedMilliseconds : 0;
            }
        }

        public override void Update()
        {
            switch (builder.status)
            {
                case AssemblyBuilderStatus.NotStarted:
                    if (dependencies.All(dep => dep.IsDone()))
                    {
                        if (dependencies.All(dep => dep.Success()))
                        {
                            stopWatch = Stopwatch.StartNew();
                            builder.Build(); // all references are built, we can kick off this builder
                        }
                        else
                        {
                            // this assembly won't be built since it's missing dependencies
                            m_Status = TaskStatus.MissingDependency;
                        }
                    }
                    break;
                case AssemblyBuilderStatus.IsCompiling:
                    m_Status = TaskStatus.IsProgress;
                    break;
                case AssemblyBuilderStatus.Finished:
                    m_Status = TaskStatus.Completed;
                    break;
            }
        }

        public override bool Success()
        {
            if (messages == null)
                return false;
            return messages.All(message => message.type != CompilerMessageType.Error);
        }
    }
}
