// File: WahJumps/Windows/MainWindow.cs
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
        private readonly Plugin plugin;

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
        private int viewMode = 0; // 0=Tabs only
        private float currentProgress = 0f; // Track progress for loading bar

        // Notification system
        private float notificationTimer = 0;
        private string notificationMessage = "";
        private MessageType notificationType = MessageType.Info;

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

        public enum MessageType { Info, Success, Warning, Error }

        public MainWindow(CsvManager csvManager, LifestreamIpcHandler lifestreamIpcHandler, Plugin plugin)
            : base("Jump Puzzle Directory", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.csvManager = csvManager;
            this.lifestreamIpcHandler = lifestreamIpcHandler;
            this.plugin = plugin;

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
            csvManager.ProgressUpdated += OnProgressUpdated;
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

        private void OnProgressUpdated(float progress)
        {
            currentProgress = progress;
        }

        private void OnCsvProcessingCompleted()
        {
            statusMessage = "Ready";
            isReady = true;
            LoadCsvData();

            // Update search filter with available data
            searchFilter.SetAvailableData(csvDataByDataCenter);

            // Show success notification
            ShowNotification("Data loading completed successfully!", MessageType.Success);
        }

        public override void Draw()
        {
            // Create a new ID scope to isolate our styling - ensures no style leakage
            ImGui.PushID("WahJumpsPlugin");

            try
            {
                // Apply consistent styling
                UiTheme.ApplyGlobalStyle();

                // Draw window chrome (header and border)
                DrawWindowChrome();

                // First render setup
                if (isFirstRender)
                {
                    ImGui.SetWindowSize(new Vector2(1100, 700), ImGuiCond.FirstUseEver);
                    isFirstRender = false;
                }

                // Loading state
                if (!isReady)
                {
                    DrawAnimatedLoadingState();
                    return;
                }

                // Draw header banner
                DrawHeaderBanner();

                // Draw top toolbar with search and options
                DrawTopToolbar();

                ImGui.Separator();

                // We've removed the view mode dropdown, so now always use tab mode
                DrawTabMode();

                // Draw travel dialog (if active)
                travelDialog.Draw();

                // Draw any active notifications
                DrawNotifications();
            }
            finally
            {
                // Clean up styling
                UiTheme.EndGlobalStyle();

                // Pop the ID scope
                ImGui.PopID();
            }
        }

        private void DrawWindowChrome()
        {
            // Draw a subtle window border
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            // Subtle gradient header
            drawList.AddRectFilledMultiColor(
                windowPos,
                new Vector2(windowPos.X + windowSize.X, windowPos.Y + 4),
                ImGui.GetColorU32(UiTheme.Primary),
                ImGui.GetColorU32(UiTheme.PrimaryLight),
                ImGui.GetColorU32(UiTheme.PrimaryLight),
                ImGui.GetColorU32(UiTheme.Primary)
            );

            // Subtle window border
            drawList.AddRect(
                windowPos,
                new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y),
                ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.35f, 0.5f)),
                0,
                ImDrawFlags.None,
                1.0f
            );
        }

        private void DrawHeaderBanner()
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            float width = ImGui.GetWindowWidth();

            // Subtle gradient background
            drawList.AddRectFilledMultiColor(
                new Vector2(pos.X, pos.Y),
                new Vector2(pos.X + width, pos.Y + 40),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.15f, 1.0f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.2f, 0.25f, 1.0f)),
                ImGui.GetColorU32(new Vector4(0.15f, 0.2f, 0.25f, 1.0f)),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.15f, 1.0f))
            );

            // Add logo text with slight glow
            string appName = "WahJumps";
            float textWidth = ImGui.CalcTextSize(appName).X;
            Vector2 textPos = new Vector2(pos.X + 15, pos.Y + 10);

            // Text shadow/glow
            drawList.AddText(
                new Vector2(textPos.X + 1, textPos.Y + 1),
                ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.5f)),
                appName
            );

            // Main text
            drawList.AddText(
                textPos,
                ImGui.GetColorU32(UiTheme.Primary),
                appName
            );

            // Version text
            string version = "v1.0.1";
            Vector2 versionSize = ImGui.CalcTextSize(version);
            drawList.AddText(
                new Vector2(pos.X + width - versionSize.X - 15, pos.Y + 15),
                ImGui.GetColorU32(new Vector4(0.6f, 0.6f, 0.6f, 1.0f)),
                version
            );

            // Advance cursor past the header
            ImGui.Dummy(new Vector2(0, 40));
        }

        private void DrawAnimatedLoadingState()
        {
            float centerY = ImGui.GetWindowHeight() * 0.4f;
            ImGui.SetCursorPosY(centerY);

            // Draw a professional looking heading
            UiTheme.CenteredText("Loading Jump Puzzle Data", UiTheme.Primary);
            ImGui.Spacing();

            // Draw a pulsing status message
            float pulseValue = (float)Math.Sin(ImGui.GetTime() * 2) * 0.1f + 0.9f;
            Vector4 pulsingColor = new Vector4(0.8f, 0.8f, 0.8f, pulseValue);

            ImGui.PushStyleColor(ImGuiCol.Text, pulsingColor);
            UiTheme.CenteredText(statusMessage);
            ImGui.PopStyleColor();

            // Draw a professional progress bar
            float progressWidth = ImGui.GetWindowWidth() * 0.7f;
            float progressX = (ImGui.GetWindowWidth() - progressWidth) * 0.5f;

            ImGui.SetCursorPosX(progressX);
            ImGui.SetCursorPosY(centerY + 50);

            // Drawing a more professional looking progress bar
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();

            // Progress bar background
            drawList.AddRectFilled(
                pos,
                new Vector2(pos.X + progressWidth, pos.Y + 20),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)),
                4.0f
            );

            if (currentProgress > 0)
            {
                // Actual progress with gradient
                float width = progressWidth * Math.Clamp(currentProgress, 0, 1);
                drawList.AddRectFilledMultiColor(
                    pos,
                    new Vector2(pos.X + width, pos.Y + 20),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.4f, 0.8f, 1.0f)),
                    ImGui.GetColorU32(new Vector4(0.2f, 0.5f, 0.9f, 1.0f)),
                    ImGui.GetColorU32(new Vector4(0.2f, 0.5f, 0.9f, 1.0f)),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.4f, 0.8f, 1.0f))
                );

                // Percentage text
                string percentText = $"{(int)(currentProgress * 100)}%";
                var textSize = ImGui.CalcTextSize(percentText);
                drawList.AddText(
                    new Vector2(
                        pos.X + (progressWidth - textSize.X) * 0.5f,
                        pos.Y + (20 - textSize.Y) * 0.5f
                    ),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 1)),
                    percentText
                );
            }

            // Advance cursor
            ImGui.Dummy(new Vector2(progressWidth, 25));

            // Add a nicer looking spinner
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - 20) * 0.5f);
            ImGui.SetCursorPosY(centerY + 90);
            DrawSpinningLoader(UiTheme.Primary);
        }

        private void DrawSpinningLoader(Vector4 color, float radius = 15.0f)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 center = new Vector2(pos.X + radius, pos.Y + radius);
            float time = (float)ImGui.GetTime() * 1.8f;

            // Glowing background
            drawList.AddCircleFilled(
                center,
                radius * 0.6f,
                ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.1f))
            );

            // Spinning dots
            int numDots = 8;
            for (int i = 0; i < numDots; i++)
            {
                float rads = time + i * 2 * MathF.PI / numDots;
                float x = center.X + MathF.Cos(rads) * radius;
                float y = center.Y + MathF.Sin(rads) * radius;

                // Size and opacity vary with position
                float dotSize = 2.0f + 2.0f * ((i + (int)(time * 1.5f)) % numDots) / (float)numDots;
                float alpha = 0.2f + 0.8f * ((i + (int)(time * 1.5f)) % numDots) / (float)numDots;

                drawList.AddCircleFilled(
                    new Vector2(x, y),
                    dotSize,
                    ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, alpha))
                );
            }

            // Dummy to advance cursor
            ImGui.Dummy(new Vector2(radius * 2, radius * 2));
        }

        private void DrawTopToolbar()
        {
            // Apply professional button styling
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);

            // Refresh button with icon
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.18f, 0.35f, 0.58f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.45f, 0.68f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.30f, 0.50f, 1.0f));

            if (ImGui.Button("Refresh Data"))
            {
                RefreshData();
                ShowNotification("Refreshing data...", MessageType.Info);
            }
            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.18f, 0.18f, 0.22f, 0.95f));
                ImGui.BeginTooltip();
                ImGui.Text("Refresh puzzle data from source");
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
            ImGui.Text($"Last Updated: {lastRefreshDate.ToString("yyyy-MM-dd HH:mm")}");

            // Add Timer button (right-aligned)
            ImGui.SameLine(ImGui.GetWindowWidth() - 80);

            // Use a better looking Timer button with a matching icon
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.7f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.35f, 0.55f, 1.0f));

            if (ImGui.Button("Timer"))
            {
                // Open the timer window
                plugin.TimerWindow.ShowTimer();
            }
            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.18f, 0.18f, 0.22f, 0.95f));
                ImGui.BeginTooltip();
                ImGui.Text("Open the timer window for speedruns");
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
            }

            ImGui.PopStyleVar(2);
        }

        private void DrawTabMode()
        {
            // Apply professional tab styling
            ApplyProfessionalTabStyling();

            using var tabBar = new ImRaii.TabBar("MainTabBar", ImGuiTabBarFlags.FittingPolicyScroll);

            if (tabBar.Success)
            {
                // Standard tabs
                strangeHousingTab.Draw();
                informationTab.Draw();

                // Favorites Tab - no boolean reference = no close button
                if (ImGui.BeginTabItem("Favorites"))
                {
                    DrawFavoritesTab();
                    ImGui.EndTabItem();
                }

                // Search Tab - no boolean reference = no close button
                if (ImGui.BeginTabItem("Search"))
                {
                    searchFilter.Draw(csvDataByDataCenter);
                    ImGui.EndTabItem();
                }

                // Settings Tab
                settingsTab.Draw();

                // Region tabs with nested data center tabs
                DrawRegionTabs();
            }

            EndProfessionalTabStyling();
        }

        private void ApplyProfessionalTabStyling()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 8));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemInnerSpacing, new Vector2(8, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4.0f);

            // Better tab colors for more professional look
            ImGui.PushStyleColor(ImGuiCol.Tab, new Vector4(0.14f, 0.16f, 0.22f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabHovered, new Vector4(0.22f, 0.24f, 0.32f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabActive, new Vector4(0.18f, 0.30f, 0.45f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabUnfocused, new Vector4(0.13f, 0.15f, 0.18f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TabUnfocusedActive, new Vector4(0.16f, 0.22f, 0.30f, 1.0f));
        }

        private void EndProfessionalTabStyling()
        {
            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar(3);
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

                // No boolean reference = no close button
                if (ImGui.BeginTabItem(regionName))
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

            if (config.ShowDataCenterColors)
            {
                var colors = UiTheme.GetDataCenterColors(dataCenterName);
                using var tabColors = new ImRaii.StyleColor(
                    (ImGuiCol.Tab, colors.Dark),
                    (ImGuiCol.TabHovered, colors.Medium),
                    (ImGuiCol.TabActive, colors.Light)
                );

                // No boolean reference = no close button
                if (ImGui.BeginTabItem(dataCenterName))
                {
                    DrawRatingTabs(puzzles);
                    ImGui.EndTabItem();
                }
            }
            else
            {
                // No boolean reference = no close button
                if (ImGui.BeginTabItem(dataCenterName))
                {
                    DrawRatingTabs(puzzles);
                    ImGui.EndTabItem();
                }
            }
        }

        private void DrawFavoritesTab()
        {
            // Show favorites if any exist
            if (favoritePuzzles.Count > 0)
            {
                using var child = new ImRaii.Child("FavoritesView", new Vector2(0, 0), true);

                // Draw the favorites table with a special column for unfavorite buttons
                DrawFavoritesTable(favoritePuzzles);
            }
            else
            {
                // No favorites message with a nice icon
                float centerY = ImGui.GetContentRegionAvail().Y * 0.4f;
                ImGui.SetCursorPosY(centerY);

                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 1.0f));
                UiTheme.CenteredText("♡");
                ImGui.SetWindowFontScale(1.5f);
                UiTheme.CenteredText("No favorites added yet");
                ImGui.SetWindowFontScale(1.0f);
                ImGui.Spacing();
                UiTheme.CenteredText("Browse puzzles and click 'Add' to favorite them");
                ImGui.PopStyleColor();
            }
        }

        private void DrawRatingTabs(List<JumpPuzzleData> puzzles)
        {
            var puzzlesByRating = puzzles
                .GroupBy(p => p.Rating)
                .OrderByDescending(g => ConvertRatingToInt(g.Key));

            using var tabBar = new ImRaii.TabBar("RatingTabs");
            if (!tabBar.Success) return;

            // Add an "All" tab first - no boolean reference = no close button
            if (ImGui.BeginTabItem("All"))
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

                // No boolean reference = no close button
                if (ImGui.BeginTabItem(ratingGroup.Key))
                {
                    DrawPuzzleTable(ratingGroup.ToList());
                    ImGui.EndTabItem();
                }
            }
        }

        // New method specifically for drawing the favorites table with unfavorite buttons
        private void DrawFavoritesTable(List<JumpPuzzleData> puzzles)
        {
            if (puzzles.Count == 0)
            {
                UiTheme.CenteredText("No puzzles available.");
                return;
            }

            // Apply professional table styling
            ApplyProfessionalTableStyling();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingStretchProp;

            if (ImGui.BeginTable("FavoritesTable", 9, flags))
            {
                // Configure columns with improved widths
                ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 200);
                ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthStretch, 130);
                ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 180);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Rules", ImGuiTableColumnFlags.WidthStretch, 170);
                ImGui.TableSetupColumn("Unfavorite", ImGuiTableColumnFlags.WidthFixed, 70); // New column specifically for unfavorite
                ImGui.TableSetupColumn("Go", ImGuiTableColumnFlags.WidthFixed, 40);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                // Draw each row
                for (int i = 0; i < puzzles.Count; i++)
                {
                    var puzzle = puzzles[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // Use Selectable to highlight the row
                    ImGui.PushID(i);
                    ImGui.Selectable($"##row_{i}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
                    ImGui.PopID();

                    // Reset cursor to start of row for the actual content
                    ImGui.TableSetColumnIndex(0);

                    // Rating with color
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

                    // Unfavorite Button
                    ImGui.TableNextColumn();
                    ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.7f, 0.1f, 0.1f, 1.0f)); // Red button
                    ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.8f, 0.2f, 0.2f, 1.0f));

                    if (ImGui.Button($"Remove##{puzzle.Id}"))
                    {
                        RemoveFromFavorites(puzzle);
                        ShowNotification("Puzzle removed from favorites", MessageType.Info);
                    }
                    ImGui.PopStyleColor(2);

                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Remove from favorites");

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

            // End professional table styling
            EndProfessionalTableStyling();
        }

        private void ApplyProfessionalTableStyling()
        {
            // Create a scoped ID to prevent style leakage
            ImGui.PushID("WahJumpsTableStyling");

            // More refined table styling
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));

            // More professional header with subtle gradient feel
            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.12f, 0.25f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.3f, 0.3f, 0.35f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.2f, 0.2f, 0.25f, 1.0f));

            // More subtle alternating row colors
            ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.16f, 0.16f, 0.18f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.20f, 0.20f, 0.22f, 1.0f));

            // Better hover effects
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, new Vector4(0.25f, 0.35f, 0.5f, 0.5f));
            ImGui.PushStyleColor(ImGuiCol.Header, new Vector4(0.2f, 0.3f, 0.45f, 0.35f));
        }

        private void EndProfessionalTableStyling()
        {
            ImGui.PopStyleColor(7);
            ImGui.PopStyleVar(2);
            ImGui.PopID();
        }

        // Updated table drawing method with professional styling
        private void DrawPuzzleTable(List<JumpPuzzleData> puzzles, bool includeAddToFavorites = true)
        {
            if (puzzles.Count == 0)
            {
                UiTheme.CenteredText("No puzzles available.");
                return;
            }

            // Apply professional table styling
            ApplyProfessionalTableStyling();

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingStretchProp;

            // Reduced column count (removed Timer column)
            int columnCount = includeAddToFavorites ? 9 : 8;

            if (ImGui.BeginTable("PuzzlesTable", columnCount, flags))
            {
                // Configure columns with improved widths
                ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch, 200); // Increased width
                ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthStretch, 130);
                ImGui.TableSetupColumn("World", ImGuiTableColumnFlags.WidthFixed, 70);
                ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch, 180);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Rules", ImGuiTableColumnFlags.WidthStretch, 170);

                if (includeAddToFavorites)
                {
                    ImGui.TableSetupColumn("Fav", ImGuiTableColumnFlags.WidthFixed, 40);
                }

                ImGui.TableSetupColumn("Go", ImGuiTableColumnFlags.WidthFixed, 40);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                // Draw each row
                for (int i = 0; i < puzzles.Count; i++)
                {
                    var puzzle = puzzles[i];
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    // Use Selectable to highlight the row - SpanAllColumns makes it cover the whole row
                    ImGui.PushID(i);
                    ImGui.Selectable($"##row_{i}", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
                    ImGui.PopID();

                    // Reset cursor to start of row for the actual content
                    ImGui.TableSetColumnIndex(0);

                    // Vertically centered rating with aligned text
                    float cellHeight = ImGui.GetTextLineHeightWithSpacing();
                    float cellY = ImGui.GetCursorPosY();
                    float textWidth = ImGui.CalcTextSize(puzzle.Rating).X;
                    float columnWidth = ImGui.GetColumnWidth();
                    float columnX = ImGui.GetCursorPosX();

                    // Center horizontally
                    ImGui.SetCursorPosX(columnX + (columnWidth - textWidth) * 0.5f);

                    // Now render with color
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
                                ShowNotification("Puzzle removed from favorites", MessageType.Info);
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
                                ShowNotification("Puzzle added to favorites", MessageType.Success);
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

            // End professional table styling
            EndProfessionalTableStyling();
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
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.18f, 0.18f, 0.22f, 0.95f));
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
                ImGui.PopStyleColor();
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

        private void ShowNotification(string message, MessageType type, float duration = 3.0f)
        {
            notificationMessage = message;
            notificationType = type;
            notificationTimer = duration;
        }

        private void DrawNotifications()
        {
            if (notificationTimer > 0)
            {
                notificationTimer -= ImGui.GetIO().DeltaTime;

                // Calculate fade in/out
                float alpha = 1.0f;
                if (notificationTimer < 0.5f)
                {
                    alpha = notificationTimer / 0.5f;
                }

                // Draw notification
                Vector2 windowSize = ImGui.GetWindowSize();
                Vector2 notificationSize = new Vector2(300, 40);
                Vector2 position = new Vector2(
                    (windowSize.X - notificationSize.X) * 0.5f,
                    windowSize.Y - notificationSize.Y - 10
                );

                ImDrawListPtr drawList = ImGui.GetWindowDrawList();
                Vector2 windowPos = ImGui.GetWindowPos();

                // Notification background
                Vector4 bgColor;
                switch (notificationType)
                {
                    case MessageType.Success:
                        bgColor = new Vector4(0.0f, 0.5f, 0.0f, 0.8f * alpha);
                        break;
                    case MessageType.Warning:
                        bgColor = new Vector4(0.9f, 0.6f, 0.0f, 0.8f * alpha);
                        break;
                    case MessageType.Error:
                        bgColor = new Vector4(0.8f, 0.0f, 0.0f, 0.8f * alpha);
                        break;
                    default: // Info
                        bgColor = new Vector4(0.1f, 0.4f, 0.7f, 0.8f * alpha);
                        break;
                }

                // Draw background with rounded corners
                drawList.AddRectFilled(
                    new Vector2(windowPos.X + position.X, windowPos.Y + position.Y),
                    new Vector2(windowPos.X + position.X + notificationSize.X, windowPos.Y + position.Y + notificationSize.Y),
                    ImGui.GetColorU32(bgColor),
                    8.0f
                );

                // Draw message text
                Vector2 textSize = ImGui.CalcTextSize(notificationMessage);
                drawList.AddText(
                    new Vector2(
                        windowPos.X + position.X + (notificationSize.X - textSize.X) * 0.5f,
                        windowPos.Y + position.Y + (notificationSize.Y - textSize.Y) * 0.5f
                    ),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, alpha)),
                    notificationMessage
                );
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
            ShowNotification("Travel command executed", MessageType.Success);
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
            csvManager.ProgressUpdated -= OnProgressUpdated;
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
                ShowNotification("Error saving favorites", MessageType.Error);
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
                ShowNotification($"Error loading data: {ex.Message}", MessageType.Error);
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
            currentProgress = 0f;
            isReady = false;
        }

        // Add a method to access the CSV data for use by the TimerWindow
        public Dictionary<string, List<JumpPuzzleData>> GetCsvDataByDataCenter()
        {
            return csvDataByDataCenter;
        }
    }
}
