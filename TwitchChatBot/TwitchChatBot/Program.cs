using System.Net.Sockets;
using TwitchLib.Api;

namespace TwitchChatBot;

internal class Program
{
    public static TwitchAPI API;
    public static TwitchClientContainer ClientContainer = new TwitchClientContainer();
    public static TwitchPubSubContainer PubSubContainer = new TwitchPubSubContainer();
    public static TwitchEventSubContainer EventSubContainer = new TwitchEventSubContainer();
    public static TTSContainer tTSContainer = new TTSContainer();
    public static SoundPlayer soundPlayer = new SoundPlayer();


    #region
    public static string BotUsername = "Geppo2tv";
    public static string BotOAuth = "41b41tlm1n6hp23b9vdjwxwi1zu1zd"; // Ne tárold az OAuth tokent így, biztonsági problémákhoz vezethet!
    public static string refreshToken = "3xc8g6jcm6fqvs7ve5eka9nk3vvbdfkoxroyomh1149pt6oaeg";

    // Az API kulcsok oldMOnkcb
    public static string clientID = "sr9wohpxzd484uil8jbwfixp8x8jnb";
    public static string secret = "jhquro5zfr1gzvwagf06qmbwyr7oto";



    #endregion

    static async Task Main(string[] args)
    {
        
        API = new TwitchAPI();
        API.Settings.Secret = secret;
        API.Settings.ClientId = clientID;
        API.Settings.AccessToken = BotOAuth;

        string userName = "Geppo2tv";
        await GetUserId(userName);

        // Az OAuth token frissítése
        await RefreshMyToken();


        ClientContainer.Initialize(BotUsername, BotOAuth);

        // ClientContainer inicializálása
        await TwitchEventSubContainer.Initialize(clientID, secret, BotOAuth);

        Console.ReadLine();
    }
    public static async Task RefreshMyToken()
    {
        try
        {
            var refreshResult = await API.Auth.RefreshAuthTokenAsync(refreshToken, secret, clientID);
            BotOAuth = refreshResult.AccessToken;
            Console.WriteLine($"Token refreshed successfully: {BotOAuth}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing token: {ex.Message}");
        }
    }

    public static async Task GetUserId(string userName)
    {
        try
        {
            // Kérj le felhasználói adatokat a Twitch API-ból
            var usersResponse = await API.Helix.Users.GetUsersAsync(logins: new List<string> { userName });

            if (usersResponse.Users.Length > 0)
            {
                // A válaszból kiemelhetjük a felhasználói ID-t
                string userId = usersResponse.Users[0].Id;
                Console.WriteLine($"Felhasználó ID: {userId}");
            }
            else
            {
                Console.WriteLine("A felhasználó nem található.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hiba történt: {ex.Message}");
        }
    }
}

