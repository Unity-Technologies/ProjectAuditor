using System.Collections.Generic;
using UnityEngine.Profiling;
using UnityEditor;
using UnityEngine;


namespace Unity.ProjectAuditor.Editor.Modules   // was namespace MyNamespace
{
    class TextureReportModule : ProjectAuditorModule 

    {
    static readonly IssueLayout k_IssueLayout = new IssueLayout
    {
        category = ProjectAuditor.GetOrRegisterCategory("Texture Analysis"),
        properties = new[]
        {
            new PropertyDefinition { type = PropertyType.Description, name = "Texture Analysis", longName = "Results of Texture Analysis "},
            new PropertyDefinition { type = PropertyType.Area, name = "Texture Recommendations", longName = "Recommendations for Optimizations for the Textures in the project." }
        }
    };

    public override IEnumerable<IssueLayout> GetLayouts()
    {
        yield return k_IssueLayout;
    }

    public override void Audit(ProjectAuditorParams projectAuditorParams, IProgress progress = null)
    {
        // Implement your analysis here

        FindTheTextures.TextureSearchAndReport();
        

        // Create an issue
    
            projectAuditorParams.onIssueFound(new ProjectIssue(IssueCategory.Texture,"Reviewing Texture Properties for Optimization."));

        // Notify the user that the analysis of this module is complete
        projectAuditorParams.onComplete();
    }

    // ONLY FOR External Modules, not internal
    /*
    [InitializeOnLoadMethod] 
    static void RegisterView()       // did comment out body for testing
    { 
        ViewDescriptor.Register(new ViewDescriptor
        {
            category = k_IssueLayout.category,
            name = "Game Object Counts Name",
            menuLabel = "CustomChecks/Game Object Counts",
            showFilters = true
        });
         
    }
    */
    }
}

public class FindTheTextures : MonoBehaviour
{
    public static string[] searchTheseFolders;                 // Manually Choose Folders to do the FindAssets search within.
   public static void TextureSearchAndReport()
    {
       var allTextures = AssetDatabase.FindAssets("t: Texture", searchTheseFolders);
        
     //  Debug.Log("Locations Searched: " +String.Join(" & ", new List<string>(searchTheseFolders).ConvertAll(i => i.ToString())));
        
       foreach (string aTexture in allTextures)
       {
           var path = AssetDatabase.GUIDToAssetPath(aTexture);
           Texture2D t = (Texture2D)AssetDatabase.LoadAssetAtPath(path, typeof(Texture2D));

           Debug.Log("Texture: " + t.name
                                 + "\t Location: " + path
                                 + "\n \t Format: " + t.format
                                 + "\n \t Shape: " + t.dimension
                                 + "\n \t Read/Write: " + t.isReadable
                                 + "\n \t Streaming MipMaps: " + t.streamingMipmaps
                                 + "\n \t Minimum MipMap Level: " + t.minimumMipmapLevel
                                 + "\n \t Resolution: " + t.width +"x" + t.height
                                 + "\n \t Size On Disk: " + ((Profiler.GetRuntimeMemorySizeLong(t)  / 1024f) / 1024f)   + "MB" 
                                 + " \n "); 
            
           // var thatTextureImporter = new TextureImporterPlatformSettings();
           // Debug.Log("Platform Specific Quality setting is currently: " + thatTextureImporter.compressionQuality);
       }
    }
    
}