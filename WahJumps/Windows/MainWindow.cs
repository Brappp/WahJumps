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
        private Dictionary<string, List<JumpPuzzleData>> csvDataByDataCenter;
        private List<JumpPuzzleData> favoritePuzzles;
        private string statusMessage;  // To hold the status message
        private bool isReady;  // To track when all CSVs are ready
        private bool isFirstRender = true; // To check if the window has been rendered before
        private DateTime lastRefreshDate;  // To track the last refresh date
        private string favoritesFilePath;  // Path for storing favorite puzzles

        public MainWindow(CsvManager csvManager)
            : base("WahJumps", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.csvManager = csvManager;
            csvDataByDataCenter = new Dictionary<string, List<JumpPuzzleData>>();
            favoritesFilePath = Path.Combine(csvManager.CsvDirectoryPath, "favorites.json");
            favoritePuzzles = LoadFavorites();
            csvManager.StatusUpdated += OnStatusUpdated;
            csvManager.CsvProcessingCompleted += OnCsvProcessingCompleted;

            statusMessage = "Initializing...";
            isReady = false;

            // Initiate the CSV download and processing
            RefreshData();
        }

        private void OnStatusUpdated(string message)
        {
            statusMessage = message;
        }

        private void OnCsvProcessingCompleted()
        {
            statusMessage = "Ready";  
            isReady = true;  
            // Load CSV data and refresh the UI
            LoadCsvData();
        }

        private void LoadCsvData()
        {
            csvDataByDataCenter.Clear();  // Clear existing data

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

                        // Set last refresh date to the file's last write time
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

                    // Sort the records by Rating and World during the loading phase
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

        public void Dispose()
        {
            csvManager.StatusUpdated -= OnStatusUpdated;
            csvManager.CsvProcessingCompleted -= OnCsvProcessingCompleted;
        }

        public override void Draw()
        {
            // Set the window size only once, during the first render
            if (isFirstRender)
            {
                ImGui.SetWindowSize(new Vector2(1200, 900), ImGuiCond.FirstUseEver);
                isFirstRender = false;  // Set to false so we don't set the size every frame
            }

            if (!isReady)
            {
                ImGui.Text($"Status: {statusMessage}");
                return;
            }

            // Display the Refresh Data button and last refresh date
            if (ImGui.Button("Refresh Data"))
            {
                RefreshData();
            }

            ImGui.SameLine();
            ImGui.Text($"Last Refreshed: {lastRefreshDate}");

            if (ImGui.BeginTabBar("DataCenterTabs"))
            {
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
            // Ensure we have favorite data
            if (favoritePuzzles.Count > 0)
            {
                ImGui.BeginChild("FavoritesTable", new Vector2(0, 600), true, ImGuiWindowFlags.HorizontalScrollbar);

                if (ImGui.BeginTable("FavoritesTable", 18, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable))
                {
                    // Define table headers
                    ImGui.TableSetupColumn("ID");
                    ImGui.TableSetupColumn("Rating");
                    ImGui.TableSetupColumn("Puzzle Name");
                    ImGui.TableSetupColumn("Builder");
                    ImGui.TableSetupColumn("World");
                    ImGui.TableSetupColumn("Address");
                    ImGui.TableSetupColumn("M");
                    ImGui.TableSetupColumn("E");
                    ImGui.TableSetupColumn("S");
                    ImGui.TableSetupColumn("P");
                    ImGui.TableSetupColumn("V");
                    ImGui.TableSetupColumn("J");
                    ImGui.TableSetupColumn("G");
                    ImGui.TableSetupColumn("L");
                    ImGui.TableSetupColumn("X");
                    ImGui.TableSetupColumn("Goals/Rules");
                    ImGui.TableSetupColumn("Remove from Favorites");
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

                        ImGui.TableNextColumn();
                        ImGui.Text(row.M);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.E);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.S);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.P);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.V);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.J);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.G);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.L);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.X);

                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(row.GoalsOrRules);

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Remove##{row.Id}"))
                        {
                            RemoveFromFavorites(row);
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
            // Group the puzzles by rating (★, ★★, etc.)
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
            // Ensure we have data
            if (csvData.Count > 0)
            {
                if (ImGui.BeginTable("CSVData", 18, ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
                {
                    // Define table headers
                    ImGui.TableSetupColumn("ID");
                    ImGui.TableSetupColumn("Rating");
                    ImGui.TableSetupColumn("Puzzle Name");
                    ImGui.TableSetupColumn("Builder");
                    ImGui.TableSetupColumn("World");
                    ImGui.TableSetupColumn("Address");
                    ImGui.TableSetupColumn("M");
                    ImGui.TableSetupColumn("E");
                    ImGui.TableSetupColumn("S");
                    ImGui.TableSetupColumn("P");
                    ImGui.TableSetupColumn("V");
                    ImGui.TableSetupColumn("J");
                    ImGui.TableSetupColumn("G");
                    ImGui.TableSetupColumn("L");
                    ImGui.TableSetupColumn("X");
                    ImGui.TableSetupColumn("Goals/Rules");
                    ImGui.TableSetupColumn("Add to Favorites");
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

                        ImGui.TableNextColumn();
                        ImGui.Text(row.M);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.E);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.S);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.P);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.V);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.J);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.G);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.L);

                        ImGui.TableNextColumn();
                        ImGui.Text(row.X);

                        ImGui.TableNextColumn();
                        ImGui.TextWrapped(row.GoalsOrRules);

                        ImGui.TableNextColumn();
                        if (ImGui.Button($"Add##{row.Id}"))
                        {
                            AddToFavorites(row);
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

        private int ConvertRatingToInt(string rating)
        {
            // Convert star ratings to numbers for sorting (★ = 1 star, ★★ = 2 stars, etc.)
            if (string.IsNullOrEmpty(rating)) return 0;
            return rating.Length; // The length of the string determines the number of stars
        }

        private void RefreshData()
        {
            // Delete old CSVs, then download and process new ones
            csvManager.DeleteExistingCsvs();
            csvManager.DownloadAndSaveIndividualCsvsAsync();
            statusMessage = "Refreshing data...";
            isReady = false;  // Wait until CSVs are processed
        }

        // Use this method to toggle window visibility
        public void ToggleVisibility()
        {
            IsOpen = !IsOpen;
        }
    }
}