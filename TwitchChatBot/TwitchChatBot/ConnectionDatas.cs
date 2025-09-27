using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client.Models;

namespace TwitchChatBot
{
    public class TwitchSecrets
    {
        public string BotUsername { get; set; }
        public string BotOAuth { get; set; }
        public string RefreshToken { get; set; }
        public string ClientID { get; set; }
        public string ClientSecret { get; set; }
    }
    public class SecretsFile { public TwitchSecrets Twitch { get; set; } }

    internal class ConnectionDatas
    {
        public ConnectionCredentials Credentials;
        public TwitchAPI API;

        public string BotUsername;
        public string BotOAuth;     
        public string refreshToken;
        public string clientID;
        public string secret;

        private readonly string _secretsPath;

        public ConnectionDatas(string secretsPath = null)
        {
            _secretsPath = secretsPath ?? Path.Combine(AppContext.BaseDirectory, "secrets.json");
        }

        public async Task InitializeAsync()
        {
            var secrets = LoadSecrets(_secretsPath);
            if (secrets?.Twitch == null)
                throw new InvalidOperationException($"Nem található Twitch szekció a secrets fájlban: {_secretsPath}");

            BotUsername = secrets.Twitch.BotUsername;
            BotOAuth = secrets.Twitch.BotOAuth;
            refreshToken = secrets.Twitch.RefreshToken;
            clientID = secrets.Twitch.ClientID;
            secret = secrets.Twitch.ClientSecret;

            API = new TwitchAPI();
            API.Settings.ClientId = clientID;
            API.Settings.Secret = secret;
            API.Settings.AccessToken = BotOAuth;

            await RefreshMyTokenAsync(saveBackToSecrets: true);

            Credentials = new ConnectionCredentials(BotUsername, BotOAuth);
        }

        public async Task RefreshMyTokenAsync(bool saveBackToSecrets = false)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return;

            var refreshResult = await API.Auth.RefreshAuthTokenAsync(refreshToken, secret, clientID);
            if (!string.IsNullOrWhiteSpace(refreshResult.AccessToken))
            {
                BotOAuth = refreshResult.AccessToken;
                API.Settings.AccessToken = BotOAuth;

                if (saveBackToSecrets)
                    SaveAccessToken(_secretsPath, BotOAuth);
            }
            if (!string.IsNullOrWhiteSpace(refreshResult.RefreshToken))
            {
                refreshToken = refreshResult.RefreshToken;
                if (saveBackToSecrets)
                    SaveRefreshToken(_secretsPath, refreshToken);
            }
        }

        private static SecretsFile LoadSecrets(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Nem található secrets.json", path);

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<SecretsFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        private static void SaveAccessToken(string path, string newAccessToken)
        {
            var s = LoadSecrets(path);
            if (s?.Twitch == null) return;
            s.Twitch.BotOAuth = newAccessToken;
            var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }

        private static void SaveRefreshToken(string path, string newRefreshToken)
        {
            var s = LoadSecrets(path);
            if (s?.Twitch == null) return;
            s.Twitch.RefreshToken = newRefreshToken;
            var json = JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }
}
