# Diagnostics
Project Auditor reports several types of recommendations. Each of them is uniquely identified by a string ID containing 3 letters, followed by 4 digits. 

The main types of recommendations are related to Code and Settings. Their IDs start with PAC and PAS respectively (Project Auditor Code/Setting).

Note that there are different ranges within both Code and Settings diagnostic IDs:
- Code:
  - `PAC0xxx`: for Unity API
  - `PAC1xxx`: for System.*
  - `PAC2xxx`: other IDs defined in code rather than in the json
- Settings:
  - `PAS0xxx`: Unity settings 
  - `PAS1xxx`: other settings IDs defined in code rather than in the json
  - `PAT0xxx`: texture related settings
  - `PAM0xxx`: mesh related settings

# Settings Diagnostics
This is a full list of all builtin settings diagnostics:

| ID      | Title                                       | Settings          | Platforms                |
|---------|---------------------------------------------|-------------------|--------------------------|
| PAS0000 | Metal API Validation                        | Player            | iOS, StandaloneOSX, tvOS |
| PAS0001 | Graphics Jobs (Experimental)                | Player            | Any                      |
| PAS0002 | Accelerometer                               | Player            | iOS                      |
| PAS0003 | Building multiple architecture              | Player            | iOS                      |
| PAS0004 | Building multiple architecture              | Player            | Android                  |
| PAS0005 | Metal & OpenGLES APIs                       | Player            | iOS                      |
| PAS0006 | Metal API                                   | Player            | iOS                      |
| PAS0007 | Prebake Collision Meshes                    | Player            | Any                      |
| PAS0008 | Optimize Mesh Data                          | Player            | Any                      |
| PAS0009 | Engine Code Stripping                       | Player            | Android, iOS, WebGL      |
| PAS0010 | Data Caching                                | Player            | WebGL                    |
| PAS0011 | Linker Target                               | Player            | WebGL                    |
| PAS0012 | Auto Sync Transforms                        | Physics           | Any                      |
| PAS0013 | Layer Collision Matrix                      | Physics           | Any                      |
| PAS0014 | Auto Sync Transforms                        | Physics2D         | Any                      |
| PAS0015 | Layer Collision Matrix                      | Physics2D         | Any                      |
| PAS0016 | Fixed Timestep                              | Time              | Any                      |
| PAS0017 | Maximum Allowed Timestep                    | Time              | Any                      |
| PAS0018 | Quality Levels                              | Quality           | Any                      |
| PAS0019 | Texture Quality                             | Quality           | Any                      |
| PAS0020 | Async Upload Time Slice                     | Quality           | Any                      |
| PAS0021 | Async Upload Buffer Size                    | Quality           | Any                      |
| PAS0022 | Shader Quality                              | Graphics          | Any                      |
| PAS0023 | Forward Rendering                           | Graphics          | Any                      |
| PAS0024 | Deferred Rendering                          | Graphics          | Any                      |
| PAS0025 | Managed Code Stripping                      | Player            | Android                  |
| PAS0026 | Managed Code Stripping                      | Player            | iOS                      |
| PAS0027 | Mipmap Stripping                            | Player            | Any                      |
| PAS0028 | Reuse Collision Callbacks                   | Physics           | Any                      |
| PAS0029 | Splash Screen                               | Player            | Any                      |
| PAS1000 | Hybrid Rendering Static batching            | Player            | Any                      |
| PAS1001 | Lit Shader Mode Forward and Deferred        | HDRP              | Any                      |
| PAS1002 | Camera Lit Shader Mode Forward and Deferred | HDRP              | Any                      |
| PAS1003 | Fog Mode Stripping                          | Graphics          | Any                      |
| PAS1004 | IL2CPP Compiler Master                      | Player            | Any                      |
| PAS1005 | IL2CPP Compiler Debug                       | Player            | Any                      |
| PAS1006 | LightMap Streaming                          | Player            | Any                      |
| PAS1007 | Texture Streaming Enabled                   | Quality           | Any                      |
| PAS1008 | SRP Batcher Enabled                         | SRP Asset         | Any                      |
| PAS1009 | URP Asset                                   | Graphics, Quality | Android, iOS, Switch     |
| PAS1010 | URP HDR                                     | Graphics, Quality | Android, iOS, Switch     |
| PAS1011 | URP MSAA                                    | Graphics, Quality | Android, iOS, Switch     |


# Asset Diagnostics
Builtin asset-specific diagnostics:

| ID      | Title                                          | Settings  | Platforms            |
|---------|------------------------------------------------|-----------|----------------------|
| PAT0000 | Texture: Mipmaps not enabled                   | Graphics  | Any                  |
| PAT0001 | Texture: Mipmaps enabled on Sprite/UI texture  | Graphics  | Any                  |
| PAT0002 | Texture: Read/Write enabled                    | Graphics  | Any                  |
| PAT0003 | Texture: Streaming Mipmaps not enabled         | Graphics  | Any                  |
| PAT0004 | Texture: Anisotropic level is more than 1      | Graphics  | Android, iOS, Switch |
| PAT0005 | Texture: Solid color texture bigger than 1x1   | Graphics  | Any                  |
| PAM0000 | Mesh: Read/Write enabled                       | Graphics  | Any                  |
| PAM0001 | Mesh: Index Format is 32 bits                  | Graphics  | Any                  |
| PAS0000 | Shader: Not compatible with SRP batcher        | Graphics  | Any                  |
