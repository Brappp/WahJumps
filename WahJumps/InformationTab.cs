using System.Numerics;
using ImGuiNET;

namespace WahJumps.Windows
{
    public class InformationTab
    {
        public void Draw()
        {
            if (ImGui.BeginTabItem("Information"))
            {
                // Difficulty Ratings Section
                if (ImGui.CollapsingHeader("Difficulty Ratings: Setting General Expectations", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6)); // Add some padding

                    ImGui.BeginChild("DifficultyRatingsScroll", new Vector2(0, 200), true, ImGuiWindowFlags.HorizontalScrollbar); // Enable scrolling
                    if (ImGui.BeginTable("DifficultyRatingsTable", 4, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
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

                        ImGui.EndTable();
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleVar(); // Reset padding
                }

                ImGui.Separator();

                // Sub-type Keys Section
                if (ImGui.CollapsingHeader("Sub-type Keys: Know What Skillset to Bring", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6)); // Add padding

                    ImGui.BeginChild("SubTypeKeysScroll", new Vector2(0, 200), true, ImGuiWindowFlags.HorizontalScrollbar); // Enable scrolling
                    if (ImGui.BeginTable("SubTypeKeysTable", 3, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                    {
                        ImGui.TableSetupColumn("Code");
                        ImGui.TableSetupColumn("Element");
                        ImGui.TableSetupColumn("More Info");

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
                    ImGui.EndChild();
                    ImGui.PopStyleVar(); // Reset padding
                }

                ImGui.Separator();

                // Other Information Section
                if (ImGui.CollapsingHeader("Other Information", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6)); // Add padding

                    ImGui.BeginChild("OtherInfoScroll", new Vector2(0, 200), true, ImGuiWindowFlags.HorizontalScrollbar); // Enable scrolling
                    if (ImGui.BeginTable("OtherInfoTable", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                    {
                        ImGui.TableSetupColumn("Term");
                        ImGui.TableSetupColumn("Explanation");

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
                    ImGui.EndChild();
                    ImGui.PopStyleVar(); // Reset padding
                }

                ImGui.Separator();

                // Puzzle Accessibility Section
                if (ImGui.CollapsingHeader("Puzzle Accessibility", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 6)); // Add padding

                    ImGui.BeginChild("PuzzleAccessibilityScroll", new Vector2(0, 200), true, ImGuiWindowFlags.HorizontalScrollbar); // Enable scrolling
                    if (ImGui.BeginTable("PuzzleAccessibilityTable", 2, ImGuiTableFlags.SizingStretchSame | ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders))
                    {
                        ImGui.TableSetupColumn("District");
                        ImGui.TableSetupColumn("Main City Aetheryte Access Conditions");

                        ImGui.TableHeadersRow();

                        AddPuzzleAccessibilityRow("Goblet", "\"Where the Heart Is (Goblet)\" in Western Thanalan (Lv 5)");
                        AddPuzzleAccessibilityRow("Lavender Beds", "\"Where the Heart Is (Lavender Beds)\" in Central Shroud (Lv 5)");
                        AddPuzzleAccessibilityRow("Mist", "\"Where the Heart Is (Mist)\" in Lower La Noscea (Lv 5)");
                        AddPuzzleAccessibilityRow("Shirogane", "\"I Dream of Shirogane\" in Kugane (Lv 61)");
                        AddPuzzleAccessibilityRow("Empyreum", "\"Ascending to Empyreum\" in Ishgard (Lv 60)");
                        AddPuzzleAccessibilityRow("Apartment Wing", "Wing 1 and Wing 2 for apartments");

                        ImGui.EndTable();
                    }
                    ImGui.EndChild();
                    ImGui.PopStyleVar(); // Reset padding
                }

                ImGui.EndTabItem();
            }
        }

        private void AddDifficultyRatingRow(string rating, string systemRating, string explanation, string squareEnixEquivalent)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(rating);
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
            ImGui.Text(term);
            ImGui.TableNextColumn();
            ImGui.TextWrapped(explanation);
        }

        private void AddPuzzleAccessibilityRow(string district, string accessConditions)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(district);
            ImGui.TableNextColumn();
            ImGui.TextWrapped(accessConditions);
        }
    }
}
