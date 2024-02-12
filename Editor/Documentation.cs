namespace Unity.ProjectAuditor.Editor
{
    static class Documentation
    {
        internal const string baseURL = "https://github.com/Unity-Technologies/ProjectAuditor/blob/";
        internal const string subURL = "/Documentation~/";
        internal const string endURL = ".md";

        internal static string GetPageUrl(string pageName)
        {
            return baseURL + ProjectAuditorPackage.Version + subURL + pageName + endURL;
        }
    }
}
