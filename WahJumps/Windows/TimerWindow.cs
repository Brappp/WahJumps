// File: WahJumps/Windows/TimerWindow.cs
using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class TimerWindow : Window, IDisposable
    {
        private readonly SpeedrunManager speedrunManager;

        // UI state
        private TimeSpan currentTime = TimeSpan.Zero;
        private Vector4 timeColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);
        private bool isMinimalMode = true;

        // Window dimensions
        private float minimalWidth = 200;
        private float minimalHeight = 80;
        private float standardWidth = 300;
        private float standardHeight = 180;

        public TimerWindow(SpeedrunManager speedrunManager)
            : base("Jump Timer",
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.AlwaysAutoResize)
        {
            this.speedrunManager = speedrunManager;

            // Subscribe to events
            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.StateChanged += OnStateChanged;

            // Set initial position
            Position = new Vector2(10, 10);
            Size = new Vector2(minimalWidth, minimalHeight);

            // Start with window closed
            IsOpen = false;
        }

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

        public override void Draw()
        {
            // Get current state from speedrun manager
            var state = speedrunManager.GetState();

            DrawTimerContent(state);
        }

        private void DrawTimerContent(SpeedrunManager.SpeedrunState state)
        {
            // Get current puzzle info
            var puzzle = speedrunManager.GetCurrentPuzzle();

            // Minimal mode just shows the time with basic controls
            if (isMinimalMode)
            {
                DrawMinimalMode(state, puzzle);
            }
            else
            {
                DrawStandardMode(state, puzzle);
            }
        }

        private void DrawMinimalMode(SpeedrunManager.SpeedrunState state, JumpPuzzleData puzzle)
        {
            // Display puzzle name if available
            if (puzzle != null)
            {
                ImGui.TextColored(UiTheme.Primary, puzzle.PuzzleName);
            }

            // Format time
            string timeText = FormatTime(currentTime);

            // Time display
            float fontSize = 1.5f;
            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);
            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Compact controls based on state
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 5);
            DrawCompactControls(state);

            // Mode toggle (at the corner)
            ImGui.SameLine(ImGui.GetWindowWidth() - 30);
            if (ImGui.Button("⬓")) // Unicode "up-pointing triangle with horizontal bar"
            {
                isMinimalMode = false;
                Size = new Vector2(standardWidth, standardHeight);
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Expand timer view");
            }
        }

        private void DrawStandardMode(SpeedrunManager.SpeedrunState state, JumpPuzzleData puzzle)
        {
            // Header section
            if (puzzle != null)
            {
                ImGui.TextColored(UiTheme.Primary, puzzle.PuzzleName);
                ImGui.SameLine(ImGui.GetWindowWidth() - 30);

                // Minimize button
                if (ImGui.Button("_")) // Unicode minus sign
                {
                    isMinimalMode = true;
                    Size = new Vector2(minimalWidth, minimalHeight);
                }
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Minimize timer");
                }

                ImGui.Separator();
            }

            // Format time
            string timeText = FormatTime(currentTime);

            // Time display - bigger in standard mode
            float fontSize = 2.0f;
            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);

            // Center the time display
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - textSize.X) / 2);

            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Current split info (if available)
            DrawCurrentSplitInfo();

            // Controls
            ImGui.Spacing();
            ImGui.Separator();
            DrawFullControls(state);
        }

        private void DrawCurrentSplitInfo()
        {
            var currentSplit = speedrunManager.GetCurrentSplit();
            var nextSplit = speedrunManager.GetNextSplit();

            if (nextSplit != null)
            {
                ImGui.Spacing();
                ImGui.TextColored(new Vector4(1.0f, 0.9f, 0.2f, 1.0f), $"Next: {nextSplit.Name}");
            }
            else if (currentSplit != null)
            {
                ImGui.Spacing();
                ImGui.TextColored(timeColor, $"Current: {currentSplit.Name}");
            }
        }

        private void DrawCompactControls(SpeedrunManager.SpeedrunState state)
        {
            // Smaller buttons in compact layout
            float buttonHeight = 24;

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    // Start button
                    if (ImGui.Button("Start", new Vector2(60, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Skip CD", new Vector2(60, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                        speedrunManager.SkipCountdown();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    // Split button
                    if (ImGui.Button("Split", new Vector2(50, buttonHeight)))
                    {
                        speedrunManager.MarkSplit();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Stop", new Vector2(50, buttonHeight)))
                    {
                        speedrunManager.StopTimer();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Reset", new Vector2(50, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Countdown:
                    // Cancel countdown button
                    if (ImGui.Button("Skip", new Vector2(80, buttonHeight)))
                    {
                        speedrunManager.SkipCountdown();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    // Reset button
                    if (ImGui.Button("Reset", new Vector2(80, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    break;
            }
        }

        private void DrawFullControls(SpeedrunManager.SpeedrunState state)
        {
            float buttonWidth = 80;
            float buttonHeight = 30;

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    // Start buttons
                    float startButtonsWidth = buttonWidth * 2 + 10;
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - startButtonsWidth) / 2);

                    if (ImGui.Button("Start Timer", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Skip Count", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                        speedrunManager.SkipCountdown();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    // Running control buttons
                    float runningButtonsWidth = buttonWidth * 3 + 20;
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - runningButtonsWidth) / 2);

                    if (ImGui.Button("Split", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.MarkSplit();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Stop", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.StopTimer();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("Reset", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Countdown:
                    // Cancel countdown button
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - buttonWidth) / 2);
                    if (ImGui.Button("Skip", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.SkipCountdown();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    // Reset button
                    float finishedButtonsWidth = buttonWidth * 2 + 10;
                    ImGui.SetCursorPosX((ImGui.GetWindowWidth() - finishedButtonsWidth) / 2);

                    if (ImGui.Button("New Run", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    ImGui.SameLine();
                    if (ImGui.Button("View Stats", new Vector2(buttonWidth, buttonHeight)))
                    {
                        // Toggle to open main window and show stats
                        // This requires a callback to main window to show it and focus the records tab
                        IsOpen = false;
                    }
                    break;
            }
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        }

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
        }
    }
}
