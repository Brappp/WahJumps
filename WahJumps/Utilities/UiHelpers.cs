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
        // Rating color mapping - centralized from multiple files
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

        // Render rating with color - used in multiple table components
        public static void RenderRatingWithColor(string rating)
        {
            Vector4 color = GetRatingColor(rating);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(rating);
            ImGui.PopStyleColor();
        }

        // Render codes with tooltips - duplicated across components
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
                ShowCodesTooltip(codes);
            }
        }

        // Code tooltip - shared logic
        private static void ShowCodesTooltip(string codes)
        {
            ImGui.BeginTooltip();
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

            ImGui.EndTooltip();
        }

        // Favorite button - standardized across components
        public static void DrawFavoriteButton(JumpPuzzleData puzzle, Func<JumpPuzzleData, bool> isFavorite, 
            Action<JumpPuzzleData> addToFavorites, Action<JumpPuzzleData> removeFromFavorites)
        {
            bool isFav = isFavorite(puzzle);

            if (isFav)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Error);
                if (ImGui.Button($"♥##{puzzle.Id}"))
                {
                    removeFromFavorites(puzzle);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Remove from favorites");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Success);
                if (ImGui.Button($"♡##{puzzle.Id}"))
                {
                    addToFavorites(puzzle);
                }
                if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add to favorites");
                ImGui.PopStyleColor();
            }
        }

        // Travel button - standardized across components
        public static void DrawTravelButton(JumpPuzzleData puzzle, Action<JumpPuzzleData> onTravel)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.Primary);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.PrimaryLight);
            if (ImGui.Button($"Travel##{puzzle.Id}"))
            {
                onTravel(puzzle);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Travel to {puzzle.World} {puzzle.Address}");
            ImGui.PopStyleColor(2);
        }

        // Speedrun button - for timer integration
        public static void DrawSpeedrunButton(JumpPuzzleData puzzle, Action<JumpPuzzleData> onSpeedrun)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.7f, 1.0f));
            if (ImGui.Button($"Timer##{puzzle.Id}"))
            {
                onSpeedrun(puzzle);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Start speedrun timer for this puzzle");
            ImGui.PopStyleColor(2);
        }

        // Standard table setup - used across multiple components
        public static bool BeginPuzzleTable(string tableId, bool includeSpeedrun = false)
        {
            UiTheme.StyleTable();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingStretchProp;

            int columnCount = includeSpeedrun ? 9 : 8;
            
            if (ImGui.BeginTable(tableId, columnCount, flags))
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
                return true;
            }
            return false;
        }

        // End table with cleanup
        public static void EndPuzzleTable()
        {
            ImGui.EndTable();
            UiTheme.EndTableStyle();
        }

        // Draw a complete puzzle table row
        public static void DrawPuzzleTableRow(JumpPuzzleData puzzle, int index,
            Func<JumpPuzzleData, bool> isFavorite,
            Action<JumpPuzzleData> addToFavorites,
            Action<JumpPuzzleData> removeFromFavorites,
            Action<JumpPuzzleData> onTravel,
            Action<JumpPuzzleData>? onSpeedrun = null)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();

            ImGui.PushID(index);
            ImGui.Selectable($"##row_{index}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
            ImGui.PopID();

            ImGui.TableSetColumnIndex(0);

            // Rating
            RenderRatingWithColor(puzzle.Rating);

            // Name
            ImGui.TableNextColumn();
            ImGui.TextWrapped(puzzle.PuzzleName);

            // Builder
            ImGui.TableNextColumn();
            ImGui.Text(puzzle.Builder);

            // World
            ImGui.TableNextColumn();
            ImGui.Text(puzzle.World);

            // Address
            ImGui.TableNextColumn();
            ImGui.TextWrapped(puzzle.Address);

            // Codes
            ImGui.TableNextColumn();
            string combinedCodes = UiComponents.CombineCodes(puzzle.M, puzzle.E, puzzle.S, puzzle.P, puzzle.V, puzzle.J, puzzle.G, puzzle.L, puzzle.X);
            RenderCodesWithTooltips(combinedCodes);

            // Favorite button
            ImGui.TableNextColumn();
            DrawFavoriteButton(puzzle, isFavorite, addToFavorites, removeFromFavorites);

            // Travel button
            ImGui.TableNextColumn();
            DrawTravelButton(puzzle, onTravel);

            // Speedrun button (optional)
            if (onSpeedrun != null)
            {
                ImGui.TableNextColumn();
                DrawSpeedrunButton(puzzle, onSpeedrun);
            }
        }
    }
} 