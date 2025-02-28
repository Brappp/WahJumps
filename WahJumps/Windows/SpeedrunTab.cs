// File: WahJumps/Windows/SpeedrunTab.cs
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class SpeedrunTab : IDisposable
    {
        private readonly SpeedrunManager speedrunManager;
        private readonly Plugin plugin;

        // Current puzzle
        private JumpPuzzleData currentPuzzle;

        // Simple state variables
        private bool isRunning = false;
        private TimeSpan currentTime;
        private int countdownRemaining;
        private string newSplitName = string.Empty;

        // Display settings
        private Vector4 timeColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);
        private enum ContentView { HowTo, Timer, Records, Splits }
        private ContentView currentView = ContentView.Timer;

        // Recently finished run
        private SpeedrunRecord lastFinishedRun = null;

        public SpeedrunTab(SpeedrunManager speedrunManager, Plugin plugin)
        {
            this.speedrunManager = speedrunManager;
            this.plugin = plugin;

            // Subscribe to events
            speedrunManager.StateChanged += OnStateChanged;
            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.CountdownTick += OnCountdownTick;
            speedrunManager.RunCompleted += OnRunCompleted;
        }

        public void SetPuzzle(JumpPuzzleData puzzle)
        {
            // Called when a puzzle is selected from the main window
            currentPuzzle = puzzle;
            speedrunManager.SetPuzzle(puzzle);
        }

        // Method to force speedrun tab to be the active tab
        public void ForceActivate()
        {
            // Can't directly force a tab open in ImGui, so we'll close all other tabs
            // which should make ImGui focus on the speedrun tab by default
            ImGui.SetTabItemClosed("Strange Housing");
            ImGui.SetTabItemClosed("Information");
            ImGui.SetTabItemClosed("Favorites");
            ImGui.SetTabItemClosed("Search");
            ImGui.SetTabItemClosed("Settings");
        }

        private void OnStateChanged(SpeedrunManager.SpeedrunState state)
        {
            isRunning = state == SpeedrunManager.SpeedrunState.Running;

            // Switch to timer view if starting a run
            if (state == SpeedrunManager.SpeedrunState.Countdown ||
                state == SpeedrunManager.SpeedrunState.Running)
            {
                currentView = ContentView.Timer;
            }
        }

        private void OnTimeUpdated(TimeSpan time)
        {
            currentTime = time;
        }

        private void OnCountdownTick(int remaining)
        {
            countdownRemaining = remaining;
        }

        private void OnRunCompleted(SpeedrunRecord record)
        {
            lastFinishedRun = record;
        }

        public void Draw()
        {
            using var tabItem = new ImRaii.TabItem("Speedrun");
            if (!tabItem.Success) return;

            // Top navigation bar
            DrawNavBar();

            ImGui.Separator();

            // Current puzzle info
            DrawCurrentPuzzleInfo();

            // Main content based on selected view
            switch (currentView)
            {
                case ContentView.HowTo:
                    DrawHowToSection();
                    break;
                case ContentView.Timer:
                    DrawTimerSection();
                    break;
                case ContentView.Records:
                    DrawRecordsSection();
                    break;
                case ContentView.Splits:
                    DrawSplitsSection();
                    break;
            }
        }

        private void DrawNavBar()
        {
            // Simple navigation buttons
            float buttonWidth = ImGui.GetContentRegionAvail().X / 4 - 8;

            if (ImGui.Button("How To", new Vector2(buttonWidth, 0)))
            {
                currentView = ContentView.HowTo;
            }

            ImGui.SameLine();

            if (ImGui.Button("Timer", new Vector2(buttonWidth, 0)))
            {
                currentView = ContentView.Timer;
            }

            ImGui.SameLine();

            if (ImGui.Button("Records", new Vector2(buttonWidth, 0)))
            {
                currentView = ContentView.Records;
                speedrunManager.LoadRecords(); // Refresh records
            }

            ImGui.SameLine();

            if (ImGui.Button("Splits", new Vector2(buttonWidth, 0)))
            {
                currentView = ContentView.Splits;
            }
        }

        private void DrawCurrentPuzzleInfo()
        {
            // Current puzzle display
            if (currentPuzzle != null)
            {
                ImGui.TextColored(UiTheme.Primary, $"Selected: {currentPuzzle.PuzzleName}");
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 300);

                // Tooltip about mini-window
                ImGui.TextColored(UiTheme.Success, "Tip: Use the mini-timer window during gameplay");

                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 100);

                // Allow changing puzzle with button
                if (ImGui.Button("Change"))
                {
                    // Return to main window to select another puzzle
                    ImGui.SetTabItemClosed("Speedrun");
                }
            }
            else
            {
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f),
                    "No puzzle selected. Select a puzzle from the main tab first.");
            }

            ImGui.Separator();
        }

        private void DrawHowToSection()
        {
            // Simple, clear instructions
            ImGui.TextColored(UiTheme.Primary, "How to Use the Speedrun Timer");
            ImGui.Spacing();

            using (var child = new ImRaii.Child("Instructions", new Vector2(0, 0), true))
            {
                ImGui.PushTextWrapPos(ImGui.GetContentRegionAvail().X);

                ImGui.TextColored(UiTheme.Primary, "1. Getting Started");
                ImGui.Text("• Select a puzzle from the main window by clicking the timer (Time) button");
                ImGui.Text("• Go to the Timer tab to start your run");
                ImGui.Text("• Use the Start Timer button to begin (with countdown) or Skip Countdown to start immediately");
                ImGui.Spacing();

                ImGui.TextColored(UiTheme.Primary, "2. During a Run");
                ImGui.Text("• The timer will show your current time");
                ImGui.Text("• Use the Split button each time you reach a checkpoint");
                ImGui.Text("• Use Stop to end your run or Reset to cancel it");
                ImGui.Text("• The mini-timer window can be kept visible while you play");
                ImGui.Spacing();

                ImGui.TextColored(UiTheme.Primary, "3. Managing Splits");
                ImGui.Text("• Go to the Splits tab to create and edit split checkpoints");
                ImGui.Text("• Add descriptive splits like \"First Jump\", \"Halfway Point\", etc.");
                ImGui.Text("• Splits help track your progress through different parts of the puzzle");
                ImGui.Spacing();

                ImGui.TextColored(UiTheme.Primary, "4. Viewing Records");
                ImGui.Text("• Check the Records tab to see your best times");
                ImGui.Text("• Records are saved automatically when you complete a run");
                ImGui.Text("• Compare your times across different attempts");
                ImGui.Spacing();

                ImGui.TextColored(UiTheme.Primary, "5. Using the Mini-Timer");
                ImGui.Text("• The mini-timer window can be toggled with the Timer button");
                ImGui.Text("• This window stays visible even when the main plugin window is closed");
                ImGui.Text("• Perfect for keeping track of your time while focusing on the game");
                ImGui.Text("• Can be expanded or minimized to show just what you need");

                ImGui.PopTextWrapPos();
            }
        }

        private void DrawTimerSection()
        {
            if (currentPuzzle == null)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f),
                    "Please select a puzzle from the main window to start timing.");
                return;
            }

            var state = speedrunManager.GetState();

            // If we're in countdown state, show large countdown
            if (state == SpeedrunManager.SpeedrunState.Countdown)
            {
                DrawCountdown();
                return;
            }

            // If we're in a finished state, show completion info
            if (state == SpeedrunManager.SpeedrunState.Finished && lastFinishedRun != null)
            {
                DrawRunCompletedInfo();
                return;
            }

            // Timer area
            ImGui.BeginChild("TimerArea", new Vector2(0, 300), true);

            // Format time
            string timeText = FormatTime(currentTime);

            // Time display
            float fontSize = 2.5f;
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - textSize.X) / 2);
            ImGui.SetCursorPosY(15);

            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);
            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Splits display
            if (state == SpeedrunManager.SpeedrunState.Running)
            {
                DrawRunningSplits();
            }

            ImGui.EndChild();

            // Control buttons
            DrawControlButtons(state);

            // Mini-timer button
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Show Mini-Timer Window", new Vector2(200, 30)))
            {
                plugin.TimerWindow.ShowTimer();
            }
            ImGui.SameLine();
            ImGui.TextColored(UiTheme.Secondary, "Keep timer visible while playing");
        }

        private void DrawCountdown()
        {
            // Calculate window dimensions
            float windowWidth = ImGui.GetContentRegionAvail().X;
            float windowHeight = 300;

            ImGui.BeginChild("CountdownArea", new Vector2(windowWidth, windowHeight), true);

            // Get current countdown value
            int countdown = countdownRemaining;

            // Super large font size
            float fontSize = 6.0f;
            string text = countdown.ToString();

            // Ensure the countdown is very large and centered
            var textSize = ImGui.CalcTextSize(text) * fontSize;

            // Center the text - make sure it's in the visible area
            float centerX = (windowWidth - textSize.X) / 2;
            float centerY = (windowHeight - textSize.Y) / 2;

            ImGui.SetCursorPos(new Vector2(centerX, centerY));

            // Draw with orange color
            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(text);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Add helpful text below
            ImGui.SetCursorPosX((windowWidth - ImGui.CalcTextSize("Get Ready!").X) / 2);
            ImGui.SetCursorPosY(centerY + textSize.Y + 20);
            ImGui.TextColored(UiTheme.Primary, "Get Ready!");

            // Skip button at the bottom
            float skipBtnWidth = 120;
            ImGui.SetCursorPosX((windowWidth - skipBtnWidth) / 2);
            ImGui.SetCursorPosY(windowHeight - 40);

            if (ImGui.Button("Skip Countdown", new Vector2(skipBtnWidth, 30)))
            {
                speedrunManager.SkipCountdown();
            }

            ImGui.EndChild();
        }

        private void DrawRunningSplits()
        {
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 20); // Add some space

            var splits = speedrunManager.GetCurrentSplits();
            int currentSplitIndex = speedrunManager.GetCurrentSplitIndex();

            if (splits.Count > 0)
            {
                ImGui.Separator();
                ImGui.TextColored(UiTheme.Primary, "Splits:");

                // Calculate max visible splits
                int maxVisibleSplits = 4;
                int startIndex = Math.Max(0, currentSplitIndex - 1);
                int endIndex = Math.Min(startIndex + maxVisibleSplits, splits.Count);

                // Draw visible splits
                for (int i = startIndex; i < endIndex; i++)
                {
                    var split = splits[i];

                    if (i < currentSplitIndex)
                    {
                        // Completed split - green checkmark
                        ImGui.TextColored(timeColor, $"✓ {split.Name}: {FormatTime(split.Time)}");
                    }
                    else if (i == currentSplitIndex + 1)
                    {
                        // Next split - yellow highlight
                        ImGui.TextColored(new Vector4(1.0f, 0.9f, 0.2f, 1.0f), $"► {split.Name}");
                    }
                    else
                    {
                        // Future split - dimmed
                        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), $"  {split.Name}");
                    }
                }

                // If there are more splits than visible
                if (endIndex < splits.Count)
                {
                    ImGui.TextColored(new Vector4(0.6f, 0.6f, 0.6f, 1.0f),
                        $"  + {splits.Count - endIndex} more splits...");
                }
            }
        }

        private void DrawControlButtons(SpeedrunManager.SpeedrunState state)
        {
            float buttonSpacing = 10;
            float windowWidth = ImGui.GetContentRegionAvail().X;
            int buttonCount = (state == SpeedrunManager.SpeedrunState.Running) ? 3 : 2;
            float buttonWidth = (windowWidth - (buttonCount - 1) * buttonSpacing) / buttonCount;

            if (state == SpeedrunManager.SpeedrunState.Idle)
            {
                // Start buttons
                if (ImGui.Button("Start Timer", new Vector2(buttonWidth, 40)))
                {
                    speedrunManager.StartCountdown();
                }

                ImGui.SameLine(0, buttonSpacing);

                if (ImGui.Button("Skip Countdown", new Vector2(buttonWidth, 40)))
                {
                    speedrunManager.StartCountdown();
                    speedrunManager.SkipCountdown();
                }
            }
            else if (state == SpeedrunManager.SpeedrunState.Running)
            {
                // Running control buttons
                if (ImGui.Button("Split", new Vector2(buttonWidth, 40)))
                {
                    speedrunManager.MarkSplit();
                }

                ImGui.SameLine(0, buttonSpacing);

                if (ImGui.Button("Stop", new Vector2(buttonWidth, 40)))
                {
                    speedrunManager.StopTimer();
                }

                ImGui.SameLine(0, buttonSpacing);

                if (ImGui.Button("Reset", new Vector2(buttonWidth, 40)))
                {
                    speedrunManager.ResetTimer();
                }
            }
            else if (state == SpeedrunManager.SpeedrunState.Finished)
            {
                // Finished state buttons
                if (ImGui.Button("New Run", new Vector2(buttonWidth, 40)))
                {
                    speedrunManager.ResetTimer();
                    lastFinishedRun = null;
                }

                ImGui.SameLine(0, buttonSpacing);

                if (ImGui.Button("View Records", new Vector2(buttonWidth, 40)))
                {
                    currentView = ContentView.Records;
                }
            }
        }

        private void DrawRunCompletedInfo()
        {
            // Show run completed info
            ImGui.BeginChild("CompletedRunArea", new Vector2(0, 300), true);

            // Header
            ImGui.TextColored(timeColor, "Run Completed!");
            ImGui.Text($"Puzzle: {lastFinishedRun.PuzzleName}");

            // Final time - large display
            string timeText = FormatTime(lastFinishedRun.Time);
            float fontSize = 2.5f;
            var textSize = ImGui.CalcTextSize(timeText) * fontSize;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - textSize.X) / 2);
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 15);

            ImGui.PushStyleColor(ImGuiCol.Text, timeColor);
            ImGui.SetWindowFontScale(fontSize);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Show splits if available
            if (lastFinishedRun.Splits.Count > 0)
            {
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + 20);
                ImGui.Separator();
                ImGui.TextColored(UiTheme.Primary, "Split Times:");

                // Simple table for splits
                if (ImGui.BeginTable("CompletedSplits", 3, ImGuiTableFlags.Borders))
                {
                    ImGui.TableSetupColumn("Split");
                    ImGui.TableSetupColumn("Split Time");
                    ImGui.TableSetupColumn("Total Time");
                    ImGui.TableHeadersRow();

                    foreach (var split in lastFinishedRun.Splits.OrderBy(s => s.Order))
                    {
                        ImGui.TableNextRow();

                        // Split name
                        ImGui.TableNextColumn();
                        ImGui.Text(split.Name);

                        // Split time
                        ImGui.TableNextColumn();
                        if (split.SplitTime.HasValue)
                        {
                            ImGui.Text(FormatTime(split.SplitTime.Value));
                        }
                        else
                        {
                            ImGui.Text("-");
                        }

                        // Total time at split
                        ImGui.TableNextColumn();
                        ImGui.Text(FormatTime(split.Time));
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.EndChild();
        }

        private void DrawRecordsSection()
        {
            // Simple records view
            ImGui.TextColored(UiTheme.Primary, "Your Records");

            // Get all records
            var allRecords = speedrunManager.GetRecords();

            if (allRecords.Count == 0)
            {
                ImGui.Text("No records yet. Complete a run to create your first record!");
                return;
            }

            // Group records by puzzle
            var recordsByPuzzle = allRecords
                .GroupBy(r => r.PuzzleName)
                .OrderBy(g => g.Key)
                .ToList();

            // Filter for current puzzle if one is selected
            if (currentPuzzle != null)
            {
                ImGui.Text($"Showing records for: {currentPuzzle.PuzzleName}");

                var currentPuzzleRecords = allRecords
                    .Where(r => r.PuzzleName == currentPuzzle.PuzzleName)
                    .OrderBy(r => r.Time)
                    .ToList();

                DrawRecordsTable(currentPuzzleRecords);
            }
            else
            {
                // Show all records grouped by puzzle
                ImGui.BeginChild("AllRecordsView", new Vector2(0, 0), true);

                foreach (var puzzleGroup in recordsByPuzzle)
                {
                    string puzzleName = puzzleGroup.Key;
                    var puzzleRecords = puzzleGroup.OrderBy(r => r.Time).ToList();

                    // Collapsing header for each puzzle
                    if (ImGui.CollapsingHeader(puzzleName))
                    {
                        DrawRecordsTable(puzzleRecords);
                    }
                }

                ImGui.EndChild();
            }
        }

        private void DrawRecordsTable(List<SpeedrunRecord> records)
        {
            if (ImGui.BeginTable("RecordsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Splits", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var record in records)
                {
                    ImGui.TableNextRow();

                    // Date
                    ImGui.TableNextColumn();
                    ImGui.Text(record.Date.ToString("yyyy-MM-dd HH:mm"));

                    // Time
                    ImGui.TableNextColumn();
                    ImGui.Text(FormatTime(record.Time));

                    // Splits count
                    ImGui.TableNextColumn();
                    ImGui.Text($"{record.Splits.Count} splits");
                }

                ImGui.EndTable();
            }
        }

        private void DrawSplitsSection()
        {
            if (currentPuzzle == null)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f),
                    "Select a puzzle from the main window first to edit splits.");
                return;
            }

            // Get current template
            var currentTemplate = speedrunManager.GetCurrentTemplate();

            if (currentTemplate == null)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f),
                    "No splits template for this puzzle.");

                if (ImGui.Button("Create Default Template"))
                {
                    // Create a template with just a finish split
                    var template = new SplitTemplate(currentPuzzle.PuzzleName + " Template")
                    {
                        PuzzleId = currentPuzzle.Id >= 0 ? currentPuzzle.Id : null,
                        PuzzleName = currentPuzzle.PuzzleName,
                        IsCustomPuzzle = currentPuzzle.Id < 0
                    };
                    template.Splits.Add(new SplitCheckpoint("Finish", 0));
                    speedrunManager.UpdateTemplate(template);
                    speedrunManager.SetTemplate(template);
                }
                return;
            }

            ImGui.TextColored(UiTheme.Primary, $"Editing Splits for: {currentPuzzle.PuzzleName}");

            // Template name
            string templateName = currentTemplate.Name;
            ImGui.SetNextItemWidth(300);
            if (ImGui.InputText("Template Name", ref templateName, 100))
            {
                currentTemplate.Name = templateName;
                speedrunManager.UpdateTemplate(currentTemplate);
            }

            ImGui.Separator();
            ImGui.Text("Splits:");

            // Splits list
            if (currentTemplate.Splits.Count > 0)
            {
                if (ImGui.BeginTable("SplitsTable", 3, ImGuiTableFlags.Borders))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableHeadersRow();

                    // Get ordered splits
                    var orderedSplits = currentTemplate.Splits
                        .OrderBy(s => s.Order)
                        .ToList();

                    for (int i = 0; i < orderedSplits.Count; i++)
                    {
                        var split = orderedSplits[i];
                        string splitName = split.Name;
                        int order = split.Order;

                        ImGui.TableNextRow();
                        ImGui.PushID(i);

                        // Name column
                        ImGui.TableNextColumn();
                        if (ImGui.InputText("##name", ref splitName, 100))
                        {
                            split.Name = splitName;
                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        // Order column
                        ImGui.TableNextColumn();
                        if (ImGui.InputInt("##order", ref order, 0))
                        {
                            split.Order = order;
                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        // Delete button
                        ImGui.TableNextColumn();
                        if (ImGui.Button("X"))
                        {
                            currentTemplate.Splits.Remove(split);
                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        ImGui.PopID();
                    }

                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                    "No splits defined yet. Add your first split below.");
            }

            // Add new split
            ImGui.Separator();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 100);
            ImGui.InputText("##newSplitName", ref newSplitName, 100);

            ImGui.SameLine();

            if (ImGui.Button("Add Split") && !string.IsNullOrEmpty(newSplitName))
            {
                // Find the next available order
                int nextOrder = 0;
                if (currentTemplate.Splits.Any())
                {
                    nextOrder = currentTemplate.Splits.Max(s => s.Order) + 1;
                }

                currentTemplate.Splits.Add(new SplitCheckpoint(newSplitName, nextOrder));
                speedrunManager.UpdateTemplate(currentTemplate);
                newSplitName = string.Empty;
            }

            // Helper text
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                "Tip: Add splits for key checkpoints in your puzzle run.");
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        }

        public void Dispose()
        {
            // Unsubscribe from events
            speedrunManager.StateChanged -= OnStateChanged;
            speedrunManager.TimeUpdated -= OnTimeUpdated;
            speedrunManager.CountdownTick -= OnCountdownTick;
            speedrunManager.RunCompleted -= OnRunCompleted;
        }
    }
}
