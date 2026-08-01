using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using WahJumps.Models;
using WahJumps.Utilities;
using WahJumps.Data;

namespace WahJumps.Windows
{
    public class InformationTab
    {
        private List<InfoData> infoData = new List<InfoData>();
        private bool dataLoaded = false;

        public InformationTab()
        {
            LoadInfoData();
        }

        private void LoadInfoData()
        {
            try
            {
                infoData = StaticInfoData.GetInfoData();
                dataLoaded = true;
            }
            catch (System.Exception ex)
            {
                Plugin.PluginLog.Error($"Failed to load static info data: {ex.Message}");
                infoData = new List<InfoData>();
                dataLoaded = false;
            }
        }

        public void Draw()
        {
            using var contentChild = new ImRaii.Child("InformationScrollArea", new Vector2(0, 0), true, ImGuiWindowFlags.HorizontalScrollbar);

            if (!dataLoaded || infoData.Count == 0)
            {
                ImGui.Text("Unable to load information data.");
                ImGui.Text($"Data loaded: {dataLoaded}");
                ImGui.Text($"Data count: {infoData.Count}");
                
                if (ImGui.Button("Retry Loading"))
                {
                    LoadInfoData();
                }
                return;
            }

            var sections = GroupDataBySections();

            foreach (var section in sections)
            {
                DrawSection(section.Key, section.Value);
                ImGui.Separator();
            }
        }

        private Dictionary<string, List<InfoData>> GroupDataBySections()
        {
            var sections = new Dictionary<string, List<InfoData>>();
            string currentSection = "";

            foreach (var row in infoData)
            {
                if (string.IsNullOrWhiteSpace(row.Section) && 
                    string.IsNullOrWhiteSpace(row.Key) && 
                    string.IsNullOrWhiteSpace(row.Value1) && 
                    string.IsNullOrWhiteSpace(row.Value2) && 
                    string.IsNullOrWhiteSpace(row.Value3))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(row.Section) && !string.IsNullOrWhiteSpace(row.Key))
                {
                    if (row.Key.Contains("Difficulty Ratings") ||
                        row.Key.Contains("Sub-type Keys") || 
                        row.Key.Contains("Other Information") || 
                        row.Key.Contains("Puzzle Accessibility"))
                    {
                        currentSection = row.Key;
                        if (!sections.ContainsKey(currentSection))
                        {
                            sections[currentSection] = new List<InfoData>();
                        }
                        continue;
                    }
                }

                if (!string.IsNullOrWhiteSpace(row.Section) && string.IsNullOrWhiteSpace(row.Key))
                {
                    if (row.Section.Contains("Having a huge list"))
                    {
                        currentSection = "Puzzle Accessibility";
                        if (!sections.ContainsKey(currentSection))
                        {
                            sections[currentSection] = new List<InfoData>();
                        }
                        continue;
                    }
                }

                if (!string.IsNullOrEmpty(currentSection))
                {
                    if (!sections.ContainsKey(currentSection))
                    {
                        sections[currentSection] = new List<InfoData>();
                    }
                    sections[currentSection].Add(row);
                }
            }

            return sections;
        }

        private void DrawSection(string sectionName, List<InfoData> sectionData)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            bool isOpen = ImGui.CollapsingHeader(sectionName, ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.PopStyleColor();

            if (!isOpen) return;

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6));

            if (sectionName.Contains("Difficulty Ratings"))
            {
                DrawDifficultyRatingsTable(sectionData);
            }
            else if (sectionName.Contains("Sub-type Keys"))
            {
                DrawSubTypeKeysTable(sectionData);
            }
            else if (sectionName.Contains("Other Information"))
            {
                DrawOtherInfoTable(sectionData);
            }
            else if (sectionName.Contains("Puzzle Accessibility"))
            {
                DrawPuzzleAccessibilityTable(sectionData);
            }
            else
            {
                DrawGenericTable(sectionData);
            }

            ImGui.PopStyleVar();
        }

        private void DrawDifficultyRatingsTable(List<InfoData> data)
        {
            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                        ImGuiTableFlags.Borders |
                                        ImGuiTableFlags.SizingStretchProp;

            if (ImGui.BeginTable("DifficultyRatingsTable", 3, tableFlags))
            {
                ImGui.TableSetupColumn("Rating");
                ImGui.TableSetupColumn("Explanation");
                ImGui.TableSetupColumn("Square-Enix Equivalent");
                ImGui.TableHeadersRow();

                foreach (var row in data)
                {
                    if (string.IsNullOrWhiteSpace(row.Key) || 
                        row.Key.Contains("Ratings are designed") ||
                        row.Key == "Rating")
                        continue;

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    DrawStarDiagramOnly(row.Key);

                    ImGui.TableNextColumn();
                    DrawExplanationOnly(row.Value2 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value3 ?? "");
                }

                ImGui.EndTable();
            }
        }

        private void DrawSubTypeKeysTable(List<InfoData> data)
        {
            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                        ImGuiTableFlags.Borders |
                                        ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("SubTypeKeysTable", 4, tableFlags))
            {
                ImGui.TableSetupColumn("Code", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Element", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("Means the puzzle:", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("More Info", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var row in data)
                {
                    if (string.IsNullOrWhiteSpace(row.Key) || 
                        row.Key.Contains("Sub-types can seem") ||
                        row.Key == "Code")
                        continue;

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                        ImGui.Text(row.Key ?? "");

                    ImGui.TableNextColumn();
                    ImGui.Text(row.Value1 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value2 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value3 ?? "");
                }

                ImGui.EndTable();
            }
        }

        private void DrawOtherInfoTable(List<InfoData> data)
        {
            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                        ImGuiTableFlags.Borders |
                                        ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("OtherInfoTable", 3, tableFlags))
            {
                ImGui.TableSetupColumn("Term", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Explanation", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("More Info", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var row in data)
                {
                    if (string.IsNullOrWhiteSpace(row.Value1) || 
                        row.Value1.Contains("Some terms may sound") ||
                        row.Value1 == "Term")
                        continue;

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                        ImGui.Text(row.Value1 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value2 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value3 ?? "");
                }

                ImGui.EndTable();
            }
        }

        private void DrawPuzzleAccessibilityTable(List<InfoData> data)
        {
            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                        ImGuiTableFlags.Borders |
                                        ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("PuzzleAccessibilityTable", 3, tableFlags))
            {
                ImGui.TableSetupColumn("District", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Main City Aethernet Access Conditions", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("More Info", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var row in data)
                {
                    if (string.IsNullOrWhiteSpace(row.Value1) || 
                        row.Value1.Contains("Having a huge list") ||
                        row.Value1 == "District")
                        continue;

                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                        ImGui.Text(row.Value1 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value2 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value3 ?? "");
                }

                ImGui.EndTable();
            }
        }

        private void DrawGenericTable(List<InfoData> data)
        {
            ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                        ImGuiTableFlags.Borders |
                                        ImGuiTableFlags.SizingStretchProp;

            if (ImGui.BeginTable("GenericTable", 5, tableFlags))
            {
                ImGui.TableSetupColumn("Section");
                ImGui.TableSetupColumn("Key");
                ImGui.TableSetupColumn("Value 1");
                ImGui.TableSetupColumn("Value 2");
                ImGui.TableSetupColumn("Value 3");
                ImGui.TableHeadersRow();

                foreach (var row in data)
                {
                    ImGui.TableNextRow();

                    ImGui.TableNextColumn();
                    ImGui.Text(row.Section ?? "");

                    ImGui.TableNextColumn();
                    ImGui.Text(row.Key ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value1 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value2 ?? "");

                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(row.Value3 ?? "");
                }

                ImGui.EndTable();
            }
        }

        private void DrawStarDiagramOnly(string rating)
        {
            Vector4 ratingColor = UiTheme.GetRatingColor(rating);

            if (string.IsNullOrEmpty(rating))
            {
                ImGui.Text("");
                return;
            }

            string displayText = rating switch
            {
                "1★" => "1★",
                "2★" => "2★★",
                "3★" => "3★★★",
                "4★" => "4★★★★",
                "5★" => "5★★★★★",
                _ => rating
            };

            using (var colorStyle = new ImRaii.StyleColor(ImGuiCol.Text, ratingColor))
            {
                ImGui.Text(displayText);
            }
        }

        private void DrawExplanationOnly(string starDiagram)
        {
            if (string.IsNullOrEmpty(starDiagram))
            {
                ImGui.Text("");
                return;
            }

            var parts = starDiagram.Split(new[] { " - " }, 2, System.StringSplitOptions.None);
            ImGui.TextWrapped(parts.Length >= 2 ? parts[1] : starDiagram);
        }
    }
}
