using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyCompilationTask
    {
        internal AssemblyBuilder builder;
        internal AssemblyCompilationTask[] dependencies;
        internal CompilerMessage[] messages;
        internal Stopwatch stopWatch;

        CompilationStatus m_CompilationStatus = CompilationStatus.NotStarted;

        internal string assemblyName => Path.GetFileNameWithoutExtension(builder.assemblyPath);

        internal string assemblyPath => builder.assemblyPath;

        internal long durationInMs => stopWatch != null ? stopWatch.ElapsedMilliseconds : 0;

        internal CompilationStatus status => m_CompilationStatus;

        internal bool IsDone()
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

        internal void Update()
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

        internal bool Success()
        {
            if (messages == null)
                return false;
            return messages.All(message => message.type != CompilerMessageType.Error);
        }
    }
}
