using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using TwitchLib.PubSub;
using Newtonsoft.Json;
using System.Net.Sockets;
using System.Net.WebSockets;
using Microsoft.AspNetCore.DataProtection;

namespace TwitchChatBot
{
    internal class EventSubs
    {

        private static async Task SubscribeToChannelPointsEvent(ClientWebSocket socket, string broadcasterUserId)
        {
            // Generáljunk egy egyedi azonosítót az előfizetéshez
            string subscriptionId = Guid.NewGuid().ToString();
            string requestId = Guid.NewGuid().ToString();

            // Az üzenet objektum helyes formátuma
            var subscribeMessage = new
            {
                type = "subscribe",
                version = "1",
                id = subscriptionId,  // Egyedi azonosító a feliratkozáshoz
                request_id = requestId,  // Egyedi kérés ID
                @event = new
                {
                    type = "channel.channel_points_custom_reward_redemption.add",  // Esemény típusa
                    condition = new
                    {
                        broadcaster_user_id = broadcasterUserId  // Csatorna felhasználó ID
                    }
                }
            };

            // A message JSON formátumban
            var jsonMessage = JsonConvert.SerializeObject(subscribeMessage);

            // A JSON üzenet byte tömbbé alakítása
            var buffer = Encoding.UTF8.GetBytes(jsonMessage);

            // WebSocket üzenet küldése
            await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);
            Console.WriteLine("Előfizetve a csatornapont jutalom eseményre...");
        }

        private static void HandleMessage(string message)
        {
            try
            {
                dynamic jsonMessage = JsonConvert.DeserializeObject(message);

                // Ellenőrzés, hogy a kapott üzenet a megfelelő típusú esemény-e
                if (jsonMessage.metadata != null && jsonMessage.metadata.message_type == "channel_points_custom_reward_redemption.add")
                {
                    Console.WriteLine("Channel Points Jutalom Kiváltva!");
                    var rewardId = jsonMessage.payload.data.reward.id;
                    var userName = jsonMessage.payload.data.user_name;
                    var redemptionStatus = jsonMessage.payload.data.status;

                    Console.WriteLine($"Felhasználó: {userName}");
                    Console.WriteLine($"Jutalom ID: {rewardId}");
                    Console.WriteLine($"Állapot: {redemptionStatus}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hiba az üzenet feldolgozása közben: {ex.Message}");
            }
        }


    }
}
