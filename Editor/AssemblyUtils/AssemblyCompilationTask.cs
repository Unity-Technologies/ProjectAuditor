using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor.Compilation;

namespace Unity.ProjectAuditor.Editor.AssemblyUtils
{
    class AssemblyCompilationTask
    {
        CompilationStatus m_CompilationStatus = CompilationStatus.NotStarted;

#pragma warning disable 618 // disable warning for obsolete AssemblyBuilder
        public AssemblyBuilder Builder;
#pragma warning restore 618
        public AssemblyCompilationTask[] Dependencies;
        public CompilerMessage[] Messages;
        public Stopwatch StopWatch;

        public string AssemblyName => Path.GetFileNameWithoutExtension(Builder.assemblyPath);

        public string AssemblyPath => Builder.assemblyPath;

        public long DurationInMs => StopWatch != null ? StopWatch.ElapsedMilliseconds : 0;

        public CompilationStatus Status => m_CompilationStatus;

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
            switch (Builder.status)
            {
                case AssemblyBuilderStatus.NotStarted:
                    if (Dependencies.All(dep => dep.IsDone()))
                    {
                        if (Dependencies.All(dep => dep.Success()))
                        {
                            StopWatch = Stopwatch.StartNew();
                            Builder.Build(); // all references are built, we can kick off this builder
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
            if (Messages == null)
                return false;
            return Messages.All(message => message.type != CompilerMessageType.Error);
        }
    }
}
