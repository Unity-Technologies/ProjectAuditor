# Issue Descriptors
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
- Assets:
  - `PAA0xxx`: texture related settings
  - `PAA1xxx`: mesh related settings
  - `PAA2xxx`: shader related settings
  - `PAA3xxx`: file system related settings (Resources folders, StreamingAssets, Addressables, etc)
  - `PAA4xxx`: AudioClip related settings

# Code Descriptors
See `com.unity.project-auditor/Data/ApiDatabase.json` for a partial list of code descriptors. Search the code for "PAC"
for more.

# Settings Descriptors
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
| PAS1012 | URP Stop NaN                                | Graphics          | Android, iOS, Switch     |
| PAS1013 | Entities Graphics Static Batching           | Player            | Any                      |                                             |                   |                          |


# Asset Descriptors
Builtin asset-specific diagnostics:

| ID      | Title                                                      | Settings                    | Platforms                       |
|---------|------------------------------------------------------------|-----------------------------|---------------------------------|
| PAA0000 | Texture: Mipmaps not enabled                               | Graphics                    | Any                             |
| PAA0001 | Texture: Mipmaps enabled on Sprite/UI texture              | Graphics                    | Any                             |
| PAA0002 | Texture: Read/Write enabled                                | Graphics                    | Any                             |
| PAA0003 | Texture: Streaming Mipmaps not enabled                     | Graphics                    | Any                             |
| PAA0004 | Texture: Anisotropic level is more than 1                  | Graphics                    | Android, iOS, Switch            |
| PAA0005 | Texture: Solid color texture bigger than 1x1 with fixer    | Graphics                    | Any                             |
| PAA0006 | Texture: Solid color texture bigger than 1x1 without fixer | Graphics                    | Any                             |
| PAA0007 | Texture Atlas: Too much empty space                        | Graphics                    | Any                             |
| PAA0008 | Sprite Atlas: Too much empty space                         | Graphics                    | Any                             |
| PAA1000 | Mesh: Read/Write enabled                                   | Graphics                    | Any                             |
| PAA1001 | Mesh: Index Format is 32 bits                              | Graphics                    | Any                             |
| PAA2000 | Shader: Not compatible with SRP batcher                    | Graphics                    | Any                             |
| PAA3000 | Files: Resources folder asset & dependencies               | BuildSize                   | Any                             |
| PAA3001 | Files: StreamingAssets folder size                         | BuildSize                   | Any                             |
| PAA4000 | Audio: Long AudioClip is not set to Streaming              | Memory                      | Any                             |
| PAA4001 | Audio: Short AudioClip is set to streaming                 | Memory                      | Any                             |
| PAA4002 | Audio: AudioClip is stereo                                 | Memory                      | Android, iOS, Switch            |
| PAA4003 | Audio: AudioClip is stereo                                 | Memory, Quality             | Any except Android, iOS, Switch |
| PAA4004 | Audio: AudioClip is set to Decompress On Load              | Memory, LoadTime            | Any                             |
| PAA4005 | Audio: Compressed AudioClip is Compressed In Memory        | CPU                         | Any                             |
| PAA4006 | Audio: Compressed clip could be optimized for mobile       | Memory, BuildSize           | Android, iOS, Switch            |
| PAA4007 | Audio: Sample Rate is over 48 kHz                          | Memory, BuildSize, LoadTime | Any                             |
| PAA4008 | Audio: Preload Audio Data is enabled                       | LoadTime                    | Any                             |
| PAA4009 | Audio: Load In Background is not enabled                   | CPU, LoadTime               | Any                             |
| PAA4010 | Audio: Compression Format is MP3                           | Quality                     | Any                             |
| PAA4011 | Audio: Source asset is in a lossy compressed format        | Quality                     | Any                             |
