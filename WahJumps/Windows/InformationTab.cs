using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
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
                // Load data from static embedded data instead of CSV file
                infoData = StaticInfoData.GetInfoData();
                dataLoaded = true;
            }
            catch (System.Exception ex)
            {
                // Log error but continue with empty data
                Plugin.PluginLog.Error($"Failed to load static info data: {ex.Message}");
                infoData = new List<InfoData>();
                dataLoaded = false;
            }
        }

        public void Draw()
        {
            using var tabItem = new ImRaii.TabItem("Information");
            if (!tabItem.Success) return;

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

            // Group data by sections
            var sections = GroupDataBySections();

            // Draw each section
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
                // Skip completely empty rows
                if (string.IsNullOrWhiteSpace(row.Section) && 
                    string.IsNullOrWhiteSpace(row.Key) && 
                    string.IsNullOrWhiteSpace(row.Value1) && 
                    string.IsNullOrWhiteSpace(row.Value2) && 
                    string.IsNullOrWhiteSpace(row.Value3))
                {
                    continue;
                }

                // Check if this is a section header (has content in Key column but empty Section)
                if (string.IsNullOrWhiteSpace(row.Section) && !string.IsNullOrWhiteSpace(row.Key))
                {
                    // This might be a section header
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

                // Check if this is a section header in the Section column (for "Having a huge list..." type entries)
                if (!string.IsNullOrWhiteSpace(row.Section) && string.IsNullOrWhiteSpace(row.Key))
                {
                    if (row.Section.Contains("Having a huge list"))
                    {
                        // This is part of Puzzle Accessibility section
                        currentSection = "Puzzle Accessibility";
                        if (!sections.ContainsKey(currentSection))
                        {
                            sections[currentSection] = new List<InfoData>();
                        }
                        continue;
                    }
                }

                // Add to current section if we have one
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
            // Draw section header
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            bool isOpen = ImGui.CollapsingHeader(sectionName, ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.PopStyleColor();

            if (!isOpen) return;

            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6));

            // Determine table structure based on section
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
                // Generic table for unknown sections
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
                    // Skip description rows and empty rows
                    if (string.IsNullOrWhiteSpace(row.Key) || 
                        row.Key.Contains("Ratings are designed") ||
                        row.Key == "Rating")
                        continue;

                    ImGui.TableNextRow();

                    // Rating column - show clean star format
                    ImGui.TableNextColumn();
                    DrawStarDiagramOnly(row.Key, row.Value2 ?? "");

                    // Other columns
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
                    // Skip description rows and headers
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
                    // Skip description rows and headers
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
                    // Skip description rows and headers
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

        private void DrawColoredRating(string rating)
        {
            if (string.IsNullOrEmpty(rating))
            {
                ImGui.Text("");
                return;
            }

            Vector4 color = GetRatingColor(rating);

            using (var colorStyle = new ImRaii.StyleColor(ImGuiCol.Text, color))
            {
                ImGui.Text(rating);
            }
        }

        private void DrawStarDiagramOnly(string rating, string starDiagram)
        {
            Vector4 ratingColor = GetRatingColor(rating);

            if (string.IsNullOrEmpty(rating))
            {
                ImGui.Text("");
                return;
            }

            // Create simplified star display based on rating
            string displayText = rating switch
            {
                "1★" => "1★",
                "2★" => "2★★",
                "3★" => "3★★★",
                "4★" => "4★★★★",
                "5★" => "5★★★★★",
                _ => rating // For special ratings like P, E, T, F
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

            // Extract just the explanation part (after " - ")
            var parts = starDiagram.Split(new[] { " - " }, 2, System.StringSplitOptions.None);
            if (parts.Length >= 2)
            {
                ImGui.TextWrapped(parts[1]); // Just the explanation part
            }
            else
            {
                ImGui.TextWrapped(starDiagram); // If no separator, show the whole thing
            }
        }

        private void DrawColoredStarDiagram(string rating, string starDiagram)
        {
            if (string.IsNullOrEmpty(starDiagram))
            {
                ImGui.Text("");
                return;
            }

            Vector4 color = GetRatingColor(rating);

            // Split the text to find the star portion and the description
            var parts = starDiagram.Split(new[] { " - " }, 2, System.StringSplitOptions.None);
            
            if (parts.Length >= 2)
            {
                // Draw the star portion with color
                using (var colorStyle = new ImRaii.StyleColor(ImGuiCol.Text, color))
                {
                    ImGui.Text(parts[0]);
                }
                
                // Draw the description on the same line with normal color
                ImGui.SameLine();
                ImGui.Text(" - ");
                ImGui.SameLine();
                ImGui.TextWrapped(parts[1]);
            }
            else
            {
                // If no " - " separator found, just draw the whole thing with color
                using (var colorStyle = new ImRaii.StyleColor(ImGuiCol.Text, color))
                {
                    ImGui.TextWrapped(starDiagram);
                }
            }
        }

        private Vector4 GetRatingColor(string rating)
        {
            return rating switch
            {
                "1★" => new Vector4(0.0f, 0.8f, 0.0f, 1.0f),      // Green
                "2★" => new Vector4(0.0f, 0.6f, 0.9f, 1.0f),      // Blue
                "3★" => new Vector4(0.9f, 0.8f, 0.0f, 1.0f),      // Yellow
                "4★" => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),      // Orange
                "5★" => new Vector4(0.9f, 0.0f, 0.0f, 1.0f),      // Red
                _ => new Vector4(0.8f, 0.8f, 0.8f, 1.0f)          // Gray for special ratings
            };
        }
    }
}
