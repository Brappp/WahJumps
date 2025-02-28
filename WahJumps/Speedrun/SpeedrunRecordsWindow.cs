// File: WahJumps/Windows/SpeedrunRecordsWindow.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using WahJumps.Data;

namespace WahJumps.Windows
{
    public class SpeedrunRecordsWindow : Window, IDisposable
    {
        private readonly SpeedrunManager speedrunManager;
        private string filterText = string.Empty;
        private int sortColumn = 3; // Default sort by time
        private bool sortAscending = true;
        private bool showSplits = true;
        private bool groupByPuzzle = true;
        private int selectedPuzzleId = -1;
        private Guid selectedCustomPuzzleId = Guid.Empty;
        private Guid selectedRecordId = Guid.Empty;
        private bool showCustomPuzzles = true;
        private bool showLoadTemplateButton = true;

        public SpeedrunRecordsWindow(SpeedrunManager speedrunManager)
            : base("Speedrun Records", ImGuiWindowFlags.None)
        {
            this.speedrunManager = speedrunManager;

            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(600, 400),
                MaximumSize = new Vector2(1200, 800)
            };

            speedrunManager.RunCompleted += OnRunCompleted;
        }

        public override void Draw()
        {
            // Top toolbar
            DrawToolbar();

            ImGui.Separator();

            // Main content
            if (groupByPuzzle)
            {
                DrawGroupedRecords();
            }
            else
            {
                DrawFlatRecords();
            }

            // Draw selected record details if applicable
            if (selectedRecordId != Guid.Empty)
            {
                DrawSelectedRecordDetails();
            }

            // Handle popup modals
            DrawPopups();
        }

        private void DrawToolbar()
        {
            // Filter
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X * 0.3f);
            ImGui.InputTextWithHint("##filter", "Filter by name or world...", ref filterText, 100);

            ImGui.SameLine();

            if (ImGui.Button("Refresh"))
            {
                speedrunManager.LoadRecords();
            }

            ImGui.SameLine();

            if (ImGui.Button("Export CSV"))
            {
                speedrunManager.SaveRecords();
                ImGui.OpenPopup("ExportSuccess");
            }

            ImGui.SameLine();

            // View options
            ImGui.Checkbox("Group by Puzzle", ref groupByPuzzle);

            ImGui.SameLine();

            ImGui.Checkbox("Show Splits", ref showSplits);

            ImGui.SameLine();

            ImGui.Checkbox("Show Custom", ref showCustomPuzzles);

            ImGui.SameLine();

            if (ImGui.Button("Reset Filters"))
            {
                filterText = "";
                selectedPuzzleId = -1;
                selectedCustomPuzzleId = Guid.Empty;
                selectedRecordId = Guid.Empty;
            }

            // Export success popup
            if (ImGui.BeginPopup("ExportSuccess"))
            {
                ImGui.Text($"Records exported to: {Path.GetFileName(speedrunManager.RecordsPath)}");
                ImGui.EndPopup();
            }
        }

        private void DrawGroupedRecords()
        {
            // Get all records
            var records = speedrunManager.GetRecords();

            // Filter records
            if (!string.IsNullOrEmpty(filterText))
            {
                string filter = filterText.ToLower();
                records = records.Where(r =>
                    r.PuzzleName.ToLower().Contains(filter) ||
                    r.World.ToLower().Contains(filter)
                ).ToList();
            }

            // Filter out custom puzzles if not showing them
            if (!showCustomPuzzles)
            {
                records = records.Where(r => !r.IsCustomPuzzle).ToList();
            }

            // Group by puzzle
            var groupedRecords = records
                .GroupBy(r => r.IsCustomPuzzle ? "Custom: " + r.PuzzleName : r.PuzzleName)
                .OrderBy(g => g.Key)
                .ToList();

            if (ImGui.BeginTabBar("PuzzleTabs"))
            {
                foreach (var group in groupedRecords)
                {
                    if (ImGui.BeginTabItem(group.Key))
                    {
                        // Get the first record in the group to check if it's custom
                        var firstRecord = group.First();
                        bool isCustom = firstRecord.IsCustomPuzzle;

                        // Sort records within this group
                        var puzzleRecords = group.ToList();
                        puzzleRecords = SortRecords(puzzleRecords, sortColumn, sortAscending);

                        // Set the selected puzzle ID
                        if (isCustom)
                        {
                            selectedPuzzleId = -1;
                            selectedCustomPuzzleId = firstRecord.CustomPuzzleId;
                        }
                        else
                        {
                            selectedPuzzleId = firstRecord.PuzzleId;
                            selectedCustomPuzzleId = Guid.Empty;
                        }

                        // Draw the records table for this puzzle
                        DrawRecordsTable(puzzleRecords);

                        ImGui.EndTabItem();
                    }
                }

                if (ImGui.BeginTabItem("All Records"))
                {
                    selectedPuzzleId = -1;
                    selectedCustomPuzzleId = Guid.Empty;

                    // Sort all records
                    records = SortRecords(records, sortColumn, sortAscending);

                    // Draw records table with all records
                    DrawRecordsTable(records);

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        private void DrawFlatRecords()
        {
            var records = speedrunManager.GetRecords();

            // Filter records
            if (!string.IsNullOrEmpty(filterText))
            {
                string filter = filterText.ToLower();
                records = records.Where(r =>
                    r.PuzzleName.ToLower().Contains(filter) ||
                    r.World.ToLower().Contains(filter)
                ).ToList();
            }

            // Filter out custom puzzles if not showing them
            if (!showCustomPuzzles)
            {
                records = records.Where(r => !r.IsCustomPuzzle).ToList();
            }

            // Filter by selected puzzle if applicable
            if (selectedPuzzleId > 0)
            {
                records = records.Where(r => r.PuzzleId == selectedPuzzleId && !r.IsCustomPuzzle).ToList();
            }
            else if (selectedCustomPuzzleId != Guid.Empty)
            {
                records = records.Where(r => r.IsCustomPuzzle && r.CustomPuzzleId == selectedCustomPuzzleId).ToList();
            }

            // Sort records
            records = SortRecords(records, sortColumn, sortAscending);

            // Draw records table
            DrawRecordsTable(records);
        }

        private void DrawRecordsTable(List<SpeedrunRecord> records)
        {
            int columns = showSplits ? 7 : 6;

            if (ImGui.BeginTable("RecordsTable", columns,
                ImGuiTableFlags.Resizable |
                ImGuiTableFlags.Sortable |
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.ScrollY))
            {
                // Setup columns
                ImGui.TableSetupColumn("Date", ImGuiTableColumnFlags.DefaultSort);
                ImGui.TableSetupColumn("Puzzle Name", ImGuiTableColumnFlags.None);
                ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.None);
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.DefaultSort);
                ImGui.TableSetupColumn("Custom Fields", ImGuiTableColumnFlags.None);

                if (showSplits)
                {
                    ImGui.TableSetupColumn("Splits", ImGuiTableColumnFlags.None);
                }

                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.NoSort);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                // Handle sorting when clicking headers
                ImGuiTableSortSpecsPtr sortSpecs = ImGui.TableGetSortSpecs();
                if (sortSpecs.SpecsDirty)
                {
                    if (sortSpecs.SpecsCount > 0)
                    {
                        var sorts = sortSpecs.Specs;
                        sortColumn = sorts.ColumnIndex;
                        sortAscending = sorts.SortDirection == ImGuiSortDirection.Ascending;
                    }
                    sortSpecs.SpecsDirty = false;
                }

                // Draw rows
                for (int i = 0; i < records.Count; i++)
                {
                    var record = records[i];
                    ImGui.TableNextRow();

                    // Style the row differently if it's selected
                    bool isSelected = record.Id == selectedRecordId;
                    if (isSelected)
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.3f, 0.5f, 0.7f, 0.4f)));
                    }

                    // Date column
                    ImGui.TableNextColumn();
                    ImGui.Text(record.Date.ToString("yyyy-MM-dd HH:mm"));

                    // Puzzle Name column
                    ImGui.TableNextColumn();
                    if (record.IsCustomPuzzle)
                    {
                        ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.3f, 1.0f), record.PuzzleName);
                    }
                    else
                    {
                        ImGui.Text(record.PuzzleName);
                    }

                    // World column
                    ImGui.TableNextColumn();
                    ImGui.Text(record.World);

                    // Time column
                    ImGui.TableNextColumn();
                    string timeText = $"{(int)record.Time.TotalMinutes:D2}:{record.Time.Seconds:D2}.{record.Time.Milliseconds / 10:D2}";
                    ImGui.Text(timeText);

                    // Custom Fields column
                    ImGui.TableNextColumn();
                    if (record.CustomFields.Count > 0)
                    {
                        string fields = string.Join(", ", record.CustomFields.Select(f => $"{f.Key}: {f.Value}"));
                        ImGui.TextWrapped(fields);
                    }
                    else
                    {
                        ImGui.Text("-");
                    }

                    // Splits column
                    if (showSplits)
                    {
                        ImGui.TableNextColumn();
                        if (record.Splits.Count > 0)
                        {
                            if (ImGui.Button($"Show Splits##{i}"))
                            {
                                selectedRecordId = record.Id;
                            }

                            ImGui.SameLine();
                            ImGui.Text($"({record.Splits.Count})");
                        }
                        else
                        {
                            ImGui.Text("-");
                        }
                    }

                    // Actions column
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Details##{i}"))
                    {
                        selectedRecordId = record.Id;
                    }

                    ImGui.SameLine();

                    if (showLoadTemplateButton && record.Splits.Count > 0)
                    {
                        if (ImGui.Button($"Template##{i}"))
                        {
                            // Create a template from this record
                            var template = record.CreateTemplate();
                            speedrunManager.UpdateTemplate(template);

                            // Set this as the current template
                            speedrunManager.SetTemplate(template);

                            ImGui.OpenPopup("TemplateCreated");
                        }

                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip("Create template from this record");
                        }

                        ImGui.SameLine();
                    }

                    if (ImGui.Button($"Delete##{i}"))
                    {
                        ImGui.OpenPopup($"Delete Record##{i}");
                    }

                    // Delete confirmation popup
                    if (ImGui.BeginPopup($"Delete Record##{i}"))
                    {
                        ImGui.Text("Are you sure you want to delete this record?");
                        ImGui.Separator();

                        if (ImGui.Button("Yes", new Vector2(80, 0)))
                        {
                            speedrunManager.RemoveRecord(record.Id);

                            if (selectedRecordId == record.Id)
                            {
                                selectedRecordId = Guid.Empty;
                            }

                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("No", new Vector2(80, 0)))
                        {
                            ImGui.CloseCurrentPopup();
                        }

                        ImGui.EndPopup();
                    }
                }

                ImGui.EndTable();
            }
        }

        private void DrawSelectedRecordDetails()
        {
            var record = speedrunManager.GetRecords().FirstOrDefault(r => r.Id == selectedRecordId);

            if (record == null)
            {
                selectedRecordId = Guid.Empty;
                return;
            }

            ImGui.Separator();

            // Collapsing header for record details
            if (ImGui.CollapsingHeader($"Record Details - {record.PuzzleName} ({FormatTime(record.Time)})", ImGuiTreeNodeFlags.DefaultOpen))
            {
                // Create two columns: general info and splits
                float availWidth = ImGui.GetContentRegionAvail().X;
                ImGui.BeginChild("DetailsLeft", new Vector2(availWidth * 0.4f, 0), false);

                // General record info
                ImGui.Text("Date: " + record.Date.ToString("yyyy-MM-dd HH:mm:ss"));
                ImGui.Text("Puzzle: " + record.PuzzleName);
                ImGui.Text("World: " + record.World);
                ImGui.Text("Time: " + FormatTime(record.Time));

                if (record.IsCustomPuzzle)
                {
                    ImGui.TextColored(new Vector4(0.3f, 0.8f, 0.3f, 1.0f), "Custom Puzzle");
                }

                // Custom fields if any
                if (record.CustomFields.Count > 0)
                {
                    ImGui.Separator();
                    ImGui.Text("Custom Fields:");

                    foreach (var field in record.CustomFields)
                    {
                        ImGui.BulletText($"{field.Key}: {field.Value}");
                    }
                }

                ImGui.EndChild();

                ImGui.SameLine();

                // Splits table
                ImGui.BeginChild("DetailsRight", new Vector2(0, 0), false);

                if (record.Splits.Count > 0)
                {
                    ImGui.Text("Splits:");

                    if (ImGui.BeginTable("SplitsDetailTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
                    {
                        ImGui.TableSetupColumn("Split Name", ImGuiTableColumnFlags.WidthStretch);
                        ImGui.TableSetupColumn("Split Time", ImGuiTableColumnFlags.WidthFixed, 100);
                        ImGui.TableSetupColumn("Overall Time", ImGuiTableColumnFlags.WidthFixed, 100);
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

                            // Overall time
                            ImGui.TableNextColumn();
                            ImGui.Text(FormatTime(split.Time));
                        }

                        ImGui.EndTable();
                    }

                    // Create template button
                    if (ImGui.Button("Create Template from These Splits"))
                    {
                        var template = record.CreateTemplate($"{record.PuzzleName} Template ({FormatTime(record.Time)})");
                        speedrunManager.UpdateTemplate(template);
                        speedrunManager.SetTemplate(template);
                        ImGui.OpenPopup("TemplateCreated");
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.0f, 1.0f), "No splits recorded for this run.");
                }

                ImGui.EndChild();
            }

            // Close button
            if (ImGui.Button("Close Details"))
            {
                selectedRecordId = Guid.Empty;
            }
        }

        private void DrawPopups()
        {
            // Template created popup
            if (ImGui.BeginPopup("TemplateCreated"))
            {
                ImGui.Text("Template created successfully!");
                ImGui.Text("It has been set as the current template for speedruns.");
                ImGui.EndPopup();
            }
        }

        private List<SpeedrunRecord> SortRecords(List<SpeedrunRecord> records, int column, bool ascending)
        {
            // Order records based on column index
            switch (column)
            {
                case 0: // Date
                    records = ascending ?
                        records.OrderBy(r => r.Date).ToList() :
                        records.OrderByDescending(r => r.Date).ToList();
                    break;
                case 1: // Puzzle Name
                    records = ascending ?
                        records.OrderBy(r => r.PuzzleName).ToList() :
                        records.OrderByDescending(r => r.PuzzleName).ToList();
                    break;
                case 2: // World
                    records = ascending ?
                        records.OrderBy(r => r.World).ToList() :
                        records.OrderByDescending(r => r.World).ToList();
                    break;
                case 3: // Time
                    records = ascending ?
                        records.OrderBy(r => r.Time).ToList() :
                        records.OrderByDescending(r => r.Time).ToList();
                    break;
            }

            return records;
        }

        private void OnRunCompleted(SpeedrunRecord record)
        {
            // Automatically select the most recent record
            selectedRecordId = record.Id;
        }

        private string FormatTime(TimeSpan time)
        {
            return $"{(int)time.TotalMinutes:D2}:{time.Seconds:D2}.{time.Milliseconds / 10:D2}";
        }

        public void Dispose()
        {
            speedrunManager.RunCompleted -= OnRunCompleted;
        }
    }
}
