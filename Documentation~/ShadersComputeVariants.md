<a name="Compute Shader Variants"></a>
# Compute Shader Variants View
The Compute Shader Variants View reports all compute shader variants included in the build.

Note that built-in shaders are included only after building the project for the target platform.

When you first view the Compute Shader Variants View, you may find that the table is empty and the Information panel contains
instructions for building your project. The shader stripping process only happens by triggering a build, so Project
Auditor requires a clean build to be triggered whilst the window is open.

## Viewing built compute shader variants
To view the built Shader Variants, run your build pipeline and Refresh:
* Click the **Clear** button
* Build the project and/or Addressables/AssetBundles
* Click the **Refresh** button

The table columns are as follows:

| Column Name                                            | Column Description                                                                                                                                                                                                                                                     | 
|--------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Shader Name**                                        | The name of the compute shader that includes this shader variant.                                                                                                                                                                                                      |
| **Tier**                                               | In the Built-in Render Pipeline, indicates the hardware tier that this shader variant targets. See [Graphics tiers and shader variants](https://docs.unity3d.com/Manual/graphics-tiers.html#shader-variants) for more information.                                     |
| **Kernel**                                             | The name of the compute kernel.                                                                                                                                                                                                                                        |
| **Kernel Thread Count** *(Unity 2021.2 or newer only)* | The total thread count of the compute kernel.                                                                                                                                                                                                                          |
| **Keywords**                                           | A list of all the shader keywords that represent this specific shader variant. See the selected item detail panel to the right for an alternative view of this keyword list.                                                                                           |
| **Platform Keywords**                                  | A list of all the [BuiltInShaderDefine](https://docs.unity3d.com/ScriptReference/Rendering.BuiltinShaderDefine.html) keywords set by the Editor for this shader variant. See the selected item detail panel to the right for an alternative view of this keyword list. |
