using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using static Kuvasz_TwitchBot.TwitchFunctions;

namespace Kuvasz_TwitchBot
{
    internal class Program
    {
        public static string redirectUri = "http://localhost:3000/oldmonk/";
        private static string accesToken = "";
        static async Task Main(string[] args)
        {
            bool useUi = args != null && args.Any(a => string.Equals(a, "--ui", StringComparison.OrdinalIgnoreCase));

            if (useUi)
            {
                var ui = new UIRender();
                await ui.Start();
            }
            else
            {
                await MainFunction();
                Console.WriteLine("Kész. Nyomj Entert a kilépéshez.");
            }

            Console.ReadLine();
        }


        private static async Task MainFunction()
        {
            Console.WriteLine("[Main] -- Start");
            var settings = LoadJson<Settings>("settings.json");
            await ManageLoginAndSync();
            await RecordVoiceAndSendMessage(accesToken, settings.model, settings.boundKey);
        }

        private static async Task RecordVoiceAndSendMessage(string oauthToken, string modell, string boundKey)
        {
            var recorder = new AudioManager();
            var vtt = new VoiceToText($"models\\ggml-{modell}.bin");

            while (true)
            {
                Console.WriteLine($"Nyomj {boundKey} gombot a felvétel indításához/leállításához (ESC = kilép).");

                bool recording = false;
                while (true)
                {
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Escape)
                        return;

                    if (!recording)
                    {
                        recorder.StartRecord("mic.wav");
                        recording = true;
                        Console.WriteLine("Felvétel indult... (nyomj megint gombot a leállításhoz)");
                    }
                    else
                    {
                        recorder.StopRecord();
                        Console.WriteLine("Felvétel leállítva, feldolgozás...");
                        break;
                    }
                }

                var text = await vtt.TranscribeAsync(@"mic.wav");
                Console.WriteLine("Felismert szöveg:");
                Console.WriteLine(text);

                string cleanMessage = MessageCleaner.turnToCommand(text);

                var chatMessage = $"";
                if (cleanMessage != text)
                {
                    chatMessage = cleanMessage;
                }
                else 
                {
                    chatMessage = $"MrDestructoid : {cleanMessage}";
                }
                Console.WriteLine("Clean Message: " + cleanMessage);

                await SafeSendMessage(chatMessage);
            }
        }
        

        public static async Task ManageLoginAndSync()
        {
            var cfg = LoadJson<TwitchConfig>("secret.json");
            string tokenFile = "tokens.json";
            var tokens = LoadTokens(tokenFile);

            if (tokens == null)
            {
                var code = await TwitchFunctions.GetTwitchAuthCode(cfg.client_id, cfg.client_secret, cfg.scopes);
                tokens = await TwitchFunctions.GetAccessToken(cfg.client_id, cfg.client_secret, code);
                SaveTokens(tokens, tokenFile);
            }
            else if (!IsTokenValid(tokens) || string.IsNullOrWhiteSpace(tokens.RefreshToken))
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(tokens.RefreshToken))
                        throw new Exception("missing refresh");

                    tokens = await RefreshAccessToken(cfg.client_id, cfg.client_secret, tokens.RefreshToken);
                    SaveTokens(tokens, tokenFile);
                }
                catch
                {
                    var code = await TwitchFunctions.GetTwitchAuthCode(cfg.client_id, cfg.client_secret, cfg.scopes);
                    tokens = await TwitchFunctions.GetAccessToken(cfg.client_id, cfg.client_secret, code);
                    SaveTokens(tokens, tokenFile);
                }
            }

            accesToken = tokens.AccessToken;
            Console.WriteLine("Twitch connection made");
            Console.WriteLine("You can progress to the STT");
        }

        private static async Task SafeSendMessage(string message)
        {
            var cfg = LoadJson<TwitchConfig>("secret.json");
            var tokenFile = "tokens.json";

            try
            {
                await TwitchFunctions.SendMessageToTwitch(accesToken, "oldmonk_bot", "geppo2tv", message);
            }
            catch (TwitchUnauthorizedException)
            {
                Console.WriteLine("Token lejárt/hibás – frissítek…");
                var tokens = LoadTokens(tokenFile);

                if (tokens == null || string.IsNullOrWhiteSpace(tokens.RefreshToken))
                {
                    var code = await TwitchFunctions.GetTwitchAuthCode(cfg.client_id, cfg.client_secret, cfg.scopes);
                    tokens = await TwitchFunctions.GetAccessToken(cfg.client_id, cfg.client_secret, code);
                }
                else
                {
                    tokens = await RefreshAccessToken(cfg.client_id, cfg.client_secret, tokens.RefreshToken);
                }

                SaveTokens(tokens, tokenFile);
                accesToken = tokens.AccessToken;

                await TwitchFunctions.SendMessageToTwitch(accesToken, "oldmonk_bot", "geppo2tv", message);
                Console.WriteLine("Új tokennel elküldve.");
            }
        }

        public static T LoadJson<T>(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"A fájl nem található: {path}");

            string json = File.ReadAllText(path);
            var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true
            });

            if (result == null)
                throw new Exception($"Nem sikerült beolvasni a JSON-t: {path}");

            return result;
        }


        public sealed class TwitchConfig
        {
            public string client_id { get; set; } = "";
            public string client_secret { get; set; } = "";
            public string scopes { get; set; } = "";
        }

        private record Settings(string channelName, string model, string captureMode, string boundKey);
    }
}
