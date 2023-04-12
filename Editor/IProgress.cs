namespace Unity.ProjectAuditor.Editor
{
    internal interface IProgress
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
    }
}
