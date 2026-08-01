using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using WahJumps.Models;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class OverviewTab
    {
        private readonly Func<Dictionary<string, List<JumpPuzzleData>>> getData;
        private readonly Func<int> getTotal;
        private readonly Func<string, string> regionForDataCenter;

        public OverviewTab(
            Func<Dictionary<string, List<JumpPuzzleData>>> getData,
            Func<int> getTotal,
            Func<string, string> regionForDataCenter)
        {
            this.getData = getData;
            this.getTotal = getTotal;
            this.regionForDataCenter = regionForDataCenter;
        }

        public void Draw()
        {
            var data = getData();
            if (data.Count == 0)
            {
                UiTheme.CenteredText("No data loaded yet. Please wait for data to load or refresh.");
                return;
            }

            int total = getTotal();
            var sortedDCs = data.OrderByDescending(dc => dc.Value.Count).ToList();

            DrawSummaryStatistics(data, total);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            using var child = new ImRaii.Child("DCOverviewScrollArea", new Vector2(0, 0), true);

            UiTheme.StyleTable();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                    ImGuiTableFlags.BordersInnerH |
                                    ImGuiTableFlags.Resizable |
                                    ImGuiTableFlags.SizingFixedFit |
                                    ImGuiTableFlags.Sortable;

            if (ImGui.BeginTable("DCComparison", 11, flags))
            {
                ImGui.TableSetupColumn("Region");
                ImGui.TableSetupColumn("Data Center");
                ImGui.TableSetupColumn("Total");
                ImGui.TableSetupColumn("Worlds");
                ImGui.TableSetupColumn("★★★★★");
                ImGui.TableSetupColumn("★★★★");
                ImGui.TableSetupColumn("★★★");
                ImGui.TableSetupColumn("★★");
                ImGui.TableSetupColumn("★");
                ImGui.TableSetupColumn("Special");
                ImGui.TableSetupColumn("Distribution", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableHeadersRow();

                foreach (var dc in sortedDCs)
                {
                    var ratings = dc.Value.GroupBy(p => p.Rating)
                        .ToDictionary(g => g.Key, g => g.Count());

                    var percentage = (float)dc.Value.Count / total * 100f;
                    var worldCount = dc.Value.Select(p => p.World).Distinct().Count();
                    var specialCount = GetSpecialPuzzleCount(dc.Value);

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(regionForDataCenter(dc.Key));

                    ImGui.TableNextColumn();
                    ImGui.Text(dc.Key);

                    ImGui.TableNextColumn();
                    ImGui.Text(dc.Value.Count.ToString());

                    ImGui.TableNextColumn();
                    ImGui.Text(worldCount.ToString());

                    foreach (string ratingKey in new[] { "★★★★★", "★★★★", "★★★", "★★", "★" })
                    {
                        ImGui.TableNextColumn();
                        int ratingCount = ratings.GetValueOrDefault(ratingKey, 0);
                        ImGui.Text(ratingCount > 0 ? ratingCount.ToString() : "-");
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(specialCount.ToString());
                    if (ImGui.IsItemHovered())
                    {
                        using var tooltip = new ImRaii.Tooltip();
                        DrawSpecialPuzzleBreakdown(dc.Value);
                    }

                    ImGui.TableNextColumn();
                    DrawMiniBarChartWithPercentage(dc.Value.Count, percentage);
                }

                ImGui.EndTable();
            }

            UiTheme.EndTableStyle();

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Top 5 Builders by Data Center"))
            {
                DrawTopBuildersTable(sortedDCs);
            }
        }

        private void DrawSummaryStatistics(Dictionary<string, List<JumpPuzzleData>> data, int total)
        {
            ImGui.TextColored(UiTheme.Primary, $"Summary: {total:N0} total puzzles across {data.Count} data centers");

            var regionTotals = new Dictionary<string, int>();
            foreach (var dc in data)
            {
                var region = regionForDataCenter(dc.Key);
                regionTotals[region] = regionTotals.GetValueOrDefault(region, 0) + dc.Value.Count;
            }

            string regionSummary = string.Join("  |  ", regionTotals.OrderByDescending(r => r.Value).Select(r => $"{r.Key}: {r.Value}"));
            ImGui.Text($"By Region: {regionSummary}");

            var allPuzzles = data.Values.SelectMany(v => v).ToList();

            var uniqueBuilders = allPuzzles
                .Where(p => !string.IsNullOrEmpty(p.Builder))
                .Select(p => p.Builder)
                .Distinct()
                .Count();

            var uniqueWorlds = allPuzzles.Select(p => p.World).Distinct().Count();
            var globalAvgDiff = CalculateAverageDifficulty(allPuzzles);

            ImGui.Text($"Unique Builders: {uniqueBuilders:N0} | Unique Worlds: {uniqueWorlds:N0} | Global Avg Difficulty: {globalAvgDiff:F1}★");
        }

        private static void DrawMiniBarChartWithPercentage(int totalForDC, float percentage)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            float barWidth = ImGui.GetColumnWidth() - 10;
            float barHeight = 18;

            drawList.AddRectFilled(
                pos,
                new Vector2(pos.X + barWidth, pos.Y + barHeight),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)),
                2.0f
            );

            float fillWidth = barWidth * (percentage / 100f);
            if (fillWidth > 0)
            {
                Vector4 fillColor = UiTheme.GetSizeBucketColor(totalForDC) with { W = 0.8f };
                drawList.AddRectFilled(
                    pos,
                    new Vector2(pos.X + fillWidth, pos.Y + barHeight),
                    ImGui.GetColorU32(fillColor),
                    2.0f
                );
            }

            string text = $"{percentage:F1}%";
            var textSize = ImGui.CalcTextSize(text);
            if (textSize.X < barWidth)
            {
                drawList.AddText(
                    new Vector2(pos.X + (barWidth - textSize.X) * 0.5f, pos.Y + (barHeight - textSize.Y) * 0.5f),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 1)),
                    text
                );
            }

            ImGui.Dummy(new Vector2(barWidth, barHeight));
        }

        private void DrawTopBuildersTable(List<KeyValuePair<string, List<JumpPuzzleData>>> sortedDCs)
        {
            UiTheme.StyleTable();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                    ImGuiTableFlags.BordersInnerH |
                                    ImGuiTableFlags.Resizable |
                                    ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("TopBuilders", 8, flags))
            {
                ImGui.TableSetupColumn("Region");
                ImGui.TableSetupColumn("Data Center");
                ImGui.TableSetupColumn("1st Place");
                ImGui.TableSetupColumn("2nd Place");
                ImGui.TableSetupColumn("3rd Place");
                ImGui.TableSetupColumn("4th Place");
                ImGui.TableSetupColumn("5th Place");
                ImGui.TableSetupColumn("Total Builders", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableHeadersRow();

                foreach (var dc in sortedDCs)
                {
                    var builderStats = dc.Value
                        .Where(p => !string.IsNullOrEmpty(p.Builder))
                        .GroupBy(p => p.Builder)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToList();

                    var totalBuilders = dc.Value
                        .Where(p => !string.IsNullOrEmpty(p.Builder))
                        .Select(p => p.Builder)
                        .Distinct()
                        .Count();

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(regionForDataCenter(dc.Key));

                    ImGui.TableNextColumn();
                    ImGui.Text(dc.Key);

                    for (int i = 0; i < 5; i++)
                    {
                        ImGui.TableNextColumn();
                        if (i < builderStats.Count)
                        {
                            var builder = builderStats[i];
                            var name = builder.Key;
                            var count = builder.Count();

                            if (name.Length > 15)
                                name = name.Substring(0, 12) + "...";

                            ImGui.Text($"{name} ({count})");

                            if (ImGui.IsItemHovered())
                            {
                                using var tooltip = new ImRaii.Tooltip();
                                ImGui.Text($"Builder: {builder.Key}");
                                ImGui.Text($"Puzzles: {count}");
                                ImGui.Text($"Avg Difficulty: {CalculateAverageDifficulty(builder.ToList()):F1}★");
                            }
                        }
                        else
                        {
                            ImGui.Text("-");
                        }
                    }

                    ImGui.TableNextColumn();
                    ImGui.Text(totalBuilders.ToString());
                }

                ImGui.EndTable();
            }

            UiTheme.EndTableStyle();
        }

        private static float CalculateAverageDifficulty(List<JumpPuzzleData> puzzles)
        {
            if (puzzles.Count == 0) return 0f;

            float totalDifficulty = 0f;
            int validRatings = 0;

            foreach (var puzzle in puzzles)
            {
                int difficulty = UiHelpers.ConvertRatingToInt(puzzle.Rating);
                if (difficulty > 0)
                {
                    totalDifficulty += difficulty;
                    validRatings++;
                }
            }

            return validRatings > 0 ? totalDifficulty / validRatings : 0f;
        }

        private static bool IsSpecialRating(string rating) =>
            rating.StartsWith("Event", StringComparison.Ordinal) ||
            rating.StartsWith("Temp", StringComparison.Ordinal) ||
            rating.StartsWith("In Flux", StringComparison.Ordinal) ||
            rating.StartsWith("Training", StringComparison.Ordinal);

        private static int GetSpecialPuzzleCount(List<JumpPuzzleData> puzzles)
        {
            return puzzles.Count(p =>
                IsSpecialRating(p.Rating) ||
                !string.IsNullOrEmpty(p.M) || !string.IsNullOrEmpty(p.E) ||
                !string.IsNullOrEmpty(p.S) || !string.IsNullOrEmpty(p.P) ||
                !string.IsNullOrEmpty(p.V) || !string.IsNullOrEmpty(p.J) ||
                !string.IsNullOrEmpty(p.G) || !string.IsNullOrEmpty(p.L) ||
                !string.IsNullOrEmpty(p.X));
        }

        private static void DrawSpecialPuzzleBreakdown(List<JumpPuzzleData> puzzles)
        {
            ImGui.Text("Special Puzzle Types:");
            ImGui.Separator();

            var specialTypes = new Dictionary<string, int>
            {
                ["Event (E)"] = puzzles.Count(p => p.Rating.StartsWith("Event", StringComparison.Ordinal)),
                ["Temp (T)"] = puzzles.Count(p => p.Rating.StartsWith("Temp", StringComparison.Ordinal)),
                ["In Flux (F)"] = puzzles.Count(p => p.Rating.StartsWith("In Flux", StringComparison.Ordinal)),
                ["Training (P)"] = puzzles.Count(p => p.Rating.StartsWith("Training", StringComparison.Ordinal)),
                ["Mystery (M)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.M)),
                ["Emote (E)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.E)),
                ["Speed (S)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.S)),
                ["Phasing (P)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.P)),
                ["Void Jump (V)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.V)),
                ["Job Gate (J)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.J)),
                ["Ghost (G)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.G)),
                ["Logic (L)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.L)),
                ["No Media (X)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.X))
            };

            foreach (var type in specialTypes.Where(t => t.Value > 0).OrderByDescending(t => t.Value))
            {
                ImGui.Text($"{type.Key}: {type.Value}");
            }
        }
    }
}
