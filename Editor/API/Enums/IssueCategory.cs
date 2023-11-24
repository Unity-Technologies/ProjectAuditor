using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Unity.ProjectAuditor.Editor
{
    // stephenm TODO: Change the summary to the following in API phase 2, when we expose the ability to make custom modules. Right now it doesn't make sense to expose or mention GetOrRegisterCategory()
    // Options for predefined categories of issues. Note that it is possible to register new categories at editor-time via ProjectAuditor.<seealso cref="ProjectAuditor.GetOrRegisterCategory"/>

    /// <summary>
    /// Identifiers for all the categories of issues reported by Project Auditor.
    /// </summary>
    /// <remarks>
    /// As Project Auditor's remit has expanded, so has the definition of what constitutes an issue category.
    /// For example, categories relating to assets, shaders or build reports represent categories of information about the project's content but do not necessarily qualify as issues that should be addressed.
    /// </remarks>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum IssueCategory
    {
        /// <summary>
        /// Category for General statistics about the analysis process and its results.
        /// </summary>
        Metadata,

        /// <summary>
        /// Issues relating to asset data or asset import settings
        /// </summary>
        AssetDiagnostic,

        /// <summary>
        /// Category for reporting shaders in the project
        /// </summary>
        Shader,

        /// <summary>
        /// Category for reporting shader variants
        /// </summary>
        ShaderVariant,

        /// <summary>
        /// Code diagnostic issues, discovered by static code analysis
        /// </summary>
        Code,

        /// <summary>
        /// Compiler errors and warnings generated whilst compiling code for static analysis
        /// </summary>
        CodeCompilerMessage,

        /// <summary>
        /// Instances of generic data types found in code. Reported because excessive use of generics can contribute to increased memory usage for IL2CPP metadata.
        /// </summary>
        GenericInstance,

        /// <summary>
        /// Issues relating to project settings
        /// </summary>
        ProjectSetting,

        /// <summary>
        /// Category for displaying information about files created during the project build process
        /// </summary>
        BuildFile,

        /// <summary>
        /// Category for displaying information about the steps of the build process and how long they took
        /// </summary>
        BuildStep,

        /// <summary>
        /// Category for build summary information
        /// </summary>
        BuildSummary,

        /// <summary>
        /// Category for information about all of the code assemblies in the project
        /// </summary>
        Assembly,

        /// <summary>
        /// Category for information about precompiled assemblies
        /// </summary>
        PrecompiledAssembly,

        /// <summary>
        /// Issues reported by the shader compiler
        /// </summary>
        ShaderCompilerMessage,

        /// <summary>
        /// Category for displaying installed packages
        /// </summary>
        Package,

        /// <summary>
        /// Category for package diagnostic information
        /// </summary>
        PackageDiagnostic,

        /// <summary>
        /// Issues relating to texture assets and texture import settings
        /// </summary>
        Texture,

        /// <summary>
        /// Issues relating to Audio Clip assets and import settings
        /// </summary>
        AudioClip,

        /// <summary>
        /// Category for displaying variants of compute shaders
        /// </summary>
        ComputeShaderVariant,

        /// <summary>
        /// Issues relating to Mesh assets and import settings
        /// </summary>
        Mesh,

        /// <summary>
        /// Issues relating to Sprite Atlas assets and import settings
        /// </summary>
        SpriteAtlas,

        /// <summary>
        /// Category for showing materials grouped by shader
        /// </summary>
        Material,

        /// <summary>
        /// Issues relating to animator controllers
        /// </summary>
        AnimatorController,

        /// <summary>
        /// Issues relating to animation clips
        /// </summary>
        AnimationClip,

        /// <summary>
        /// Issues relating to avatars
        /// </summary>
        Avatar,

        /// <summary>
        /// Issues relating to avatar masks
        /// </summary>
        AvatarMask,

        /// <summary>
        /// Issues that could result in undesired behavior if domain reloading is disabled
        /// </summary>
        DomainReload,

        /// <summary>
        /// Enum value indicating the first available custom category
        /// </summary>
        FirstCustomCategory,

        /// <summary>
        /// Issues relating to mesh assets inside build data
        /// </summary>
        BuildDataMesh,

        /// <summary>
        /// Issues relating to animation clip assets inside build data
        /// </summary>
        BuildDataAnimationClip,

        /// <summary>
        /// Issues relating to shader assets inside build data
        /// </summary>
        BuildDataShader,

        /// <summary>
        /// Issues relating to texture assets inside build data
        /// </summary>
        BuildDataTexture2D,

        /// <summary>
        /// Issues relating to audio clip assets inside build data
        /// </summary>
        BuildDataAudioClip,

        /// <summary>
        /// Issues summarizing serialized object types inside build data
        /// </summary>
        BuildDataSummary,

        /// <summary>
        /// Issues listing all serialized object types inside build data
        /// </summary>
        BuildDataList,

        /// <summary>
        /// Issues relating to build data
        /// </summary>
        BuildDataDiagnostic,
    }
}
