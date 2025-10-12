using System;
using System.IO;
using System.Threading.Tasks;
using Whisper.net;

namespace Kuvasz_TwitchBot
{
    internal class VoiceToText
    {
        private readonly string _modelPath;

        public VoiceToText(string modelPath)
        {
            _modelPath = Path.GetFullPath(modelPath);
        }

        public async Task<string> TranscribeAsync(string audioFilePath)
        {
            if (!File.Exists(_modelPath))
                throw new FileNotFoundException($"Nem található a Whisper modell: {_modelPath}");

            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException($"Nem található az audió fájl: {audioFilePath}");

            using var factory = WhisperFactory.FromPath(_modelPath);
            using var processor = factory.CreateBuilder()
                                         .WithLanguage("hu")     // magyar
                                         .Build();

            using var audioStream = File.OpenRead(audioFilePath);

            var sb = new System.Text.StringBuilder();
            await foreach (var result in processor.ProcessAsync(audioStream))
                sb.Append(result.Text);

            return sb.ToString();
        }
    }
}
