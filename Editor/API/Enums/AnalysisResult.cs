namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Identifiers for the results of analysis for a Module and for a whole Report
    /// </summary>
    public enum AnalysisResult
    {
        /// <summary>
        /// Analysis is still in progress
        /// </summary>
        InProgress,

        /// <summary>
        /// Analysis completed successfully
        /// </summary>
        Success,

        /// <summary>
        /// Analysis failed
        /// </summary>
        Failure,

        /// <summary>
        /// Analysis was cancelled by the user
        /// </summary>
        Cancelled
    }
}
