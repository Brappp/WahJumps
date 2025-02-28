// File: WahJumps/Windows/SpeedrunOverlayWindow.cs
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using WahJumps.Data;

namespace WahJumps.Windows
{
    public class SpeedrunOverlayWindow : Window, IDisposable
    {
        private readonly SpeedrunManager speedrunManager;
        private Dictionary<string, string> customFields = new Dictionary<string, string>();
        private bool isConfiguring = false;
        private string customKeyInput = string.Empty;
        private string customValueInput = string.Empty;

        public SpeedrunOverlayWindow(SpeedrunManager speedrunManager)
            : base("Speedrun Timer",
                   ImGuiWindowFlags.NoScrollbar |
                   ImGuiWindowFlags.AlwaysAutoResize |
                   ImGuiWindowFlags.NoCollapse |
                   ImGuiWindowFlags.NoSavedSettings)
        {
            this.speedrunManager = speedrunManager;

            // Set initial position to top-right corner
            Position = new Vector2(ImGui.GetIO().DisplaySize.X - 220, 50);
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(200, 100),
                MaximumSize = new Vector2(300, 200)
            };

            // Register to manager events
            speedrunManager.StateChanged += OnStateChanged;
            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.CountdownTick += OnCountdownTick;
        }

        public override void Draw()
        {
            speedrunManager.Update();

            var state = speedrunManager.GetState();
            var currentTime = speedrunManager.GetCurrentTime();
            var currentPuzzle = speedrunManager.GetCurrentPuzzle();

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    DrawIdleState(currentPuzzle);
                    break;
                case SpeedrunManager.SpeedrunState.Countdown:
                    DrawCountdownState();
                    break;
                case SpeedrunManager.SpeedrunState.Running:
                    DrawRunningState(currentTime, currentPuzzle);
                    break;
                case SpeedrunManager.SpeedrunState.Finished:
                    DrawFinishedState(currentTime, currentPuzzle);
                    break;
            }
        }

        private void DrawIdleState(JumpPuzzleData selectedPuzzle)
        {
            if (selectedPuzzle != null)
            {
                ImGui.Text($"Selected: {selectedPuzzle.PuzzleName}");
                ImGui.Text($"World: {selectedPuzzle.World}");
            }
            else
            {
                ImGui.Text("No puzzle selected");
            }

            ImGui.Separator();

            if (selectedPuzzle != null)
            {
                if (ImGui.Button("Start", new Vector2(80, 30)))
                {
                    speedrunManager.StartCountdown(customFields);
                }

                ImGui.SameLine();
            }

            if (ImGui.Button(isConfiguring ? "Hide Config" : "Configure", new Vector2(100, 30)))
            {
                isConfiguring = !isConfiguring;
            }

            if (isConfiguring)
            {
                DrawConfigSection();
            }
        }

        private void DrawConfigSection()
        {
            ImGui.Separator();

            // Countdown setting
            var countdown = speedrunManager.DefaultCountdown;
            if (ImGui.SliderInt("Countdown", ref countdown, 0, 10))
            {
                speedrunManager.DefaultCountdown = countdown;
            }

            // Custom fields
            ImGui.Text("Custom Fields:");

            foreach (var field in customFields.ToList())
            {
                ImGui.Text($"{field.Key}: {field.Value}");
                ImGui.SameLine();
                if (ImGui.Button($"X##{field.Key}"))
                {
                    customFields.Remove(field.Key);
                }
            }

            // Add new custom field
            ImGui.InputText("Key", ref customKeyInput, 32);
            ImGui.InputText("Value", ref customValueInput, 64);

            if (ImGui.Button("Add Field") && !string.IsNullOrEmpty(customKeyInput))
            {
                customFields[customKeyInput] = customValueInput;
                customKeyInput = string.Empty;
                customValueInput = string.Empty;
            }
        }

        private void DrawCountdownState()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.0f, 1.0f));

            var countdown = speedrunManager.GetCountdownRemaining();
            var fontSize = ImGui.GetFontSize() * 3;
            var text = countdown.ToString();
            var textSize = ImGui.CalcTextSize(text);

            float windowWidth = ImGui.GetWindowWidth();
            float windowHeight = ImGui.GetWindowHeight();

            ImGui.SetCursorPosX((windowWidth - textSize.X) / 2);
            ImGui.SetCursorPosY((windowHeight - textSize.Y) / 2 - fontSize);

            ImGui.SetWindowFontScale(3.0f);
            ImGui.Text(text);
            ImGui.SetWindowFontScale(1.0f);

            ImGui.PopStyleColor();

            ImGui.SetCursorPosX((windowWidth - 100) / 2);
            if (ImGui.Button("Skip", new Vector2(100, 0)))
            {
                speedrunManager.SkipCountdown();
            }
        }

        private void DrawRunningState(TimeSpan time, JumpPuzzleData puzzle)
        {
            if (puzzle != null)
            {
                ImGui.Text(puzzle.PuzzleName);
            }

            // Format time as mm:ss.ms
            string timeText = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.8f, 0.2f, 1.0f));

            var fontSize = ImGui.GetFontSize() * 2;
            var textSize = ImGui.CalcTextSize(timeText);

            float windowWidth = ImGui.GetWindowWidth();

            ImGui.SetCursorPosX((windowWidth - textSize.X * 2) / 2);

            ImGui.SetWindowFontScale(2.0f);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);

            ImGui.PopStyleColor();

            if (ImGui.Button("Stop", new Vector2(80, 30)))
            {
                speedrunManager.StopTimer();
            }

            ImGui.SameLine();

            if (ImGui.Button("Reset", new Vector2(80, 30)))
            {
                speedrunManager.ResetTimer();
            }
        }

        private void DrawFinishedState(TimeSpan time, JumpPuzzleData puzzle)
        {
            ImGui.Text("Run Complete!");

            if (puzzle != null)
            {
                ImGui.Text(puzzle.PuzzleName);
            }

            // Format time as mm:ss.ms
            string timeText = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.8f, 0.2f, 1.0f));

            var fontSize = ImGui.GetFontSize() * 2;
            var textSize = ImGui.CalcTextSize(timeText);

            float windowWidth = ImGui.GetWindowWidth();

            ImGui.SetCursorPosX((windowWidth - textSize.X * 2) / 2);

            ImGui.SetWindowFontScale(2.0f);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);

            ImGui.PopStyleColor();

            if (ImGui.Button("New Run", new Vector2(90, 30)))
            {
                speedrunManager.ResetTimer();
            }
        }

        private void OnStateChanged(SpeedrunManager.SpeedrunState state)
        {
            // Handle state changes (could add sound effects here)
        }

        private void OnTimeUpdated(TimeSpan time)
        {
            // This is called every frame when timer is running
        }

        private void OnCountdownTick(int remaining)
        {
            // Play sound effect on countdown ticks
            if (speedrunManager.PlaySoundOnCountdown)
            {
                // Could play sound through Dalamud here
            }
        }

        public void Dispose()
        {
            speedrunManager.StateChanged -= OnStateChanged;
            speedrunManager.TimeUpdated -= OnTimeUpdated;
            speedrunManager.CountdownTick -= OnCountdownTick;
        }
    }
}
