using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kuvasz_TwitchBot
{
    internal class AudioManager
    {
        private WaveInEvent? waveIn;
        private WaveFileWriter? writer;
        private string? outputFilePath;
        public static void ConvertTo16kMonoWav(string inputFile, string outputFile)
        {
            using var reader = new AudioFileReader(inputFile);
            ISampleProvider sampleProvider;
            if (reader.WaveFormat.Channels == 1)
            {
                sampleProvider = reader;
            }
            else
            {
                sampleProvider = new StereoToMonoSampleProvider(reader);
            }

            var resampler = new WdlResamplingSampleProvider(sampleProvider, 16000);

            WaveFileWriter.CreateWaveFile16(outputFile, resampler);
        }
        public void StartRecord(string filePath = "recorded.wav")
        {
            if (waveIn != null)
            {
                Console.WriteLine("⚠️ Már folyamatban van a felvétel.");
                return;
            }

            outputFilePath = filePath;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputFilePath))!);

            waveIn = new WaveInEvent
            {
                WaveFormat = new WaveFormat(16000, 16, 1)
            };

            writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat);

            waveIn.DataAvailable += (s, a) =>
            {
                var w = writer;
                if (w != null)
                    w.Write(a.Buffer, 0, a.BytesRecorded);
            };

            waveIn.RecordingStopped += (s, a) =>
            {
                writer?.Dispose();
                writer = null;
                waveIn?.Dispose();
                waveIn = null;

                if (a.Exception != null)
                    Console.WriteLine($"❌ Hiba a felvétel közben: {a.Exception.Message}");
            };

            waveIn.StartRecording();
            Console.WriteLine("🎤 Felvétel elindult...");
        }
        public void StopRecord()
        {
            if (waveIn == null)
                return;

            waveIn.StopRecording();
            Console.WriteLine($"🛑 Felvétel leállt. Elmentve ide: {outputFilePath}");
        }
    }
}

