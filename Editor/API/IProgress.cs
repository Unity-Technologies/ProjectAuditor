namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Provides methods to create and manage an object which can represent progress of the project analysis process.
    /// </summary>
    public interface IProgress
    {
        /// <summary>
        /// Initializes the progress object.
        /// </summary>
        /// <param name="title">Title</param>
        /// <param name="description">Description</param>
        /// <param name="total">Number of steps</param>
        void Start(string title, string description, int total);

        /// <summary>
        /// Advances the progress object by one step.
        /// </summary>
        /// <param name="description">Updated message</param>
        void Advance(string description = "");

        /// <summary>
        /// Clear and hide the progress object.
        /// </summary>
        void Clear();

        /// <summary>
        /// Checks if the progress operation has been cancelled.
        /// </summary>
        /// <returns>True if cancelled, otherwise false.</returns>
        bool IsCancelled { get; }
    }
}
