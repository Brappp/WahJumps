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
            // Add a pulsing effect based on time
            float pulseFactor = 0.2f * (float)Math.Sin(ImGui.GetTime() * 3.0) + 1.0f;

            // Background gradient for countdown
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            // Top to bottom gradient
            drawList.AddRectFilledMultiColor(
                windowPos,
                new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.2f, 1.0f)),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.2f, 1.0f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.3f, 1.0f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.3f, 1.0f))
            );

            // Large countdown display with pulse effect
            float fontSize = 5.0f * pulseFactor; // Apply pulsing
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

            // Get Ready text with glow effect
            string readyText = "Get Ready!";
            textSize = ImGui.CalcTextSize(readyText);
            centerX = (ImGui.GetWindowWidth() - textSize.X) * 0.5f;

            // Shadowed text effect
            Vector2 shadowPos = new Vector2(centerX + 1, centerY + 61);
            ImGui.SetCursorPos(shadowPos);
            ImGui.TextColored(new Vector4(0.0f, 0.0f, 0.0f, 0.5f), readyText);

            ImGui.SetCursorPos(new Vector2(centerX, centerY + 60));
            ImGui.TextColored(UiTheme.Primary, readyText);

            // Skip button
            string skipText = "Skip";
            float skipButtonWidth = 120;
            centerX = (ImGui.GetWindowWidth() - skipButtonWidth) * 0.5f;

            ImGui.SetCursorPos(new Vector2(centerX, centerY + 100));

            // Better skip button styling
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.3f, 0.3f, 0.4f, 0.7f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.4f, 0.4f, 0.5f, 0.8f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.5f, 0.5f, 0.6f, 0.9f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);

            if (ImGui.Button("Skip Countdown", new Vector2(skipButtonWidth, 30)))
            {
                speedrunManager.SkipCountdown();
            }

            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);
        }

        private void DrawTimerContent(SpeedrunManager.SpeedrunState state)
        {
            float contentWidth = ImGui.GetContentRegionAvail().X;

            // Draw puzzle name in a more prominent way if available
            var puzzle = speedrunManager.GetCurrentPuzzle();
            if (puzzle != null)
            {
                // Use a subtle background for the puzzle title
                ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.1f, 0.1f, 0.15f, 0.5f));
                using var titleChild = new ImRaii.Child("PuzzleTitle", new Vector2(-1, 30), true);

                // Center the title text
                string title = puzzle.PuzzleName;
                float textWidth = ImGui.CalcTextSize(title).X;
                ImGui.SetCursorPosX((contentWidth - textWidth) * 0.5f);
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5); // Vertical centering
                ImGui.TextColored(UiTheme.Primary, title);

                ImGui.PopStyleColor();
            }

            ImGui.Spacing();

            // Format time
            string timeText = FormatTime(currentTime);

            // Apply dynamic color based on elapsed time
            // Green -> Yellow -> Red as time increases (if running)
            if (state == SpeedrunManager.SpeedrunState.Running)
            {
                if (currentTime.TotalMinutes < 3)
                {
                    // 0-3 minutes: Green
                    timeColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);
                }
                else if (currentTime.TotalMinutes < 10)
                {
                    // 3-10 minutes: Yellow
                    timeColor = new Vector4(0.9f, 0.9f, 0.0f, 1.0f);
                }
                else
                {
                    // 10+ minutes: Red
                    timeColor = new Vector4(1.0f, 0.3f, 0.3f, 1.0f);
                }
            }
            else if (state == SpeedrunManager.SpeedrunState.Finished)
            {
                // Finished state: use a gold/achievement color
                timeColor = new Vector4(1.0f, 0.8f, 0.0f, 1.0f);
            }

            // Time display - large and centered
            float fontSize = 2.5f; // Make it slightly larger
            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);

            // Center the time display
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            ImGui.SetCursorPosX((contentWidth - textSize.X) * 0.5f);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 10);

            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Add labels above controls if running
            if (state == SpeedrunManager.SpeedrunState.Running)
            {
                string statusLabel = "Running...";
                textSize = ImGui.CalcTextSize(statusLabel);
                ImGui.SetCursorPosX((contentWidth - textSize.X) * 0.5f);
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), statusLabel);
            }
            else if (state == SpeedrunManager.SpeedrunState.Finished)
            {
                string statusLabel = "Completed!";
                textSize = ImGui.CalcTextSize(statusLabel);
                ImGui.SetCursorPosX((contentWidth - textSize.X) * 0.5f);
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.0f, 1.0f), statusLabel);
            }

            // Control buttons - more spaced
            ImGui.Spacing();
            ImGui.Spacing();

            DrawControls(state);

            // Simplified footer with just countdown value and close
            ImGui.PushStyleColor(ImGuiCol.Separator, new Vector4(0.3f, 0.3f, 0.3f, 0.7f));
            ImGui.Separator();
            ImGui.PopStyleColor();

            // Align countdown text to left and close button to right
            ImGui.Text($"Countdown: {countdownSeconds}s");
            ImGui.SameLine(contentWidth - 60);

            // Better looking close button
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.2f, 0.25f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.3f, 0.35f, 1.0f));
            if (ImGui.Button("Close", new Vector2(50, 0)))
            {
                IsOpen = false;
            }
            ImGui.PopStyleColor(2);
        }

        private void DrawControls(SpeedrunManager.SpeedrunState state)
        {
            float buttonWidth = 80; // Slightly wider buttons
            float contentWidth = ImGui.GetContentRegionAvail().X;

            // Apply consistent button styling with nicer colors
            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    // Start button with green color
                    float startX = (contentWidth - buttonWidth) * 0.5f;
                    ImGui.SetCursorPosX(startX);

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.0f, 0.5f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.0f, 0.6f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.0f, 0.7f, 0.4f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

                    if (ImGui.Button("Start", new Vector2(buttonWidth, 24)))
                    {
                        speedrunManager.StartCountdown();
                    }

                    ImGui.PopStyleColor(4);
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    // Stop button with red color
                    startX = (contentWidth - buttonWidth) * 0.5f;
                    ImGui.SetCursorPosX(startX);

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.1f, 0.1f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.9f, 0.3f, 0.3f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

                    if (ImGui.Button("Stop", new Vector2(buttonWidth, 24)))
                    {
                        speedrunManager.StopTimer();
                    }

                    ImGui.PopStyleColor(4);
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    // Reset button with blue color
                    ImGui.SetCursorPosX((contentWidth - buttonWidth) * 0.5f);

                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.6f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.7f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.4f, 0.6f, 0.8f, 1.0f));
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f));

                    if (ImGui.Button("Reset", new Vector2(buttonWidth, 24)))
                    {
                        speedrunManager.ResetTimer();
                    }

                    ImGui.PopStyleColor(4);
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
