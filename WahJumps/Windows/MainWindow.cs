using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Newtonsoft.Json;
using WahJumps.Data;
using WahJumps.Handlers;
using WahJumps.Logging;

namespace WahJumps.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private readonly CsvManager csvManager;
        private readonly LifestreamIpcHandler lifestreamIpcHandler;
        private readonly StrangeHousingTab strangeHousingTab; // Reference to StrangeHousingTab
        private readonly InformationTab informationTab;       // Reference to InformationTab
        private Dictionary<string, List<JumpPuzzleData>> csvDataByDataCenter;
        private List<JumpPuzzleData> favoritePuzzles;
        private string statusMessage;
        private bool isReady;
        private bool isFirstRender = true;
        private DateTime lastRefreshDate;
        private string favoritesFilePath;

        private string globalSearchQuery = string.Empty;  // Stores the search query
        private List<JumpPuzzleData> globalSearchResults = new List<JumpPuzzleData>();  // Stores the search results

        // Fields for the selected filters
        private string selectedServerFilter = "All"; // Default to All servers
        private string selectedRatingFilter = "All"; // Default to All ratings

        public MainWindow(CsvManager csvManager, LifestreamIpcHandler lifestreamIpcHandler)
            : base("WahJumps", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.csvManager = csvManager;
            this.lifestreamIpcHandler = lifestreamIpcHandler;
            strangeHousingTab = new StrangeHousingTab(); // Initialize StrangeHousingTab
            informationTab = new InformationTab();       // Initialize InformationTab
            csvDataByDataCenter = new Dictionary<string, List<JumpPuzzleData>>();
            favoritesFilePath = Path.Combine(csvManager.CsvDirectoryPath, "favorites.json");
            favoritePuzzles = LoadFavorites();
            csvManager.StatusUpdated += OnStatusUpdated;
            csvManager.CsvProcessingCompleted += OnCsvProcessingCompleted;

            statusMessage = "Initializing...";
            isReady = false;

            RefreshData();
        }

        public void ToggleVisibility()
        {
            IsOpen = !IsOpen;
        }

        private void OnStatusUpdated(string message)
        {
            statusMessage = message;
        }

        private void OnCsvProcessingCompleted()
        {
            statusMessage = "Ready";
            isReady = true;
            LoadCsvData();
        }

        public override void Draw()
        {
            if (isFirstRender)
            {
                ImGui.SetWindowSize(new Vector2(1200, 900), ImGuiCond.FirstUseEver);
                isFirstRender = false;
            }

            if (!isReady)
            {
                ImGui.Text($"Status: {statusMessage}");
                return;
            }

            if (ImGui.Button("Refresh Data"))
            {
                RefreshData();
            }

            ImGui.SameLine();
            ImGui.Text($"Last Refreshed: {lastRefreshDate}");

            // Existing tab bar rendering logic
            if (ImGui.BeginTabBar("MainTabBar"))
            {
                strangeHousingTab.Draw(); // Render StrangeHousingTab
                informationTab.Draw();    // Render InformationTab

                if (ImGui.BeginTabItem("Favorites"))
                {
                    DrawFavoriteTab();
                    ImGui.EndTabItem();
                }

                // Global Search Tab
                if (ImGui.BeginTabItem("Global Search"))
                {
                    DrawGlobalSearchTab();
                    ImGui.EndTabItem();
                }

                // Render server tabs with colors
                foreach (var dataCenter in csvDataByDataCenter)
                {
                    // Apply colors based on data center group
                    PushServerColorStyle(dataCenter.Key); // Apply the appropriate color

                    if (ImGui.BeginTabItem(dataCenter.Key))
                    {
                        DrawRatingTabs(dataCenter.Value);
                        ImGui.EndTabItem();
                    }

                    ImGui.PopStyleColor(3); // Restore the original colors
                }

                ImGui.EndTabBar();
            }
        }

        // New method to draw the global search tab with filters
        // Updated method to draw the global search tab without filters
        // Updated method to draw the global search tab with real-time search
        private void DrawGlobalSearchTab()
        {
            // Capture the current query before modifying it
            string previousQuery = globalSearchQuery;

            // Global Search Bar
            ImGui.InputText("Global Search", ref globalSearchQuery, 100);

            // If the query has changed, perform the search
            if (!previousQuery.Equals(globalSearchQuery, StringComparison.Ordinal))
            {
                PerformGlobalSearch(globalSearchQuery); // Perform the search as the user types
            }

            // Display Search Results if any
            if (globalSearchResults.Count > 0)
            {
                ImGui.Text($"Search Results for '{globalSearchQuery}':");

                if (ImGui.BeginTable("SearchResultsTable", 10, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Puzzle Name", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Codes", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("GoalsOrRules", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Add to Favorites", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Travel", ImGuiTableColumnFlags.WidthFixed);

                    ImGui.TableHeadersRow();

                    foreach (var row in globalSearchResults)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Id.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Rating);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.PuzzleName);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Builder);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.World);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Address);

                        ImGui.TableNextColumn();
                        string combinedCodes = CombineCodes(row.M, row.E, row.S, row.P, row.V, row.J, row.G, row.L, row.X);
                        ImGui.Text(combinedCodes);

                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(row.GoalsOrRules);

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Add##{row.Id}"))
                        {
                            AddToFavorites(row);
                        }

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Travel##{row.Id}"))
                        {
                            var travelCommand = FormatTravelCommand(row);
                            lifestreamIpcHandler.ExecuteLiCommand(travelCommand);
                        }
                    }

                    ImGui.EndTable();
                }
            }
            else if (!string.IsNullOrEmpty(globalSearchQuery))
            {
                ImGui.Text("No matching results found.");
            }
        }



        // Perform search with filters
        private void PerformGlobalSearch(string query)
        {
            globalSearchResults.Clear();

            if (string.IsNullOrWhiteSpace(query))
            {
                return;  // No need to search if query is empty or null
            }

            // Search across all data centers
            foreach (var dataCenter in csvDataByDataCenter)
            {
                if (selectedServerFilter != "All" && selectedServerFilter != dataCenter.Key)
                {
                    continue; // Skip this server if it's filtered out
                }

                foreach (var puzzle in dataCenter.Value)
                {
                    // Apply Rating Filter
                    if (selectedRatingFilter != "All" && puzzle.Rating != selectedRatingFilter)
                    {
                        continue; // Skip this puzzle if the rating doesn't match
                    }

                    // Search query match on puzzle name, builder, or world
                    if (puzzle.PuzzleName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        puzzle.World.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        puzzle.Builder.Contains(query, StringComparison.OrdinalIgnoreCase))
                    {
                        globalSearchResults.Add(puzzle);  // Add matching puzzle to the result
                    }
                }
            }
        }

        // Helper method to apply colors based on the server
        private void PushServerColorStyle(string dataCenterKey)
        {
            switch (dataCenterKey.ToLower())
            {
                // NA Blues
                case "aether":
                case "primal":
                case "crystal":
                case "dynamis":
                    ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.098f, 0.608f, 0.8f, 1.0f)); // Dark #189BCC
                    ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.365f, 0.729f, 0.859f, 1.0f)); // Medium #5DBADB
                    ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.639f, 0.843f, 0.922f, 1.0f)); // Light #A3D7EB
                    break;

                // EU Purples
                case "light":
                case "chaos":
                    ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.624f, 0.529f, 0.718f, 1.0f)); // Dark #9F87B7
                    ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.773f, 0.718f, 0.831f, 1.0f)); // Medium #C5B7D4
                    ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.875f, 0.843f, 0.906f, 1.0f)); // Light #DFD7E7
                    break;

                // Materia Yellows
                case "materia":
                    ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(1.0f, 0.764f, 0.509f, 1.0f)); // Dark #FFC382
                    ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.988f, 0.851f, 0.706f, 1.0f)); // Medium #FCD9B4
                    ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.996f, 0.941f, 0.882f, 1.0f)); // Light #FEF0E1
                    break;

                // Japan Reds
                case "elemental":
                case "gaia":
                case "mana":
                case "meteor":
                    ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.847f, 0.42f, 0.467f, 1.0f)); // Dark #D86B77
                    ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.894f, 0.592f, 0.627f, 1.0f)); // Medium #E497A0
                    ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.953f, 0.827f, 0.839f, 1.0f)); // Light #F3D3D6
                    break;

                default:
                    // Default colors if no matching data center is found
                    ImGui.PushStyleColor(ImGuiCol.Tab, ImGui.GetStyle().Colors[(int)ImGuiCol.Tab]);
                    ImGui.PushStyleColor(ImGuiCol.TabHovered, ImGui.GetStyle().Colors[(int)ImGuiCol.TabHovered]);
                    ImGui.PushStyleColor(ImGuiCol.TabActive, ImGui.GetStyle().Colors[(int)ImGuiCol.TabActive]);
                    break;
            }
        }

        private void DrawFavoriteTab()
        {
            if (favoritePuzzles.Count > 0)
            {
                ImGui.BeginChild("FavoritesTable", new Vector2(0, 600), true, ImGuiWindowFlags.HorizontalScrollbar);

                if (ImGui.BeginTable("FavoritesTable", 10, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
                {
                    ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Puzzle Name", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Codes", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("GoalsOrRules", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Remove from Favorites", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Travel", ImGuiTableColumnFlags.WidthFixed);

                    ImGui.TableHeadersRow();

                    foreach (var row in favoritePuzzles)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Id.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Rating);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.PuzzleName);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Builder);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.World);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Address);

                        // Merge Codes (M, E, S, P, V, J, G, L, X) into one column
                        ImGui.TableNextColumn();
                        string combinedCodes = CombineCodes(row.M, row.E, row.S, row.P, row.V, row.J, row.G, row.L, row.X);
                        ImGui.Text(combinedCodes);

                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(row.GoalsOrRules);

                        // Remove from Favorites Button
                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Remove##{row.Id}"))
                        {
                            RemoveFromFavorites(row);
                        }

                        // Travel Button
                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Travel##{row.Id}"))
                        {
                            var travelCommand = FormatTravelCommand(row);
                            lifestreamIpcHandler.ExecuteLiCommand(travelCommand);
                        }
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }
            else
            {
                ImGui.Text("No favorites added yet.");
            }
        }

        private void DrawRatingTabs(List<JumpPuzzleData> csvData)
        {
            var puzzlesByRating = csvData.GroupBy(p => p.Rating);

            if (ImGui.BeginTabBar("RatingTabs"))
            {
                foreach (var ratingGroup in puzzlesByRating)
                {
                    if (ImGui.BeginTabItem($"{ratingGroup.Key}"))
                    {
                        DrawJumpPuzzlesTab(ratingGroup.ToList());
                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }
        }

        private void DrawJumpPuzzlesTab(List<JumpPuzzleData> csvData)
        {
            if (csvData.Count > 0)
            {
                if (ImGui.BeginTable("CSVData", 10, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
                {
                    ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Puzzle Name", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Codes", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Goals/Rules", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Add to Favorites", ImGuiTableColumnFlags.WidthFixed);
                    ImGui.TableSetupColumn("Travel", ImGuiTableColumnFlags.WidthFixed);

                    ImGui.TableHeadersRow();

                    foreach (var row in csvData)
                    {
                        ImGui.TableNextRow();

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Id.ToString());

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Rating);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.PuzzleName);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Builder);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.World);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.Address);

                        // Merge Codes (M, E, S, P, V, J, G, L, X) into one column
                        ImGui.TableNextColumn();
                        string combinedCodes = CombineCodes(row.M, row.E, row.S, row.P, row.V, row.J, row.G, row.L, row.X);
                        ImGui.Text(combinedCodes);

                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(row.GoalsOrRules);

                        // Add to Favorites Button
                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Add##{row.Id}"))
                        {
                            AddToFavorites(row);
                        }

                        // Travel Button
                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Travel##{row.Id}"))
                        {
                            var travelCommand = FormatTravelCommand(row);
                            lifestreamIpcHandler.ExecuteLiCommand(travelCommand);
                        }
                    }

                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.Text("No data available for this rating.");
            }
        }

        // Helper function to combine codes into a single string
        private string CombineCodes(params string[] codes)
        {
            List<string> combinedCodes = new List<string>();

            foreach (var code in codes)
            {
                if (!string.IsNullOrEmpty(code))
                {
                    combinedCodes.Add(code);
                }
            }

            return string.Join(", ", combinedCodes);
        }

        private string FormatTravelCommand(JumpPuzzleData row)
        {
            var world = row.World; // Example: Coeurl
            var address = row.Address; // Example: Goblet Ward 4 Wing 2 Apartment 51

            if (address.Contains("Room"))
            {
                // Handle FC room, just remove the room information
                address = address.Split("Room")[0].Trim();
            }
            else if (address.Contains("Apartment"))
            {
                // Split the address for apartment cases
                var parts = address.Split("Apartment");
                var apartmentPart = parts[1].Trim();  // Extract the apartment number
                address = parts[0].Trim();  // Keep the ward and wing part

                // Handle Wing logic for subdivisions
                if (address.Contains("Wing 2"))
                {
                    // Replace Wing 2 with "subdivision"
                    address = address.Replace("Wing 2", "subdivision").Trim();
                    address = $"{address} Apartment {apartmentPart}";
                }
                else if (address.Contains("Wing 1"))
                {
                    // Remove "Wing 1" (it can be ignored)
                    address = address.Replace("Wing 1", "").Trim();
                    address = $"{address} Apartment {apartmentPart}";
                }
                else
                {
                    // No wings, keep it simple
                    address = $"{address} Apartment {apartmentPart}";
                }
            }

            DisplayTravelMessage(world, address);  // Display travel message

            // Return the formatted command
            return $"/travel {world} {address}";
        }

        private void DisplayTravelMessage(string world, string address)
        {
            var message = $"[WahJumps] Traveling to: {world} {address}";
            Plugin.ChatGui.Print(message);
        }

        public void Dispose()
        {
            csvManager.StatusUpdated -= OnStatusUpdated;
            csvManager.CsvProcessingCompleted -= OnCsvProcessingCompleted;
        }

        private List<JumpPuzzleData> LoadFavorites()
        {
            try
            {
                if (File.Exists(favoritesFilePath))
                {
                    var json = File.ReadAllText(favoritesFilePath);
                    return JsonConvert.DeserializeObject<List<JumpPuzzleData>>(json) ?? new List<JumpPuzzleData>();
                }
            }
            catch (Exception ex)
            {
                CustomLogger.Log($"Error loading favorites: {ex.Message}");
            }

            return new List<JumpPuzzleData>();
        }

        private void SaveFavorites()
        {
            try
            {
                var json = JsonConvert.SerializeObject(favoritePuzzles, Formatting.Indented);
                File.WriteAllText(favoritesFilePath, json);
            }
            catch (Exception ex)
            {
                CustomLogger.Log($"Error saving favorites: {ex.Message}");
            }
        }

        private void AddToFavorites(JumpPuzzleData puzzle)
        {
            if (!favoritePuzzles.Any(p => p.Id == puzzle.Id))
            {
                favoritePuzzles.Add(puzzle);
                SaveFavorites();
            }
        }

        private void RemoveFromFavorites(JumpPuzzleData puzzle)
        {
            favoritePuzzles.RemoveAll(p => p.Id == puzzle.Id);
            SaveFavorites();
        }

        private void LoadCsvData()
        {
            csvDataByDataCenter.Clear();

            var dataCenters = WorldData.GetDataCenterInfo();
            foreach (var dataCenter in dataCenters)
            {
                var filePath = Path.Combine(csvManager.CsvDirectoryPath, $"{dataCenter.CsvName}_cleaned.csv");
                if (File.Exists(filePath))
                {
                    var data = LoadCsvDataFromFile(filePath);
                    if (data != null && data.Count > 0)
                    {
                        csvDataByDataCenter[dataCenter.DataCenter] = data;
                        CustomLogger.Log($"Loaded {data.Count} records for {dataCenter.DataCenter}");

                        lastRefreshDate = File.GetLastWriteTime(filePath);
                    }
                    else
                    {
                        CustomLogger.Log($"No data found for {dataCenter.DataCenter}");
                    }
                }
                else
                {
                    CustomLogger.Log($"CSV file does not exist for {dataCenter.DataCenter}");
                }
            }
        }

        private List<JumpPuzzleData> LoadCsvDataFromFile(string filePath)
        {
            try
            {
                using (var reader = new StreamReader(filePath))
                using (var csv = new CsvHelper.CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<JumpPuzzleData>().ToList();

                    records.Sort((x, y) =>
                    {
                        int ratingComparison = ConvertRatingToInt(y.Rating).CompareTo(ConvertRatingToInt(x.Rating));
                        if (ratingComparison == 0)
                        {
                            return string.Compare(x.World, y.World, StringComparison.Ordinal);
                        }
                        return ratingComparison;
                    });

                    return records;
                }
            }
            catch (Exception ex)
            {
                CustomLogger.Log($"Error loading CSV file: {filePath}, Exception: {ex.Message}");
                return new List<JumpPuzzleData>();
            }
        }

        private int ConvertRatingToInt(string rating)
        {
            if (string.IsNullOrEmpty(rating)) return 0;
            return rating.Length;
        }

        private void RefreshData()
        {
            csvManager.DeleteExistingCsvs();
            csvManager.DownloadAndSaveIndividualCsvsAsync();
            statusMessage = "Refreshing data...";
            isReady = false;
        }
    }
}
