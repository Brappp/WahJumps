// File: WahJumps/Windows/MainWindow.cs
// Status: UPDATED - Colored region tabs

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Newtonsoft.Json;
using WahJumps.Configuration;
using WahJumps.Data;
using WahJumps.Handlers;
using WahJumps.Logging;
using WahJumps.Utilities;
using WahJumps.Windows.Components;

namespace WahJumps.Windows
{
    public class MainWindow : Window, IDisposable
    {
        // Dependencies
        private readonly CsvManager csvManager;
        private readonly LifestreamIpcHandler lifestreamIpcHandler;
        private readonly SettingsManager settingsManager;

        // UI Tabs
        private readonly StrangeHousingTab strangeHousingTab;
        private readonly InformationTab informationTab;
        private readonly SettingsTab settingsTab;
        private readonly SearchFilterComponent searchFilter;
        private readonly TravelDialog travelDialog;

        // Data
        private Dictionary<string, List<JumpPuzzleData>> csvDataByDataCenter;
        private List<JumpPuzzleData> favoritePuzzles;

        // UI State
        private string statusMessage;
        private bool isReady;
        private bool isFirstRender = true;
        private DateTime lastRefreshDate;
        private string favoritesFilePath;
        private int viewMode = 0; // 0=Tabs, 1=Unified Search

        // Tab names for TabBar
        private readonly string[] mainTabs = new[] { "Strange Housing", "Information", "Favorites", "Search", "Settings" };

        // Region grouping for data centers with their representative colors
        private readonly Dictionary<string, (List<string> DataCenters, Vector4 TabColor, Vector4 HoverColor, Vector4 ActiveColor)> regionGroups = new Dictionary<string, (List<string>, Vector4, Vector4, Vector4)>
        {
            { "NA", (new List<string> { "Aether", "Crystal", "Dynamis", "Primal" },
                    new Vector4(0.098f, 0.4f, 0.6f, 1.0f),      // NA Dark Blue
                    new Vector4(0.2f, 0.5f, 0.7f, 1.0f),        // NA Hover Blue
                    new Vector4(0.3f, 0.6f, 0.8f, 1.0f)) },     // NA Active Blue
                    
            { "EU", (new List<string> { "Chaos", "Light" },
                    new Vector4(0.4f, 0.3f, 0.5f, 1.0f),        // EU Dark Purple
                    new Vector4(0.5f, 0.4f, 0.6f, 1.0f),        // EU Hover Purple
                    new Vector4(0.6f, 0.5f, 0.7f, 1.0f)) },     // EU Active Purple
                    
            { "OCE", (new List<string> { "Materia" },
                    new Vector4(0.7f, 0.5f, 0.2f, 1.0f),        // OCE Dark Gold
                    new Vector4(0.8f, 0.6f, 0.3f, 1.0f),        // OCE Hover Gold
                    new Vector4(0.9f, 0.7f, 0.4f, 1.0f)) },     // OCE Active Gold
                    
            { "JP", (new List<string> { "Elemental", "Gaia", "Mana", "Meteor" },
                    new Vector4(0.6f, 0.2f, 0.2f, 1.0f),        // JP Dark Red
                    new Vector4(0.7f, 0.3f, 0.3f, 1.0f),        // JP Hover Red
                    new Vector4(0.8f, 0.4f, 0.4f, 1.0f)) }      // JP Active Red
        };

        public MainWindow(CsvManager csvManager, LifestreamIpcHandler lifestreamIpcHandler)
            : base("Jump Puzzle Directory", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.csvManager = csvManager;
            this.lifestreamIpcHandler = lifestreamIpcHandler;

            // Initialize settings
            settingsManager = new SettingsManager(Plugin.PluginInterface, csvManager.CsvDirectoryPath);
            var config = settingsManager.Configuration;

            // Set initial values from configuration
            viewMode = config.DefaultViewMode;

            // Initialize UI components
            strangeHousingTab = new StrangeHousingTab();
            informationTab = new InformationTab();
            settingsTab = new SettingsTab(settingsManager, csvManager.CsvDirectoryPath, OnSettingsChanged);

            // Initialize data
            csvDataByDataCenter = new Dictionary<string, List<JumpPuzzleData>>();
            favoritesFilePath = Path.Combine(csvManager.CsvDirectoryPath, "favorites.json");
            favoritePuzzles = LoadFavorites();

            // Initialize search component with callbacks
            searchFilter = new SearchFilterComponent(
                IsFavorite,
                AddToFavorites,
                RemoveFromFavorites,
                OnTravelRequest
            );

            // Initialize travel dialog
            travelDialog = new TravelDialog(
                ExecuteTravel,
                () => { } // Empty cancel action
            );

            // Register event handlers
            csvManager.StatusUpdated += OnStatusUpdated;
            csvManager.CsvProcessingCompleted += OnCsvProcessingCompleted;

            // Set initial state
            statusMessage = "Initializing...";
            isReady = false;

            // Apply logging setting
            CustomLogger.IsLoggingEnabled = config.EnableLogging;

            // Load data
            RefreshData();
        }

        private void OnSettingsChanged()
        {
            var config = settingsManager.Configuration;
            CustomLogger.IsLoggingEnabled = config.EnableLogging;
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

            // Update search filter with available data
            searchFilter.SetAvailableData(csvDataByDataCenter);
        }

        public override void Draw()
        {
            // Create a new ID scope to isolate our styling
            ImGui.PushID("WahJumpsPlugin");

            try
            {
                // Apply consistent styling
                UiTheme.ApplyGlobalStyle();

                // First render setup
                if (isFirstRender)
                {
                    ImGui.SetWindowSize(new Vector2(1100, 700), ImGuiCond.FirstUseEver);
                    isFirstRender = false;
                }

                // Loading state
                if (!isReady)
                {
                    DrawLoadingState();
                    return;
                }

                // Draw top toolbar with search and options
                DrawTopToolbar();

                ImGui.Separator();

                // Main content
                if (viewMode == 0)
                {
                    // Tab view mode
                    DrawTabMode();
                }
                else
                {
                    // Unified search mode
                    DrawUnifiedSearchMode();
                }

                // Draw travel dialog (if active)
                travelDialog.Draw();
            }
            finally
            {
                // Clean up styling
                UiTheme.EndGlobalStyle();

                // Always pop the ID scope to ensure styles don't leak
                ImGui.PopID();
            }
        }

        private void DrawLoadingState()
        {
            float centerY = ImGui.GetWindowHeight() * 0.4f;
            ImGui.SetCursorPosY(centerY);

            UiTheme.CenteredText("Loading Jump Puzzle Data", UiTheme.Primary);
            ImGui.Spacing();
            UiTheme.CenteredText(statusMessage);

            // Progress indicator
            float progressWidth = ImGui.GetWindowWidth() * 0.7f;
            float progressX = (ImGui.GetWindowWidth() - progressWidth) * 0.5f;

            ImGui.SetCursorPosX(progressX);
            ImGui.SetCursorPosY(centerY + 50);

            // Animated loading bar
            float progress = (float)Math.Sin(ImGui.GetTime() * 1.5f) * 0.25f + 0.5f;
            ImGui.ProgressBar(progress, new Vector2(progressWidth, 20), "");
        }

        private void DrawTopToolbar()
        {
            // First row: Main controls
            if (ImGui.Button("Refresh Data"))
            {
                RefreshData();
            }

            ImGui.SameLine();
            ImGui.Text($"Last Updated: {lastRefreshDate.ToString("yyyy-MM-dd HH:mm")}");

            ImGui.SameLine(ImGui.GetWindowWidth() - 270);

            string[] modes = new[] { "Tabbed View", "Search View" };
            ImGui.SetNextItemWidth(160);
            if (ImGui.Combo("##viewMode", ref viewMode, modes, modes.Length))
            {
                // Save this preference
                settingsManager.Configuration.DefaultViewMode = viewMode;
                settingsManager.SaveConfiguration();
            }

            ImGui.SameLine();

            // Logging checkbox
            bool enableLogging = settingsManager.Configuration.EnableLogging;
            if (ImGui.Checkbox("Log", ref enableLogging))
            {
                settingsManager.Configuration.EnableLogging = enableLogging;
                CustomLogger.IsLoggingEnabled = enableLogging;
                settingsManager.SaveConfiguration();
            }
        }

        private void DrawTabMode()
        {
            // Add improved tab styling
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(10, 6));
            ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.12f, 0.15f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.2f, 0.4f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.3f, 0.5f, 0.7f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4.0f);

            using var tabBar = new ImRaii.TabBar("MainTabBar", ImGuiTabBarFlags.FittingPolicyScroll);

            if (tabBar.Success)
            {
                // Standard tabs
                strangeHousingTab.Draw();
                informationTab.Draw();

                // Favorites Tab
                bool favTabOpen = true;
                if (ImGui.BeginTabItem("Favorites", ref favTabOpen))
                {
                    DrawFavoritesTab();
                    ImGui.EndTabItem();
                }

                // Search Tab
                bool searchTabOpen = true;
                if (ImGui.BeginTabItem("Search", ref searchTabOpen))
                {
                    searchFilter.Draw(csvDataByDataCenter);
                    ImGui.EndTabItem();
                }

                // Settings Tab
                settingsTab.Draw();

                // Region tabs with nested data center tabs
                DrawRegionTabs();
            }

            ImGui.PopStyleVar(2);
            ImGui.PopStyleColor(3);
        }

        // Method for region-based data center tabs
        private void DrawRegionTabs()
        {
            foreach (var region in regionGroups)
            {
                string regionName = region.Key;
                var regionData = region.Value;

                // Apply region-specific colors
                using var regionColors = new ImRaii.StyleColor(
                    (ImGuiCol.Tab, regionData.TabColor),
                    (ImGuiCol.TabHovered, regionData.HoverColor),
                    (ImGuiCol.TabActive, regionData.ActiveColor)
                );

                bool isRegionOpen = true;
                if (ImGui.BeginTabItem(regionName, ref isRegionOpen))
                {
                    // Create a nested tab bar for this region's data centers
                    using var dcTabBar = new ImRaii.TabBar($"{regionName}DataCenters", ImGuiTabBarFlags.FittingPolicyScroll);

                    if (dcTabBar.Success)
                    {
                        // Draw each data center in this region
                        foreach (string dataCenterName in regionData.DataCenters)
                        {
                            // Only draw if we have data for this data center
                            if (csvDataByDataCenter.ContainsKey(dataCenterName))
                            {
                                DrawDataCenterTab(dataCenterName, csvDataByDataCenter[dataCenterName]);
                            }
                        }
                    }

                    ImGui.EndTabItem();
                }
            }
        }

        private void DrawDataCenterTab(string dataCenterName, List<JumpPuzzleData> puzzles)
        {
            // Apply data center color theming if enabled
            var config = settingsManager.Configuration;

            // Use bool manually to handle tab opening
            bool isOpen = true;

            if (config.ShowDataCenterColors)
            {
                var colors = UiTheme.GetDataCenterColors(dataCenterName);
                using var tabColors = new ImRaii.StyleColor(
                    (ImGuiCol.Tab, colors.Dark),
                    (ImGuiCol.TabHovered, colors.Medium),
                    (ImGuiCol.TabActive, colors.Light)
                );

                if (ImGui.BeginTabItem(dataCenterName, ref isOpen))
                {
                    DrawRatingTabs(puzzles);
                    ImGui.EndTabItem();
                }
            }
            else
            {
                if (ImGui.BeginTabItem(dataCenterName, ref isOpen))
                {
                    DrawRatingTabs(puzzles);
                    ImGui.EndTabItem();
                }
            }
        }

        private void DrawUnifiedSearchMode()
        {
            using var child = new ImRaii.Child("UnifiedSearchView", Vector2.Zero, false);

            searchFilter.Draw(csvDataByDataCenter);
        }

        private void DrawFavoritesTab()
        {
            if (favoritePuzzles.Count > 0)
            {
                using var child = new ImRaii.Child("FavoritesView", new Vector2(0, 0), true);

                DrawPuzzleTable(favoritePuzzles, false);
            }
            else
            {
                UiTheme.CenteredText("No favorites added yet.");
                ImGui.Spacing();
                UiTheme.CenteredText("Browse puzzles and click 'Add' to favorite them.");
            }
        }

        private void DrawRatingTabs(List<JumpPuzzleData> puzzles)
        {
            var puzzlesByRating = puzzles
                .GroupBy(p => p.Rating)
                .OrderByDescending(g => ConvertRatingToInt(g.Key));

            using var tabBar = new ImRaii.TabBar("RatingTabs");
            if (!tabBar.Success) return;

            // Add an "All" tab first - using bool instead of ImRaii.TabItem
            bool allTabOpen = true;
            if (ImGui.BeginTabItem("All", ref allTabOpen))
            {
                DrawPuzzleTable(puzzles);
                ImGui.EndTabItem();
            }

            // Then rating-specific tabs
            foreach (var ratingGroup in puzzlesByRating)
            {
                // Color the tab based on rating
                Vector4 tabColor = GetRatingColor(ratingGroup.Key);

                using var colors = new ImRaii.StyleColor(
                    (ImGuiCol.TabActive, tabColor)
                );

                bool ratingTabOpen = true;
                if (ImGui.BeginTabItem(ratingGroup.Key, ref ratingTabOpen))
                {
                    DrawPuzzleTable(ratingGroup.ToList());
                    ImGui.EndTabItem();
                }
            }
        }

        // Condensed table drawing method
        private void DrawPuzzleTable(List<JumpPuzzleData> puzzles, bool includeAddToFavorites = true)
        {
            if (puzzles.Count == 0)
            {
                UiTheme.CenteredText("No puzzles available.");
                return;
            }

            // Apply consistent table styling
            UiTheme.StyleTable();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingStretchProp;

            if (ImGui.BeginTable("PuzzlesTable", includeAddToFavorites ? 9 : 8, flags))
            {
                // Configure columns, now more condensed
                ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 180);
                ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthStretch, 120);
                ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 160);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Rules", ImGuiTableColumnFlags.WidthStretch, 150);

                if (includeAddToFavorites)
                {
                    ImGui.TableSetupColumn("Fav", ImGuiTableColumnFlags.WidthFixed, 35);
                }

                ImGui.TableSetupColumn("Go", ImGuiTableColumnFlags.WidthFixed, 35);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                // Draw each row
                foreach (var puzzle in puzzles)
                {
                    ImGui.TableNextRow();

                    // Rating
                    ImGui.TableNextColumn();
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
                    if (includeAddToFavorites)
                    {
                        ImGui.TableNextColumn();
                        bool isFav = IsFavorite(puzzle);

                        if (isFav)
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Error);
                            if (ImGui.Button($"♥##{puzzle.Id}"))
                            {
                                RemoveFromFavorites(puzzle);
                            }
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Remove from favorites");
                            ImGui.PopStyleColor();
                        }
                        else
                        {
                            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Success);
                            if (ImGui.Button($"♡##{puzzle.Id}"))
                            {
                                AddToFavorites(puzzle);
                            }
                            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Add to favorites");
                            ImGui.PopStyleColor();
                        }
                    }

                    // Travel Button (compact icon)
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
                    if (ImGui.Button($"→##{puzzle.Id}"))
                    {
                        OnTravelRequest(puzzle);
                    }
                    if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Travel to {puzzle.World} {puzzle.Address}");
                    ImGui.PopStyleColor();
                }

                ImGui.EndTable();
            }

            // End table styling
            UiTheme.EndTableStyle();
        }

        private void RenderRatingWithColor(string rating)
        {
            Vector4 color = GetRatingColor(rating);

            using var textColor = new ImRaii.StyleColor(ImGuiCol.Text, color);
            ImGui.Text(rating);
        }

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

        private Vector4 GetRatingColor(string rating)
        {
            switch (rating)
            {
                case "1★":
                    return new Vector4(0.0f, 0.8f, 0.0f, 1.0f); // Green
                case "2★":
                    return new Vector4(0.0f, 0.6f, 0.9f, 1.0f); // Blue
                case "3★":
                    return new Vector4(0.9f, 0.8f, 0.0f, 1.0f); // Yellow
                case "4★":
                    return new Vector4(1.0f, 0.5f, 0.0f, 1.0f); // Orange
                case "5★":
                    return new Vector4(0.9f, 0.0f, 0.0f, 1.0f); // Red
                case "E":
                    return new Vector4(0.5f, 0.5f, 1.0f, 1.0f); // Light blue
                case "T":
                    return new Vector4(1.0f, 0.5f, 1.0f, 1.0f); // Light purple
                case "F":
                    return new Vector4(0.5f, 1.0f, 0.5f, 1.0f); // Light green
                default:
                    return new Vector4(0.8f, 0.8f, 0.8f, 1.0f); // Gray
            }
        }

        // Check if a puzzle is in favorites
        private bool IsFavorite(JumpPuzzleData puzzle)
        {
            return favoritePuzzles.Any(p => p.Id == puzzle.Id);
        }

        // Add a puzzle to favorites
        private void AddToFavorites(JumpPuzzleData puzzle)
        {
            if (!IsFavorite(puzzle))
            {
                favoritePuzzles.Add(puzzle);
                SaveFavorites();
            }
        }

        // Remove a puzzle from favorites
        private void RemoveFromFavorites(JumpPuzzleData puzzle)
        {
            favoritePuzzles.RemoveAll(p => p.Id == puzzle.Id);
            SaveFavorites();
        }

        // Called when travel button is clicked
        private void OnTravelRequest(JumpPuzzleData puzzle)
        {
            var config = settingsManager.Configuration;

            if (config.ShowTravelConfirmation)
            {
                // Show travel confirmation dialog
                travelDialog.Open(puzzle);
            }
            else
            {
                // Travel directly without confirmation
                string travelCommand = FormatTravelCommand(puzzle);
                ExecuteTravel(travelCommand);
            }
        }

        // Execute the travel command
        private void ExecuteTravel(string travelCommand)
        {
            DisplayTravelMessage(travelCommand);
            lifestreamIpcHandler.ExecuteLiCommand(travelCommand);
        }

        private string FormatTravelCommand(JumpPuzzleData puzzle)
        {
            var world = puzzle.World;
            var address = puzzle.Address;

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

            // Return the formatted command
            return $"/travel {world} {address}";
        }

        private void DisplayTravelMessage(string travelCommand)
        {
            var message = $"[WahJumps] Executing: {travelCommand}";
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

                    // Sort records by rating (descending), then by world (ascending)
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

            // Count stars
            int stars = rating.Count(c => c == '★');
            if (stars > 0) return stars;

            // Special ratings
            switch (rating)
            {
                case "E": return 2; // Training puzzles ranked between 1★ and 2★
                case "T": return 2; // Event puzzles also between 1★ and 2★
                case "F": return 2; // In flux puzzles similarly between 1★ and 2★
                default: return 0;
            }
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
