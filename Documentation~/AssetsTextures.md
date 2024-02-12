<a name="AssetsTextures"></a>
# Textures View
The Textures View shows all textures in the project's Assets folder, along with their properties and asset import
settings. 

Note: The Packages folder is excluded from this scan; only the Assets folder is reviewed.

The table columns are as follows:

| Column Name       | Column Description                                                                                                                                                                                                                                                 | 
|-------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Name**          | The texture file name.                                                                                                                                                                                                                                             |
| **Shape**         | The [TextureImporterShape](https://docs.unity3d.com/ScriptReference/TextureImporterShape.html): Texture2D, TextureCube, Texture2DArray or Texture3D.                                                                                                               |
| **Importer Type** | The [TextureImporterType](https://docs.unity3d.com/ScriptReference/TextureImporterType.html) that is selected in the texture's Importer settings.                                                                                                                  |
| **Format**        | The texture format of the imported texture asset on the analysis target platform.                                                                                                                                                                                  |
| **Compression**   | The [TextureImporterCompression](https://docs.unity3d.com/ScriptReference/TextureImporterCompression.html) option for the analysis target platform. This may be the value set in the Default tab of the importer, or may have been overridden on the Platform tab. |
| **MipMaps**       | Whether the **Generate Mip Maps** checkbox is ticked in the import settings.                                                                                                                                                                                       |
| **Readable**      | Whether the **Read/Write Enabled** checkbox is ticked in the import settings.                                                                                                                                                                                      |
| **Resolution**    | Resolution of the imported texture asset. May differ from the source asset resolution depending on the import settings.                                                                                                                                            |
| **Size**          | File size of the imported texture asset.                                                                                                                                                                                                                           |
| **Streaming**     | Whether the **Streaming Mipmaps** checkbox is ticked in the import settings.                                                                                                                                                                                       |
| **Path**          | The full path to the source asset within the Assets folder.                                                                                                                                                                                                        |
