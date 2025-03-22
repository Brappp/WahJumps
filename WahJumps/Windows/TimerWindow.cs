// File: WahJumps/Windows/TimerWindow.cs
using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class TimerWindow : Window, IDisposable
    {
        // Core elements we'll keep
        private readonly SpeedrunManager speedrunManager;
        private readonly Plugin plugin;

        // UI state
        private TimeSpan currentTime = TimeSpan.Zero;
        private int countdownRemaining = 0;
        private Vector4 timeColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);
        private int countdownSeconds = 3; // Default countdown seconds

        // Window dimensions
        private float windowWidth = 250;
        private float windowHeight = 100;

        public TimerWindow(SpeedrunManager speedrunManager, Plugin plugin)
            : base("Jump Timer", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.speedrunManager = speedrunManager;
            this.plugin = plugin;

            // Subscribe to events
            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.StateChanged += OnStateChanged;
            speedrunManager.CountdownTick += OnCountdownTick;

            // Set initial position and size
            Position = new Vector2(10, 10);
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(windowWidth, windowHeight),
                MaximumSize = new Vector2(windowWidth * 2, windowHeight * 2)
            };

            // Start with window closed
            IsOpen = false;
        }

        // Event handlers
        private void OnTimeUpdated(TimeSpan time)
        {
            currentTime = time;
        }

        private void OnStateChanged(SpeedrunManager.SpeedrunState state)
        {
            // Make sure window is visible when timer is running
            if (state == SpeedrunManager.SpeedrunState.Running ||
                state == SpeedrunManager.SpeedrunState.Countdown)
            {
                if (!IsOpen)
                {
                    IsOpen = true;
                }
            }
        }

        private void OnCountdownTick(int remaining)
        {
            countdownRemaining = remaining;
        }

        public override void Draw()
        {
            // Get current state from speedrun manager
            var state = speedrunManager.GetState();

            // Handle countdown overlay if in countdown state
            if (state == SpeedrunManager.SpeedrunState.Countdown)
            {
                DrawCountdown();
                return;
            }

            DrawTimerContent(state);
        }

        private void DrawCountdown()
        {
            // Large countdown display
            float fontSize = 5.0f;
            string countText = countdownRemaining.ToString();

            var textSize = ImGui.CalcTextSize(countText) * fontSize;
            float centerX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;
            float centerY = (ImGui.GetWindowHeight() - textSize.Y) * 0.5f - 20;

            ImGui.SetCursorPos(new Vector2(centerX, centerY));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(countText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Get Ready text
            string readyText = "Get Ready!";
            textSize = ImGui.CalcTextSize(readyText);
            centerX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;

            ImGui.SetCursorPos(new Vector2(centerX, centerY + 60));
            ImGui.TextColored(UiTheme.Primary, readyText);

            // Skip button
            string skipText = "Skip";
            float skipButtonWidth = 80;
            centerX = (ImGui.GetWindowWidth() - skipButtonWidth) * 0.5f;

            ImGui.SetCursorPos(new Vector2(centerX, centerY + 100));
            if (ImGui.Button("Skip", new Vector2(skipButtonWidth, 25)))
            {
                speedrunManager.SkipCountdown();
            }
        }

        private void DrawTimerContent(SpeedrunManager.SpeedrunState state)
        {
            float contentWidth = ImGui.GetContentRegionAvail().X;

            // Get current puzzle if any
            var puzzle = speedrunManager.GetCurrentPuzzle();
            if (puzzle != null)
            {
                // Show minimal puzzle info
                ImGui.TextColored(UiTheme.Primary, puzzle.PuzzleName);
            }

            // Format time
            string timeText = FormatTime(currentTime);

            // Time display
            float fontSize = 2.0f;
            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);

            // Center the time display
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            ImGui.SetCursorPosX((contentWidth - textSize.X) * 0.5f);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);

            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Control buttons
            ImGui.Spacing();
            ImGui.Spacing();

            DrawControls(state);

            // Countdown configuration
            ImGui.Separator();
            ImGui.Text("Countdown: ");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(50);

            if (ImGui.InputInt("##countdownValue", ref countdownSeconds, 1))
            {
                if (countdownSeconds < 0) countdownSeconds = 0;
                if (countdownSeconds > 10) countdownSeconds = 10;

                // Update speedrun manager countdown setting
                speedrunManager.DefaultCountdown = countdownSeconds;
            }

            // Close button
            ImGui.SameLine(contentWidth - 60);
            if (ImGui.Button("Close", new Vector2(50, 0)))
            {
                IsOpen = false;
            }
        }

        private void DrawControls(SpeedrunManager.SpeedrunState state)
        {
            float buttonWidth = 70;
            float contentWidth = ImGui.GetContentRegionAvail().X;

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    // Centered start buttons
                    float startX = (contentWidth - buttonWidth * 2 - 10) * 0.5f;
                    ImGui.SetCursorPosX(startX);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Play.ToIconString() + " Start", new Vector2(buttonWidth, 0)))
                    {
                        speedrunManager.StartCountdown();
                    }
                    ImGui.PopFont();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString() + " Skip", new Vector2(buttonWidth, 0)))
                    {
                        speedrunManager.StartCountdown();
                        speedrunManager.SkipCountdown();
                    }
                    ImGui.PopFont();
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    // Running control buttons
                    startX = (contentWidth - buttonWidth * 2 - 10) * 0.5f;
                    ImGui.SetCursorPosX(startX);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Stop.ToIconString() + " Stop", new Vector2(buttonWidth, 0)))
                    {
                        speedrunManager.StopTimer();
                    }
                    ImGui.PopFont();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Undo.ToIconString() + " Reset", new Vector2(buttonWidth, 0)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    ImGui.PopFont();
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    // Reset button
                    ImGui.SetCursorPosX((contentWidth - buttonWidth) * 0.5f);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Undo.ToIconString() + " Reset", new Vector2(buttonWidth, 0)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    ImGui.PopFont();
                    break;
            }
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        }

        // Window visibility control
        public void Toggle()
        {
            IsOpen = !IsOpen;
        }

        public void ShowTimer()
        {
            IsOpen = true;
        }

        public void HideTimer()
        {
            IsOpen = false;
        }

        public void Dispose()
        {
            // Unsubscribe from events
            speedrunManager.TimeUpdated -= OnTimeUpdated;
            speedrunManager.StateChanged -= OnStateChanged;
            speedrunManager.CountdownTick -= OnCountdownTick;
        }
    }
}
