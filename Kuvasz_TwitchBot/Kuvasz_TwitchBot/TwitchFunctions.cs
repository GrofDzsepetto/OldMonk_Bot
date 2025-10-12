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
            foreach (string key in ctx.Request.QueryString.AllKeys)
            {
                Console.WriteLine($"{key}: {ctx.Request.QueryString[key]}");
            }
            var code = ctx.Request.QueryString["code"];
            var buf = Encoding.UTF8.GetBytes("<html><body>Csak Kuvasznak sikerülhet, ha kuvasz vagy akkor sikerült</body></html>");
            ctx.Response.ContentLength64 = buf.Length;
            await ctx.Response.OutputStream.WriteAsync(buf, 0, buf.Length);
            listener.Stop();
            return code ?? throw new Exception("Auth code nem érkezett meg.");
        }
        public record TwitchTokenResponse(string access_token, string refresh_token, int expires_in, string token_type, string[] scope);
        public static async Task<string> GetAccessToken(string clientId, string clientSecret, string code)
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

            var token = System.Text.Json.JsonSerializer.Deserialize<TwitchTokenResponse>(json);
            if (token is null || string.IsNullOrWhiteSpace(token.access_token))
                throw new Exception("Nem sikerült kinyerni az access_token-t a válaszból.");

            return token.access_token;
        }
        public static async Task SendMessageToTwitch(string oauthToken, string botUsername, string channel, string message)
        {
            using var client = new TcpClient("irc.chat.twitch.tv", 6667);
            using var writer = new StreamWriter(client.GetStream()) { AutoFlush = true };
            using var reader = new StreamReader(client.GetStream());

            await writer.WriteLineAsync($"PASS oauth:{oauthToken}");
            await writer.WriteLineAsync($"NICK {botUsername}");
            await writer.WriteLineAsync($"JOIN #{channel}");
            await writer.WriteLineAsync($"PRIVMSG #{channel} :{message}");
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
