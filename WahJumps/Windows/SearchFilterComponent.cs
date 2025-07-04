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

        public void ResetFilters()
        {
            searchQuery = string.Empty;
            selectedRating = "All Ratings";
            selectedDataCenter = "All Data Centers";
            selectedWorld = "All Worlds";
            selectedDistrict = "All Districts";
        }

        public void Draw(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            DrawCompactFilterSection();

            ImGui.Separator();

            PerformSearch(allData);
            DrawResults();
        }

        private void DrawCompactFilterSection()
        {
            if (allPuzzles.Count > 0)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Gray);
                ImGui.Text($"Showing {searchResults.Count} of {allPuzzles.Count} puzzles");
                ImGui.PopStyleColor();
                ImGui.Spacing();
            }

            float searchWidth = ImGui.GetContentRegionAvail().X * 0.6f;
            float ratingWidth = ImGui.GetContentRegionAvail().X * 0.35f;

            ImGui.SetNextItemWidth(searchWidth);
            ImGui.InputTextWithHint("##search", "🔍 Search by name, builder, world, or description...", ref searchQuery, 256);
            
            ImGui.SameLine();
            
            ImGui.SetNextItemWidth(ratingWidth);
            if (ImGui.BeginCombo("##rating", selectedRating))
            {
                string[] allRatings = { "All Ratings", "★★★★★", "★★★★", "★★★", "★★", "★", "Training ☆", "Event ☆", "In Flux ☆", "Temp ☆" };
                foreach (var rating in allRatings)
                {
                    bool isSelected = selectedRating == rating;
                    if (ImGui.Selectable(rating, isSelected))
                    {
                        selectedRating = rating;
                    }
                    if (isSelected) ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            float filterWidth = (ImGui.GetContentRegionAvail().X - 20) / 3;

            ImGui.SetNextItemWidth(filterWidth);
            if (ImGui.BeginCombo("##datacenter", selectedDataCenter))
            {
                foreach (var dc in dataCenters)
                {
                    bool isSelected = selectedDataCenter == dc;
                    if (ImGui.Selectable(dc, isSelected))
                    {
                        selectedDataCenter = dc;
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

            ImGui.SetNextItemWidth(filterWidth);
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
                    : availableWorlds.Skip(1);

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

            ImGui.SetNextItemWidth(filterWidth);
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

            ImGui.Text("Quick Rating:");
            ImGui.SameLine();

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

            ImGui.Spacing();
            if (ImGui.Button("Reset All Filters"))
            {
                ResetFilters();
            }

            ImGui.Spacing();
        }

        private void DrawResults()
        {
            if (searchResults.Count == 0)
            {
                ImGui.Dummy(new Vector2(0, 20));
                UiTheme.CenteredText("No puzzles found");
                ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Gray);
                UiTheme.CenteredText("Try adjusting your search terms or filters");
                ImGui.PopStyleColor();
                return;
            }

            ImGui.Text($"Found {searchResults.Count} puzzles");
            ImGui.SameLine();
            
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Gray);
            ImGui.Text("• Sorted by rating and name");
            ImGui.PopStyleColor();

            ImGui.Spacing();

            DrawPuzzleTable();
        }

        private void DrawPuzzleTable()
        {
            using var table = UiHelpers.BeginPuzzleTable("SearchResultsTable");
            if (table.Success)
            {
                for (int i = 0; i < searchResults.Count; i++)
                {
                    UiHelpers.DrawPuzzleTableRow(searchResults[i], i, isFavorite, addToFavorites, removeFromFavorites, onTravel);
                }
            }
        }

        private void PerformSearch(Dictionary<string, List<JumpPuzzleData>> allData)
        {
            searchResults.Clear();

            var dataCentersToSearch = selectedDataCenter == "All Data Centers"
                ? allData.Keys.ToList()
                : new List<string> { selectedDataCenter };

            foreach (var dc in dataCentersToSearch)
            {
                if (!allData.ContainsKey(dc)) continue;

                var puzzles = allData[dc];

                var filteredPuzzles = puzzles.Where(p =>
                {
                    if (selectedRating != "All Ratings" && p.Rating != selectedRating)
                        return false;

                    if (selectedWorld != "All Worlds" && p.World != selectedWorld)
                        return false;

                    if (selectedDistrict != "All Districts")
                    {
                        bool containsDistrict = p.Address.Contains(selectedDistrict, StringComparison.OrdinalIgnoreCase);
                        if (!containsDistrict)
                            return false;
                    }

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
