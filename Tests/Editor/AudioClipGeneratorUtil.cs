using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Random = System.Random;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

// WAV generation adapted from https://www.dima.to/blog/how-to-generate-wave-files-with-random-audio-data-in-c/
namespace Unity.ProjectAuditor.EditorTests
{
    internal static class AudioClipGeneratorUtil
    {
        public static byte[] CreateTestWav(int numSamples, ushort numChannels, int frequency)
        {
            var random = new Random();
            var data = new float[numSamples * numChannels];
            for (var i = 0; i < data.Length; i++)
            {
                // Poor quality not-white noise, but at least it's data. Feel free to replace with sine waves or whatever if needed.
                data[i] = (float)random.Next(0, 65537) / 65536;
                if (random.Next(0, 2) == 0)
                    data[i] = -data[i];
            }

            var stream = new MemoryStream();

            // The following values are based on http://soundfile.sapp.org/doc/WaveFormat/
            const ushort bitsPerSample = (ushort)16;
            const string chunkId = "RIFF";
            const string format = "WAVE";
            const string subChunk1Id = "fmt ";
            const uint subChunk1Size = (uint)16;
            const ushort audioFormat = (ushort)1;
            var sampleRate = (uint)frequency;
            var byteRate = (uint)(sampleRate * numChannels * bitsPerSample / 8);
            var blockAlign = (ushort)(numChannels * bitsPerSample / 8);
            const string subChunk2Id = "data";
            var subChunk2Size = (uint)(data.Length * numChannels * bitsPerSample / 8);
            var chunkSize = (uint)(36 + subChunk2Size);
            // Start writing the file.
            WriteString(stream, chunkId);
            WriteInteger(stream, chunkSize);
            WriteString(stream, format);
            WriteString(stream, subChunk1Id);
            WriteInteger(stream, subChunk1Size);
            WriteShort(stream, audioFormat);
            WriteShort(stream, numChannels);
            WriteInteger(stream, sampleRate);
            WriteInteger(stream, byteRate);
            WriteShort(stream, blockAlign);
            WriteShort(stream, bitsPerSample);
            WriteString(stream, subChunk2Id);
            WriteInteger(stream, subChunk2Size);
            foreach (var sample in data)
            {
                // De-normalize the samples to 16 bits.
                var deNormalizedSample = (short)0;
                if (sample > 0)
                {
                    var temp = sample * short.MaxValue;
                    if (temp > short.MaxValue)
                        temp = short.MaxValue;
                    deNormalizedSample = (short)temp;
                }
                if (sample < 0)
                {
                    var temp = sample * (-short.MinValue);
                    if (temp < short.MinValue)
                        temp = short.MinValue;
                    deNormalizedSample = (short)temp;
                }
                WriteShort(stream, (ushort)deNormalizedSample);
            }

            return stream.GetBuffer();
        }

        private static void WriteString(Stream stream, string value)
        {
            foreach (var character in value)
                stream.WriteByte((byte)character);
        }

        private static void WriteInteger(Stream stream, uint value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
            stream.WriteByte((byte)((value >> 16) & 0xFF));
            stream.WriteByte((byte)((value >> 24) & 0xFF));
        }

        private static void WriteShort(Stream stream, ushort value)
        {
            stream.WriteByte((byte)(value & 0xFF));
            stream.WriteByte((byte)((value >> 8) & 0xFF));
        }
    }
}
