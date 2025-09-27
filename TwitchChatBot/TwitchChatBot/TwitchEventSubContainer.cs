using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchChatBot;

internal class TwitchEventSubContainer
{
    public static  TwitchClientContainer clientContainer = new TwitchClientContainer();
    public static TTSContainer tTSContainer = new TTSContainer();
    public static SoundPlayer soundPlayer = new SoundPlayer();
    private static string ClientId = "YOUR_CLIENT_ID";
    private static string ClientSecret = "YOUR_CLIENT_SECRET";
    private const string WebSocketUri = "wss://eventsub.wss.twitch.tv/ws";
    private static string accountID = "152046433";  // Replace with actual broadcaster user ID
    private static string AccessToken = "YOUR_ACCESS_TOKEN";  // User access token with required scope
    private static string sessionId = null;

    public static async Task Initialize(string clientID, string clientSecret, string accessToken)
    {
        ClientId = clientID;
        ClientSecret = clientSecret;
        AccessToken = accessToken;

        Console.WriteLine("Twitch WebSocket EventSub kliens indítása...");

        // Step 1: Initial connection to WebSocket
        await ConnectToWebSocket(WebSocketUri);
    }

    private static async Task SubscribeToChannelPoints()
    {
        using (HttpClient client = new HttpClient())
        {
            // Set the Authorization header with the user access token
            client.DefaultRequestHeaders.Add("Client-Id", ClientId);
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + AccessToken);

            // Define the subscription request body
            var subscriptionRequest = new
            {
                type = "channel.channel_points_custom_reward_redemption.add",  // Event type for channel point redemptions
                version = "1",  // API version
                condition = new
                {
                    broadcaster_user_id = accountID  // The broadcaster's user ID to listen for
                },
                transport = new
                {
                    method = "websocket",  // Using WebSocket as transport
                    session_id = sessionId  // Use the WebSocket session ID from the connection
                }
            };

            // Convert subscription request to JSON
            string jsonContent = JsonConvert.SerializeObject(subscriptionRequest);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            try
            {
                // Send the request to subscribe
                HttpResponseMessage response = await client.PostAsync("https://api.twitch.tv/helix/eventsub/subscriptions", content);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Subscription successful!");
                    Console.WriteLine(responseBody);  // You can log the response to get the subscription ID and other details
                }
                else
                {
                    Console.WriteLine("Subscription failed!");
                    Console.WriteLine(responseBody);  // This will show error details if the request fails
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    private static async Task ConnectToWebSocket(string uri)
    {
        using (ClientWebSocket socket = new ClientWebSocket())
        {
            // Connect to the WebSocket
            await socket.ConnectAsync(new Uri(uri), CancellationToken.None);
            Console.WriteLine("Kapcsolódva a WebSocket-hez...");

            // Receive messages in a loop
            var buffer = new byte[1024 * 4];
            while (socket.State == WebSocketState.Open)
            {
                Console.WriteLine("Still going...");
                try
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        Console.WriteLine("WebSocket kapcsolat bezárva.");
                    }
                    else
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        Console.WriteLine($"Üzenet érkezett: {message}");

                        // Process the message to extract session_id from the "session_welcome" message
                        if (message.Contains("\"message_type\":\"session_welcome\""))
                        {
                            var jsonMessage = JsonConvert.DeserializeObject<dynamic>(message);
                            sessionId = jsonMessage.payload.session.id;  // Extract session ID
                            Console.WriteLine($"Session ID: {sessionId}");

                            // Now that we have the session_id, we can subscribe to the event
                            await SubscribeToChannelPoints();
                        }
                        else if (message.Contains("\"message_type\":\"notification\""))
                        {
                            // If the message is a notification with an event section, handle it
                            HandleEventNotification(message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Hiba történt az üzenet fogadása közben: {ex.Message}");
                }
            }
        }
    }

    private static void HandleEventNotification(string message)
    {
        try
        {
            // Deserialize the incoming message into a JObject to handle dynamic properties safely
            var jsonMessage = JsonConvert.DeserializeObject<JObject>(message);

            // Ensure that the 'event' section is present
            if (jsonMessage["payload"] != null && jsonMessage["payload"]["event"] != null)
            {
                var eventData = jsonMessage["payload"]["event"];

                // Get the reward details
                var rewardTitle = eventData["reward"]?["title"]?.ToString();
                var rewardCost = eventData["reward"]?["cost"]?.ToString();
                var rewardMsg = eventData["user_input"]?.ToString();

                // Check if both reward title and cost are available
                if (!string.IsNullOrEmpty(rewardTitle) && !string.IsNullOrEmpty(rewardCost))
                {
                    // Display the reward title and cost
                    Console.WriteLine($"Reward: {rewardTitle} | Cost: {rewardCost} points {rewardMsg} ");

                    ///ide hang effekt épitése
                    ///
                    // clientContainer.TTS(rewardMsg);
                    // 
                    soundPlayer.InitSounds(rewardTitle);

                    bool soundRedeemed = SoundPlayer.issoundRedeem;


                    if (soundRedeemed)
                    {
                        return;
                    }
                    else 
                    {
                        Console.WriteLine($"Ez lesz felolvasva: {rewardMsg}");
                        tTSContainer.TTSInitialize(rewardMsg);
                    }

                }
                else
                {
                    Console.WriteLine("Reward details not available in the event.");
                }
            }
            else
            {
                Console.WriteLine("No event data found in the message.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hiba történt az esemény feldolgozása közben: {ex.Message}");
        }
    }
}

