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
        private float currentProgress = 0f;

        // Notification system
        private float notificationTimer = 0;
        private string notificationMessage = "";
        private MessageType notificationType = MessageType.Info;

        // Region grouping for data centers with their representative colors
        private readonly Dictionary<string, (List<string> DataCenters, Vector4 TabColor, Vector4 HoverColor, Vector4 ActiveColor)> regionGroups = new Dictionary<string, (List<string>, Vector4, Vector4, Vector4)>
        {
            {
                "NA",
                (
                    new List<string> { "Aether", "Crystal", "Dynamis", "Primal" },
                    new Vector4(0.098f, 0.4f, 0.6f, 1.0f),      // NA Dark Blue
                    new Vector4(0.2f, 0.5f, 0.7f, 1.0f),        // NA Hover Blue
                    new Vector4(0.3f, 0.6f, 0.8f, 1.0f)         // NA Active Blue
                )
            },
            {
                "EU",
                (
                    new List<string> { "Chaos", "Light" },
                    new Vector4(0.4f, 0.3f, 0.5f, 1.0f),        // EU Dark Purple
                    new Vector4(0.5f, 0.4f, 0.6f, 1.0f),        // EU Hover Purple
                    new Vector4(0.6f, 0.5f, 0.7f, 1.0f)         // EU Active Purple
                )
            },
            {
                "OCE",
                (
                    new List<string> { "Materia" },
                    new Vector4(0.7f, 0.5f, 0.2f, 1.0f),        // OCE Dark Gold
                    new Vector4(0.8f, 0.6f, 0.3f, 1.0f),        // OCE Hover Gold
                    new Vector4(0.9f, 0.7f, 0.4f, 1.0f)         // OCE Active Gold
                )
            },
            {
                "JP",
                (
                    new List<string> { "Elemental", "Gaia", "Mana", "Meteor" },
                    new Vector4(0.6f, 0.2f, 0.2f, 1.0f),        // JP Dark Red
                    new Vector4(0.7f, 0.3f, 0.3f, 1.0f),        // JP Hover Red
                    new Vector4(0.8f, 0.4f, 0.4f, 1.0f)         // JP Active Red
                )
            }
        };

        public enum MessageType { Info, Success, Warning, Error }

        public MainWindow(CsvManager csvManager, LifestreamIpcHandler lifestreamIpcHandler, Plugin plugin)
            : base("Jump Puzzle Directory", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.csvManager = csvManager;
            this.lifestreamIpcHandler = lifestreamIpcHandler;
            this.plugin = plugin;

            settingsManager = new SettingsManager(Plugin.PluginInterface, csvManager.CsvDirectoryPath);
            var config = settingsManager.Configuration;

            viewMode = config.DefaultViewMode;

            strangeHousingTab = new StrangeHousingTab();
            informationTab = new InformationTab();

            csvDataByDataCenter = new Dictionary<string, List<JumpPuzzleData>>();
            favoritesFilePath = Path.Combine(csvManager.CsvDirectoryPath, "favorites.json");
            favoritePuzzles = LoadFavorites();

            searchFilter = new SearchFilterComponent(
                IsFavorite,
                AddToFavorites,
                RemoveFromFavorites,
                OnTravelRequest
            );

            travelDialog = new TravelDialog(
                ExecuteTravel,
                () => { }
            );

            csvManager.StatusUpdated += OnStatusUpdated;
            csvManager.ProgressUpdated += OnProgressUpdated;
            csvManager.CsvProcessingCompleted += OnCsvProcessingCompleted;

            statusMessage = "Initializing...";
            isReady = false;

            CustomLogger.IsLoggingEnabled = config.EnableLogging;

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

        private void OnProgressUpdated(float progress)
        {
            currentProgress = progress;
        }

        private void OnCsvProcessingCompleted()
        {
            statusMessage = "Ready";
            isReady = true;
            LoadCsvData();

            searchFilter.SetAvailableData(csvDataByDataCenter);

            ShowNotification("Data loading completed successfully!", MessageType.Success);
        }

        public override void Draw()
        {
            ImGui.PushID("WahJumpsPlugin");

            try
            {
                UiTheme.ApplyGlobalStyle();

                DrawWindowChrome();

                if (isFirstRender)
                {
                    ImGui.SetWindowSize(new Vector2(1100, 700), ImGuiCond.FirstUseEver);
                    isFirstRender = false;
                }

                if (!isReady)
                {
                    DrawAnimatedLoadingState();
                    return;
                }

                DrawTopToolbar();

                ImGui.Separator();

                DrawTabMode();

                travelDialog.Draw();

                DrawNotifications();
            }
            finally
            {
                UiTheme.EndGlobalStyle();

                ImGui.PopID();
            }
        }

        private void DrawWindowChrome()
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 windowPos = ImGui.GetWindowPos();
            Vector2 windowSize = ImGui.GetWindowSize();

            drawList.AddRectFilledMultiColor(
                windowPos,
                new Vector2(windowPos.X + windowSize.X, windowPos.Y + 4),
                ImGui.GetColorU32(UiTheme.Primary),
                ImGui.GetColorU32(UiTheme.PrimaryLight),
                ImGui.GetColorU32(UiTheme.PrimaryLight),
                ImGui.GetColorU32(UiTheme.Primary)
            );

            drawList.AddRect(
                windowPos,
                new Vector2(windowPos.X + windowSize.X, windowPos.Y + windowSize.Y),
                ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.35f, 0.5f)),
                0,
                ImDrawFlags.None,
                1.0f
            );
        }

        private void DrawAnimatedLoadingState()
        {
            float centerY = ImGui.GetWindowHeight() * 0.4f;
            ImGui.SetCursorPosY(centerY);

            UiTheme.CenteredText("Loading Jump Puzzle Data", UiTheme.Primary);
            ImGui.Spacing();

            float pulseValue = (float)Math.Sin(ImGui.GetTime() * 2) * 0.1f + 0.9f;
            Vector4 pulsingColor = new Vector4(0.8f, 0.8f, 0.8f, pulseValue);

            ImGui.PushStyleColor(ImGuiCol.Text, pulsingColor);
            UiTheme.CenteredText(statusMessage);
            ImGui.PopStyleColor();

            float progressWidth = ImGui.GetWindowWidth() * 0.7f;
            float progressX = (ImGui.GetWindowWidth() - progressWidth) * 0.5f;

            ImGui.SetCursorPosX(progressX);
            ImGui.SetCursorPosY(centerY + 50);

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();

            drawList.AddRectFilled(
                pos,
                new Vector2(pos.X + progressWidth, pos.Y + 20),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)),
                4.0f
            );

            if (currentProgress > 0)
            {
                float width = progressWidth * Math.Clamp(currentProgress, 0, 1);
                drawList.AddRectFilledMultiColor(
                    pos,
                    new Vector2(pos.X + width, pos.Y + 20),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.4f, 0.8f, 1.0f)),
                    ImGui.GetColorU32(new Vector4(0.2f, 0.5f, 0.9f, 1.0f)),
                    ImGui.GetColorU32(new Vector4(0.2f, 0.5f, 0.9f, 1.0f)),
                    ImGui.GetColorU32(new Vector4(0.0f, 0.4f, 0.8f, 1.0f))
                );

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

            ImGui.Dummy(new Vector2(progressWidth, 25));

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

            drawList.AddCircleFilled(
                center,
                radius * 0.6f,
                ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, 0.1f))
            );

            int numDots = 8;
            for (int i = 0; i < numDots; i++)
            {
                float rads = time + i * 2 * MathF.PI / numDots;
                float x = center.X + MathF.Cos(rads) * radius;
                float y = center.Y + MathF.Sin(rads) * radius;

                float dotSize = 2.0f + 2.0f * ((i + (int)(time * 1.5f)) % numDots) / (float)numDots;
                float alpha = 0.2f + 0.8f * ((i + (int)(time * 1.5f)) % numDots) / (float)numDots;

                drawList.AddCircleFilled(
                    new Vector2(x, y),
                    dotSize,
                    ImGui.GetColorU32(new Vector4(color.X, color.Y, color.Z, alpha))
                );
            }

            ImGui.Dummy(new Vector2(radius * 2, radius * 2));
        }

        private void DrawTopToolbar()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(8, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);

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

            var config = settingsManager.Configuration;
            bool showTravelConfirmation = config.ShowTravelConfirmation;
            
            Vector4 toggleColor = showTravelConfirmation 
                ? new Vector4(0.3f, 0.3f, 0.3f, 1.0f)  // Dark gray when enabled
                : new Vector4(0.2f, 0.2f, 0.2f, 1.0f); // Darker gray when disabled
            
            ImGui.PushStyleColor(ImGuiCol.Button, toggleColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(toggleColor.X + 0.1f, toggleColor.Y + 0.1f, toggleColor.Z + 0.1f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(toggleColor.X - 0.1f, toggleColor.Y - 0.1f, toggleColor.Z - 0.1f, 1.0f));

            string confirmText = showTravelConfirmation ? "Confirm: ON" : "Confirm: OFF";
            if (ImGui.Button(confirmText))
            {
                config.ShowTravelConfirmation = !config.ShowTravelConfirmation;
                settingsManager.SaveConfiguration();
                string status = config.ShowTravelConfirmation ? "enabled" : "disabled";
                ShowNotification($"Travel confirmation {status}", MessageType.Info);
            }
            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.18f, 0.18f, 0.22f, 0.95f));
                ImGui.BeginTooltip();
                ImGui.Text("Toggle travel confirmation dialog");
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.25f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.35f, 0.35f, 0.35f, 1.0f));

            if (ImGui.Button("GitHub"))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "https://github.com/Brappp/WahJumps",
                    UseShellExecute = true
                });
            }
            ImGui.PopStyleColor(3);

            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.18f, 0.18f, 0.22f, 0.95f));
                ImGui.BeginTooltip();
                ImGui.Text("Open WahJumps on GitHub");
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
            ImGui.Text($"Last Updated: {lastRefreshDate.ToString("yyyy-MM-dd HH:mm")}");

            ImGui.SameLine(ImGui.GetWindowWidth() - 80);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.7f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.35f, 0.55f, 1.0f));

            if (ImGui.Button("Timer"))
            {
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
            ApplyProfessionalTabStyling();

            using var tabBar = new ImRaii.TabBar("MainTabBar", ImGuiTabBarFlags.FittingPolicyScroll);

            if (tabBar.Success)
            {
                strangeHousingTab.Draw();
                informationTab.Draw();

                if (ImGui.BeginTabItem("Favorites"))
                {
                    DrawFavoritesTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Search"))
                {
                    searchFilter.Draw(csvDataByDataCenter);
                    ImGui.EndTabItem();
                }

                // Data Center Comparison Tab - new addition
                if (ImGui.BeginTabItem("DC Overview"))
                {
                    DrawDataCenterComparison();
                    ImGui.EndTabItem();
                }

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
            // Create tab name with puzzle count
            string tabName = $"{dataCenterName} ({puzzles.Count})";
            
            // Color coding based on data center size
            Vector4 sizeIndicatorColor = puzzles.Count switch
            {
                < 10 => new Vector4(0.6f, 0.6f, 0.6f, 1.0f),    // Gray for small (< 10)
                < 50 => new Vector4(0.7f, 0.6f, 0.5f, 1.0f),    // Soft brown for medium (10-49)
                < 100 => new Vector4(0.4f, 0.8f, 0.8f, 1.0f),   // Cyan for large (50-99)
                _ => new Vector4(0.4f, 0.8f, 0.4f, 1.0f)         // Green for very large (100+)
            };

            // Apply data center color theming if enabled
            var config = settingsManager.Configuration;

            if (config.ShowDataCenterColors)
            {
                var colors = UiTheme.GetDataCenterColors(dataCenterName);
                using var tabColors = new ImRaii.StyleColor(
                    (ImGuiCol.Tab, colors.Dark),
                    (ImGuiCol.TabHovered, colors.Medium),
                    (ImGuiCol.TabActive, colors.Light),
                    (ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)) // White text
                );

                // No boolean reference = no close button
                if (ImGui.BeginTabItem(tabName))
                {
                    DrawRatingTabs(puzzles);
                    ImGui.EndTabItem();
                }
            }
            else
            {
                using var textColor = new ImRaii.StyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 1.0f, 1.0f)); // White text
                
                // No boolean reference = no close button
                if (ImGui.BeginTabItem(tabName))
                {
                    DrawRatingTabs(puzzles);
                    ImGui.EndTabItem();
                }
            }
        }

        private void DrawDataCenterComparison()
        {
            ImGui.Text("Data Center Statistics & Distribution");
            ImGui.Separator();
            
            if (csvDataByDataCenter.Count == 0)
            {
                UiTheme.CenteredText("No data loaded yet. Please wait for data to load or refresh.");
                return;
            }
            
            // Sort by puzzle count (descending)
            var sortedDCs = csvDataByDataCenter
                .OrderByDescending(dc => dc.Value.Count)
                .ToList();
            
            // Calculate totals for percentages
            var totalPuzzles = csvDataByDataCenter.Values.Sum(v => v.Count);
            
            // Draw summary statistics at the top
            DrawSummaryStatistics(totalPuzzles);
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            
            // Create a single scrollable area for all tables
            using var child = new ImRaii.Child("DCOverviewScrollArea", new Vector2(0, 0), true);
            
            // Apply consistent table styling
            UiTheme.StyleTable();
            
            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.SizingFixedFit |
                                   ImGuiTableFlags.Sortable;

            if (ImGui.BeginTable("DCComparison", 11, flags))
            {
                ImGui.TableSetupColumn("Region");
                ImGui.TableSetupColumn("Data Center");
                ImGui.TableSetupColumn("Total");
                ImGui.TableSetupColumn("Worlds");
                ImGui.TableSetupColumn("★★★★★");
                ImGui.TableSetupColumn("★★★★");
                ImGui.TableSetupColumn("★★★");
                ImGui.TableSetupColumn("★★");
                ImGui.TableSetupColumn("★");
                ImGui.TableSetupColumn("Special");
                ImGui.TableSetupColumn("Distribution", ImGuiTableColumnFlags.WidthStretch);
                
                ImGui.TableHeadersRow();
                
                foreach (var dc in sortedDCs)
                {
                    var ratings = dc.Value.GroupBy(p => p.Rating)
                        .ToDictionary(g => g.Key, g => g.Count());
                    
                    var percentage = (float)dc.Value.Count / totalPuzzles * 100f;
                    
                    // Calculate additional stats
                    var worldCount = dc.Value.Select(p => p.World).Distinct().Count();
                    var specialCount = GetSpecialPuzzleCount(dc.Value);
                    
                    ImGui.TableNextRow();
                    
                    // Region
                    ImGui.TableNextColumn();
                    var region = GetRegionForDataCenter(dc.Key);
                    ImGui.Text(region);
                    
                    // Data Center name
                    ImGui.TableNextColumn();
                    ImGui.Text(dc.Key);
                    
                    // Total count
                    ImGui.TableNextColumn();
                    ImGui.Text(dc.Value.Count.ToString());
                    
                    // World count
                    ImGui.TableNextColumn();
                    ImGui.Text(worldCount.ToString());
                    
                    // Rating columns
                    ImGui.TableNextColumn();
                    var fiveStarCount = ratings.GetValueOrDefault("★★★★★", 0);
                    if (fiveStarCount > 0)
                    {
                        ImGui.Text(fiveStarCount.ToString());
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                    
                    ImGui.TableNextColumn();
                    var fourStarCount = ratings.GetValueOrDefault("★★★★", 0);
                    if (fourStarCount > 0)
                    {
                        ImGui.Text(fourStarCount.ToString());
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                    
                    ImGui.TableNextColumn();
                    var threeStarCount = ratings.GetValueOrDefault("★★★", 0);
                    if (threeStarCount > 0)
                    {
                        ImGui.Text(threeStarCount.ToString());
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                    
                    ImGui.TableNextColumn();
                    var twoStarCount = ratings.GetValueOrDefault("★★", 0);
                    if (twoStarCount > 0)
                    {
                        ImGui.Text(twoStarCount.ToString());
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                    
                    ImGui.TableNextColumn();
                    var oneStarCount = ratings.GetValueOrDefault("★", 0);
                    if (oneStarCount > 0)
                    {
                        ImGui.Text(oneStarCount.ToString());
                    }
                    else
                    {
                        ImGui.Text("-");
                    }
                    
                    // Special puzzles count
                    ImGui.TableNextColumn();
                    ImGui.Text(specialCount.ToString());
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.BeginTooltip();
                        DrawSpecialPuzzleBreakdown(dc.Value);
                        ImGui.EndTooltip();
                    }
                    
                    // Mini bar chart with percentage
                    ImGui.TableNextColumn();
                    DrawMiniBarChartWithPercentage(ratings, dc.Value.Count, totalPuzzles, percentage);
                }
                
                ImGui.EndTable();
            }

            UiTheme.EndTableStyle();
            
            // Top Builders section
            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            DrawTopBuildersTable(sortedDCs);
        }

        private string GetRegionForDataCenter(string dataCenterName)
        {
            foreach (var region in regionGroups)
            {
                if (region.Value.DataCenters.Contains(dataCenterName))
                {
                    return region.Key;
                }
            }
            return "Unknown";
        }

        private void DrawTopBuildersTable(List<KeyValuePair<string, List<JumpPuzzleData>>> sortedDCs)
        {
            // Make the header more prominent
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            ImGui.Text("Top 5 Builders by Data Center");
            ImGui.PopStyleColor();
            ImGui.Separator();
            
            UiTheme.StyleTable();
            
            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.SizingFixedFit;

            if (ImGui.BeginTable("TopBuilders", 8, flags))
            {
                ImGui.TableSetupColumn("Region");
                ImGui.TableSetupColumn("Data Center");
                ImGui.TableSetupColumn("1st Place");
                ImGui.TableSetupColumn("2nd Place");
                ImGui.TableSetupColumn("3rd Place");
                ImGui.TableSetupColumn("4th Place");
                ImGui.TableSetupColumn("5th Place");
                ImGui.TableSetupColumn("Total Builders", ImGuiTableColumnFlags.WidthStretch);
                
                ImGui.TableHeadersRow();
                
                foreach (var dc in sortedDCs)
                {
                    var builderStats = dc.Value
                        .Where(p => !string.IsNullOrEmpty(p.Builder))
                        .GroupBy(p => p.Builder)
                        .OrderByDescending(g => g.Count())
                        .Take(5)
                        .ToList();
                    
                    var totalBuilders = dc.Value
                        .Where(p => !string.IsNullOrEmpty(p.Builder))
                        .Select(p => p.Builder)
                        .Distinct()
                        .Count();
                    
                    ImGui.TableNextRow();
                    
                    // Region
                    ImGui.TableNextColumn();
                    var region = GetRegionForDataCenter(dc.Key);
                    ImGui.Text(region);
                    
                    // Data Center name
                    ImGui.TableNextColumn();
                    ImGui.Text(dc.Key);
                    
                    // Top 5 builders
                    for (int i = 0; i < 5; i++)
                    {
                        ImGui.TableNextColumn();
                        if (i < builderStats.Count)
                        {
                            var builder = builderStats[i];
                            var name = builder.Key;
                            var count = builder.Count();
                            var avgDiff = CalculateAverageDifficulty(builder.ToList());
                            
                            // Truncate long names
                            if (name.Length > 15)
                                name = name.Substring(0, 12) + "...";
                            
                            ImGui.Text($"{name} ({count})");
                            
                            if (ImGui.IsItemHovered())
                            {
                                ImGui.BeginTooltip();
                                ImGui.Text($"Builder: {builder.Key}");
                                ImGui.Text($"Puzzles: {count}");
                                ImGui.Text($"Avg Difficulty: {avgDiff:F1}★");
                                ImGui.EndTooltip();
                            }
                        }
                        else
                        {
                            ImGui.Text("-");
                        }
                    }
                    
                    // Total builders
                    ImGui.TableNextColumn();
                    ImGui.Text(totalBuilders.ToString());
                }
                
                ImGui.EndTable();
            }

            UiTheme.EndTableStyle();
        }

        private void DrawMiniBarChartWithPercentage(Dictionary<string, int> ratings, int totalForDC, int grandTotal, float percentage)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            float barWidth = ImGui.GetColumnWidth() - 10;
            float barHeight = 20;
            
            // Background
            drawList.AddRectFilled(
                pos,
                new Vector2(pos.X + barWidth, pos.Y + barHeight),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)),
                2.0f
            );
            
            // Filled portion based on percentage of total
            float fillWidth = barWidth * (percentage / 100f);
            if (fillWidth > 0)
            {
                // Color based on data center size
                Vector4 fillColor = totalForDC switch
                {
                    < 10 => new Vector4(0.6f, 0.6f, 0.6f, 0.8f),    // Gray
                    < 50 => new Vector4(0.7f, 0.6f, 0.5f, 0.8f),    // Soft brown for medium (10-49)
                    < 100 => new Vector4(0.4f, 0.8f, 0.8f, 0.8f),   // Cyan for large (50-99)
                    _ => new Vector4(0.4f, 0.8f, 0.4f, 0.8f)         // Green for very large (100+)
                };
                
                drawList.AddRectFilled(
                    pos,
                    new Vector2(pos.X + fillWidth, pos.Y + barHeight),
                    ImGui.GetColorU32(fillColor),
                    2.0f
                );
            }
            
            // Text overlay with percentage
            string text = $"{percentage:F1}%";
            var textSize = ImGui.CalcTextSize(text);
            if (textSize.X < barWidth)
            {
                drawList.AddText(
                    new Vector2(
                        pos.X + (barWidth - textSize.X) * 0.5f,
                        pos.Y + (barHeight - textSize.Y) * 0.5f
                    ),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 1)),
                    text
                );
            }
            
            ImGui.Dummy(new Vector2(barWidth, barHeight));
        }

        private float CalculateAverageDifficulty(List<JumpPuzzleData> puzzles)
        {
            if (puzzles.Count == 0) return 0f;
            
            float totalDifficulty = 0f;
            int validRatings = 0;
            
            foreach (var puzzle in puzzles)
            {
                int difficulty = ConvertRatingToInt(puzzle.Rating);
                if (difficulty > 0)
                {
                    totalDifficulty += difficulty;
                    validRatings++;
                }
            }
            
            return validRatings > 0 ? totalDifficulty / validRatings : 0f;
        }

        private string GetTopBuilder(List<JumpPuzzleData> puzzles)
        {
            if (puzzles.Count == 0) return "-";
            
            var builderCounts = puzzles
                .Where(p => !string.IsNullOrEmpty(p.Builder))
                .GroupBy(p => p.Builder)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault();
            
            if (builderCounts == null) return "-";
            
            var count = builderCounts.Count();
            var name = builderCounts.Key;
            
            // Truncate long names
            if (name.Length > 12)
                name = name.Substring(0, 9) + "...";
                
            return $"{name} ({count})";
        }

        private int GetSpecialPuzzleCount(List<JumpPuzzleData> puzzles)
        {
            return puzzles.Count(p => 
                p.Rating == "E" || p.Rating == "T" || p.Rating == "F" || p.Rating == "P" ||
                !string.IsNullOrEmpty(p.M) || !string.IsNullOrEmpty(p.E) || 
                !string.IsNullOrEmpty(p.S) || !string.IsNullOrEmpty(p.P) ||
                !string.IsNullOrEmpty(p.V) || !string.IsNullOrEmpty(p.J) ||
                !string.IsNullOrEmpty(p.G) || !string.IsNullOrEmpty(p.L) ||
                !string.IsNullOrEmpty(p.X));
        }

        private void DrawSpecialPuzzleBreakdown(List<JumpPuzzleData> puzzles)
        {
            ImGui.Text("Special Puzzle Types:");
            ImGui.Separator();
            
            var specialTypes = new Dictionary<string, int>
            {
                ["Event (E)"] = puzzles.Count(p => p.Rating == "E"),
                ["Temp (T)"] = puzzles.Count(p => p.Rating == "T"),
                ["In Flux (F)"] = puzzles.Count(p => p.Rating == "F"),
                ["Training (P)"] = puzzles.Count(p => p.Rating == "P"),
                ["Mystery (M)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.M)),
                ["Emote (E)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.E)),
                ["Speed (S)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.S)),
                ["Phasing (P)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.P)),
                ["Void Jump (V)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.V)),
                ["Job Gate (J)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.J)),
                ["Ghost (G)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.G)),
                ["Logic (L)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.L)),
                ["No Media (X)"] = puzzles.Count(p => !string.IsNullOrEmpty(p.X))
            };
            
            foreach (var type in specialTypes.Where(t => t.Value > 0).OrderByDescending(t => t.Value))
            {
                ImGui.Text($"{type.Key}: {type.Value}");
            }
        }

        private void DrawSummaryStatistics(int totalPuzzles)
        {
            // Enhanced summary statistics
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            ImGui.Text($"Summary: {totalPuzzles:N0} total puzzles across {csvDataByDataCenter.Count} data centers");
            ImGui.PopStyleColor();
            
            ImGui.Spacing();
            
            // Show region breakdown
            var regionTotals = new Dictionary<string, int>();
            foreach (var dc in csvDataByDataCenter)
            {
                var region = GetRegionForDataCenter(dc.Key);
                regionTotals[region] = regionTotals.GetValueOrDefault(region, 0) + dc.Value.Count;
            }
            
            ImGui.Text("By Region: ");
            ImGui.SameLine();
            bool first = true;
            foreach (var region in regionTotals.OrderByDescending(r => r.Value))
            {
                if (!first) 
                {
                    ImGui.SameLine();
                    ImGui.Text(" | ");
                    ImGui.SameLine();
                }
                
                ImGui.Text($"{region.Key}: {region.Value}");
                
                first = false;
            }
            
            ImGui.Spacing();
            
            // Global statistics
            var allPuzzles = csvDataByDataCenter.Values.SelectMany(v => v).ToList();
            
            // Total unique builders
            var uniqueBuilders = allPuzzles
                .Where(p => !string.IsNullOrEmpty(p.Builder))
                .Select(p => p.Builder)
                .Distinct()
                .Count();
            
            // Total unique worlds
            var uniqueWorlds = allPuzzles
                .Select(p => p.World)
                .Distinct()
                .Count();
            
            // Global average difficulty
            var globalAvgDiff = CalculateAverageDifficulty(allPuzzles);
            
            // Most common special mechanics
            var mechanicCounts = new Dictionary<string, int>
            {
                ["Mystery"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.M)),
                ["Emote"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.E)),
                ["Speed"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.S)),
                ["Phasing"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.P)),
                ["Void Jump"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.V)),
                ["Job Gate"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.J)),
                ["Ghost"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.G)),
                ["Logic"] = allPuzzles.Count(p => !string.IsNullOrEmpty(p.L))
            };
            
            var topMechanic = mechanicCounts.OrderByDescending(m => m.Value).FirstOrDefault();
            
            ImGui.Text($"Unique Builders: {uniqueBuilders:N0} | Unique Worlds: {uniqueWorlds:N0} | Global Avg Difficulty: {globalAvgDiff:F1}★");
            if (topMechanic.Value > 0)
            {
                ImGui.Text($"Most Common Mechanic: {topMechanic.Key} ({topMechanic.Value} puzzles)");
            }
        }

        public void Dispose()
        {
            csvManager.StatusUpdated -= OnStatusUpdated;
            csvManager.ProgressUpdated -= OnProgressUpdated;
            csvManager.CsvProcessingCompleted -= OnCsvProcessingCompleted;
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

        // Add a method to access the configuration for debug command
        public PluginConfiguration GetConfiguration()
        {
            return settingsManager.Configuration;
        }

        private void DrawDataCenterInfo(string dataCenterName, List<JumpPuzzleData> puzzles)
        {
            // Quick stats for this data center
            var ratingCounts = puzzles.GroupBy(p => p.Rating)
                .ToDictionary(g => g.Key, g => g.Count());
            
            var worldCounts = puzzles.GroupBy(p => p.World)
                .ToDictionary(g => g.Key, g => g.Count());

            ImGui.Text($"Data Center Overview: {puzzles.Count} total puzzles");
            ImGui.SameLine();
            
            // Show rating distribution with white text
            var ratings = new[] { "★★★★★", "★★★★", "★★★", "★★", "★" };
            bool first = true;
            foreach (var rating in ratings)
            {
                if (ratingCounts.ContainsKey(rating) && ratingCounts[rating] > 0)
                {
                    if (!first) ImGui.SameLine();
                    
                    // Use white text instead of colored text
                    ImGui.Text($"{rating}: {ratingCounts[rating]}");
                    
                    first = false;
                }
            }
            
            ImGui.Separator();
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

            // Add an "All" tab first with total count
            if (ImGui.BeginTabItem($"All ({puzzles.Count})"))
            {
                DrawPuzzleTable(puzzles);
                ImGui.EndTabItem();
            }

            // Then rating-specific tabs with counts (no special coloring)
            foreach (var ratingGroup in puzzlesByRating)
            {
                string tabName = $"{ratingGroup.Key} ({ratingGroup.Count()})";

                // No boolean reference = no close button
                if (ImGui.BeginTabItem(tabName))
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
            
            // Apply consistent table styling
            UiTheme.StyleTable();
            
            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                   ImGuiTableFlags.Borders |
                                   ImGuiTableFlags.Resizable |
                                   ImGuiTableFlags.ScrollY |
                                   ImGuiTableFlags.SizingFixedFit |
                                   ImGuiTableFlags.Sortable;

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
                    UiHelpers.RenderRatingWithColor(puzzle.Rating);
                    
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
                    UiHelpers.RenderCodesWithTooltips(combinedCodes);

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

            // End table styling
            UiTheme.EndTableStyle();
        }

        // Updated table drawing method with professional styling
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
                                   ImGuiTableFlags.SizingFixedFit |
                                   ImGuiTableFlags.Sortable;

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
                    
                    // Vertically and horizontally centered rating
                    float rowHeight = ImGui.GetTextLineHeightWithSpacing();
                    float textHeight = ImGui.GetTextLineHeight();
                    float cellY = ImGui.GetCursorPosY();
                    float textWidth = ImGui.CalcTextSize(puzzle.Rating).X;
                    float columnWidth = ImGui.GetColumnWidth();
                    float columnX = ImGui.GetCursorPosX();
                            
                    // Center horizontally
                    ImGui.SetCursorPosX(columnX + (columnWidth - textWidth) * 0.5f);
                    
                    // Center vertically - move cursor up to align to top of cell
                    ImGui.SetCursorPosY(cellY + (rowHeight - textHeight) * 0.1f);

                    // Now render with color
                    UiHelpers.RenderRatingWithColor(puzzle.Rating);

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
                    UiHelpers.RenderCodesWithTooltips(combinedCodes);

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

            // End table styling
            UiTheme.EndTableStyle();
        }


    }
}
