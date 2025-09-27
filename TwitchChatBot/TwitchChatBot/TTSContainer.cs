using System;
using System.Diagnostics;

namespace TwitchChatBot
{
    internal class TTSContainer
    {
        public void TTSInitialize(string textmsg)
        {
            Console.WriteLine("Elindult ez a szar");
            string textToSpeak = textmsg;
            string voice = "hu"; // Magyar nyelv, m1 variáns
            string espeakPath = @"C:\Program Files\eSpeak NG\espeak-ng.exe"; // Add meg az eSpeakNG pontos helyét

            // Sebesség, hangmagasság, hangerő testreszabása
            int speed = 220; // Szavak percenként
            int pitch = 55;  // Hangmagasság
            int volume = 130; // Hangerőű

            int gap = 4;

            int inkantation = 20;

            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = espeakPath,
                Arguments = $"-v {voice} -s {speed} -p {pitch} -a {volume} -g {gap} -k {inkantation} \"{textToSpeak}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
            }

            Console.WriteLine("végetért a tts");
        }
    }
}
