// File: WahJumps/Windows/SearchFilterComponent.cs
// Status: FIXED VERSION - Compatible with condensed UI components and fixed row highlighting

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Utilities;
using WahJumps.Windows.Components;

namespace WahJumps.Windows
{
    public class SearchFilterComponent
    {
        // Available rating options
        private static readonly string[] RatingOptions = { "All", "★★★★★", "★★★★", "★★★", "★★", "★", "Training ☆", "Event ☆", "In Flux ☆", "Temp ☆" };

        // Filter state
        private string searchQuery = string.Empty;
        private string selectedRating = "All";
        private string selectedDataCenter = "All";
        private string selectedWorld = "All";
        private string selectedDistrict = "All";

        // Search results
        private List<JumpPuzzleData> searchResults = new List<JumpPuzzleData>();

        // Available data centers, worlds and districts (populated from data)
        private List<string> dataCenters = new List<string>();
        private Dictionary<string, List<string>> worldsByDataCenter = new Dictionary<string, List<string>>();
        private List<string> availableWorlds = new List<string>();
        private List<string> districts = new List<string> { "All", "Mist", "The Goblet", "The Lavender Beds", "Empyreum", "Shirogane" };

        // Actions for favorites and travel
        private readonly Func<JumpPuzzleData, bool> isFavorite;
        private readonly Action<JumpPuzzleData> addToFavorites;
        private readonly Action<JumpPuzzleData> removeFromFavorites;
        private readonly Action<JumpPuzzleData> onTravel;

        public SearchFilterComponent(
            Func<JumpPuzzleData, bool> isFavorite,
            Action<JumpPuzzleData> addToFavorites,
            Action<JumpPuzzleData> removeFromFavorites,
            Action<JumpPuzzleData> onTravel)
        {
            this.isFavorite = isFavorite;
            this.addToFavorites = addToFavorites;
            this.removeFromFavorites = removeFromFavorites;
            this.onTravel = onTravel;

            // Initialize the data centers list
            ResetFilters();
        }

        // Initializes the data for filtering
        public void SetAvailableData(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            dataCenters.Clear();
            worldsByDataCenter.Clear();
            availableWorlds.Clear();

            dataCenters.Add("All");
            availableWorlds.Add("All");

            foreach (var dc in allData)
            {
                dataCenters.Add(dc.Key);

                // Extract unique worlds from this data center
                var worlds = dc.Value
                    .Select(p => p.World)
                    .Distinct()
                    .OrderBy(w => w)
                    .ToList();

                worldsByDataCenter[dc.Key] = worlds;

                // Add to overall worlds list
                foreach (var world in worlds)
                {
                    if (!availableWorlds.Contains(world))
                    {
                        availableWorlds.Add(world);
                    }
                }
            }

            // Sort alphabetically
            dataCenters.Sort();
            availableWorlds.Sort();
        }

        // Reset filters to default values
        public void ResetFilters()
        {
            searchQuery = string.Empty;
            selectedRating = "All";
            selectedDataCenter = "All";
            selectedWorld = "All";
            selectedDistrict = "All";
        }

        // Draw search and filter UI
        public void Draw(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            DrawSearchAndFilters();

            // If we have search results or an active search, show them
            if (!string.IsNullOrWhiteSpace(searchQuery) ||
                selectedRating != "All" ||
                selectedDataCenter != "All" ||
                selectedWorld != "All" ||
                selectedDistrict != "All")
            {
                // Run search with current filters
                PerformSearch(allData);

                // Display results
                DrawSearchResults();
            }
            else
            {
                using (var centered = new ImRaii.Group())
                {
                    UiTheme.CenteredText("Enter search terms or select filters to find puzzles");
                }
            }
        }

        // Draw search input and filter dropdowns
        private void DrawSearchAndFilters()
        {
            // Use the modern search input style
            UiComponents.FilterHeader("Search Jump Puzzles", !string.IsNullOrWhiteSpace(searchQuery));

            // Search box - takes up the full width with compact style
            UiComponents.SearchBox(ref searchQuery, "Search for puzzles by name, builder, or description...");

            ImGui.Spacing();

            // Filter row - more condensed layout
            float availWidth = ImGui.GetContentRegionAvail().X;
            float itemWidth = (availWidth / 3) - 8;

            // Rating filter
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.BeginCombo("Rating", selectedRating))
            {
                foreach (var rating in RatingOptions)
                {
                    bool isSelected = selectedRating == rating;
                    if (ImGui.Selectable(rating, isSelected))
                    {
                        selectedRating = rating;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();

            // Data Center filter
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.BeginCombo("Data Center", selectedDataCenter))
            {
                foreach (var dc in dataCenters)
                {
                    bool isSelected = selectedDataCenter == dc;
                    if (ImGui.Selectable(dc, isSelected))
                    {
                        selectedDataCenter = dc;

                        // If we selected a specific data center, adjust available worlds
                        if (selectedDataCenter != "All" && selectedWorld != "All")
                        {
                            // Check if the currently selected world exists in this data center
                            if (!worldsByDataCenter.ContainsKey(selectedDataCenter) ||
                                !worldsByDataCenter[selectedDataCenter].Contains(selectedWorld))
                            {
                                selectedWorld = "All"; // Reset to All if not available
                            }
                        }
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();

            // World filter - changes based on selected Data Center
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.BeginCombo("World", selectedWorld))
            {
                // "All" option is always available
                bool isAllSelected = selectedWorld == "All";
                if (ImGui.Selectable("All", isAllSelected))
                {
                    selectedWorld = "All";
                }

                if (isAllSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }

                // If a specific data center is selected, only show its worlds
                if (selectedDataCenter != "All" && worldsByDataCenter.ContainsKey(selectedDataCenter))
                {
                    foreach (var world in worldsByDataCenter[selectedDataCenter])
                    {
                        bool isSelected = selectedWorld == world;
                        if (ImGui.Selectable(world, isSelected))
                        {
                            selectedWorld = world;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }
                else
                {
                    // Otherwise show all worlds
                    foreach (var world in availableWorlds)
                    {
                        if (world == "All") continue; // Skip as we already added it

                        bool isSelected = selectedWorld == world;
                        if (ImGui.Selectable(world, isSelected))
                        {
                            selectedWorld = world;
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                }

                ImGui.EndCombo();
            }

            // Second row of filters
            ImGui.Spacing();

            // District filter
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.BeginCombo("District", selectedDistrict))
            {
                foreach (var district in districts)
                {
                    bool isSelected = selectedDistrict == district;
                    if (ImGui.Selectable(district, isSelected))
                    {
                        selectedDistrict = district;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();

            // Reset filters button
            if (ImGui.Button("Reset Filters", new System.Numerics.Vector2(itemWidth, 0)))
            {
                ResetFilters();
            }

            ImGui.Separator();
        }

        // Draw search results table
        private void DrawSearchResults()
        {
            if (searchResults.Count == 0)
            {
                UiTheme.CenteredText("No matches found. Try adjusting your search or filters.");
                return;
            }

            ImGui.Text($"Found {searchResults.Count} matching puzzles");

            // Draw the table with results using the new table styling
            DrawPuzzleTable(searchResults);
        }

        // Helper method to draw puzzle table with styling
        private void DrawPuzzleTable(List<JumpPuzzleData> puzzles)
        {
            // Apply consistent table styling
            UiTheme.StyleTable();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingStretchProp;

            if (ImGui.BeginTable("SearchResultsTable", 9, flags))
            {
                // Configure columns, now more condensed
                ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 180);
                ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthStretch, 120);
                ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 160);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Rules", ImGuiTableColumnFlags.WidthStretch, 150);
                ImGui.TableSetupColumn("Fav", ImGuiTableColumnFlags.WidthFixed, 35);
                ImGui.TableSetupColumn("Go", ImGuiTableColumnFlags.WidthFixed, 35);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                // Draw each row
                for (int i = 0; i < puzzles.Count; i++)
                {
                    var puzzle = puzzles[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // Add AllowItemOverlap to allow clicks through to buttons
                    ImGui.PushID(i);
                    ImGui.Selectable($"##row_{i}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
                    ImGui.PopID();

                    // Reset cursor to start of row for the actual content
                    ImGui.TableSetColumnIndex(0);

                    // Rating column with color
                    RenderRatingWithColor(puzzle.Rating);

                    // Puzzle Name
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

                    // Codes (compacted)
                    ImGui.TableNextColumn();
                    string combinedCodes = UiComponents.CombineCodes(puzzle.M, puzzle.E, puzzle.S, puzzle.P, puzzle.V, puzzle.J, puzzle.G, puzzle.L, puzzle.X);
                    RenderCodesWithTooltips(combinedCodes);

                    // Goals/Rules
                    ImGui.TableNextColumn();
                    ImGui.TextWrapped(puzzle.GoalsOrRules);

                    // Favorite Button (compact icon)
                    ImGui.TableNextColumn();
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

                    // Travel Button (compact icon)
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
                    if (ImGui.Button($"→##{puzzle.Id}"))
                    {
                        onTravel(puzzle);
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Travel to {puzzle.World} {puzzle.Address}");
                    ImGui.PopStyleColor();
                }

                ImGui.EndTable();
            }

            // End table styling
            UiTheme.EndTableStyle();
        }

        // Helper to render rating with appropriate color
        private void RenderRatingWithColor(string rating)
        {
            Vector4 color;

            switch (rating)
            {
                case "★★★★★":
                    color = new Vector4(0.9f, 0.0f, 0.0f, 1.0f); // Red
                    break;
                case "★★★★":
                    color = new Vector4(1.0f, 0.5f, 0.0f, 1.0f); // Orange
                    break;
                case "★★★":
                    color = new Vector4(0.9f, 0.8f, 0.0f, 1.0f); // Yellow
                    break;
                case "★★":
                    color = new Vector4(0.0f, 0.6f, 0.9f, 1.0f); // Blue
                    break;
                case "★":
                    color = new Vector4(0.0f, 0.8f, 0.0f, 1.0f); // Green
                    break;
                default:
                    color = new Vector4(0.8f, 0.8f, 0.8f, 1.0f); // Gray
                    break;
            }

            using var textColor = new ImRaii.StyleColor(ImGuiCol.Text, color);
            ImGui.Text(rating);
        }

        // Helper to render codes with tooltips
        private void RenderCodesWithTooltips(string codes)
        {
            if (string.IsNullOrEmpty(codes))
            {
                ImGui.Text("-");
                return;
            }

            ImGui.Text(codes);

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text("Puzzle Type Codes:");
                ImGui.Separator();

                Dictionary<string, string> codeDescriptions = new Dictionary<string, string>
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
                    else
                    {
                        ImGui.BulletText(trimmedCode);
                    }
                }

                ImGui.EndTooltip();
            }
        }

        // Perform search based on current filters
        private void PerformSearch(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            searchResults.Clear();

            // Determine which data centers to search based on filter
            var dataCentersToSearch = selectedDataCenter == "All"
                ? allData.Keys.ToList()
                : new List<string> { selectedDataCenter };

            foreach (var dc in dataCentersToSearch)
            {
                if (!allData.ContainsKey(dc)) continue;

                var puzzles = allData[dc];

                // Filter the puzzles
                var filteredPuzzles = puzzles.Where(p =>
                {
                    // Rating filter
                    if (selectedRating != "All" && p.Rating != selectedRating)
                        return false;

                    // World filter
                    if (selectedWorld != "All" && p.World != selectedWorld)
                        return false;

                    // District filter
                    if (selectedDistrict != "All")
                    {
                        bool containsDistrict = p.Address.Contains(selectedDistrict, StringComparison.OrdinalIgnoreCase);
                        if (!containsDistrict)
                            return false;
                    }

                    // Text search (if provided)
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
                        // Check name, builder, world, and description for matches
                        return
                            p.PuzzleName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                            p.Builder.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                            p.World.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                            p.GoalsOrRules.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                            p.Address.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
                    }

                    return true;
                });

                searchResults.AddRange(filteredPuzzles);
            }

            // Sort search results by rating (descending) then by name
            searchResults = searchResults
                .OrderByDescending(p => ConvertRatingToSortValue(p.Rating))
                .ThenBy(p => p.PuzzleName)
                .ToList();
        }

        // Helper method to convert rating to a numeric value for sorting
        private int ConvertRatingToSortValue(string rating)
        {
            // Extract numeric stars or use a default value
            if (rating.Contains("★"))
            {
                return rating.Count(c => c == '★');
            }

            // Special ratings
            switch (rating)
            {
                case "Training ☆": return 1;
                case "Event ☆": return 0;
                case "In Flux ☆": return 0;
                case "Temp ☆": return 0;
                default: return 0;
            }
        }
    }
}
