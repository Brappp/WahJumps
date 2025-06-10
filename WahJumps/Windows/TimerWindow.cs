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
        // Core elements
        private readonly SpeedrunManager speedrunManager;
        private readonly Plugin plugin;

        // UI state
        private TimeSpan currentTime = TimeSpan.Zero;
        private int countdownRemaining = 0;
        private Vector4 timeColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);

        // Animation state
        private float pulseAnimation = 0f;
        private float glowIntensity = 0f;
        private DateTime lastStateChange = DateTime.Now;

        // Window styling
        private readonly Vector2 windowSize = new Vector2(280, 180);

        // Colors and themes
        private readonly Vector4 primaryGreen = new Vector4(0.2f, 0.8f, 0.3f, 1.0f);
        private readonly Vector4 warningYellow = new Vector4(1.0f, 0.8f, 0.0f, 1.0f);
        private readonly Vector4 dangerRed = new Vector4(1.0f, 0.3f, 0.3f, 1.0f);
        private readonly Vector4 finishedGold = new Vector4(1.0f, 0.8f, 0.2f, 1.0f);
        private readonly Vector4 countdownOrange = new Vector4(1.0f, 0.6f, 0.1f, 1.0f);

        public TimerWindow(SpeedrunManager speedrunManager, Plugin plugin)
            : base("Jump Timer##WahJumpsTimer", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse)
        {
            this.speedrunManager = speedrunManager;
            this.plugin = plugin;

            // Subscribe to events
            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.StateChanged += OnStateChanged;
            speedrunManager.CountdownTick += OnCountdownTick;

            // Set initial size and position
            Size = windowSize;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = windowSize,
                MaximumSize = windowSize
            };

            // Set initial position to prevent jumping
            Position = new Vector2(100, 100);
            PositionCondition = ImGuiCond.FirstUseEver;

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
            lastStateChange = DateTime.Now;
            
            // Only auto-show window when timer starts (not on every state change)
            if (state == SpeedrunManager.SpeedrunState.Countdown && !IsOpen)
            {
                IsOpen = true;
            }

            // Reset animations on state change
            pulseAnimation = 0f;
            glowIntensity = 0f;
        }

        private void OnCountdownTick(int remaining)
        {
            countdownRemaining = remaining;
            // Remove shake effect to prevent window jumping
            // shakeOffset = 2.0f;
        }

        public override void Draw()
        {
            // Update animations
            UpdateAnimations();

            // Apply modern window styling
            ApplyWindowStyling();

            try
            {
                var state = speedrunManager.GetState();

                // Handle countdown overlay
                if (state == SpeedrunManager.SpeedrunState.Countdown)
                {
                    DrawModernCountdown();
                    return;
                }

                DrawModernTimerContent(state);
            }
            finally
            {
                // Clean up styling
                CleanupWindowStyling();
            }
        }

        private void UpdateAnimations()
        {
            // Remove all animations for crisp, clean rendering
            pulseAnimation = 0f;
            glowIntensity = 0f;
        }

        private void ApplyWindowStyling()
        {
            // HD window styling with higher precision
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 12.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 16));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 8.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));

            // HD dynamic window background with better gradients
            var state = speedrunManager.GetState();
            Vector4 bgColor = state switch
            {
                SpeedrunManager.SpeedrunState.Running => new Vector4(0.05f, 0.15f, 0.05f, 0.98f),
                SpeedrunManager.SpeedrunState.Countdown => new Vector4(0.15f, 0.08f, 0.03f, 0.98f),
                SpeedrunManager.SpeedrunState.Finished => new Vector4(0.15f, 0.12f, 0.03f, 0.98f),
                _ => new Vector4(0.06f, 0.06f, 0.15f, 0.98f)
            };

            ImGui.PushStyleColor(ImGuiCol.WindowBg, bgColor);
        }

        private void CleanupWindowStyling()
        {
            // Pop in reverse order: 1 color, 5 style vars
            ImGui.PopStyleColor(1);
            ImGui.PopStyleVar(5);
        }

        private void DrawModernCountdown()
        {
            var contentSize = ImGui.GetContentRegionAvail();
            var drawList = ImGui.GetWindowDrawList();
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();

            // Remove shake effect to prevent window jumping
            // if (shakeOffset > 0)
            // {
            //     float shakeX = (float)(Math.Sin(ImGui.GetTime() * 30) * shakeOffset);
            //     float shakeY = (float)(Math.Cos(ImGui.GetTime() * 25) * shakeOffset * 0.5f);
            //     ImGui.SetWindowPos(new Vector2(windowPos.X + shakeX, windowPos.Y + shakeY));
            // }

            // Clean static background gradient
            Vector4 gradientTop = new Vector4(0.06f, 0.03f, 0.015f, 1.0f);
            Vector4 gradientBottom = new Vector4(0.03f, 0.015f, 0.006f, 1.0f);

            drawList.AddRectFilledMultiColor(
                windowPos + new Vector2(0, 25), // Account for title bar
                windowPos + windowSize,
                ImGui.GetColorU32(gradientTop),
                ImGui.GetColorU32(gradientTop),
                ImGui.GetColorU32(gradientBottom),
                ImGui.GetColorU32(gradientBottom)
            );

            // Large countdown number with moderate scaling for crispness
            string countText = countdownRemaining.ToString();
            
            // Use moderate font scaling for size without fuzziness
            float fontSize = 2.5f;
            var textSize = ImGui.CalcTextSize(countText) * fontSize;
            float centerX = (contentSize.X - textSize.X) * 0.5f;
            float centerY = (contentSize.Y - textSize.Y) * 0.5f - 30;

            // Main countdown text - large but crisp
            ImGui.SetCursorPos(new Vector2(centerX, centerY));
            ImGui.PushStyleColor(ImGuiCol.Text, countdownOrange);
            try
            {
                // Use moderate font scaling for good size/crispness balance
                ImGui.SetWindowFontScale(fontSize);
                ImGui.Text(countText);
                ImGui.SetWindowFontScale(1.0f);
            }
            finally
            {
                ImGui.PopStyleColor(1);
            }

            // Clean "Get Ready!" text
            string readyText = "Get Ready!";
            textSize = ImGui.CalcTextSize(readyText);
            centerX = (contentSize.X - textSize.X) * 0.5f;

            ImGui.SetCursorPos(new Vector2(centerX, centerY + 80));
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 0.9f));
            try
            {
                ImGui.Text(readyText);
            }
            finally
            {
                ImGui.PopStyleColor(1);
            }

            // Modern skip button - centered
            float buttonWidth = 140f;
            float buttonCenterX = (contentSize.X - buttonWidth) * 0.5f;
            ImGui.SetCursorPos(new Vector2(buttonCenterX, centerY + 130));
            DrawModernButton("Skip Countdown", new Vector2(buttonWidth, 32), 
                new Vector4(0.4f, 0.3f, 0.2f, 0.8f),
                new Vector4(0.5f, 0.4f, 0.3f, 0.9f),
                () => speedrunManager.SkipCountdown());
        }

        private void DrawModernTimerContent(SpeedrunManager.SpeedrunState state)
        {
            var contentSize = ImGui.GetContentRegionAvail();

            // Puzzle name header with modern styling
            var puzzle = speedrunManager.GetCurrentPuzzle();
            if (puzzle != null)
            {
                DrawPuzzleHeader(puzzle, contentSize.X);
            }

            ImGui.Spacing();

            // Main timer display with enhanced styling
            DrawMainTimer(state, contentSize.X);

            ImGui.Spacing();

            // Status indicator
            DrawStatusIndicator(state, contentSize.X);

            ImGui.Spacing();

            // Control buttons
            DrawModernControls(state, contentSize.X);
        }

        private void DrawPuzzleHeader(JumpPuzzleData puzzle, float contentWidth)
        {
            string title = puzzle.PuzzleName;
            if (title.Length > 25) title = title.Substring(0, 22) + "...";

            var textSize = ImGui.CalcTextSize(title);
            ImGui.SetCursorPosX((contentWidth - textSize.X) * 0.5f);

            // HD header with enhanced background and border
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            
            // Background with gradient
            drawList.AddRectFilledMultiColor(
                new Vector2(pos.X - 12, pos.Y - 4),
                new Vector2(pos.X + textSize.X + 12, pos.Y + textSize.Y + 4),
                ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.35f, 0.4f)),
                ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.35f, 0.4f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.25f, 0.2f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.25f, 0.2f))
            );
            
            // Subtle border
            drawList.AddRect(
                new Vector2(pos.X - 12, pos.Y - 4),
                new Vector2(pos.X + textSize.X + 12, pos.Y + textSize.Y + 4),
                ImGui.GetColorU32(new Vector4(0.4f, 0.4f, 0.5f, 0.3f)),
                6.0f,
                ImDrawFlags.None,
                1.0f
            );

            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            try
            {
                ImGui.Text(title);
            }
            finally
            {
                ImGui.PopStyleColor(1);
            }
        }

        private void DrawMainTimer(SpeedrunManager.SpeedrunState state, float contentWidth)
        {
            string timeText = FormatTime(currentTime);

            // Dynamic color based on time and state
            timeColor = state switch
            {
                SpeedrunManager.SpeedrunState.Running when currentTime.TotalMinutes < 2 => primaryGreen,
                SpeedrunManager.SpeedrunState.Running when currentTime.TotalMinutes < 5 => warningYellow,
                SpeedrunManager.SpeedrunState.Running => dangerRed,
                SpeedrunManager.SpeedrunState.Finished => finishedGold,
                _ => new Vector4(0.8f, 0.8f, 0.8f, 1.0f)
            };

            // Large timer display with moderate scaling for crispness
            float fontSize = 2.0f;
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            float centerX = (contentWidth - textSize.X) * 0.5f;

            // Store current cursor position for main text
            float currentY = ImGui.GetCursorPosY();

            // Main timer text - large but crisp
            ImGui.SetCursorPos(new Vector2(centerX, currentY));
            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);
            try
            {
                // Use moderate font scaling for good size/crispness balance
                ImGui.SetWindowFontScale(fontSize);
                ImGui.Text(timeText);
                ImGui.SetWindowFontScale(1.0f);
            }
            finally
            {
                ImGui.PopStyleColor(1);
            }
        }

        private void DrawStatusIndicator(SpeedrunManager.SpeedrunState state, float contentWidth)
        {
            string statusText = state switch
            {
                SpeedrunManager.SpeedrunState.Running => "● RUNNING",
                SpeedrunManager.SpeedrunState.Finished => "✓ COMPLETED",
                SpeedrunManager.SpeedrunState.Idle => "○ Ready",
                _ => ""
            };

            if (string.IsNullOrEmpty(statusText)) return;

            Vector4 statusColor = state switch
            {
                SpeedrunManager.SpeedrunState.Running => primaryGreen,
                SpeedrunManager.SpeedrunState.Finished => finishedGold,
                _ => new Vector4(0.6f, 0.6f, 0.6f, 1.0f)
            };

            var textSize = ImGui.CalcTextSize(statusText);
            ImGui.SetCursorPosX((contentWidth - textSize.X) * 0.5f);
            ImGui.PushStyleColor(ImGuiCol.Text, statusColor);
            try
            {
                ImGui.Text(statusText);
            }
            finally
            {
                ImGui.PopStyleColor(1);
            }
        }

        private void DrawModernControls(SpeedrunManager.SpeedrunState state, float contentWidth)
        {
            float buttonWidth = 100;
            float centerX = (contentWidth - buttonWidth) * 0.5f;

            ImGui.SetCursorPosX(centerX);

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    DrawModernButton("START", new Vector2(buttonWidth, 32),
                        new Vector4(0.2f, 0.6f, 0.3f, 0.8f),
                        new Vector4(0.3f, 0.7f, 0.4f, 0.9f),
                        () => speedrunManager.StartCountdown());
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    DrawModernButton("STOP", new Vector2(buttonWidth, 32),
                        new Vector4(0.7f, 0.2f, 0.2f, 0.8f),
                        new Vector4(0.8f, 0.3f, 0.3f, 0.9f),
                        () => speedrunManager.StopTimer());
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    DrawModernButton("RESET", new Vector2(buttonWidth, 32),
                        new Vector4(0.3f, 0.4f, 0.6f, 0.8f),
                        new Vector4(0.4f, 0.5f, 0.7f, 0.9f),
                        () => speedrunManager.ResetTimer());
                    break;
            }
        }



        private void DrawModernButton(string text, Vector2 size, Vector4 normalColor, Vector4 hoverColor, Action onClick)
        {
            // HD button styling with enhanced effects
            ImGui.PushStyleColor(ImGuiCol.Button, normalColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, hoverColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(hoverColor.X * 1.3f, hoverColor.Y * 1.3f, hoverColor.Z * 1.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, Vector4.One);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 10.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 8));

            try
            {
                if (ImGui.Button(text, size))
                {
                    onClick?.Invoke();
                }
            }
            finally
            {
                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor(4);
            }
        }

        private string FormatTime(TimeSpan time)
        {
            if (time.TotalHours >= 1)
            {
                return $"{(int)time.TotalHours:D1}:{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
            }
            return $"{time.Minutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
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
