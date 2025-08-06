using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
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

            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.StateChanged += OnStateChanged;
            speedrunManager.CountdownTick += OnCountdownTick;

            Size = windowSize;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = windowSize,
                MaximumSize = windowSize
            };

            Position = new Vector2(100, 100);
            PositionCondition = ImGuiCond.FirstUseEver;
            IsOpen = false;
        }

        private void OnTimeUpdated(TimeSpan time)
        {
            currentTime = time;
        }

        private void OnStateChanged(SpeedrunManager.SpeedrunState state)
        {
            lastStateChange = DateTime.Now;
            
            if (state == SpeedrunManager.SpeedrunState.Countdown && !IsOpen)
            {
                IsOpen = true;
            }

            pulseAnimation = 0f;
            glowIntensity = 0f;
        }

        private void OnCountdownTick(int remaining)
        {
            countdownRemaining = remaining;
        }

        public override void Draw()
        {
            UpdateAnimations();
            
            using var windowStyle = new ImRaii.TimerWindowStyle(speedrunManager.GetState());
            var state = speedrunManager.GetState();

            if (state == SpeedrunManager.SpeedrunState.Countdown)
            {
                DrawModernCountdown();
                return;
            }

            DrawModernTimerContent(state);
        }

        private void UpdateAnimations()
        {
            pulseAnimation = 0f;
            glowIntensity = 0f;
        }

        private void DrawModernCountdown()
        {
            var contentSize = ImGui.GetContentRegionAvail();
            var drawList = ImGui.GetWindowDrawList();
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();

            Vector4 gradientTop = new Vector4(0.06f, 0.03f, 0.015f, 1.0f);
            Vector4 gradientBottom = new Vector4(0.03f, 0.015f, 0.006f, 1.0f);

            drawList.AddRectFilledMultiColor(
                windowPos + new Vector2(0, 25),
                windowPos + windowSize,
                ImGui.GetColorU32(gradientTop),
                ImGui.GetColorU32(gradientTop),
                ImGui.GetColorU32(gradientBottom),
                ImGui.GetColorU32(gradientBottom)
            );

            string countText = countdownRemaining.ToString();
            float centerY = (contentSize.Y - ImGui.CalcTextSize(countText).Y * 2.5f) * 0.5f - 30;

            ImGui.SetCursorPosY(centerY);
            ImRaii.CenteredText(countText, countdownOrange, 2.5f);

            ImGui.SetCursorPosY(centerY + 80);
            ImRaii.CenteredText("Get Ready!", new Vector4(1.0f, 1.0f, 1.0f, 0.9f));

            float buttonWidth = 140f;
            float buttonCenterX = (contentSize.X - buttonWidth) * 0.5f;
            ImGui.SetCursorPos(new Vector2(buttonCenterX, centerY + 130));
            
            if (ImRaii.StyledButton("Skip Countdown", new Vector2(buttonWidth, 32), 
                new Vector4(0.4f, 0.3f, 0.2f, 0.8f),
                new Vector4(0.5f, 0.4f, 0.3f, 0.9f)))
            {
                speedrunManager.SkipCountdown();
            }
        }

        private void DrawModernTimerContent(SpeedrunManager.SpeedrunState state)
        {
            var contentSize = ImGui.GetContentRegionAvail();

            var puzzle = speedrunManager.GetCurrentPuzzle();
            if (puzzle != null)
            {
                DrawPuzzleHeader(puzzle, contentSize.X);
            }

            ImGui.Spacing();
            DrawMainTimer(state, contentSize.X);
            ImGui.Spacing();
            DrawStatusIndicator(state, contentSize.X);
            ImGui.Spacing();
            DrawModernControls(state, contentSize.X);
        }

        private void DrawPuzzleHeader(JumpPuzzleData puzzle, float contentWidth)
        {
            string title = puzzle.PuzzleName;
            if (title.Length > 25) title = title.Substring(0, 22) + "...";

            var textSize = ImGui.CalcTextSize(title);
            ImGui.SetCursorPosX((contentWidth - textSize.X) * 0.5f);

            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            
            drawList.AddRectFilledMultiColor(
                new Vector2(pos.X - 12, pos.Y - 4),
                new Vector2(pos.X + textSize.X + 12, pos.Y + textSize.Y + 4),
                ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.35f, 0.4f)),
                ImGui.GetColorU32(new Vector4(0.25f, 0.25f, 0.35f, 0.4f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.25f, 0.2f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.25f, 0.2f))
            );
            
            drawList.AddRect(
                new Vector2(pos.X - 12, pos.Y - 4),
                new Vector2(pos.X + textSize.X + 12, pos.Y + textSize.Y + 4),
                ImGui.GetColorU32(new Vector4(0.4f, 0.4f, 0.5f, 0.3f)),
                6.0f,
                ImDrawFlags.None,
                1.0f
            );

            using var textColor = ImRaii.PushColor(ImGuiCol.Text, UiTheme.Primary);
            ImGui.Text(title);
        }

        private void DrawMainTimer(SpeedrunManager.SpeedrunState state, float contentWidth)
        {
            string timeText = FormatTime(currentTime);

            timeColor = state switch
            {
                SpeedrunManager.SpeedrunState.Running when currentTime.TotalMinutes < 2 => primaryGreen,
                SpeedrunManager.SpeedrunState.Running when currentTime.TotalMinutes < 5 => warningYellow,
                SpeedrunManager.SpeedrunState.Running => dangerRed,
                SpeedrunManager.SpeedrunState.Finished => finishedGold,
                _ => new Vector4(0.8f, 0.8f, 0.8f, 1.0f)
            };

            ImRaii.CenteredText(timeText, timeColor, 2.0f);
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

            ImRaii.CenteredText(statusText, statusColor);
        }

        private void DrawModernControls(SpeedrunManager.SpeedrunState state, float contentWidth)
        {
            float buttonWidth = 100;
            float centerX = (contentWidth - buttonWidth) * 0.5f;

            ImGui.SetCursorPosX(centerX);

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    if (ImRaii.StyledButton("START", new Vector2(buttonWidth, 32),
                        new Vector4(0.2f, 0.6f, 0.3f, 0.8f),
                        new Vector4(0.3f, 0.7f, 0.4f, 0.9f)))
                    {
                        speedrunManager.StartCountdown();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    if (ImRaii.StyledButton("STOP", new Vector2(buttonWidth, 32),
                        new Vector4(0.7f, 0.2f, 0.2f, 0.8f),
                        new Vector4(0.8f, 0.3f, 0.3f, 0.9f)))
                    {
                        speedrunManager.StopTimer();
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    if (ImRaii.StyledButton("RESET", new Vector2(buttonWidth, 32),
                        new Vector4(0.3f, 0.4f, 0.6f, 0.8f),
                        new Vector4(0.4f, 0.5f, 0.7f, 0.9f)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    break;
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
        public new void Toggle()
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
            speedrunManager.TimeUpdated -= OnTimeUpdated;
            speedrunManager.StateChanged -= OnStateChanged;
            speedrunManager.CountdownTick -= OnCountdownTick;
        }
    }
}
