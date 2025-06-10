using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Windows.Components;

namespace WahJumps.Utilities
{
    public static class UiHelpers
    {
        public static Vector4 GetRatingColor(string rating)
        {
            return rating switch
            {
                "1★" => new Vector4(0.0f, 0.8f, 0.0f, 1.0f),      // Green
                "2★" => new Vector4(0.0f, 0.6f, 0.9f, 1.0f),      // Blue  
                "3★" => new Vector4(0.9f, 0.8f, 0.0f, 1.0f),      // Yellow
                "4★" => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),      // Orange
                "5★" => new Vector4(0.9f, 0.0f, 0.0f, 1.0f),      // Red
                _ when rating.Contains("★★★★★") => new Vector4(0.9f, 0.0f, 0.0f, 1.0f),
                _ when rating.Contains("★★★★") => new Vector4(1.0f, 0.5f, 0.0f, 1.0f),
                _ when rating.Contains("★★★") => new Vector4(0.9f, 0.8f, 0.0f, 1.0f),
                _ when rating.Contains("★★") => new Vector4(0.0f, 0.6f, 0.9f, 1.0f),
                _ when rating.Contains("★") => new Vector4(0.0f, 0.8f, 0.0f, 1.0f),
                _ => new Vector4(0.8f, 0.8f, 0.8f, 1.0f)
            };
        }

        public static void RenderRatingWithColor(string rating)
        {
            using var color = ImRaii.PushColor(ImGuiCol.Text, GetRatingColor(rating));
            ImGui.Text(rating);
        }

        public static void RenderCodesWithTooltips(string codes)
        {
            if (string.IsNullOrEmpty(codes))
            {
                ImGui.Text("-");
                return;
            }

            ImGui.Text(codes);

            if (ImGui.IsItemHovered())
            {
                using var tooltip = new ImRaii.Tooltip();
                ImGui.Text("Puzzle Types:");
                ImGui.Separator();

                var codeDescriptions = new Dictionary<string, string>
                {
                    { "M", "Mystery - Hard-to-find or maze-like paths" },
                    { "E", "Emote - Requires emote interaction" },
                    { "S", "Speed - Sprinting and time-based actions" },
                    { "P", "Phasing - Furniture interactions that phase you" },
                    { "V", "Void Jump - Requires jumping into void" },
                    { "J", "Job Gate - Requires specific jobs" },
                    { "G", "Ghost - Disappearances of furnishings" },
                    { "L", "Logic - Logic-based puzzle solving" },
                    { "X", "No Media - No streaming/recording allowed" }
                };

                string[] codeParts = codes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string code in codeParts)
                {
                    string trimmedCode = code.Trim();
                    if (codeDescriptions.TryGetValue(trimmedCode, out string description))
                    {
                        ImGui.BulletText($"{trimmedCode}: {description}");
                    }
                }
            }
        }

        public static void DrawFavoriteButton(JumpPuzzleData puzzle, Func<JumpPuzzleData, bool> isFavorite, 
            Action<JumpPuzzleData> addToFavorites, Action<JumpPuzzleData> removeFromFavorites)
        {
            bool isFav = isFavorite(puzzle);

            if (isFav)
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, UiTheme.Error);
                if (ImGui.Button($"♥##{puzzle.Id}"))
                {
                    removeFromFavorites(puzzle);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Remove from favorites");
            }
            else
            {
                using var color = ImRaii.PushColor(ImGuiCol.Text, UiTheme.Success);
                if (ImGui.Button($"♡##{puzzle.Id}"))
                {
                    addToFavorites(puzzle);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add to favorites");
            }
        }

        public static void DrawTravelButton(JumpPuzzleData puzzle, Action<JumpPuzzleData> onTravel)
        {
            using var colors = ImRaii.PushColor(ImGuiCol.Button, UiTheme.Primary);
            using var hoverColor = ImRaii.PushColor(ImGuiCol.ButtonHovered, UiTheme.PrimaryLight);
            
            if (ImGui.Button($"Travel##{puzzle.Id}"))
            {
                onTravel(puzzle);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Travel to {puzzle.World} {puzzle.Address}");
        }

        public static void DrawSpeedrunButton(JumpPuzzleData puzzle, Action<JumpPuzzleData> onSpeedrun)
        {
            if (ImRaii.StyledButton($"Timer##{puzzle.Id}", Vector2.Zero,
                new Vector4(0.2f, 0.4f, 0.6f, 1.0f),
                new Vector4(0.3f, 0.5f, 0.7f, 1.0f)))
            {
                onSpeedrun(puzzle);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Start speedrun timer for this puzzle");
        }

        public static ImRaii.StyledTable BeginPuzzleTable(string tableId, bool includeSpeedrun = false)
        {
            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingStretchProp;

            int columnCount = includeSpeedrun ? 9 : 8;
            
            var table = new ImRaii.StyledTable(tableId, columnCount, flags);
            
            if (table.Success)
            {
                ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 200);
                ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthStretch, 120);
                ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 180);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Fav", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Go", ImGuiTableColumnFlags.WidthFixed, 60);
                if (includeSpeedrun)
                {
                    ImGui.TableSetupColumn("Timer", ImGuiTableColumnFlags.WidthFixed, 60);
                }

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();
            }
            
            return table;
        }

        public static void EndPuzzleTable()
        {
            // This method is now obsolete - use 'using var table = BeginPuzzleTable()' instead
        }

        public static void DrawPuzzleTableRow(JumpPuzzleData puzzle, int index,
            Func<JumpPuzzleData, bool> isFavorite,
            Action<JumpPuzzleData> addToFavorites,
            Action<JumpPuzzleData> removeFromFavorites,
            Action<JumpPuzzleData> onTravel,
            Action<JumpPuzzleData>? onSpeedrun = null)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            using var id = new ImRaii.Id(index);
            ImGui.Selectable($"##row_{index}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);

            ImGui.TableSetColumnIndex(0);

            RenderRatingWithColor(puzzle.Rating);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(puzzle.PuzzleName);

            ImGui.TableNextColumn();
            ImGui.Text(puzzle.Builder);

            ImGui.TableNextColumn();
            ImGui.Text(puzzle.World);

            ImGui.TableNextColumn();
            ImGui.TextWrapped(puzzle.Address);

            ImGui.TableNextColumn();
            string combinedCodes = UiComponents.CombineCodes(puzzle.M, puzzle.E, puzzle.S, puzzle.P, puzzle.V, puzzle.J, puzzle.G, puzzle.L, puzzle.X);
            RenderCodesWithTooltips(combinedCodes);

            ImGui.TableNextColumn();
            DrawFavoriteButton(puzzle, isFavorite, addToFavorites, removeFromFavorites);

            ImGui.TableNextColumn();
            DrawTravelButton(puzzle, onTravel);

            if (onSpeedrun != null)
            {
                ImGui.TableNextColumn();
                DrawSpeedrunButton(puzzle, onSpeedrun);
            }
        }
    }
} 