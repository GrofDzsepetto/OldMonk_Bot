using System;
using System.Collections.Generic;
using TwitchLib.Api;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Client;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TwitchChatBot
{
    public class TwitchPubSubContainer
    {
        private static TwitchPubSub pubSub;
        static string channelID = "152046433"; // Azonosító (csatornaID)
        public static string channelName = "Geppo2tv";
        public static string _aoutToken = "your_oauth_token";


        static void Initialize(string AuthToken)
        {
            _aoutToken = AuthToken;
            pubSub = new TwitchPubSub();
            pubSub.OnPubSubServiceConnected += PubSub_OnConnected;
            pubSub.OnChannelPointsRewardRedeemed += PubSub_OnChannelPointsRewardRedeemed;

            pubSub.Connect();

            Console.ReadLine();
        }

        private static void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"[LOG] {e.DateTime}: {e.Data}");
        }

        private static void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"[BOT] Csatlakozva a(z) {e.AutoJoinChannel} csatornához.");
        }

        private static void PubSub_OnConnected(object sender, EventArgs e)
        {
            Console.WriteLine("[PubSub] Csatlakozva.");

            pubSub.ListenToChannelPoints(channelID);
            pubSub.SendTopics(_aoutToken);

        }

        private static void PubSub_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs e)
        {
            var reward = e.RewardRedeemed.Redemption.Reward;
            var user = e.RewardRedeemed.Redemption.User.DisplayName;

            Console.WriteLine($"[REWARD] {user} kiváltotta: {reward.Title} ({reward.Cost} pont)");
        }
    }
}
