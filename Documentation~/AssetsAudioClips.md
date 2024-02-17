<a name="AssetsAudioClips"></a>
# AudioClips View
The AudioClips View shows all AudioClip assets in the project's Assets folder, along with their properties and asset
import settings.

Note: The Packages folder is excluded from this scan; only the Assets folder is reviewed.

The table columns are as follows:

| Column Name                 | Column Description                                                                                                                                                                                    | 
|-----------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **Name**                    | The AudioClip file name.                                                                                                                                                                              |
| **Format**                  | The file format of the source audio file.                                                                                                                                                             |
| **Length**                  | The duration of the AudioClip, in the format `minutes:seconds.milliseconds`.                                                                                                                          |
| **Source File Size**        | The source asset file size.                                                                                                                                                                           |
| **Imported File Size**      | The imported AudioClip asset file size.                                                                                                                                                               |
| **Runtime Size (Estimate)** | An estimate of the runtime memory footprint of one instance of this AudioClip, if it is playing. Includes estimated sizes of buffers for decompression and streaming, if the AudioClip requires them. |
| **Compression Ratio**       | Compression ratio, calculated from the Source File Size and Imported File Size.                                                                                                                       |
| **Compression Format**      | The compression format of the imported AudioClip asset.                                                                                                                                               |
| **Sample Rate**             | The imported AudioClip's sample rate, in hertz (Hz) or kilohertz (kHz).                                                                                                                               |
| **Force To Mono**           | Whether the **Force To Mono** checkbox is ticked in the import settings.                                                                                                                              |
| **Load In Background**      | Whether the **Load In Background** checkbox is ticked in the import settings.                                                                                                                         |
| **Preload Audio Data**      | Whether the **Preload Audio Data** checkbox is ticked in the import settings.                                                                                                                         |
| **Load Type**               | The [AudioClipLoadType](https://docs.unity3d.com/ScriptReference/AudioClipLoadType.html) selected in the import settings: DecompressOnLoad, CompressedInMemory or Streaming.                          |
| **Path**                    | The full path to the source asset within the Assets folder.                                                                                                                                           |
