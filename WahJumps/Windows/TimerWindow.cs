// File: WahJumps/Windows/TimerWindow.cs
using System;
using System.Numerics;
using System.Linq;
using System.Collections.Generic;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class TimerWindow : Window, IDisposable
    {
        private readonly SpeedrunManager speedrunManager;
        private readonly Plugin plugin;

        // UI state
        private TimeSpan currentTime = TimeSpan.Zero;
        private int countdownRemaining = 0;
        private Vector4 timeColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);
        private bool isMinimalMode = true;
        private bool showPuzzleSelector = false;
        private string filterText = "";

        // Window dimensions - these are now for initial sizes
        private float minimalWidth = 250;
        private float minimalHeight = 100;
        private float standardWidth = 350;
        private float standardHeight = 250;

        public TimerWindow(SpeedrunManager speedrunManager, Plugin plugin)
            : base("Jump Timer",
                ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse |
                ImGuiWindowFlags.AlwaysAutoResize) // Added auto-resize flag for dynamic content
        {
            this.speedrunManager = speedrunManager;
            this.plugin = plugin;

            // Subscribe to events
            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.StateChanged += OnStateChanged;
            speedrunManager.CountdownTick += OnCountdownTick;

            // Set initial position and size - using base.SizeConstraints instead of Size
            Position = new Vector2(10, 10);
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(minimalWidth, minimalHeight),
                MaximumSize = new Vector2(1000, 800)
            };

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

        private void OnCountdownTick(int remaining)
        {
            countdownRemaining = remaining;
        }

        public override void Draw()
        {
            // Get current state from speedrun manager
            var state = speedrunManager.GetState();

            // Handle popup for puzzle selection
            if (showPuzzleSelector)
            {
                DrawPuzzleSelectorPopup();
            }

            // Draw countdown overlay if in countdown state
            if (state == SpeedrunManager.SpeedrunState.Countdown)
            {
                DrawCountdownOverlay();
                return;
            }

            DrawTimerContent(state);
        }

        private void DrawCountdownOverlay()
        {
            // Create a larger overlay for countdown
            Vector2 countdownSize = new Vector2(250, 200);

            // No need to manually set size with AlwaysAutoResize flag

            // Draw large countdown number
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

            ImGui.SetCursorPos(new Vector2(centerX, centerY + 80));
            ImGui.TextColored(UiTheme.Primary, readyText);

            // Skip button
            string skipText = "Skip";
            float skipButtonWidth = 80;
            centerX = (ImGui.GetWindowWidth() - skipButtonWidth) * 0.5f;

            ImGui.SetCursorPos(new Vector2(centerX, centerY + 120));
            if (ImGui.Button("Skip", new Vector2(skipButtonWidth, 25)))
            {
                speedrunManager.SkipCountdown();
            }
        }

        private void DrawTimerContent(SpeedrunManager.SpeedrunState state)
        {
            // Get current puzzle info
            var puzzle = speedrunManager.GetCurrentPuzzle();

            // Set size constraints based on mode
            if (isMinimalMode)
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new Vector2(minimalWidth, minimalHeight),
                    MaximumSize = new Vector2(minimalWidth * 2, minimalHeight * 2)
                };
            }
            else
            {
                SizeConstraints = new WindowSizeConstraints
                {
                    MinimumSize = new Vector2(standardWidth, standardHeight),
                    MaximumSize = new Vector2(standardWidth * 2, standardHeight * 2)
                };
            }

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
            float contentWidth = ImGui.GetContentRegionAvail().X;

            // Puzzle selection row at the top
            if (puzzle != null)
            {
                // Truncate puzzle name if too long
                string displayName = puzzle.PuzzleName;
                float nameWidth = ImGui.CalcTextSize(displayName).X;

                if (nameWidth > contentWidth - 90)
                {
                    // Cut name to fit
                    while (nameWidth > contentWidth - 90 && displayName.Length > 10)
                    {
                        displayName = displayName.Substring(0, displayName.Length - 5) + "...";
                        nameWidth = ImGui.CalcTextSize(displayName).X;
                    }
                }

                ImGui.TextColored(UiTheme.Primary, displayName);

                ImGui.SameLine(contentWidth - 80);

                // The change button with FontAwesome icon
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Random.ToIconString() + "##change"))
                {
                    showPuzzleSelector = true;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Change puzzle");
                }

                // Expand button with better icon
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Expand.ToIconString() + "##expand"))
                {
                    isMinimalMode = false;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Expand view");
                }
            }
            else
            {
                ImGui.TextColored(UiTheme.Warning, "No puzzle selected");

                ImGui.SameLine(contentWidth - 80);

                // Select button with FontAwesome icon
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Search.ToIconString() + "##select"))
                {
                    showPuzzleSelector = true;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Select a puzzle");
                }

                // Expand button with better icon
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Expand.ToIconString() + "##expand"))
                {
                    isMinimalMode = false;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Expand view");
                }
            }

            // Format time
            string timeText = FormatTime(currentTime);

            // Time display
            float fontSize = 1.5f;
            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);

            // Center the time display
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            ImGui.SetCursorPosX((contentWidth - textSize.X) / 2);

            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Current/next split info
            var nextSplit = speedrunManager.GetNextSplit();
            if (nextSplit != null && state == SpeedrunManager.SpeedrunState.Running)
            {
                // Truncate split name if needed
                string splitName = nextSplit.Name;
                float nextWidth = ImGui.CalcTextSize($"Next: {splitName}").X;

                if (nextWidth > contentWidth)
                {
                    while (nextWidth > contentWidth && splitName.Length > 10)
                    {
                        splitName = splitName.Substring(0, splitName.Length - 5) + "...";
                        nextWidth = ImGui.CalcTextSize($"Next: {splitName}").X;
                    }
                }

                // Center the next split text
                ImGui.SetCursorPosX((contentWidth - nextWidth) / 2);
                ImGui.TextColored(new Vector4(1.0f, 0.9f, 0.2f, 1.0f),
                    $"Next: {splitName}");
            }

            // Compact controls based on state
            DrawCompactControls(state);
        }

        private void DrawStandardMode(SpeedrunManager.SpeedrunState state, JumpPuzzleData puzzle)
        {
            float contentWidth = ImGui.GetContentRegionAvail().X;

            // Header section with puzzle info
            if (puzzle != null)
            {
                // Calculate space for text vs buttons
                float textWidth = ImGui.CalcTextSize(puzzle.PuzzleName).X;
                float availableWidth = contentWidth - 85; // Space for buttons

                // Truncate if needed
                string displayName = puzzle.PuzzleName;
                if (textWidth > availableWidth)
                {
                    while (textWidth > availableWidth && displayName.Length > 10)
                    {
                        displayName = displayName.Substring(0, displayName.Length - 5) + "...";
                        textWidth = ImGui.CalcTextSize(displayName).X;
                    }
                }

                ImGui.TextColored(UiTheme.Primary, displayName);

                ImGui.SameLine(contentWidth - 80);

                // Change button with FontAwesome icon
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Random.ToIconString() + "##change"))
                {
                    showPuzzleSelector = true;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Change puzzle");
                }

                // Minimize button with better icon
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Compress.ToIconString() + "##minimize"))
                {
                    isMinimalMode = true;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Minimize view");
                }

                // Additional puzzle details in expanded view
                ImGui.Text($"World: {puzzle.World}");
                ImGui.SameLine(contentWidth - 80);
                ImGui.TextColored(UiTheme.Secondary, $"Rating: {puzzle.Rating}");
            }
            else
            {
                ImGui.TextColored(UiTheme.Warning, "No puzzle selected");

                ImGui.SameLine(contentWidth - 80);

                // Select button with FontAwesome icon
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Search.ToIconString() + "##select"))
                {
                    showPuzzleSelector = true;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Select a puzzle");
                }

                // Minimize button with better icon
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Compress.ToIconString() + "##minimize"))
                {
                    isMinimalMode = true;
                }
                ImGui.PopFont();

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Minimize view");
                }
            }

            ImGui.Separator();

            // Format time
            string timeText = FormatTime(currentTime);

            // Time display - bigger in standard mode
            float fontSize = 2.0f;
            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);

            // Center the time display
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            ImGui.SetCursorPosX((contentWidth - textSize.X) / 2);

            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Current split info (if available)
            DrawSplitsInfo();

            // Controls
            ImGui.Spacing();
            ImGui.Separator();
            DrawFullControls(state);

            // Additional button row in standard mode
            ImGui.Spacing();

            // Two buttons centered
            float buttonWidth = 130;
            float totalWidth = buttonWidth * 2 + 10; // Two buttons with spacing
            float startX = (contentWidth - totalWidth) / 2;
            ImGui.SetCursorPosX(startX);

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.List.ToIconString() + " Main Window", new Vector2(buttonWidth, 0)))
            {
                plugin.ToggleVisibility();
            }
            ImGui.PopFont();

            ImGui.SameLine();

            ImGui.PushFont(UiBuilder.IconFont);
            if (ImGui.Button(FontAwesomeIcon.Trophy.ToIconString() + " Records", new Vector2(buttonWidth, 0)))
            {
                plugin.ToggleSpeedrunRecords();
            }
            ImGui.PopFont();
        }

        private void DrawSplitsInfo()
        {
            var currentSplit = speedrunManager.GetCurrentSplit();
            var nextSplit = speedrunManager.GetNextSplit();
            var splits = speedrunManager.GetCurrentSplits();
            var currentSplitIndex = speedrunManager.GetCurrentSplitIndex();

            if (splits.Count == 0) return;

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextColored(UiTheme.Primary, "Splits:");

            // Show up to 3 splits centered around the current one
            int startIdx = Math.Max(0, currentSplitIndex - 1);
            int endIdx = Math.Min(splits.Count - 1, startIdx + 2);
            float contentWidth = ImGui.GetContentRegionAvail().X;

            for (int i = startIdx; i <= endIdx; i++)
            {
                if (i < 0 || i >= splits.Count) continue;

                // Truncate split name if needed
                string displayName = splits[i].Name;
                float nameWidth = ImGui.CalcTextSize(displayName).X;

                if (nameWidth > contentWidth - 20)
                {
                    while (nameWidth > contentWidth - 20 && displayName.Length > 10)
                    {
                        displayName = displayName.Substring(0, displayName.Length - 5) + "...";
                        nameWidth = ImGui.CalcTextSize(displayName).X;
                    }
                }

                if (i < currentSplitIndex)
                {
                    // Completed split with checkmark icon
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(timeColor, $"{FontAwesomeIcon.Check.ToIconString()} {displayName}");
                    ImGui.PopFont();
                }
                else if (i == currentSplitIndex + 1)
                {
                    // Next split with arrow icon
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(new Vector4(1.0f, 0.9f, 0.2f, 1.0f),
                        $"{FontAwesomeIcon.ArrowRight.ToIconString()} {displayName}");
                    ImGui.PopFont();
                }
                else
                {
                    // Current or future split
                    ImGui.Text($"  {displayName}");
                }
            }

            // Show how many more splits after the visible ones
            if (endIdx < splits.Count - 1)
            {
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                    $"{FontAwesomeIcon.EllipsisH.ToIconString()} {splits.Count - endIdx - 1} more...");
                ImGui.PopFont();
            }
        }

        private void DrawCompactControls(SpeedrunManager.SpeedrunState state)
        {
            // Smaller buttons in compact layout
            float buttonHeight = 24;
            float contentWidth = ImGui.GetContentRegionAvail().X;

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    // Start buttons - centered
                    float totalWidth = 130; // Sum of button widths
                    float startX = (contentWidth - totalWidth) / 2;
                    ImGui.SetCursorPosX(startX);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Play.ToIconString() + " Start", new Vector2(60, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                    }
                    ImGui.PopFont();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString() + " Skip", new Vector2(60, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                        speedrunManager.SkipCountdown();
                    }
                    ImGui.PopFont();
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    // Running controls - centered
                    totalWidth = 170; // Sum of button widths + spacing
                    startX = (contentWidth - totalWidth) / 2;
                    ImGui.SetCursorPosX(startX);

                    // Split button with icon
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Flag.ToIconString() + "##split", new Vector2(buttonHeight, buttonHeight)))
                    {
                        speedrunManager.MarkSplit();
                    }
                    ImGui.PopFont();

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Mark split");
                    }

                    ImGui.SameLine();

                    // Stop button with icon
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Stop.ToIconString() + "##stop", new Vector2(buttonHeight, buttonHeight)))
                    {
                        speedrunManager.StopTimer();
                    }
                    ImGui.PopFont();

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Stop timer");
                    }

                    ImGui.SameLine();

                    // Reset button with icon
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Undo.ToIconString() + "##reset", new Vector2(buttonHeight, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    ImGui.PopFont();

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Reset timer");
                    }
                    break;

                case SpeedrunManager.SpeedrunState.Countdown:
                    // Cancel countdown button - centered
                    float skipWidth = 80;
                    ImGui.SetCursorPosX((contentWidth - skipWidth) / 2);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString() + " Skip", new Vector2(skipWidth, buttonHeight)))
                    {
                        speedrunManager.SkipCountdown();
                    }
                    ImGui.PopFont();
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    // Reset button - centered
                    float resetWidth = 80;
                    ImGui.SetCursorPosX((contentWidth - resetWidth) / 2);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Undo.ToIconString() + " Reset", new Vector2(resetWidth, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    ImGui.PopFont();
                    break;
            }
        }

        private void DrawFullControls(SpeedrunManager.SpeedrunState state)
        {
            float buttonWidth = 80;
            float buttonHeight = 30;
            float contentWidth = ImGui.GetContentRegionAvail().X;

            switch (state)
            {
                case SpeedrunManager.SpeedrunState.Idle:
                    // Start buttons
                    float startButtonsWidth = buttonWidth * 2 + 10;
                    ImGui.SetCursorPosX((contentWidth - startButtonsWidth) / 2);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Play.ToIconString() + " Start", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                    }
                    ImGui.PopFont();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString() + " Skip", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.StartCountdown();
                        speedrunManager.SkipCountdown();
                    }
                    ImGui.PopFont();
                    break;

                case SpeedrunManager.SpeedrunState.Running:
                    // Running control buttons
                    float runningButtonsWidth = buttonWidth * 3 + 20;
                    ImGui.SetCursorPosX((contentWidth - runningButtonsWidth) / 2);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Flag.ToIconString() + " Split", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.MarkSplit();
                    }
                    ImGui.PopFont();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Stop.ToIconString() + " Stop", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.StopTimer();
                    }
                    ImGui.PopFont();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Undo.ToIconString() + " Reset", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    ImGui.PopFont();
                    break;

                case SpeedrunManager.SpeedrunState.Countdown:
                    // Cancel countdown button
                    ImGui.SetCursorPosX((contentWidth - buttonWidth) / 2);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Forward.ToIconString() + " Skip", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.SkipCountdown();
                    }
                    ImGui.PopFont();
                    break;

                case SpeedrunManager.SpeedrunState.Finished:
                    // Reset button
                    float finishedButtonsWidth = buttonWidth * 2 + 10;
                    ImGui.SetCursorPosX((contentWidth - finishedButtonsWidth) / 2);

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Undo.ToIconString() + " New", new Vector2(buttonWidth, buttonHeight)))
                    {
                        speedrunManager.ResetTimer();
                    }
                    ImGui.PopFont();

                    ImGui.SameLine();

                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button(FontAwesomeIcon.Trophy.ToIconString() + " Stats", new Vector2(buttonWidth, buttonHeight)))
                    {
                        // Open main window and show records tab
                        plugin.MainWindow.ToggleVisibility();
                        plugin.SpeedrunTab.ForceActivate();
                    }
                    ImGui.PopFont();
                    break;
            }
        }

        private void DrawPuzzleSelectorPopup()
        {
            // Open a popup for puzzle selection
            ImGui.OpenPopup("PuzzleSelectorPopup");

            // Calculate a good size for the popup
            Vector2 center = ImGui.GetMainViewport().GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.Appearing);

            if (ImGui.BeginPopupModal("PuzzleSelectorPopup", ref showPuzzleSelector,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
            {
                // Title
                ImGui.TextColored(UiTheme.Primary, "Select a Puzzle");
                ImGui.Separator();

                // Search box with icon
                ImGui.PushFont(UiBuilder.IconFont);
                ImGui.Text(FontAwesomeIcon.Search.ToIconString());
                ImGui.PopFont();

                ImGui.SameLine();
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                ImGui.InputTextWithHint("##search", "Search by name...", ref filterText, 100);

                ImGui.Spacing();

                // Get all available puzzles from the plugin
                var recentlyUsedPuzzles = GetRecentlyUsedPuzzles();
                var allPuzzles = GetAllAvailablePuzzles();

                // Filter puzzles based on search text
                List<JumpPuzzleData> filteredPuzzles;
                if (!string.IsNullOrWhiteSpace(filterText))
                {
                    string lowerFilter = filterText.ToLower();
                    filteredPuzzles = allPuzzles
                        .Where(p => p.PuzzleName.ToLower().Contains(lowerFilter) ||
                                    p.Builder.ToLower().Contains(lowerFilter) ||
                                    p.World.ToLower().Contains(lowerFilter))
                        .ToList();
                }
                else
                {
                    filteredPuzzles = allPuzzles;
                }

                // Display in a scrollable area
                ImGui.BeginChild("PuzzlesList", new Vector2(0, 290), true);

                // Recently used section
                if (recentlyUsedPuzzles.Count > 0 && string.IsNullOrWhiteSpace(filterText))
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(UiTheme.Primary, FontAwesomeIcon.History.ToIconString() + " Recently Used");
                    ImGui.PopFont();

                    ImGui.Separator();

                    foreach (var puzzle in recentlyUsedPuzzles)
                    {
                        if (ImGui.Selectable($"{puzzle.PuzzleName} ({puzzle.World})##recent_{puzzle.Id}", false))
                        {
                            speedrunManager.SetPuzzle(puzzle);
                            showPuzzleSelector = false;
                        }
                    }

                    ImGui.Spacing();

                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(UiTheme.Primary, FontAwesomeIcon.List.ToIconString() + " All Puzzles");
                    ImGui.PopFont();

                    ImGui.Separator();
                }

                // All puzzles (or filtered results)
                if (filteredPuzzles.Count == 0)
                {
                    ImGui.PushFont(UiBuilder.IconFont);
                    ImGui.TextColored(UiTheme.Error,
                        FontAwesomeIcon.ExclamationTriangle.ToIconString() + " No puzzles found matching your search.");
                    ImGui.PopFont();
                }
                else
                {
                    foreach (var puzzle in filteredPuzzles)
                    {
                        // Show rating with color matching the difficulty
                        Vector4 ratingColor = GetRatingColor(puzzle.Rating);
                        string ratingText = $" [{puzzle.Rating}]";

                        if (ImGui.Selectable($"{puzzle.PuzzleName} ({puzzle.World})##all_{puzzle.Id}", false))
                        {
                            speedrunManager.SetPuzzle(puzzle);
                            showPuzzleSelector = false;
                        }

                        // Add tooltip with more info
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.BeginTooltip();
                            ImGui.Text($"Name: {puzzle.PuzzleName}");
                            ImGui.Text($"Builder: {puzzle.Builder}");
                            ImGui.Text($"World: {puzzle.World}");
                            ImGui.Text($"Rating: ");
                            ImGui.SameLine();
                            ImGui.TextColored(ratingColor, puzzle.Rating);

                            // Add goals/rules if available
                            if (!string.IsNullOrEmpty(puzzle.GoalsOrRules))
                            {
                                ImGui.Separator();
                                ImGui.Text("Goals/Rules:");
                                ImGui.TextWrapped(puzzle.GoalsOrRules);
                            }

                            ImGui.EndTooltip();
                        }
                    }
                }

                ImGui.EndChild();

                // Close button at the bottom
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 120) / 2);

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button(FontAwesomeIcon.Times.ToIconString() + " Close", new Vector2(120, 0)))
                {
                    showPuzzleSelector = false;
                }
                ImGui.PopFont();

                ImGui.EndPopup();
            }
        }

        // Helper methods to get puzzles
        private List<JumpPuzzleData> GetRecentlyUsedPuzzles()
        {
            // This would be better if the plugin tracked recently used puzzles
            // For now, just return a current puzzle if it exists
            var currentPuzzle = speedrunManager.GetCurrentPuzzle();
            var list = new List<JumpPuzzleData>();

            if (currentPuzzle != null)
            {
                list.Add(currentPuzzle);
            }

            return list;
        }

        private List<JumpPuzzleData> GetAllAvailablePuzzles()
        {
            // Get puzzles from the plugin's data
            var allPuzzles = new List<JumpPuzzleData>();

            try
            {
                // Get data directly from plugin
                var dataCenterDict = plugin.MainWindow.GetCsvDataByDataCenter();
                if (dataCenterDict != null)
                {
                    foreach (var dc in dataCenterDict)
                    {
                        if (dc.Value != null)
                        {
                            allPuzzles.AddRange(dc.Value);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // If we can't get the data, use a fallback
                var currentPuzzle = speedrunManager.GetCurrentPuzzle();
                if (currentPuzzle != null)
                {
                    allPuzzles.Add(currentPuzzle);
                }
                else
                {
                    // Create a dummy puzzle as a fallback
                    allPuzzles.Add(new JumpPuzzleData
                    {
                        Id = -999,
                        PuzzleName = "Example Puzzle",
                        Builder = "Example Builder",
                        World = "Example World",
                        Rating = "3★"
                    });
                }
            }

            return allPuzzles.OrderBy(p => p.PuzzleName).ToList();
        }

        private Vector4 GetRatingColor(string rating)
        {
            switch (rating)
            {
                case "1★":
                    return new Vector4(0.0f, 0.8f, 0.0f, 1.0f); // Green
                case "2★":
                    return new Vector4(0.0f, 0.6f, 0.9f, 1.0f); // Blue
                case "3★":
                    return new Vector4(0.9f, 0.8f, 0.0f, 1.0f); // Yellow
                case "4★":
                    return new Vector4(1.0f, 0.5f, 0.0f, 1.0f); // Orange
                case "5★":
                    return new Vector4(0.9f, 0.0f, 0.0f, 1.0f); // Red
                case "E":
                    return new Vector4(0.5f, 0.5f, 1.0f, 1.0f); // Light blue
                case "T":
                    return new Vector4(1.0f, 0.5f, 1.0f, 1.0f); // Light purple
                case "F":
                    return new Vector4(0.5f, 1.0f, 0.5f, 1.0f); // Light green
                default:
                    return new Vector4(0.8f, 0.8f, 0.8f, 1.0f); // Gray
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
            speedrunManager.CountdownTick -= OnCountdownTick;
        }
    }
}
