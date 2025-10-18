using System;
using System.IO;
using System.Net.Sockets;
using System.Net.Security;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net;

namespace Kuvasz_TwitchBot
{
    internal class TwitchFunctions
    {
        public static async Task<string> GetTwitchAuthCode(string clientId, string clientSecret, string scopes)
        {
            var redirect = "http://localhost:3000/oldmonk/";
            using var listener = new HttpListener();
            listener.Prefixes.Add(redirect);
            listener.Start();
            var url = $"https://id.twitch.tv/oauth2/authorize?response_type=code&client_id={Uri.EscapeDataString(clientId)}&redirect_uri={Uri.EscapeDataString(redirect)}&scope={Uri.EscapeDataString(scopes)}&force_verify=true";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            var ctx = await listener.GetContextAsync();
            Console.WriteLine("== QueryString paraméterek ==");
            var code = ctx.Request.QueryString["code"];
            var buf = Encoding.UTF8.GetBytes("<html><body>Csak Kuvasznak sikerülhet, ha kuvasz vagy akkor sikerült</body></html>");
            ctx.Response.ContentLength64 = buf.Length;
            await ctx.Response.OutputStream.WriteAsync(buf, 0, buf.Length);
            listener.Stop();
            return code ?? throw new Exception("Auth code nem érkezett meg.");
        }
        public record TwitchTokenResponse(string access_token, string refresh_token, int expires_in, string token_type, string[] scope);
        public static async Task<TwitchTokens> GetAccessToken(string clientId, string clientSecret, string code)
        {
            var redirect = "http://localhost:3000/oldmonk/";
            using var client = new HttpClient();

            var data = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirect
            };

            var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent(data));
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var token = JsonSerializer.Deserialize<TwitchTokenResponse>(json);

            if (token is null || string.IsNullOrWhiteSpace(token.access_token))
                throw new Exception("Nem sikerült kinyerni az access_token-t.");

            return new TwitchTokens(
                token.access_token,
                token.refresh_token,
                DateTime.UtcNow.AddSeconds(token.expires_in)
            );
        }
        public record TwitchTokens(string AccessToken, string RefreshToken, DateTime ExpiresAt);
        public static void SaveTokens(TwitchTokens tokens, string path)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(tokens);
            File.WriteAllText(path, json);
        }
        public static TwitchTokens? LoadTokens(string path)
        {
            if (!File.Exists(path)) return null;
            var json = File.ReadAllText(path);
            return System.Text.Json.JsonSerializer.Deserialize<TwitchTokens>(json);
        }
        public static bool IsTokenValid(TwitchTokens tokens)
        {
            return tokens.ExpiresAt > DateTime.UtcNow;
        }
        public static async Task<TwitchTokens> RefreshAccessToken(string clientId, string clientSecret, string refreshToken)
        {
            using var client = new HttpClient();
            var data = new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            };

            var response = await client.PostAsync("https://id.twitch.tv/oauth2/token", new FormUrlEncodedContent(data));
            var body = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                // Log everything you can; this makes the cause obvious
                throw new HttpRequestException($"Twitch refresh failed ({(int)response.StatusCode}): {body}");
            }

            var token = JsonSerializer.Deserialize<TwitchTokenResponse>(body);
            if (token is null || string.IsNullOrWhiteSpace(token.access_token))
                throw new Exception("Nem sikerült frissíteni az access token-t (válasz üres/hibás).");

            return new TwitchTokens(
                token.access_token,
                token.refresh_token,
                DateTime.UtcNow.AddSeconds(token.expires_in)
            );
        }
        public class TwitchUnauthorizedException : Exception
        {
            public TwitchUnauthorizedException(string msg) : base(msg) { }
        }

        public static async Task SendMessageToTwitch(string oauthToken, string botUsername, string channel, string message)
        {
            botUsername = botUsername.ToLowerInvariant();
            channel = channel.ToLowerInvariant();

            using var client = new TcpClient();
            await client.ConnectAsync("irc.chat.twitch.tv", 6667);

            using var stream = client.GetStream();
            using var writer = new StreamWriter(stream) { AutoFlush = true, NewLine = "\r\n" };
            using var reader = new StreamReader(stream);

            await writer.WriteLineAsync($"PASS oauth:{oauthToken}");
            await writer.WriteLineAsync($"NICK {botUsername}");

            var welcome = false;
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (sw.Elapsed < TimeSpan.FromSeconds(5))
            {
                if (!stream.DataAvailable) { await Task.Delay(50); continue; }
                var line = await reader.ReadLineAsync();
                if (line is null) break;
                Console.WriteLine("<< " + line);

                if (line.StartsWith("PING"))
                    await writer.WriteLineAsync(line.Replace("PING", "PONG"));

                if (line.Contains("Login authentication failed", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("Improperly formatted auth", StringComparison.OrdinalIgnoreCase))
                    throw new TwitchUnauthorizedException("Login authentication failed");

                if (line.Contains(" 001 "))
                {
                    welcome = true;
                    break;
                }
            }

            if (!welcome)
                throw new Exception("Nem érkezett 001 welcome – sikertelen bejelentkezés?");

            await writer.WriteLineAsync($"JOIN #{channel}");

            await writer.WriteLineAsync($"PRIVMSG #{channel} :{message}");
            await Task.Delay(200);
        }

    }
    public sealed class TwitchToken
    {
        public string access_token { get; set; } = "";
        public string refresh_token { get; set; } = "";
        public int expires_in { get; set; }
        public string token_type { get; set; } = "";
        public string[] scope { get; set; } = Array.Empty<string>();
    }
}
