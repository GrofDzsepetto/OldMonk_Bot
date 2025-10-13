using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using static Kuvasz_TwitchBot.TwitchFunctions;
using static System.Formats.Asn1.AsnWriter;


namespace Kuvasz_TwitchBot
{
    internal class Program
    {
        public static string redirectUri = "http://localhost:3000/oldmonk/";

        static async Task Main(string[] args)
        {
            var cfg = LoadConfig();

            string tokenFile = "tokens.json";
            var tokens = LoadTokens(tokenFile);

            if (tokens == null)
            {
                // AuthCode és AccessToken lekérése egyszer
                var code = await TwitchFunctions.GetTwitchAuthCode(cfg.client_id, cfg.client_secret, cfg.scopes);
                var accessToken = await TwitchFunctions.GetAccessToken(cfg.client_id, cfg.client_secret, code, out var refreshToken);

                tokens = new TwitchTokens(accessToken, refreshToken, DateTime.UtcNow.AddHours(1));
                SaveTokens(tokens, tokenFile);
            }
            else if (!IsTokenValid(tokens))
            {
                tokens = await RefreshAccessToken(cfg.client_id, cfg.client_secret, tokens.RefreshToken);
                SaveTokens(tokens, tokenFile);
            }

            Console.WriteLine("Token készen áll.");
            // await TwitchFunctions.SendMessageToTwitch(tokens.AccessToken, "oldmonk_bot", "geppo2tv", "testmessage");



        // ==================================== VOICE CAPTURE  ====================================
        await RecordVoiceAndSendMessage(tokens.AccessToken);
            Console.ReadLine();
        }

        private static async Task RecordVoiceAndSendMessage(string oauthToken)
        {
            var recorder = new AudioManager();
            var vtt = new VoiceToText(@"models\ggml-medium.bin"); // init egyszer, ne minden körben

            while (true)
            {
                Console.WriteLine("🎹 Nyomj bármilyen gombot a felvétel indításához/leállításához (ESC = kilép).");

                bool recording = false;
                while (true)
                {
                    var key = Console.ReadKey(true);

                    if (key.Key == ConsoleKey.Escape)
                        return; // kilép az egész loopból

                    if (!recording)
                    {
                        recorder.StartRecord("mic.wav");
                        recording = true;
                        Console.WriteLine("🎙️ Felvétel indult... (nyomj megint gombot a leállításhoz)");
                    }
                    else
                    {
                        recorder.StopRecord();
                        Console.WriteLine("🛑 Felvétel leállítva, feldolgozás...");
                        break;
                    }
                }
                var text = await vtt.TranscribeAsync(@"mic.wav");
                Console.WriteLine("Felismert szöveg:");
                Console.WriteLine(text);

                var chatMessage = $"MrDestructoid : {text}";
                await TwitchFunctions.SendMessageToTwitch(oauthToken, "oldmonk_bot", "geppo2tv", chatMessage);
            }
        }
        public static TwitchConfig LoadConfig(string path = "secret.json")
        {
            if (!File.Exists(path))
                throw new FileNotFoundException($"A konfigurációs fájl nem található: {path}");

            string json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<TwitchConfig>(json)
                         ?? throw new Exception("Nem sikerült beolvasni a konfigurációt.");

            return config;
        }
        public sealed class TwitchConfig
        {
            public string client_id { get; set; } = "";
            public string client_secret { get; set; } = "";
            public string scopes { get; set; } = "";
        }

    }
}
