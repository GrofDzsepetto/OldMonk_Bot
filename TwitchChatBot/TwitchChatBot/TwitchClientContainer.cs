using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using System.Speech.Synthesis;
using TwitchLib.PubSub.Events;

namespace TwitchChatBot
{
    internal class TwitchClientContainer
    {

        public TwitchClient Client;
        public ConnectionCredentials Credentials;
        
        public async void Initialize(string botUser, string botOAuthToken)
        {
            Client = new TwitchClient();
            Credentials = new ConnectionCredentials(botUser, botOAuthToken);
            Console.WriteLine($"Username: {botUser}, Token: {botOAuthToken}");
            
            Client.OnConnected += OnConnected;
            Client.OnJoinedChannel += JoinedChannel;
            Client.OnMessageReceived += MessageRecieved;
            Client.OnChatCommandReceived += OnChatCommandReceived;
           // Client.OnLog += OnLog;
            Client.Initialize(Credentials);
            Client.Connect();
        }

        public void SendMessage(string message)
        {
            Client.SendMessage(Client.JoinedChannels[0], message);
        }
        public void ChatCommand(string trigger, string message, OnChatCommandReceivedArgs e)
        {
            if (e.Command.CommandText.Equals(trigger, StringComparison.OrdinalIgnoreCase))
            {
                SendMessage(message);
            }
        }

        private void OnChatCommandReceived(object? sender, OnChatCommandReceivedArgs e)
        {
            ChatCommand("beep", "boop", e);
            ChatCommand("pisi", "kaka", e);
        }
        
        private void OnLog(object? sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"Log: {e.Data}");
        }

        private void OnConnected(object? sender, TwitchLib.Client.Events.OnConnectedArgs e)
        {
            Console.WriteLine($"I Have Connected");
            Client.JoinChannel("geppo2tv");
        }
        private void JoinedChannel(object? sender, TwitchLib.Client.Events.OnJoinedChannelArgs e)
        {
            Console.WriteLine($"I have connected to the channel named: {e.Channel}.");
            //SendMessage("Itten vagyok");
        }
        private void MessageRecieved(object? sender, TwitchLib.Client.Events.OnMessageReceivedArgs e)
        {
            Console.WriteLine($"Message from {e.ChatMessage.Username} : {e.ChatMessage.Message} ");
        }

    }
}
