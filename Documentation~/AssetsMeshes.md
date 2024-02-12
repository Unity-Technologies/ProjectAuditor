<a name="AssetsMeshes"></a>
# Meshes View
The Meshes View shows all mesh assets in the project's Assets folder, along with their properties and asset import
settings.

Note: The Packages folder is excluded from this scan; only the Assets folder is reviewed.

The table columns are as follows:

| Column Name        | Column Description                                                                                                                                                                                                                                                                                 | 
|--------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Name**           | The file name of the source FBX file containing this mesh. Note that FBX files can contain multiple meshes, so a single FBX file can be responsible for multiple rows in the table. To group meshes by FBX, enable the **Show Hierarchy** button and select **Group By: Path** from the drop-down. |
| **Vertex Count**   | The number of vertices in the mesh.                                                                                                                                                                                                                                                                |
| **Triangle Count** | The number of triangles in the mesh.                                                                                                                                                                                                                                                               |
| **Compression**    | The [ModelImporterMeshCompression](https://docs.unity3d.com/ScriptReference/ModelImporterMeshCompression.html) option selected in the mesh import settings.                                                                                                                                        |
| **Size**           | File size of the imported mesh asset.                                                                                                                                                                                                                                                              |
| **Path**           | The full path to the source asset within the Assets folder.                                                                                                                                                                                                                                        |

