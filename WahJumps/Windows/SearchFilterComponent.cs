// File: WahJumps/Windows/SearchFilterComponent.cs
// Status: REDESIGNED VERSION - Modern search with consistent UI styling

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
        // Available rating options with cleaner display
        private static readonly string[] RatingOptions = { "All Ratings", "★★★★★", "★★★★", "★★★", "★★", "★", "Training ☆", "Event ☆", "In Flux ☆", "Temp ☆" };

        // Filter state
        private string searchQuery = string.Empty;
        private string selectedRating = "All Ratings";
        private string selectedDataCenter = "All Data Centers";
        private string selectedWorld = "All Worlds";
        private string selectedDistrict = "All Districts";

        // Search results and display
        private List<JumpPuzzleData> searchResults = new List<JumpPuzzleData>();
        private List<JumpPuzzleData> allPuzzles = new List<JumpPuzzleData>();
        private bool showAdvancedFilters = false;

        // Available data centers, worlds and districts
        private List<string> dataCenters = new List<string>();
        private Dictionary<string, List<string>> worldsByDataCenter = new Dictionary<string, List<string>>();
        private List<string> availableWorlds = new List<string>();
        private List<string> districts = new List<string> { "All Districts", "Mist", "The Goblet", "The Lavender Beds", "Empyreum", "Shirogane" };

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
        }

        // Initializes the data for filtering
        public void SetAvailableData(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            dataCenters.Clear();
            worldsByDataCenter.Clear();
            availableWorlds.Clear();
            allPuzzles.Clear();

            dataCenters.Add("All Data Centers");
            availableWorlds.Add("All Worlds");

            foreach (var dc in allData)
            {
                dataCenters.Add(dc.Key);
                allPuzzles.AddRange(dc.Value);

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

            // Perform initial search to populate results
            PerformSearch(allData);
        }

        // Reset filters to default values
        public void ResetFilters()
        {
            searchQuery = string.Empty;
            selectedRating = "All Ratings";
            selectedDataCenter = "All Data Centers";
            selectedWorld = "All Worlds";
            selectedDistrict = "All Districts";
            showAdvancedFilters = false;
        }

        // Main draw method with improved layout
        public void Draw(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            DrawSearchHeader();
            DrawSearchInput();
            DrawQuickFilters();
            
            if (showAdvancedFilters)
            {
                DrawAdvancedFilters();
            }

            ImGui.Separator();

            // Perform search and display results
            PerformSearch(allData);
            DrawResults();
        }

        // Compact search header
        private void DrawSearchHeader()
        {
            // Header with search icon and title
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            ImGui.Text("🔍 Search Jump Puzzles");
            ImGui.PopStyleColor();

            ImGui.SameLine();
            
            // Stats display
            if (allPuzzles.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Gray);
                ImGui.Text($"({searchResults.Count} of {allPuzzles.Count} puzzles)");
                ImGui.PopStyleColor();
            }

            ImGui.Spacing();
        }

        // Improved search input
        private void DrawSearchInput()
        {
            // Search box with consistent styling
            ImGui.PushItemWidth(-80);
            bool searchChanged = ImGui.InputTextWithHint("##search", "🔍 Search by name, builder, world, or description...", ref searchQuery, 256);
            ImGui.PopItemWidth();

            // Clear button if there's text
            if (!string.IsNullOrEmpty(searchQuery))
            {
                ImGui.SameLine();
                if (ImGui.Button("Clear"))
                {
                    searchQuery = string.Empty;
                }
            }

            ImGui.Spacing();
        }

        // Quick filter buttons that match existing UI
        private void DrawQuickFilters()
        {
            ImGui.Text("Quick Filters:");
            ImGui.SameLine();

            // Rating buttons with consistent styling
            string[] quickRatings = { "★★★★★", "★★★★", "★★★", "★★", "★" };
            foreach (var rating in quickRatings)
            {
                ImGui.SameLine();
                
                bool isSelected = selectedRating == rating;
                if (isSelected)
                {
                    ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.Primary);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.Primary);
                }

                if (ImGui.SmallButton(rating))
                {
                    selectedRating = isSelected ? "All Ratings" : rating;
                }

                if (isSelected)
                {
                    ImGui.PopStyleColor(2);
                }
            }

            // Advanced filters toggle
            ImGui.SameLine();
            ImGui.Dummy(new Vector2(20, 0));
            ImGui.SameLine();
            
            if (ImGui.Button(showAdvancedFilters ? "▼ Hide Filters" : "▶ More Filters"))
            {
                showAdvancedFilters = !showAdvancedFilters;
            }

            // Reset button
            ImGui.SameLine();
            if (ImGui.Button("Reset All"))
            {
                ResetFilters();
            }

            ImGui.Spacing();
        }

        // Advanced filter options with consistent styling
        private void DrawAdvancedFilters()
        {
            float itemWidth = (ImGui.GetContentRegionAvail().X - 40) / 3;

            // Data Center filter
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.BeginCombo("##datacenter", selectedDataCenter))
            {
                foreach (var dc in dataCenters)
                {
                    bool isSelected = selectedDataCenter == dc;
                    if (ImGui.Selectable(dc, isSelected))
                    {
                        selectedDataCenter = dc;
                        // Reset world if it doesn't exist in selected DC
                        if (selectedDataCenter != "All Data Centers" && selectedWorld != "All Worlds")
                        {
                            if (!worldsByDataCenter.ContainsKey(selectedDataCenter) ||
                                !worldsByDataCenter[selectedDataCenter].Contains(selectedWorld))
                            {
                                selectedWorld = "All Worlds";
                            }
                        }
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();

            // World filter
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.BeginCombo("##world", selectedWorld))
            {
                bool isAllSelected = selectedWorld == "All Worlds";
                if (ImGui.Selectable("All Worlds", isAllSelected))
                {
                    selectedWorld = "All Worlds";
                }
                if (isAllSelected) ImGui.SetItemDefaultFocus();

                var worldsToShow = selectedDataCenter != "All Data Centers" && worldsByDataCenter.ContainsKey(selectedDataCenter)
                    ? worldsByDataCenter[selectedDataCenter]
                    : availableWorlds.Skip(1); // Skip "All Worlds"

                foreach (var world in worldsToShow)
                {
                    bool isSelected = selectedWorld == world;
                    if (ImGui.Selectable(world, isSelected))
                    {
                        selectedWorld = world;
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            ImGui.SameLine();

            // District filter
            ImGui.SetNextItemWidth(itemWidth);
            if (ImGui.BeginCombo("##district", selectedDistrict))
            {
                foreach (var district in districts)
                {
                    bool isSelected = selectedDistrict == district;
                    if (ImGui.Selectable(district, isSelected))
                    {
                        selectedDistrict = district;
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // Labels for the combos
            ImGui.Text("Data Center");
            ImGui.SameLine(itemWidth + 20);
            ImGui.Text("World");
            ImGui.SameLine((itemWidth * 2) + 40);
            ImGui.Text("District");

            ImGui.Spacing();
        }

        // Results display with improved table layout
        private void DrawResults()
        {
            if (searchResults.Count == 0)
            {
                // Empty state
                ImGui.Dummy(new Vector2(0, 20));
                UiTheme.CenteredText("No puzzles found");
                ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Gray);
                UiTheme.CenteredText("Try adjusting your search terms or filters");
                ImGui.PopStyleColor();
                return;
            }

            // Results header
            ImGui.Text($"Found {searchResults.Count} puzzles");
            ImGui.SameLine();
            
            // Sort info
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Gray);
            ImGui.Text("• Sorted by rating and name");
            ImGui.PopStyleColor();

            ImGui.Spacing();

            // Draw improved table
            DrawPuzzleTable();
        }

        // Improved table layout that fits the existing UI better
        private void DrawPuzzleTable()
        {
            // Apply consistent table styling
            UiTheme.StyleTable();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingStretchProp;

            if (ImGui.BeginTable("SearchResultsTable", 8, flags))
            {
                // Configure columns with better proportions
                ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 200);
                ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthStretch, 120);
                ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 180);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Fav", ImGuiTableColumnFlags.WidthFixed, 40);
                ImGui.TableSetupColumn("Go", ImGuiTableColumnFlags.WidthFixed, 60);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                // Draw each row with improved styling
                for (int i = 0; i < searchResults.Count; i++)
                {
                    var puzzle = searchResults[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // Add row selection for better UX
                    ImGui.PushID(i);
                    ImGui.Selectable($"##row_{i}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
                    ImGui.PopID();

                    // Reset cursor to start of row for the actual content
                    ImGui.TableSetColumnIndex(0);

                    // Rating column with color
                    RenderRatingWithColor(puzzle.Rating);

                    // Puzzle Name
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
                    ImGui.TextWrapped(puzzle.PuzzleName);
                    ImGui.PopStyleColor();

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

                    // Favorite Button
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

                    // Travel Button
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.Primary);
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.PrimaryLight);
                    if (ImGui.Button($"Travel##{puzzle.Id}"))
                    {
                        onTravel(puzzle);
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Travel to {puzzle.World} {puzzle.Address}");
                    ImGui.PopStyleColor(2);
                }

                ImGui.EndTable();
            }

            // End table styling
            UiTheme.EndTableStyle();
        }

        // Helper to render rating with appropriate color
        private void RenderRatingWithColor(string rating)
        {
            Vector4 color = GetRatingColor(rating);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(rating);
            ImGui.PopStyleColor();
        }

        // Get color for rating display
        private Vector4 GetRatingColor(string rating)
        {
            if (rating.Contains("★★★★★")) return new Vector4(1.0f, 0.8f, 0.2f, 1.0f); // Gold
            if (rating.Contains("★★★★")) return new Vector4(0.8f, 0.4f, 1.0f, 1.0f);   // Purple
            if (rating.Contains("★★★")) return new Vector4(0.2f, 0.8f, 1.0f, 1.0f);    // Blue
            if (rating.Contains("★★")) return new Vector4(0.2f, 1.0f, 0.4f, 1.0f);     // Green
            if (rating.Contains("★")) return new Vector4(1.0f, 1.0f, 1.0f, 1.0f);      // White
            return new Vector4(0.7f, 0.7f, 0.7f, 1.0f); // Gray for special ratings
        }

        // Helper to render codes with tooltips
        private void RenderCodesWithTooltips(string codes)
        {
            if (string.IsNullOrEmpty(codes))
            {
                ImGui.Text("-");
                return;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Accent);
            ImGui.Text(codes);
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ShowCodesTooltip(codes);
            }
        }

        // Show tooltip for puzzle type codes
        private void ShowCodesTooltip(string codes)
        {
            ImGui.BeginTooltip();
            ImGui.Text("Puzzle Types:");
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
            }

            ImGui.EndTooltip();
        }

        // Perform search based on current filters
        private void PerformSearch(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            searchResults.Clear();

            // Determine which data centers to search
            var dataCentersToSearch = selectedDataCenter == "All Data Centers"
                ? allData.Keys.ToList()
                : new List<string> { selectedDataCenter };

            foreach (var dc in dataCentersToSearch)
            {
                if (!allData.ContainsKey(dc)) continue;

                var puzzles = allData[dc];

                // Apply filters
                var filteredPuzzles = puzzles.Where(p =>
                {
                    // Rating filter
                    if (selectedRating != "All Ratings" && p.Rating != selectedRating)
                        return false;

                    // World filter
                    if (selectedWorld != "All Worlds" && p.World != selectedWorld)
                        return false;

                    // District filter
                    if (selectedDistrict != "All Districts")
                    {
                        bool containsDistrict = p.Address.Contains(selectedDistrict, StringComparison.OrdinalIgnoreCase);
                        if (!containsDistrict)
                            return false;
                    }

                    // Text search
                    if (!string.IsNullOrWhiteSpace(searchQuery))
                    {
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

            // Sort results by rating (descending) then by name
            searchResults = searchResults
                .OrderByDescending(p => ConvertRatingToSortValue(p.Rating))
                .ThenBy(p => p.PuzzleName)
                .ToList();
        }

        // Helper method to convert rating to numeric value for sorting
        private int ConvertRatingToSortValue(string rating)
        {
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
