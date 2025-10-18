using ClickableTransparentOverlay;
using ImGuiNET;
using System.IO;
using System.Text.Json;

namespace Kuvasz_TwitchBot
{
    public class UIRender : Overlay
    {
        // input mezők
        string channelName = "";

        // dropdown
        int currentItem = 0;
        readonly string[] comboItems = { "Small", "Medium", "Large" };

        // checkboxok
        bool pushtoTalkcb = false;
        bool doubleTapTalkcb = true;

        // key binding
        string boundKey = "None";
        string currentPressed = "None";
        bool listening = false;

        // ablak
        bool windowOpen = true;

        // validáció flag-ek
        bool showChannelError = false;
        bool showBoundKeyError = false;
        public UIRender()
        {
            LoadSettings(); // csak egyszer, amikor a UI példány létrejön
        }
        protected override void Render()
        {

            ImGui.Begin("Bot Settings", ref windowOpen, ImGuiWindowFlags.AlwaysAutoResize);

            ImGui.Text("Hello User!");

            // input mező
            ImGui.InputText("Destination Channel", ref channelName, 100);
            if (showChannelError && string.IsNullOrWhiteSpace(channelName))
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.3f, 0.3f, 1), "Please enter a channel name!");

            ImGui.Separator();

            // dropdown
            ImGui.Text("Language Model:");
            if (ImGui.BeginCombo("Válassz opciót", comboItems[currentItem]))
            {
                for (int i = 0; i < comboItems.Length; i++)
                {
                    bool selected = (i == currentItem);
                    if (ImGui.Selectable(comboItems[i], selected)) currentItem = i;
                    if (selected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.Separator();

            // checkboxok
            bool changed1 = ImGui.Checkbox("Push to talk", ref pushtoTalkcb);
            ImGui.SameLine();
            bool changed2 = ImGui.Checkbox("Double tap", ref doubleTapTalkcb);
            if (changed1 && pushtoTalkcb) doubleTapTalkcb = false;
            if (changed2 && doubleTapTalkcb) pushtoTalkcb = false;

            ImGui.Separator();

            // key listening
            currentPressed = "None";
            if (ImGui.IsMouseDown(ImGuiMouseButton.Left)) currentPressed = "MouseLeft";
            else if (ImGui.IsMouseDown(ImGuiMouseButton.Right)) currentPressed = "MouseRight";
            else if (ImGui.IsMouseDown(ImGuiMouseButton.Middle)) currentPressed = "MouseMiddle";
            else
            {
                int begin = (int)ImGuiKey.NamedKey_BEGIN;
                int end = (int)ImGuiKey.NamedKey_END;

                for (int k = begin; k < end; k++)
                {
                    var key = (ImGuiKey)k;
                    if (key == ImGuiKey.ModCtrl || key == ImGuiKey.ModShift ||
                        key == ImGuiKey.ModAlt || key == ImGuiKey.ModSuper)
                        continue;

                    if (ImGui.IsKeyDown(key)) { currentPressed = key.ToString(); break; }
                }

                if (currentPressed == "None")
                {
                    var io = ImGui.GetIO();
                    if (io.KeyCtrl) currentPressed = "Ctrl";
                    else if (io.KeyShift) currentPressed = "Shift";
                    else if (io.KeyAlt) currentPressed = "Alt";
                    else if (io.KeySuper) currentPressed = "Super";
                }
            }

            if (listening && currentPressed != "None")
            {
                boundKey = currentPressed;
                listening = false;
                showBoundKeyError = false;
            }

            if (!listening)
            {
                if (ImGui.Button("Set key (listen)")) listening = true;
            }
            else
            {
                ImGui.TextDisabled(" Listening... ");
                ImGui.SameLine();
                if (ImGui.Button("Cancel")) listening = false;
            }

            ImGui.SameLine();
            ImGui.InputText("Bound key", ref boundKey, 64, ImGuiInputTextFlags.ReadOnly);

            if (showBoundKeyError && !IsValidBoundKey())
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.3f, 0.3f, 1), "Please set a key binding!");

            ImGui.Separator();

            // SAVE & RUN – csak flaget állítunk
            if (ImGui.Button("💾 Save & Close"))
            {
                bool channelOk = !string.IsNullOrWhiteSpace(channelName);
                bool keyOk = IsValidBoundKey();

                showChannelError = !channelOk;
                showBoundKeyError = !keyOk;

                if (channelOk && keyOk)
                {
                    var talkMode = GetCaptureMode();
                    var languageModel = comboItems[currentItem];

                    saveSettings(channelName, languageModel, talkMode, boundKey);
                    Close();
                }
            }

            ImGui.End();  // <-- pontosan egyszer

            if (!windowOpen)
            {
                Close();
            }
        }


        private bool IsValidBoundKey()
            => !string.IsNullOrWhiteSpace(boundKey) &&
               !string.Equals(boundKey, "None", StringComparison.OrdinalIgnoreCase);

        private string GetCaptureMode()
        {
            if (pushtoTalkcb) return "ptt";
            if (doubleTapTalkcb) return "dtt";
            return "none";
        }
        private void LoadSettings()
        {
            if (!File.Exists("settings.json")) return;

            try
            {
                var json = File.ReadAllText("settings.json");
                var data = JsonSerializer.Deserialize<Settings>(json);

                if (data != null)
                {
                    channelName = data.channelName ?? "";
                    boundKey = data.boundKey ?? "None";
                    currentItem = Array.FindIndex(comboItems, s =>
                        s.Equals(data.model, StringComparison.OrdinalIgnoreCase));
                    if (currentItem < 0) currentItem = 0;

                    pushtoTalkcb = data.captureMode == "ptt";
                    doubleTapTalkcb = data.captureMode == "dtt";
                }

                Console.WriteLine("[LOAD] settings.json betöltve a UI-ba.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[LOAD] Hiba a settings betöltésekor: " + ex.Message);
            }
        }
        private void saveSettings(string channelName, string comboitem, string capturemode, string boundkey)
        {
            var payload = new
            {
                channelName = channelName,
                model = comboItems[currentItem].ToLowerInvariant(),
                captureMode = GetCaptureMode(),
                boundKey = boundKey
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText("settings.json", json);

            Console.WriteLine("[SAVE] settings.json frissítve.");
        }

        private record Settings(string channelName, string model, string captureMode, string boundKey);
    }
}
