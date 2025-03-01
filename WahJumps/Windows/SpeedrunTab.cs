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

        // Split practice mode
        private int practicingSplitIndex = -1;
        private TimeSpan practiceSplitStartTime;
        private bool isPracticingAnyMode = false;
        private List<TimeSpan> bestPracticeTimes = new List<TimeSpan>();

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

            // Initialize practice best times list when a new puzzle is selected
            ResetPracticeTimes();
        }

        private void ResetPracticeTimes()
        {
            var splits = speedrunManager.GetCurrentTemplate()?.Splits;
            if (splits != null)
            {
                bestPracticeTimes = new List<TimeSpan>(new TimeSpan[splits.Count]);
                for (int i = 0; i < bestPracticeTimes.Count; i++)
                {
                    bestPracticeTimes[i] = TimeSpan.MaxValue;
                }
            }
            else
            {
                bestPracticeTimes = new List<TimeSpan>();
            }
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

            // If we were in practice mode and the run stopped, record the time
            if (isPracticingAnyMode && state == SpeedrunManager.SpeedrunState.Finished)
            {
                RecordPracticeTime();
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

        private void StartSplitPractice(int splitIndex)
        {
            // Reset current run and start practicing a specific split
            speedrunManager.ResetTimer();
            practicingSplitIndex = splitIndex;
            isPracticingAnyMode = true;
            practiceSplitStartTime = TimeSpan.Zero;

            // Start the timer immediately in practice mode
            speedrunManager.StartCountdown();
            speedrunManager.SkipCountdown();
        }

        private void RecordPracticeTime()
        {
            if (practicingSplitIndex >= 0 && practicingSplitIndex < bestPracticeTimes.Count)
            {
                // Record the practice time if it's better than previous attempts
                if (currentTime < bestPracticeTimes[practicingSplitIndex] || bestPracticeTimes[practicingSplitIndex] == TimeSpan.MaxValue)
                {
                    bestPracticeTimes[practicingSplitIndex] = currentTime;
                }
            }

            // Reset practice mode state
            isPracticingAnyMode = false;
            practicingSplitIndex = -1;
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

                // Display practice mode indicator if active
                if (isPracticingAnyMode)
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f),
                        "PRACTICE MODE ACTIVE");

                    if (practicingSplitIndex >= 0)
                    {
                        var splits = speedrunManager.GetCurrentTemplate()?.Splits;
                        if (splits != null && practicingSplitIndex < splits.Count)
                        {
                            ImGui.SameLine();
                            ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f),
                                $"Split: {splits[practicingSplitIndex].Name}");
                        }
                    }
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
                ImGui.Text("• Select a puzzle from the main window by clicking the timer (⏱) button");
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
                ImGui.Text("• You can practice individual splits by clicking the Practice button next to any split");
                ImGui.Text("• This helps you perfect difficult sections of a puzzle");
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
                ImGui.Spacing();

                ImGui.TextColored(UiTheme.Primary, "6. Chat Commands");
                ImGui.Text("• Use /jumptimer start - Start the timer");
                ImGui.Text("• Use /jumptimer stop - Stop the timer");
                ImGui.Text("• Use /jumptimer split - Mark a split");
                ImGui.Text("• Use /jumptimer reset - Reset the timer");
                ImGui.Text("• Use /jumptimer show - Show the timer window");
                ImGui.Text("• Use /jumptimer hide - Hide the timer window");
                ImGui.Spacing();

                ImGui.TextColored(UiTheme.Primary, "7. Split Practice Mode");
                ImGui.Text("• In the Splits tab, you can click Practice next to any split");
                ImGui.Text("• This lets you focus on mastering specific sections");
                ImGui.Text("• Your best practice times are saved for each split");
                ImGui.Text("• Use this feature to improve your weakest segments");

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
            if (state == SpeedrunManager.SpeedrunState.Finished && lastFinishedRun != null && !isPracticingAnyMode)
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

            // Practice mode indicator if active
            if (isPracticingAnyMode)
            {
                ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize("PRACTICE MODE").X) / 2);
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f), "PRACTICE MODE");

                if (practicingSplitIndex >= 0)
                {
                    var splits = speedrunManager.GetCurrentTemplate()?.Splits;
                    if (splits != null && practicingSplitIndex < splits.Count)
                    {
                        string splitName = splits[practicingSplitIndex].Name;
                        ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(splitName).X) / 2);
                        ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f), splitName);

                        // Show best practice time if available
                        if (bestPracticeTimes.Count > practicingSplitIndex &&
                            bestPracticeTimes[practicingSplitIndex] != TimeSpan.MaxValue)
                        {
                            string bestTime = $"Best: {FormatTime(bestPracticeTimes[practicingSplitIndex])}";
                            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - ImGui.CalcTextSize(bestTime).X) / 2);
                            ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.0f, 1.0f), bestTime);
                        }
                    }
                }
            }

            // Splits display
            if (state == SpeedrunManager.SpeedrunState.Running && !isPracticingAnyMode)
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
                if (!isPracticingAnyMode)
                {
                    if (ImGui.Button("Split", new Vector2(buttonWidth, 40)))
                    {
                        speedrunManager.MarkSplit();
                    }
                }
                else
                {
                    if (ImGui.Button("Complete", new Vector2(buttonWidth, 40)))
                    {
                        speedrunManager.StopTimer();
                    }
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
                    isPracticingAnyMode = false;
                    practicingSplitIndex = -1;
                }
            }
            else if (state == SpeedrunManager.SpeedrunState.Finished)
            {
                // Finished state buttons
                if (ImGui.Button("New Run", new Vector2(buttonWidth, 40)))
                {
                    speedrunManager.ResetTimer();
                    lastFinishedRun = null;
                    isPracticingAnyMode = false;
                    practicingSplitIndex = -1;
                }

                ImGui.SameLine(0, buttonSpacing);

                if (isPracticingAnyMode)
                {
                    if (ImGui.Button("Practice Again", new Vector2(buttonWidth, 40)))
                    {
                        int currentSplit = practicingSplitIndex;
                        StartSplitPractice(currentSplit);
                    }
                }
                else
                {
                    if (ImGui.Button("View Records", new Vector2(buttonWidth, 40)))
                    {
                        currentView = ContentView.Records;
                    }
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

                // If there are any records with splits, show detailed view
                var recordWithSplits = currentPuzzleRecords.FirstOrDefault(r => r.Splits.Count > 0);
                if (recordWithSplits != null)
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.TextColored(UiTheme.Primary, "Detailed Split Analysis");

                    DrawDetailedSplitsTable(currentPuzzleRecords);
                }
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

                        // If there are any records with splits, show detailed view
                        var recordWithSplits = puzzleRecords.FirstOrDefault(r => r.Splits.Count > 0);
                        if (recordWithSplits != null)
                        {
                            ImGui.Spacing();
                            ImGui.TextColored(UiTheme.Primary, "Detailed Split Analysis");
                            DrawDetailedSplitsTable(puzzleRecords);
                        }
                    }
                }

                ImGui.EndChild();
            }
        }

        private void DrawRecordsTable(List<SpeedrunRecord> records)
        {
            if (ImGui.BeginTable("RecordsTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Splits", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthStretch);
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
                    ImGui.Text($"{record.Splits.Count}");

                    // Actions - View details
                    ImGui.TableNextColumn();
                    ImGui.PushID(record.Id.ToString());
                    if (ImGui.Button("Details") && record.Splits.Count > 0)
                    {
                        // Show details popup
                        ImGui.OpenPopup("RecordDetailsPopup");
                        lastFinishedRun = record; // Use this to display details
                    }

                    // Details popup
                    if (ImGui.BeginPopup("RecordDetailsPopup"))
                    {
                        ImGui.Text($"Record from {record.Date}");
                        ImGui.Text($"Total Time: {FormatTime(record.Time)}");
                        ImGui.Separator();

                        if (record.Splits.Count > 0)
                        {
                            // Simple table for splits in popup
                            if (ImGui.BeginTable("DetailSplitsTable", 3, ImGuiTableFlags.Borders))
                            {
                                ImGui.TableSetupColumn("Split");
                                ImGui.TableSetupColumn("Split Time");
                                ImGui.TableSetupColumn("Total Time");
                                ImGui.TableHeadersRow();

                                foreach (var split in record.Splits.OrderBy(s => s.Order))
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
                        else
                        {
                            ImGui.Text("No splits available for this record.");
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }
        }

        private void DrawDetailedSplitsTable(List<SpeedrunRecord> records)
        {
            // Find the best record (fastest) with splits
            var bestRecord = records
                .Where(r => r.Splits.Count > 0)
                .OrderBy(r => r.Time)
                .FirstOrDefault();

            if (bestRecord == null) return;

            // Get all split names in order from the best record
            var splitNames = bestRecord.Splits
                .OrderBy(s => s.Order)
                .Select(s => s.Name)
                .ToList();

            if (ImGui.BeginTable("DetailedSplitsTable", splitNames.Count + 2, ImGuiTableFlags.Borders | ImGuiTableFlags.ScrollX))
            {
                // Setup header columns
                ImGui.TableSetupColumn("Date/Time", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Total", ImGuiTableColumnFlags.WidthFixed, 100);

                // Setup split columns
                foreach (var splitName in splitNames)
                {
                    ImGui.TableSetupColumn(splitName, ImGuiTableColumnFlags.WidthFixed, 100);
                }

                ImGui.TableHeadersRow();

                // For each record with splits
                foreach (var record in records.Where(r => r.Splits.Count > 0).OrderBy(r => r.Time).Take(5))
                {
                    ImGui.TableNextRow();

                    // Date and total time
                    ImGui.TableNextColumn();
                    ImGui.Text(record.Date.ToString("yyyy-MM-dd HH:mm"));

                    ImGui.TableNextColumn();
                    ImGui.Text(FormatTime(record.Time));

                    // Each split time
                    var recordSplits = record.Splits.OrderBy(s => s.Order).ToList();

                    for (int i = 0; i < splitNames.Count; i++)
                    {
                        ImGui.TableNextColumn();

                        // Find corresponding split in this record
                        var split = recordSplits.FirstOrDefault(s => s.Name == splitNames[i]);

                        if (split != null && split.SplitTime.HasValue)
                        {
                            // Color good/bad splits based on comparison to best
                            var bestSplit = bestRecord.Splits
                                .FirstOrDefault(s => s.Name == splitNames[i]);

                            if (bestSplit != null && bestSplit.SplitTime.HasValue)
                            {
                                float ratio = (float)(split.SplitTime.Value.TotalMilliseconds / bestSplit.SplitTime.Value.TotalMilliseconds);

                                if (ratio <= 1.05f) // Within 5% of best
                                {
                                    ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.2f, 1.0f),
                                        FormatTime(split.SplitTime.Value));
                                }
                                else if (ratio <= 1.2f) // Within 20% of best
                                {
                                    ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.0f, 1.0f),
                                        FormatTime(split.SplitTime.Value));
                                }
                                else // Much worse than best
                                {
                                    ImGui.TextColored(new Vector4(0.9f, 0.4f, 0.4f, 1.0f),
                                        FormatTime(split.SplitTime.Value));
                                }
                            }
                            else
                            {
                                ImGui.Text(FormatTime(split.SplitTime.Value));
                            }
                        }
                        else
                        {
                            ImGui.Text("-");
                        }
                    }
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

                    // Initialize practice times
                    ResetPracticeTimes();
                }
                return;
            }

            ImGui.TextColored(UiTheme.Primary, $"Splits for: {currentPuzzle.PuzzleName}");

            // Template name
            string templateName = currentTemplate.Name;
            ImGui.SetNextItemWidth(300);
            if (ImGui.InputText("Template Name", ref templateName, 100))
            {
                currentTemplate.Name = templateName;
                speedrunManager.UpdateTemplate(currentTemplate);
            }

            ImGui.Separator();
            ImGui.TextColored(UiTheme.Primary, "Splits:");

            // Informational text for practice mode
            ImGui.TextColored(UiTheme.Secondary, "Click 'Practice' to time yourself on individual sections");

            // Splits list
            if (currentTemplate.Splits.Count > 0)
            {
                if (ImGui.BeginTable("SplitsTable", 5, ImGuiTableFlags.Borders))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthFixed, 60);
                    ImGui.TableSetupColumn("Best Practice", ImGuiTableColumnFlags.WidthFixed, 100);
                    ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 140);
                    ImGui.TableSetupColumn("Practice", ImGuiTableColumnFlags.WidthFixed, 80);
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

                        // Best practice time column
                        ImGui.TableNextColumn();
                        if (i < bestPracticeTimes.Count && bestPracticeTimes[i] != TimeSpan.MaxValue)
                        {
                            ImGui.TextColored(timeColor, FormatTime(bestPracticeTimes[i]));
                        }
                        else
                        {
                            ImGui.Text("No data");
                        }

                        // Actions column
                        ImGui.TableNextColumn();

                        // Move up button
                        if (i > 0 && ImGui.Button("Up##" + i))
                        {
                            // Swap this split with the one above it
                            int thisOrder = split.Order;
                            int prevOrder = orderedSplits[i - 1].Order;

                            split.Order = prevOrder;
                            orderedSplits[i - 1].Order = thisOrder;

                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        ImGui.SameLine();

                        // Move down button
                        if (i < orderedSplits.Count - 1 && ImGui.Button("Down##" + i))
                        {
                            // Swap this split with the one below it
                            int thisOrder = split.Order;
                            int nextOrder = orderedSplits[i + 1].Order;

                            split.Order = nextOrder;
                            orderedSplits[i + 1].Order = thisOrder;

                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        ImGui.SameLine();

                        // Delete button
                        if (ImGui.Button("Delete##" + i))
                        {
                            currentTemplate.Splits.Remove(split);

                            // Remove the corresponding practice time
                            if (i < bestPracticeTimes.Count)
                            {
                                bestPracticeTimes.RemoveAt(i);
                            }

                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        // Practice column
                        ImGui.TableNextColumn();
                        if (ImGui.Button("Practice##" + i))
                        {
                            StartSplitPractice(i);
                            currentView = ContentView.Timer;
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

                // Add a new practice time slot
                bestPracticeTimes.Add(TimeSpan.MaxValue);

                newSplitName = string.Empty;
            }

            ImGui.Spacing();

            // Full run practice button
            if (currentTemplate.Splits.Count > 0)
            {
                if (ImGui.Button("Practice Full Run", new Vector2(200, 30)))
                {
                    isPracticingAnyMode = true;
                    practicingSplitIndex = -1;
                    speedrunManager.ResetTimer();
                    speedrunManager.StartCountdown();
                    currentView = ContentView.Timer;
                }
            }

            // Helper text
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                "Tip: Create splits for key checkpoints in your puzzle run.");
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
