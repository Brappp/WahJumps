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

            // Export success popup
            if (ImGui.BeginPopup("ExportSuccess"))
            {
                ImGui.Text($"Records exported to: {Path.GetFileName(speedrunManager.RecordsPath)}");
                ImGui.EndPopup();
            }

            ImGui.Separator();

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

            // Sort records
            records = SortRecords(records, sortColumn, sortAscending);

            // Draw records table
            DrawRecordsTable(records);
        }

        private void DrawRecordsTable(List<SpeedrunRecord> records)
        {
            if (ImGui.BeginTable("RecordsTable", 6,
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

                    // Date column
                    ImGui.TableNextColumn();
                    ImGui.Text(record.Date.ToString("yyyy-MM-dd HH:mm"));

                    // Puzzle Name column
                    ImGui.TableNextColumn();
                    ImGui.Text(record.PuzzleName);

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

                    // Actions column
                    ImGui.TableNextColumn();
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
            // Could add notifications or animations when a run completes
        }

        public void Dispose()
        {
            speedrunManager.RunCompleted -= OnRunCompleted;
        }
    }
}
