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

            if (ImGui.BeginTabBar("MainTabBar"))
            {
                strangeHousingTab.Draw(); // Render StrangeHousingTab
                informationTab.Draw();    // Render InformationTab

                if (ImGui.BeginTabItem("Favorites"))
                {
                    DrawFavoriteTab();
                    ImGui.EndTabItem();
                }

                foreach (var dataCenter in csvDataByDataCenter)
                {
                    if (ImGui.BeginTabItem(dataCenter.Key))
                    {
                        DrawRatingTabs(dataCenter.Value);
                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
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
                    ImGui.TableSetupColumn("Puzzle Name"    , ImGuiTableColumnFlags.WidthFixed);
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
