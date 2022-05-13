using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyCompilationTask
    {
        public AssemblyBuilder builder;
        public AssemblyCompilationTask[] dependencies;
        public CompilerMessage[] messages;
        public Stopwatch stopWatch;

        CompilationStatus m_CompilationStatus = CompilationStatus.NotStarted;

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

        public CompilationStatus status
        {
            get
            {
                return m_CompilationStatus;
            }
        }

        public bool IsDone()
        {
            switch (m_CompilationStatus)
            {
                case CompilationStatus.Compiled:
                case CompilationStatus.MissingDependency:
                    return true;
                default:
                    return false;
            }
        }

        public void Update()
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
                            m_CompilationStatus = CompilationStatus.MissingDependency;
                        }
                    }
                    break;
                case AssemblyBuilderStatus.IsCompiling:
                    m_CompilationStatus = CompilationStatus.IsCompiling;
                    break;
                case AssemblyBuilderStatus.Finished:
                    m_CompilationStatus = CompilationStatus.Compiled;
                    break;
            }
        }

        public bool Success()
        {
            if (messages == null)
                return false;
            return messages.All(message => message.type != CompilerMessageType.Error);
        }
    }
}
