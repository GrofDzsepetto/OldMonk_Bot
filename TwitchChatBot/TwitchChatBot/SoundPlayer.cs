using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using NAudio.Wave;
using Newtonsoft.Json.Linq;

namespace TwitchChatBot
{
    internal class SoundPlayer
    {
        public static TTSContainer tTSContainer = new TTSContainer();
        public static bool issoundRedeem = false;
        public void InitSounds(string selectedSound) 
        {
            string jsonPath = "D:\\GitHub\\OldMonkcb\\TwitchChatBot\\TwitchChatBot\\Sounds.json"; // A JSON fájl neve
            string jsonString = File.ReadAllText(jsonPath);
            JObject jsonObject = JObject.Parse(jsonString);
            string keyword = selectedSound;

            JToken result = jsonObject.SelectToken($"$.['{keyword}'].path");

            if (result != null)
            {
                Console.WriteLine(result);
                PlaySound(result.ToString());
                issoundRedeem=true;
            }
            else
            {
                Console.WriteLine("Nincs ilyen kulcs.");
                issoundRedeem = false ;
            }
        }

        public static async Task PlaySound(string musicPath)
        {
            Console.WriteLine("MP3 fájl lejátszása a háttérben...");

            using (var audioFile = new AudioFileReader(musicPath))
            using (var outputDevice = new WaveOutEvent())
            {
                outputDevice.Init(audioFile);
                outputDevice.Play();

                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    await Task.Delay(1000); // Várakozás, amíg a lejátszás befejeződik
                }
            }

            Console.WriteLine("Lejátszás vége.");
        }


    }
}
