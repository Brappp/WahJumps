// File: WahJumps/Windows/SpeedrunOverlayWindow.cs
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class SpeedrunOverlayWindow : Window, IDisposable
    {
        private readonly SpeedrunManager speedrunManager;
        private Dictionary<string, string> customFields = new Dictionary<string, string>();
        private bool isConfiguring = false;
        private bool isSplitEditing = false;
        private bool isCustomPuzzleCreating = false;
        private bool isTemplateSelecting = false;
        private string customKeyInput = string.Empty;
        private string customValueInput = string.Empty;
        private string newSplitName = string.Empty;
        private string newPuzzleName = string.Empty;
        private string newPuzzleDesc = string.Empty;
        private string newPuzzleCreator = string.Empty;
        private string customPuzzleSearch = string.Empty;
        private string templateSearch = string.Empty;
        private Vector2 lastPosition = new Vector2(-1, -1);
        private int selectedTab = 0;
        private List<CustomPuzzle> customPuzzles = new List<CustomPuzzle>();
        private List<SplitTemplate> templates = new List<SplitTemplate>();

        // Visual customization
        private Vector4 splitCompletedColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);
        private Vector4 splitPendingColor = new Vector4(1.0f, 1.0f, 1.0f, 0.7f);
        private Vector4 splitAheadColor = new Vector4(0.0f, 0.8f, 0.2f, 1.0f);
        private Vector4 splitBehindColor = new Vector4(0.8f, 0.0f, 0.0f, 1.0f);
        private bool minimalMode = false;

        public SpeedrunOverlayWindow(SpeedrunManager speedrunManager)
            : base("Speedrun Timer",
                   ImGuiWindowFlags.NoScrollbar |
                   ImGuiWindowFlags.AlwaysAutoResize |
                   ImGuiWindowFlags.NoCollapse)
        {
            this.speedrunManager = speedrunManager;

            // Make the window movable by not setting a fixed position
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(200, 100),
                MaximumSize = new Vector2(1000, 800)
            };

            // Register to manager events
            speedrunManager.StateChanged += OnStateChanged;
            speedrunManager.TimeUpdated += OnTimeUpdated;
            speedrunManager.CountdownTick += OnCountdownTick;
            speedrunManager.SplitCompleted += OnSplitCompleted;
            speedrunManager.TemplateChanged += OnTemplateChanged;
            speedrunManager.CustomPuzzleAdded += OnCustomPuzzleAdded;

            // Load custom puzzles and templates
            RefreshData();
        }

        private void RefreshData()
        {
            customPuzzles = speedrunManager.GetCustomPuzzles();
            templates = speedrunManager.GetTemplates();
        }

        private void OnCustomPuzzleAdded(CustomPuzzle puzzle)
        {
            RefreshData();
        }

        private void OnTemplateChanged(SplitTemplate template)
        {
            RefreshData();
        }

        private void OnSplitCompleted(SplitCheckpoint split)
        {
            // Play a sound or add visual feedback when a split is completed
        }

        public override void Draw()
        {
            // Remember window position if it's moved
            Vector2 currentPos = ImGui.GetWindowPos();
            if (ImGui.IsWindowFocused() && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                lastPosition = currentPos;
            }
            else if (lastPosition.X >= 0 && lastPosition.Y >= 0 && !ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                Position = lastPosition;
            }

            speedrunManager.Update();

            var state = speedrunManager.GetState();
            var currentTime = speedrunManager.GetCurrentTime();
            var currentPuzzle = speedrunManager.GetCurrentPuzzle();

            // If we're in minimal mode, just show the timer
            if (minimalMode && state == SpeedrunManager.SpeedrunState.Running)
            {
                DrawMinimalRunningState(currentTime);
                return;
            }

            // Main tab bar for different modes
            if (ImGui.BeginTabBar("SpeedrunTabs"))
            {
                if (ImGui.BeginTabItem("Timer"))
                {
                    selectedTab = 0;
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
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Splits"))
                {
                    selectedTab = 1;
                    DrawSplitsEditor();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Custom Puzzles"))
                {
                    selectedTab = 2;
                    DrawCustomPuzzlesManager();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Templates"))
                {
                    selectedTab = 3;
                    DrawTemplatesManager();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Settings"))
                {
                    selectedTab = 4;
                    DrawSettingsTab();
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            // Draw popups
            DrawPopups();
        }

        private void DrawMinimalRunningState(TimeSpan time)
        {
            // Format time as mm:ss.ms
            string timeText = $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.8f, 0.2f, 1.0f));
            ImGui.SetWindowFontScale(1.5f);
            ImGui.Text(timeText);
            ImGui.SetWindowFontScale(1.0f);
            ImGui.PopStyleColor();

            // Small button to exit minimal mode
            if (ImGui.Button("▢"))
            {
                minimalMode = false;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Exit minimal mode");
            }

            ImGui.SameLine();

            // Split button
            if (ImGui.Button("Split"))
            {
                speedrunManager.MarkSplit();
            }

            ImGui.SameLine();

            // Reset button
            if (ImGui.Button("Reset"))
            {
                speedrunManager.ResetTimer();
                minimalMode = false;
            }
        }

        private void DrawIdleState(JumpPuzzleData selectedPuzzle)
        {
            if (selectedPuzzle != null)
            {
                ImGui.Text($"Selected: {selectedPuzzle.PuzzleName}");
                ImGui.Text($"World: {selectedPuzzle.World}");

                // Check if there's a template
                var template = speedrunManager.GetCurrentTemplate();
                if (template != null)
                {
                    ImGui.Text($"Template: {template.Name}");

                    // Show splits preview
                    if (template.Splits.Count > 0)
                    {
                        ImGui.Separator();
                        ImGui.Text("Splits:");

                        foreach (var split in template.Splits.OrderBy(s => s.Order))
                        {
                            ImGui.BulletText(split.Name);
                        }
                    }
                }
            }
            else
            {
                ImGui.Text("No puzzle selected");
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f),
                    "Select a puzzle from the main window or create a custom puzzle");
            }

            ImGui.Separator();

            if (selectedPuzzle != null)
            {
                if (ImGui.Button("Start", new Vector2(80, 30)))
                {
                    speedrunManager.StartCountdown(customFields);
                }

                ImGui.SameLine();

                if (ImGui.Button("Select Template"))
                {
                    isTemplateSelecting = true;
                    RefreshData();
                }

                ImGui.SameLine();
            }

            if (ImGui.Button("Custom Puzzle"))
            {
                isCustomPuzzleCreating = true;
                newPuzzleName = "";
                newPuzzleDesc = "";
                newPuzzleCreator = "";
            }

            ImGui.SameLine();

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

            ImGui.Separator();

            // Visual options
            if (ImGui.CollapsingHeader("Visual Options", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Checkbox("Minimal Mode During Run", ref minimalMode);

                // Fixed Vector4 to Vector3 conversions for ColorEdit3
                Vector3 completedColor = new Vector3(splitCompletedColor.X, splitCompletedColor.Y, splitCompletedColor.Z);
                if (ImGui.ColorEdit3("Completed Split Color", ref completedColor))
                {
                    splitCompletedColor = new Vector4(completedColor.X, completedColor.Y, completedColor.Z, splitCompletedColor.W);
                }

                Vector3 pendingColor = new Vector3(splitPendingColor.X, splitPendingColor.Y, splitPendingColor.Z);
                if (ImGui.ColorEdit3("Pending Split Color", ref pendingColor))
                {
                    splitPendingColor = new Vector4(pendingColor.X, pendingColor.Y, pendingColor.Z, splitPendingColor.W);
                }

                Vector3 aheadColor = new Vector3(splitAheadColor.X, splitAheadColor.Y, splitAheadColor.Z);
                if (ImGui.ColorEdit3("Ahead Time Color", ref aheadColor))
                {
                    splitAheadColor = new Vector4(aheadColor.X, aheadColor.Y, aheadColor.Z, splitAheadColor.W);
                }

                Vector3 behindColor = new Vector3(splitBehindColor.X, splitBehindColor.Y, splitBehindColor.Z);
                if (ImGui.ColorEdit3("Behind Time Color", ref behindColor))
                {
                    splitBehindColor = new Vector4(behindColor.X, behindColor.Y, behindColor.Z, splitBehindColor.W);
                }
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

            // Draw splits if we have any
            var currentSplits = speedrunManager.GetCurrentSplits();
            var currentSplitIndex = speedrunManager.GetCurrentSplitIndex();

            if (currentSplits.Count > 0)
            {
                ImGui.Separator();

                float tableHeight = Math.Min(currentSplits.Count * 25, 200);
                if (ImGui.BeginTable("SplitsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new Vector2(0, tableHeight)))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 80);

                    for (int i = 0; i < currentSplits.Count; i++)
                    {
                        var split = currentSplits[i];
                        ImGui.TableNextRow();

                        // Name column
                        ImGui.TableNextColumn();
                        if (i < currentSplitIndex)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, splitCompletedColor);
                            ImGui.Text(split.Name);
                            ImGui.PopStyleColor();
                        }
                        else if (i == currentSplitIndex + 1) // Next split
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                            ImGui.Text($"► {split.Name}");
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, splitPendingColor);
                            ImGui.Text(split.Name);
                            ImGui.PopStyleColor();
                        }

                        // Split time column
                        ImGui.TableNextColumn();
                        if (split.SplitTime.HasValue)
                        {
                            var splitTime = split.SplitTime.Value;
                            string splitTimeText = $"{(int)splitTime.TotalMinutes:D2}:{splitTime.Seconds:D2}.{splitTime.Milliseconds / 10:D2}";
                            ImGui.Text(splitTimeText);
                        }
                        else
                        {
                            ImGui.Text("-");
                        }

                        // Total time column
                        ImGui.TableNextColumn();
                        if (split.IsCompleted)
                        {
                            var totalTime = split.Time;
                            string totalTimeText = $"{(int)totalTime.TotalMinutes:D2}:{totalTime.Seconds:D2}.{totalTime.Milliseconds / 10:D2}";
                            ImGui.Text(totalTimeText);
                        }
                        else
                        {
                            ImGui.Text("-");
                        }
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.Separator();

            if (ImGui.Button("Split", new Vector2(80, 30)))
            {
                speedrunManager.MarkSplit();
            }

            ImGui.SameLine();

            if (ImGui.Button("Stop", new Vector2(80, 30)))
            {
                speedrunManager.StopTimer();
            }

            ImGui.SameLine();

            if (ImGui.Button("Reset", new Vector2(80, 30)))
            {
                speedrunManager.ResetTimer();
            }

            ImGui.SameLine();

            if (ImGui.Button("Mini", new Vector2(80, 30)))
            {
                minimalMode = true;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Switch to minimal mode");
            }
        }

        private void DrawFinishedState(TimeSpan time, JumpPuzzleData puzzle)
        {
            ImGui.TextColored(new Vector4(0.0f, 0.8f, 0.2f, 1.0f), "Run Complete!");

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

            // Draw final splits
            var currentSplits = speedrunManager.GetCurrentSplits();

            if (currentSplits.Count > 0)
            {
                ImGui.Separator();
                ImGui.Text("Splits:");

                if (ImGui.BeginTable("CompletedSplitsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                {
                    ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupColumn("Split", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 80);
                    ImGui.TableHeadersRow();

                    foreach (var split in currentSplits.OrderBy(s => s.Order))
                    {
                        ImGui.TableNextRow();

                        // Name column
                        ImGui.TableNextColumn();
                        ImGui.Text(split.Name);

                        // Split time column
                        ImGui.TableNextColumn();
                        if (split.SplitTime.HasValue)
                        {
                            var splitTime = split.SplitTime.Value;
                            string splitTimeText = $"{(int)splitTime.TotalMinutes:D2}:{splitTime.Seconds:D2}.{splitTime.Milliseconds / 10:D2}";
                            ImGui.Text(splitTimeText);
                        }
                        else
                        {
                            ImGui.Text("-");
                        }

                        // Total time column
                        ImGui.TableNextColumn();
                        var totalTime = split.Time;
                        string totalTimeText = $"{(int)totalTime.TotalMinutes:D2}:{totalTime.Seconds:D2}.{totalTime.Milliseconds / 10:D2}";
                        ImGui.Text(totalTimeText);
                    }

                    ImGui.EndTable();
                }
            }

            ImGui.Separator();

            if (ImGui.Button("Create Template", new Vector2(120, 30)))
            {
                // Create a template from this run
                var currentRun = speedrunManager.GetRecords().LastOrDefault();
                if (currentRun != null)
                {
                    var template = currentRun.CreateTemplate();
                    speedrunManager.UpdateTemplate(template);
                    ImGui.OpenPopup("TemplateSaved");
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("New Run", new Vector2(90, 30)))
            {
                speedrunManager.ResetTimer();
            }

            // Template saved popup
            if (ImGui.BeginPopup("TemplateSaved"))
            {
                ImGui.Text("Template created successfully!");
                ImGui.Text("You can find it in the Templates tab.");
                ImGui.EndPopup();
            }
        }

        private void DrawSplitsEditor()
        {
            var currentTemplate = speedrunManager.GetCurrentTemplate();
            var currentPuzzle = speedrunManager.GetCurrentPuzzle();

            if (currentPuzzle == null)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f),
                    "No puzzle selected. Please select a puzzle first.");
                return;
            }

            ImGui.Text($"Editing splits for: {currentPuzzle.PuzzleName}");

            if (currentTemplate == null)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f),
                    "No template selected. Creating a default template.");

                if (ImGui.Button("Create Default Template"))
                {
                    // Create a simple template with just a finish split
                    var template = new SplitTemplate(currentPuzzle.PuzzleName + " Template");
                    template.PuzzleName = currentPuzzle.PuzzleName;
                    template.PuzzleId = currentPuzzle.Id >= 0 ? currentPuzzle.Id : null;
                    template.IsCustomPuzzle = currentPuzzle.Id < 0;

                    // Add a finish split
                    template.Splits.Add(new SplitCheckpoint("Finish", 0));

                    // Save and set as current
                    speedrunManager.UpdateTemplate(template);
                    speedrunManager.SetTemplate(template);
                }

                return;
            }

            ImGui.Text($"Template: {currentTemplate.Name}");

            // Template name edit
            string templateName = currentTemplate.Name;
            if (ImGui.InputText("Template Name", ref templateName, 100))
            {
                currentTemplate.Name = templateName;
                speedrunManager.UpdateTemplate(currentTemplate);
            }

            ImGui.Separator();

            // Splits editing
            ImGui.Text("Splits:");

            if (ImGui.BeginTable("SplitsEditorTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Order", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableHeadersRow();

                var orderedSplits = currentTemplate.Splits
                    .OrderBy(s => s.Order)
                    .ToList();

                for (int i = 0; i < orderedSplits.Count; i++)
                {
                    var split = orderedSplits[i];
                    string splitName = split.Name;
                    int order = split.Order;

                    ImGui.TableNextRow();

                    // Name column
                    ImGui.TableNextColumn();
                    ImGui.PushID($"split_{i}");
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

                    // Actions column
                    ImGui.TableNextColumn();

                    // Move up button
                    if (i > 0)
                    {
                        if (ImGui.Button("▲##up"))
                        {
                            // Swap with previous
                            SwapSplitOrders(currentTemplate, i - 1, i);
                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        ImGui.SameLine();
                    }

                    // Move down button
                    if (i < orderedSplits.Count - 1)
                    {
                        if (ImGui.Button("▼##down"))
                        {
                            // Swap with next
                            SwapSplitOrders(currentTemplate, i, i + 1);
                            speedrunManager.UpdateTemplate(currentTemplate);
                        }

                        ImGui.SameLine();
                    }

                    // Delete button
                    if (ImGui.Button("X##delete"))
                    {
                        currentTemplate.Splits.Remove(split);
                        speedrunManager.UpdateTemplate(currentTemplate);
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            // Add new split
            ImGui.Separator();
            ImGui.InputText("New Split Name", ref newSplitName, 100);

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

            ImGui.SameLine();

            if (ImGui.Button("Save Template"))
            {
                speedrunManager.UpdateTemplate(currentTemplate);
            }

            ImGui.SameLine();

            if (ImGui.Button("Duplicate Template"))
            {
                var duplicate = currentTemplate.Clone();
                speedrunManager.UpdateTemplate(duplicate);
                speedrunManager.SetTemplate(duplicate);
            }
        }

        private void SwapSplitOrders(SplitTemplate template, int index1, int index2)
        {
            var orderedSplits = template.Splits.OrderBy(s => s.Order).ToList();

            if (index1 >= 0 && index1 < orderedSplits.Count &&
                index2 >= 0 && index2 < orderedSplits.Count)
            {
                // Swap the order values of the two splits
                int tempOrder = orderedSplits[index1].Order;
                orderedSplits[index1].Order = orderedSplits[index2].Order;
                orderedSplits[index2].Order = tempOrder;
            }
        }

        private void DrawCustomPuzzlesManager()
        {
            ImGui.Text("Custom Puzzles");

            // Search for existing custom puzzles
            ImGui.InputTextWithHint("##search", "Search custom puzzles...", ref customPuzzleSearch, 100);

            ImGui.Separator();

            // Show list of custom puzzles
            if (ImGui.BeginTable("CustomPuzzlesTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Creator", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableHeadersRow();

                var filteredPuzzles = customPuzzles;
                if (!string.IsNullOrEmpty(customPuzzleSearch))
                {
                    string search = customPuzzleSearch.ToLower();
                    filteredPuzzles = customPuzzles
                        .Where(p => p.Name.ToLower().Contains(search) ||
                                    p.Creator.ToLower().Contains(search) ||
                                    p.Description.ToLower().Contains(search))
                        .ToList();
                }

                foreach (var puzzle in filteredPuzzles)
                {
                    ImGui.TableNextRow();

                    // Name column
                    ImGui.TableNextColumn();
                    ImGui.Text(puzzle.Name);

                    // Creator column
                    ImGui.TableNextColumn();
                    ImGui.Text(puzzle.Creator);

                    // Description column
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(puzzle.Description);

                    // Actions column
                    ImGui.TableNextColumn();
                    ImGui.PushID(puzzle.Id.ToString());

                    if (ImGui.Button("Select"))
                    {
                        speedrunManager.SetCustomPuzzle(puzzle);
                        ImGui.SetTabItemClosed("Custom Puzzles");
                        selectedTab = 0; // Switch to Timer tab
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Edit"))
                    {
                        // Edit this puzzle
                        isCustomPuzzleCreating = true;
                        newPuzzleName = puzzle.Name;
                        newPuzzleDesc = puzzle.Description;
                        newPuzzleCreator = puzzle.Creator;
                        ImGui.OpenPopup("EditCustomPuzzle");
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Delete"))
                    {
                        ImGui.OpenPopup("DeleteCustomPuzzle");
                    }

                    // Delete confirmation popup
                    if (ImGui.BeginPopup("DeleteCustomPuzzle"))
                    {
                        ImGui.Text($"Are you sure you want to delete '{puzzle.Name}'?");
                        ImGui.Text("This action cannot be undone.");

                        if (ImGui.Button("Yes, Delete"))
                        {
                            speedrunManager.RemoveCustomPuzzle(puzzle.Id);
                            RefreshData();
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Cancel"))
                        {
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.EndPopup();
                    }

                    // Edit puzzle popup
                    if (ImGui.BeginPopup("EditCustomPuzzle"))
                    {
                        ImGui.Text("Edit Custom Puzzle");
                        ImGui.Separator();

                        ImGui.InputText("Name", ref newPuzzleName, 100);
                        ImGui.InputText("Creator", ref newPuzzleCreator, 100);
                        ImGui.InputTextMultiline("Description", ref newPuzzleDesc, 1000, new Vector2(0, 100));

                        if (ImGui.Button("Save Changes"))
                        {
                            // Update the puzzle
                            puzzle.Name = newPuzzleName;
                            puzzle.Creator = newPuzzleCreator;
                            puzzle.Description = newPuzzleDesc;
                            puzzle.LastModified = DateTime.Now;

                            speedrunManager.UpdateCustomPuzzle(puzzle);
                            RefreshData();
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Cancel"))
                        {
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.Separator();

            if (ImGui.Button("Create New Custom Puzzle"))
            {
                isCustomPuzzleCreating = true;
                newPuzzleName = "";
                newPuzzleDesc = "";
                newPuzzleCreator = "";
                ImGui.OpenPopup("CreateCustomPuzzle");
            }
        }

        private void DrawTemplatesManager()
        {
            ImGui.Text("Split Templates");

            // Search for templates
            ImGui.InputTextWithHint("##searchTemplate", "Search templates...", ref templateSearch, 100);

            ImGui.Separator();

            // Show list of templates
            if (ImGui.BeginTable("TemplatesTable", 4, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Puzzle", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Splits", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 210);
                ImGui.TableHeadersRow();

                var filteredTemplates = templates;
                if (!string.IsNullOrEmpty(templateSearch))
                {
                    string search = templateSearch.ToLower();
                    filteredTemplates = templates
                        .Where(t => t.Name.ToLower().Contains(search) ||
                                    t.PuzzleName?.ToLower().Contains(search) == true)
                        .ToList();
                }

                foreach (var template in filteredTemplates)
                {
                    ImGui.TableNextRow();

                    // Name column
                    ImGui.TableNextColumn();
                    ImGui.Text(template.Name);

                    // Puzzle column
                    ImGui.TableNextColumn();
                    ImGui.Text(template.PuzzleName ?? "Generic");

                    // Splits count column
                    ImGui.TableNextColumn();
                    ImGui.Text(template.Splits.Count.ToString());

                    // Actions column
                    ImGui.TableNextColumn();
                    ImGui.PushID(template.Id.ToString());

                    if (ImGui.Button("Select"))
                    {
                        speedrunManager.SetTemplate(template);
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Edit"))
                    {
                        speedrunManager.SetTemplate(template);
                        ImGui.SetTabItemClosed("Templates");
                        selectedTab = 1; // Switch to Splits editor tab
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Duplicate"))
                    {
                        var duplicate = speedrunManager.DuplicateTemplate(template.Id);
                        if (duplicate != null)
                        {
                            RefreshData();
                        }
                    }

                    ImGui.SameLine();

                    if (ImGui.Button("Delete"))
                    {
                        ImGui.OpenPopup("DeleteTemplate");
                    }

                    // Delete confirmation popup
                    if (ImGui.BeginPopup("DeleteTemplate"))
                    {
                        ImGui.Text($"Are you sure you want to delete template '{template.Name}'?");

                        if (ImGui.Button("Yes, Delete"))
                        {
                            speedrunManager.RemoveTemplate(template.Id);
                            RefreshData();
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Cancel"))
                        {
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.EndPopup();
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();
            }

            ImGui.Separator();

            if (ImGui.Button("Create New Template"))
            {
                string newName = "New Template " + DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                var template = speedrunManager.CreateTemplate(newName);

                // Add a default finish split
                template.Splits.Add(new SplitCheckpoint("Finish", 0));
                speedrunManager.UpdateTemplate(template);

                RefreshData();
            }
        }

        private void DrawSettingsTab()
        {
            ImGui.Text("Speedrun Settings");

            ImGui.Separator();

            // Countdown setting
            var countdown = speedrunManager.DefaultCountdown;
            if (ImGui.SliderInt("Countdown Seconds", ref countdown, 0, 10))
            {
                speedrunManager.DefaultCountdown = countdown;
            }

            // Auto-save setting
            var autoSave = speedrunManager.AutoSaveRecords;
            if (ImGui.Checkbox("Auto-Save Records", ref autoSave))
            {
                speedrunManager.AutoSaveRecords = autoSave;
            }

            // Play sound setting
            var playSound = speedrunManager.PlaySoundOnCountdown;
            if (ImGui.Checkbox("Play Sound on Countdown", ref playSound))
            {
                speedrunManager.PlaySoundOnCountdown = playSound;
            }

            // Show split comparison
            var showComparison = speedrunManager.ShowSplitComparison;
            if (ImGui.Checkbox("Show Split Comparison", ref showComparison))
            {
                speedrunManager.ShowSplitComparison = showComparison;
            }

            // Window settings
            ImGui.Separator();
            ImGui.Text("Window Settings");

            bool isMovable = true; // Window is always movable now
            ImGui.BeginDisabled();
            ImGui.Checkbox("Movable Window", ref isMovable);
            ImGui.EndDisabled();

            ImGui.Checkbox("Minimal Mode During Run", ref minimalMode);

            if (ImGui.CollapsingHeader("Visual Settings"))
            {
                // Fixed Vector4 to Vector3 conversions for ColorEdit3
                Vector3 completedColor = new Vector3(splitCompletedColor.X, splitCompletedColor.Y, splitCompletedColor.Z);
                if (ImGui.ColorEdit3("Completed Split Color", ref completedColor))
                {
                    splitCompletedColor = new Vector4(completedColor.X, completedColor.Y, completedColor.Z, splitCompletedColor.W);
                }

                Vector3 pendingColor = new Vector3(splitPendingColor.X, splitPendingColor.Y, splitPendingColor.Z);
                if (ImGui.ColorEdit3("Pending Split Color", ref pendingColor))
                {
                    splitPendingColor = new Vector4(pendingColor.X, pendingColor.Y, pendingColor.Z, splitPendingColor.W);
                }

                Vector3 aheadColor = new Vector3(splitAheadColor.X, splitAheadColor.Y, splitAheadColor.Z);
                if (ImGui.ColorEdit3("Ahead Time Color", ref aheadColor))
                {
                    splitAheadColor = new Vector4(aheadColor.X, aheadColor.Y, aheadColor.Z, splitAheadColor.W);
                }

                Vector3 behindColor = new Vector3(splitBehindColor.X, splitBehindColor.Y, splitBehindColor.Z);
                if (ImGui.ColorEdit3("Behind Time Color", ref behindColor))
                {
                    splitBehindColor = new Vector4(behindColor.X, behindColor.Y, behindColor.Z, splitBehindColor.W);
                }
            }
        }

        private void DrawPopups()
        {
            // Custom puzzle creation popup
            if (isCustomPuzzleCreating)
            {
                ImGui.OpenPopup("CreateCustomPuzzle");
                isCustomPuzzleCreating = false;
            }

            if (ImGui.BeginPopup("CreateCustomPuzzle"))
            {
                ImGui.Text("Create Custom Puzzle");
                ImGui.Separator();

                ImGui.InputText("Puzzle Name", ref newPuzzleName, 100);
                ImGui.InputText("Creator Name", ref newPuzzleCreator, 100);
                ImGui.InputTextMultiline("Description", ref newPuzzleDesc, 1000, new Vector2(0, 100));

                if (ImGui.Button("Create") && !string.IsNullOrEmpty(newPuzzleName))
                {
                    var puzzle = speedrunManager.CreateCustomPuzzle(
                        newPuzzleName, newPuzzleDesc, newPuzzleCreator);

                    speedrunManager.SetCustomPuzzle(puzzle);

                    RefreshData();
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            // Template selection popup
            if (isTemplateSelecting)
            {
                ImGui.OpenPopup("SelectTemplate");
                isTemplateSelecting = false;
            }

            // Use larger popup size
            ImGui.SetNextWindowSize(new Vector2(500, 400), ImGuiCond.Appearing);

            if (ImGui.BeginPopup("SelectTemplate"))
            {
                ImGui.Text("Select Template");
                ImGui.Separator();

                // Filter by puzzle if we have one selected
                var currentPuzzle = speedrunManager.GetCurrentPuzzle();
                List<SplitTemplate> availableTemplates = templates;

                if (currentPuzzle != null)
                {
                    if (currentPuzzle.Id >= 0)
                    {
                        // Standard puzzle
                        var puzzleTemplates = templates.Where(t =>
                            t.PuzzleId == currentPuzzle.Id || t.PuzzleId == null).ToList();

                        if (puzzleTemplates.Any())
                        {
                            availableTemplates = puzzleTemplates;
                        }
                    }
                    else
                    {
                        // Custom puzzle
                        var customTemplates = templates.Where(t =>
                            t.IsCustomPuzzle && t.PuzzleName == currentPuzzle.PuzzleName || t.PuzzleId == null).ToList();

                        if (customTemplates.Any())
                        {
                            availableTemplates = customTemplates;
                        }
                    }
                }

                // Search field
                ImGui.InputTextWithHint("##templateSearchPopup", "Search templates...", ref templateSearch, 100);

                // Filter by search
                if (!string.IsNullOrEmpty(templateSearch))
                {
                    string search = templateSearch.ToLower();
                    availableTemplates = availableTemplates
                        .Where(t => t.Name.ToLower().Contains(search) ||
                                    t.PuzzleName?.ToLower().Contains(search) == true)
                        .ToList();
                }

                ImGui.BeginChild("TemplatesList", new Vector2(0, 300), true);

                // No templates message
                if (availableTemplates.Count == 0)
                {
                    ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f),
                        "No templates found. Create one in the Templates tab.");
                }

                // List templates
                foreach (var template in availableTemplates)
                {
                    ImGui.PushID(template.Id.ToString());

                    if (ImGui.Selectable(template.Name, false, ImGuiSelectableFlags.AllowDoubleClick))
                    {
                        // Updated: Use ImGuiMouseButton enum instead of integer
                        if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            // Double-click to select and close
                            speedrunManager.SetTemplate(template);
                            ImGui.CloseCurrentPopup();
                        }
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        ImGui.Text($"Puzzle: {template.PuzzleName ?? "Generic"}");
                        ImGui.Text($"Splits: {template.Splits.Count}");

                        if (template.Splits.Count > 0)
                        {
                            ImGui.Separator();
                            ImGui.Text("Splits:");
                            foreach (var split in template.Splits.OrderBy(s => s.Order))
                            {
                                ImGui.BulletText(split.Name);
                            }
                        }

                        ImGui.EndTooltip();
                    }

                    ImGui.PopID();
                }

                ImGui.EndChild();

                if (ImGui.Button("Select") && ImGui.IsItemActive())
                {
                    // Find the currently hovered template
                    foreach (var template in availableTemplates)
                    {
                        if (ImGui.IsItemHovered())
                        {
                            speedrunManager.SetTemplate(template);
                            ImGui.CloseCurrentPopup();
                            break;
                        }
                    }
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
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
            speedrunManager.SplitCompleted -= OnSplitCompleted;
            speedrunManager.TemplateChanged -= OnTemplateChanged;
            speedrunManager.CustomPuzzleAdded -= OnCustomPuzzleAdded;
        }
    }
}
