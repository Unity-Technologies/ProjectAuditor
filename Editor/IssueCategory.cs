namespace Unity.ProjectAuditor.Editor
{
    /// <summary>
    /// Predefined categories of issues. Note that it is possible to register new categories at editor-time via ProjectAuditor.GetOrRegisterCategory()
    /// </summary>
    public enum IssueCategory
    {
        MetaData,
        Resource,
        Shader,
        ShaderVariant,
        Code,
        CodeCompilerMessage,
        GenericInstance,
        ProjectSetting,
        BuildFile,
        BuildStep,
        BuildSummary,
        Assembly,
        PrecompiledAssembly,
        ShaderCompilerMessage,
        Package,
        PackageVersion,
        Texture,
        AudioClip,
        FirstCustomCategory
    }
}
