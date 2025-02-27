// File: WahJumps/Windows/InformationTab.cs
// Status: UPDATED VERSION - Fixed formatting issues

using System.Numerics;
using ImGuiNET;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class InformationTab
    {
        // Array to store column widths after auto-adjusting the first table
        private float[] columnWidths = new float[4];
        private bool isInitialized = false;

        public void Draw()
        {
            // Fixed TabItem usage
            using var tabItem = new ImRaii.TabItem("Information");
            if (!tabItem.Success) return;

            using var contentChild = new ImRaii.Child("InformationScrollArea", new Vector2(0, 0), true, ImGuiWindowFlags.HorizontalScrollbar);

            // Difficulty Ratings Table
            DrawDifficultyRatingsTable();

            ImGui.Separator();

            // Sub-type Keys Table
            DrawSubTypeKeysTable();

            ImGui.Separator();

            // Other Information Table
            DrawOtherInfoTable();

            ImGui.Separator();

            // Puzzle Accessibility Table
            DrawPuzzleAccessibilityTable();
        }

        private void DrawDifficultyRatingsTable()
        {
            // Improved header styling
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            bool isOpen = ImGui.CollapsingHeader("Difficulty Ratings: Setting General Expectations", ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.PopStyleColor();

            if (isOpen)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6));

                ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                            ImGuiTableFlags.Borders |
                                            ImGuiTableFlags.SizingStretchProp;

                if (ImGui.BeginTable("DifficultyRatingsTable", 4, tableFlags))
                {
                    ImGui.TableSetupColumn("Rating");
                    ImGui.TableSetupColumn("Original System Rating");
                    ImGui.TableSetupColumn("Star Diagram and Explanation");
                    ImGui.TableSetupColumn("Square-Enix Equivalent");

                    ImGui.TableHeadersRow();

                    AddDifficultyRatingRow("1★", "Beginner", "★☆☆☆☆ - Designed to be easy", "Cliffhanger GATEs; Fall of Belah'dia");
                    AddDifficultyRatingRow("2★", "Medium", "★★☆☆☆ - A solid challenge", "Kugane Tower; Sylphstep GATE");
                    AddDifficultyRatingRow("3★", "Hard", "★★★☆☆ - Challenging for most", "Moonfire Tower (Event)");
                    AddDifficultyRatingRow("4★", "Satan", "★★★★☆ - Extremely broad difficulty", "Moonfire Tower (Toothpick section)");
                    AddDifficultyRatingRow("5★", "God", "★★★★★ - For the hardest puzzles", "No Square-Enix equivalent");
                    AddDifficultyRatingRow("E", "Training", "Training - Teaches or introduces new techniques", "");
                    AddDifficultyRatingRow("T", "Event", "Event-specific puzzle, only available sometimes", "");
                    AddDifficultyRatingRow("F", "In Flux", "Puzzle undergoes changes", "");

                    // Store column widths for other tables if not initialized yet
                    if (!isInitialized)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            columnWidths[i] = ImGui.GetColumnWidth(i);
                        }
                        isInitialized = true;
                    }

                    ImGui.EndTable();
                }

                ImGui.PopStyleVar();
            }
        }

        private void DrawSubTypeKeysTable()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            bool isOpen = ImGui.CollapsingHeader("Sub-type Keys: Know What Skillset to Bring", ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.PopStyleColor();

            if (isOpen)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6));

                ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                            ImGuiTableFlags.Borders |
                                            ImGuiTableFlags.SizingFixedFit;

                if (ImGui.BeginTable("SubTypeKeysTable", 3, tableFlags))
                {
                    // Use column widths from first table if available
                    if (isInitialized)
                    {
                        ImGui.TableSetupColumn("Code", ImGuiTableColumnFlags.WidthFixed, columnWidths[0]);
                        ImGui.TableSetupColumn("Element", ImGuiTableColumnFlags.WidthFixed, columnWidths[1]);
                        ImGui.TableSetupColumn("More Info", ImGuiTableColumnFlags.WidthFixed, columnWidths[2]);
                    }
                    else
                    {
                        ImGui.TableSetupColumn("Code");
                        ImGui.TableSetupColumn("Element");
                        ImGui.TableSetupColumn("More Info");
                    }

                    ImGui.TableHeadersRow();

                    AddSubTypeKeyRow("M", "Mystery", "Hard-to-find or maze-like paths, tricky");
                    AddSubTypeKeyRow("E", "Emote", "Requires emote interaction");
                    AddSubTypeKeyRow("S", "Speed", "Sprinting and time-based actions");
                    AddSubTypeKeyRow("P", "Phasing", "Furniture interactions that phase you through");
                    AddSubTypeKeyRow("V", "Void Jump", "Requires jumping into void");
                    AddSubTypeKeyRow("J", "Job Gate", "Requires specific jobs");
                    AddSubTypeKeyRow("G", "Ghost", "Disappearances of furnishings");
                    AddSubTypeKeyRow("L", "Logic", "Logic-based puzzle solving");
                    AddSubTypeKeyRow("X", "No Media", "No streaming/recording allowed");

                    ImGui.EndTable();
                }

                ImGui.PopStyleVar();
            }
        }

        private void DrawOtherInfoTable()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            bool isOpen = ImGui.CollapsingHeader("Other Information", ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.PopStyleColor();

            if (isOpen)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6));

                ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                            ImGuiTableFlags.Borders |
                                            ImGuiTableFlags.SizingFixedFit;

                if (ImGui.BeginTable("OtherInfoTable", 2, tableFlags))
                {
                    // Use column widths from first table if available
                    if (isInitialized)
                    {
                        ImGui.TableSetupColumn("Term", ImGuiTableColumnFlags.WidthFixed, columnWidths[0]);
                        ImGui.TableSetupColumn("Explanation", ImGuiTableColumnFlags.WidthFixed, columnWidths[1]);
                    }
                    else
                    {
                        ImGui.TableSetupColumn("Term");
                        ImGui.TableSetupColumn("Explanation");
                    }

                    ImGui.TableHeadersRow();

                    AddOtherInfoRow("No media tag", "Some builders prefer no streaming or videos");
                    AddOtherInfoRow("Goals/Rules", "Conditions or rules to complete the puzzle");
                    AddOtherInfoRow("Bonus Stages", "Additional areas/conditions for extra rewards");
                    AddOtherInfoRow("Friend Teleport", "Friends can teleport directly to you");
                    AddOtherInfoRow("Housing cube", "Invisible walls or floor creating puzzles");
                    AddOtherInfoRow("The Void", "Jumping through blank space");
                    AddOtherInfoRow("Slides", "Emote-triggered movements");
                    AddOtherInfoRow("Ghosting", "Furniture disappearing to solve puzzles");

                    ImGui.EndTable();
                }

                ImGui.PopStyleVar();
            }
        }

        private void DrawPuzzleAccessibilityTable()
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            bool isOpen = ImGui.CollapsingHeader("Puzzle Accessibility", ImGuiTreeNodeFlags.DefaultOpen);
            ImGui.PopStyleColor();

            if (isOpen)
            {
                ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6));

                ImGuiTableFlags tableFlags = ImGuiTableFlags.RowBg |
                                            ImGuiTableFlags.Borders |
                                            ImGuiTableFlags.SizingFixedFit;

                if (ImGui.BeginTable("PuzzleAccessibilityTable", 2, tableFlags))
                {
                    // Use column widths from first table if available
                    if (isInitialized)
                    {
                        ImGui.TableSetupColumn("District", ImGuiTableColumnFlags.WidthFixed, columnWidths[0]);
                        ImGui.TableSetupColumn("Main City Aetheryte Access Conditions", ImGuiTableColumnFlags.WidthFixed, columnWidths[1]);
                    }
                    else
                    {
                        ImGui.TableSetupColumn("District");
                        ImGui.TableSetupColumn("Main City Aetheryte Access Conditions");
                    }

                    ImGui.TableHeadersRow();

                    AddPuzzleAccessibilityRow("Goblet", "\"Where the Heart Is (Goblet)\" in Western Thanalan (Lv 5)");
                    AddPuzzleAccessibilityRow("Lavender Beds", "\"Where the Heart Is (Lavender Beds)\" in Central Shroud (Lv 5)");
                    AddPuzzleAccessibilityRow("Mist", "\"Where the Heart Is (Mist)\" in Lower La Noscea (Lv 5)");
                    AddPuzzleAccessibilityRow("Shirogane", "\"I Dream of Shirogane\" in Kugane (Lv 61)");
                    AddPuzzleAccessibilityRow("Empyreum", "\"Ascending to Empyreum\" in Ishgard (Lv 60)");
                    AddPuzzleAccessibilityRow("Apartment Wing", "Wing 1 and Wing 2 for apartments");

                    ImGui.EndTable();
                }

                ImGui.PopStyleVar();
            }
        }

        private void AddDifficultyRatingRow(string rating, string systemRating, string explanation, string squareEnixEquivalent)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();

            // Apply color to rating text based on difficulty
            switch (rating)
            {
                case "1★":
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.8f, 0.0f, 1.0f)))
                        ImGui.Text(rating);
                    break;
                case "2★":
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, new Vector4(0.0f, 0.6f, 0.9f, 1.0f)))
                        ImGui.Text(rating);
                    break;
                case "3★":
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.8f, 0.0f, 1.0f)))
                        ImGui.Text(rating);
                    break;
                case "4★":
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.0f, 1.0f)))
                        ImGui.Text(rating);
                    break;
                case "5★":
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, new Vector4(0.9f, 0.0f, 0.0f, 1.0f)))
                        ImGui.Text(rating);
                    break;
                default:
                    using (var color = new ImRaii.StyleColor(ImGuiCol.Text, new Vector4(0.8f, 0.8f, 0.8f, 1.0f)))
                        ImGui.Text(rating);
                    break;
            }

            ImGui.TableNextColumn();
            ImGui.Text(systemRating);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(explanation);

            ImGui.TableNextColumn();
            ImGui.Text(squareEnixEquivalent);
        }

        private void AddSubTypeKeyRow(string code, string element, string moreInfo)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            using (var color = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                ImGui.Text(code);

            ImGui.TableNextColumn();
            ImGui.Text(element);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(moreInfo);
        }

        private void AddOtherInfoRow(string term, string explanation)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            using (var color = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                ImGui.Text(term);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(explanation);
        }

        private void AddPuzzleAccessibilityRow(string district, string accessConditions)
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            using (var color = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                ImGui.Text(district);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(accessConditions);
        }
    }
}
