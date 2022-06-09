namespace Unity.ProjectAuditor.Editor.Utils
{
    public enum TaskStatus
    {
        NotStarted,
        IsProgress,
        Completed,
        MissingDependency
    }

    public abstract class ITask
    {
        protected TaskStatus m_Status = TaskStatus.NotStarted;

        public ITask[] dependencies;

        public bool IsDone()
        {
            switch (m_Status)
            {
                case TaskStatus.Completed:
                case TaskStatus.MissingDependency:
                    return true;
                default:
                    return false;
            }
        }

        public abstract void Update();
        public abstract bool Success();

        public TaskStatus status
        {
            get
            {
                return m_Status;
            }
        }
    }
}
