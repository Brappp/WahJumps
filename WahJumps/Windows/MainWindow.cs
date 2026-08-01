using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using Dalamud.Bindings.ImGui;
using Newtonsoft.Json;
using WahJumps.Configuration;
using WahJumps.Data;
using WahJumps.Handlers;
using WahJumps.Logging;
using WahJumps.Models;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class MainWindow : Window, IDisposable
    {
        private const string ViewAll = "all";
        private const string ViewFavorites = "favorites";
        private const string ViewOverview = "overview";
        private const string ViewInformation = "info";
        private const string ViewCommunity = "community";
        private const string ViewDcPrefix = "dc:";

        private const float SidebarWidth = 150f;
        private const string AllWorlds = "All Worlds";
        private const string AllDistricts = "All Districts";

        private static readonly string[] Districts = { AllDistricts, "Mist", "The Goblet", "The Lavender Beds", "Empyreum", "Shirogane" };
        private static readonly string[] RatingChipKeys = { "All", "★★★★★", "★★★★", "★★★", "★★", "★", "Special" };

        private static readonly (string Name, Vector4 Color, string[] DataCenters)[] Regions =
        {
            ("NA", new Vector4(0.35f, 0.62f, 0.85f, 1.0f), new[] { "Aether", "Crystal", "Dynamis", "Primal" }),
            ("EU", new Vector4(0.65f, 0.55f, 0.80f, 1.0f), new[] { "Chaos", "Light" }),
            ("OCE", new Vector4(0.85f, 0.68f, 0.38f, 1.0f), new[] { "Materia" }),
            ("JP", new Vector4(0.85f, 0.48f, 0.48f, 1.0f), new[] { "Elemental", "Gaia", "Mana", "Meteor" }),
        };

        public enum MessageType { Info, Success, Warning, Error }

        private readonly CsvManager csvManager;
        private readonly LifestreamIpcHandler lifestreamIpcHandler;
        private readonly SettingsManager settingsManager;

        private readonly StrangeHousingTab strangeHousingTab;
        private readonly InformationTab informationTab;
        private readonly OverviewTab overviewTab;

        private readonly Dictionary<string, List<JumpPuzzleData>> csvDataByDataCenter = new();
        private readonly Dictionary<string, List<string>> worldsByDataCenter = new();
        private List<string> allWorlds = new();
        private List<JumpPuzzleData> favoritePuzzles;
        private readonly string favoritesFilePath;
        private DateTime lastRefreshDate;
        private int totalPuzzleCount;
        private int uniqueBuilderCount;

        private string statusMessage;
        private bool isReady;
        private volatile bool dataReloadPending;
        private float currentProgress;

        private string selectedView;
        private bool sidebarVisible;
        private string ratingFilter = "All";
        private string searchQuery = string.Empty;
        private string worldFilter = AllWorlds;
        private string districtFilter = AllDistricts;

        private int dataVersion;
        private int favoritesVersion;
        private string? cachedBaseKey;
        private List<JumpPuzzleData> cachedBaseRows = new();
        private string? cachedVisibleKey;
        private List<JumpPuzzleData> cachedVisibleRows = new();
        private string? cachedWidthsKey;
        private float[]? cachedColumnWidths;
        private float toolStripWidth;

        private string notificationMessage = string.Empty;
        private MessageType notificationType = MessageType.Info;
        private DateTime notificationExpiry = DateTime.MinValue;

        public MainWindow(CsvManager csvManager, LifestreamIpcHandler lifestreamIpcHandler)
            : base("Jump Puzzle Directory", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
        {
            this.csvManager = csvManager;
            this.lifestreamIpcHandler = lifestreamIpcHandler;

            Size = new Vector2(760, 460);
            SizeCondition = ImGuiCond.FirstUseEver;
            SizeConstraints = new WindowSizeConstraints
            {
                MinimumSize = new Vector2(640, 400),
                MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
            };

            settingsManager = new SettingsManager(Plugin.PluginInterface);
            var config = settingsManager.Configuration;

            selectedView = string.IsNullOrEmpty(config.LastSelectedView) ? ViewAll : config.LastSelectedView;
            sidebarVisible = !config.SidebarHidden;

            strangeHousingTab = new StrangeHousingTab(
                () => lifestreamIpcHandler.IsAvailable,
                () => (totalPuzzleCount, uniqueBuilderCount, allWorlds.Count));
            informationTab = new InformationTab();
            overviewTab = new OverviewTab(() => csvDataByDataCenter, () => totalPuzzleCount, GetRegionForDataCenter);

            favoritesFilePath = Path.Combine(csvManager.CsvDirectoryPath, "favorites.json");
            favoritePuzzles = LoadFavorites();

            csvManager.StatusUpdated += OnStatusUpdated;
            csvManager.ProgressUpdated += OnProgressUpdated;
            csvManager.CsvProcessingCompleted += OnCsvProcessingCompleted;

            statusMessage = "Initializing...";
            isReady = false;

            CustomLogger.IsLoggingEnabled = config.EnableLogging;

            RefreshData();
        }

        public void Dispose()
        {
            csvManager.StatusUpdated -= OnStatusUpdated;
            csvManager.ProgressUpdated -= OnProgressUpdated;
            csvManager.CsvProcessingCompleted -= OnCsvProcessingCompleted;

            settingsManager.SaveConfiguration();
        }

        public void ToggleVisibility() => IsOpen = !IsOpen;

        public PluginConfiguration GetConfiguration() => settingsManager.Configuration;

        public override void Draw()
        {
            ImGui.PushID("WahJumpsPlugin");

            try
            {
                UiTheme.ApplyGlobalStyle();

                if (dataReloadPending)
                {
                    dataReloadPending = false;
                    LoadCsvData();
                    OnDataLoaded();
                }

                if (!isReady)
                {
                    DrawLoadingState();
                    return;
                }

                float statusHeight = ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y + 2;

                if (sidebarVisible)
                {
                    DrawSidebar(statusHeight);
                    ImGui.SameLine();
                }

                using (new ImRaii.Child("MainPane", new Vector2(0, -statusHeight)))
                {
                    DrawControlRow();
                    ImGui.Separator();
                    DrawContent();
                }

                DrawStatusBar();
            }
            finally
            {
                UiTheme.EndGlobalStyle();
                ImGui.PopID();
            }
        }

        private void DrawLoadingState()
        {
            ImGui.SetCursorPosY(ImGui.GetWindowHeight() * 0.35f);

            UiTheme.CenteredText("Loading jump puzzle data", UiTheme.Primary);
            UiTheme.CenteredText(statusMessage);
            ImGui.Spacing();

            float barWidth = ImGui.GetWindowWidth() * 0.6f;
            ImGui.SetCursorPosX((ImGui.GetWindowWidth() - barWidth) * 0.5f);
            ImGui.ProgressBar(currentProgress, new Vector2(barWidth, 18), $"{(int)(currentProgress * 100)}%");
        }

        private void DrawSidebar(float statusHeight)
        {
            using var sidebarBg = new ImRaii.StyleColor(ImGuiCol.ChildBg, UiTheme.SidebarBg);
            using var child = new ImRaii.Child("Sidebar", new Vector2(SidebarWidth, -statusHeight), true);
            using var spacing = new ImRaii.StyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 3));

            DrawToolStrip();
            ImGui.Separator();

            using (new ImRaii.StyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f))
            using (new ImRaii.StyleColor(
                (ImGuiCol.FrameBg, UiTheme.SearchBg),
                (ImGuiCol.Border, UiTheme.SearchBorder)))
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - ImGui.GetFrameHeight() - 10);
                ImGui.InputTextWithHint("##puzzleSearch", "Search...", ref searchQuery, 256);
            }
            ImGui.SameLine(0, 4);
            DrawFilterPopupButton();
            ImGui.Spacing();

            DrawNavRow(ViewAll, "All Puzzles", totalPuzzleCount, UiTheme.Gray);
            DrawNavRow(ViewFavorites, "Favorites", favoritePuzzles.Count, UiTheme.Gray);
            DrawNavRow(ViewOverview, "DC Overview", null, UiTheme.Gray);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            foreach (var region in Regions)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, region.Color);
                bool open = ImGui.TreeNodeEx(region.Name, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.SpanAvailWidth);
                ImGui.PopStyleColor();

                if (!open) continue;

                foreach (string dataCenter in region.DataCenters)
                {
                    if (csvDataByDataCenter.TryGetValue(dataCenter, out var puzzles))
                    {
                        DrawNavRow(ViewDcPrefix + dataCenter, dataCenter, puzzles.Count, UiTheme.GetSizeBucketColor(puzzles.Count));
                    }
                }

                ImGui.TreePop();
            }
        }

        private void DrawNavRow(string view, string label, int? count, Vector4 countColor)
        {
            bool selected = selectedView == view;
            if (ImGui.Selectable($"{label}##nav_{view}", selected))
            {
                SelectView(view);
            }

            if (count.HasValue)
            {
                string countText = count.Value.ToString();
                ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.CalcTextSize(countText).X - 14);
                ImGui.TextColored(countColor, countText);
            }
        }

        private void DrawToolStrip()
        {
            if (toolStripWidth > 0)
            {
                ImGui.SetCursorPosX(Math.Max(ImGui.GetCursorPosX(), (ImGui.GetWindowWidth() - toolStripWidth) * 0.5f));
            }

            ImGui.BeginGroup();

            if (ImGuiComponents.IconButton("refreshData", FontAwesomeIcon.Sync))
            {
                RefreshData();
                ShowNotification("Refreshing puzzle data...", MessageType.Info);
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Refresh puzzle data");

            ImGui.SameLine();
            DrawViewIcon("informationView", FontAwesomeIcon.InfoCircle, ViewInformation, "Ratings & puzzle code reference");

            ImGui.SameLine();
            DrawViewIcon("communityView", FontAwesomeIcon.Home, ViewCommunity, "Strange Housing community & credits");

            ImGui.EndGroup();
            toolStripWidth = ImGui.GetItemRectSize().X;
        }

        private void DrawViewIcon(string id, FontAwesomeIcon icon, string view, string tooltip)
        {
            bool active = selectedView == view;
            using (new ImRaii.ConditionalStyle(ImGuiCol.Button, UiTheme.SelectionActive, active))
            {
                if (ImGuiComponents.IconButton(id, icon))
                {
                    SelectView(view);
                }
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        private void DrawControlRow()
        {
            if (ImGuiComponents.IconButton("sidebarToggle", FontAwesomeIcon.Bars))
            {
                sidebarVisible = !sidebarVisible;
                settingsManager.Configuration.SidebarHidden = !sidebarVisible;
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip(sidebarVisible ? "Hide sidebar" : "Show sidebar");

            ImGui.SameLine();
            ImGui.AlignTextToFramePadding();

            var (title, subtitle) = GetViewMeta();
            ImGui.TextColored(UiTheme.TextBright, title);
            if (!string.IsNullOrEmpty(subtitle))
            {
                ImGui.SameLine();
                ImGui.TextColored(UiTheme.Gray, subtitle);
            }

            if (!IsTableView()) return;

            ImGui.SameLine();
            ImGui.Dummy(new Vector2(6, 0));

            DrawRatingChips();

            if (!sidebarVisible)
            {
                float filterButtonWidth = ImGui.GetFrameHeight() + 6;
                float searchWidth = 190;
                float rightEdge = ImGui.GetWindowWidth() - searchWidth - filterButtonWidth - 18;

                ImGui.SameLine(Math.Max(ImGui.GetCursorPosX(), rightEdge));
                using (new ImRaii.StyleVar(ImGuiStyleVar.FrameBorderSize, 1.0f))
                using (new ImRaii.StyleColor(
                    (ImGuiCol.FrameBg, UiTheme.SearchBg),
                    (ImGuiCol.Border, UiTheme.SearchBorder)))
                {
                    ImGui.SetNextItemWidth(searchWidth);
                    ImGui.InputTextWithHint("##puzzleSearch", "Search name, builder, world...", ref searchQuery, 256);
                }

                ImGui.SameLine();
                DrawFilterPopupButton();
            }
        }

        private void DrawRatingChips()
        {
            var baseRows = GetBaseRows();

            Span<int> starCounts = stackalloc int[6];
            int specialCount = 0;
            foreach (var puzzle in baseRows)
            {
                int stars = UiHelpers.CountStars(puzzle.Rating);
                if (stars == 0) specialCount++;
                else if (stars <= 5) starCounts[stars]++;
            }

            for (int i = 0; i < RatingChipKeys.Length; i++)
            {
                string key = RatingChipKeys[i];
                int count = key switch
                {
                    "All" => baseRows.Count,
                    "Special" => specialCount,
                    _ => starCounts[key.Length]
                };

                string label = key == "Special" ? "Special ☆" : key;
                bool active = ratingFilter == key;

                Vector4 chipColor = key switch
                {
                    "All" => active ? UiTheme.TextBright : UiTheme.TextDim,
                    "Special" => UiTheme.RatingSpecial,
                    _ => UiTheme.GetRatingColor(key)
                };
                Vector4 activeBg = key == "All"
                    ? UiTheme.SelectionActive
                    : new Vector4(chipColor.X, chipColor.Y, chipColor.Z, 0.25f);
                Vector4 hoverBg = key == "All"
                    ? UiTheme.PanelHover
                    : new Vector4(chipColor.X, chipColor.Y, chipColor.Z, 0.14f);

                ImGui.SameLine();
                using (new ImRaii.StyleVar(ImGuiStyleVar.FrameRounding, 10.0f))
                using (new ImRaii.StyleColor(
                    (ImGuiCol.Button, active ? activeBg : UiTheme.PanelBg2),
                    (ImGuiCol.ButtonHovered, active ? activeBg : hoverBg),
                    (ImGuiCol.Text, chipColor)))
                {
                    if (ImGui.SmallButton($"{label} {count}##chip{i}"))
                    {
                        ratingFilter = key;
                    }
                }
            }
        }

        private void DrawFilterPopupButton()
        {
            bool filtersActive = worldFilter != AllWorlds || districtFilter != AllDistricts;
            using (new ImRaii.ConditionalStyle(ImGuiCol.Button, UiTheme.SelectionActive, filtersActive))
            {
                if (ImGuiComponents.IconButton("extraFilters", FontAwesomeIcon.Filter))
                {
                    ImGui.OpenPopup("FiltersPopup");
                }
            }
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("World / district filters");

            using var popup = new ImRaii.Popup("FiltersPopup");
            if (!popup.Success) return;

            ImGui.TextColored(UiTheme.Primary, "Filters");
            ImGui.Separator();

            var worlds = GetWorldOptionsForCurrentView();
            ImGui.SetNextItemWidth(180);
            using (var combo = new ImRaii.Combo("World", worldFilter))
            {
                if (combo.Success)
                {
                    foreach (var world in worlds)
                    {
                        if (ImGui.Selectable(world, worldFilter == world))
                        {
                            worldFilter = world;
                        }
                    }
                }
            }

            ImGui.SetNextItemWidth(180);
            using (var combo = new ImRaii.Combo("District", districtFilter))
            {
                if (combo.Success)
                {
                    foreach (var district in Districts)
                    {
                        if (ImGui.Selectable(district, districtFilter == district))
                        {
                            districtFilter = district;
                        }
                    }
                }
            }

            ImGui.Spacing();
            if (ImGui.Button("Reset filters"))
            {
                worldFilter = AllWorlds;
                districtFilter = AllDistricts;
            }
        }

        private IEnumerable<string> GetWorldOptionsForCurrentView()
        {
            yield return AllWorlds;

            var scoped = selectedView.StartsWith(ViewDcPrefix, StringComparison.Ordinal)
                         && worldsByDataCenter.TryGetValue(selectedView.Substring(ViewDcPrefix.Length), out var dcWorlds)
                ? dcWorlds
                : allWorlds;

            foreach (var world in scoped)
            {
                yield return world;
            }
        }

        private void DrawContent()
        {
            using var content = new ImRaii.Child("MainContent");

            switch (selectedView)
            {
                case ViewOverview:
                    overviewTab.Draw();
                    break;
                case ViewInformation:
                    informationTab.Draw();
                    break;
                case ViewCommunity:
                    strangeHousingTab.Draw();
                    break;
                default:
                    DrawPuzzleTableView();
                    break;
            }
        }

        private void DrawPuzzleTableView()
        {
            if (selectedView == ViewFavorites && favoritePuzzles.Count == 0)
            {
                float centerY = ImGui.GetContentRegionAvail().Y * 0.4f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerY);

                using var gray = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Gray);
                UiTheme.CenteredText("♡");
                UiTheme.CenteredText("No favorites added yet");
                ImGui.Spacing();
                UiTheme.CenteredText("Browse puzzles and click ♡ to favorite them");
                return;
            }

            var rows = GetVisibleRows();
            if (rows.Count == 0)
            {
                float centerY = ImGui.GetContentRegionAvail().Y * 0.4f;
                ImGui.SetCursorPosY(ImGui.GetCursorPosY() + centerY);
                UiTheme.CenteredText("No puzzles match the current filters");
                using var gray = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Gray);
                UiTheme.CenteredText("Try clearing the search or rating filter");
                return;
            }

            DrawPuzzleTable(rows);
        }

        private void DrawPuzzleTable(List<JumpPuzzleData> puzzles)
        {
            UiTheme.StyleTable();

            float[] widths = GetColumnWidths(puzzles);

            ImGuiTableFlags flags = ImGuiTableFlags.RowBg |
                                    ImGuiTableFlags.BordersInnerH |
                                    ImGuiTableFlags.ScrollY |
                                    ImGuiTableFlags.ScrollX |
                                    ImGuiTableFlags.SizingFixedFit |
                                    ImGuiTableFlags.NoSavedSettings;

            if (ImGui.BeginTable("PuzzlesTable", 6, flags))
            {
                ImGui.TableSetupColumn("Rating", ImGuiTableColumnFlags.WidthFixed, widths[0]);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, widths[1]);
                ImGui.TableSetupColumn("Builder", ImGuiTableColumnFlags.WidthFixed, widths[2]);
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed, widths[3]);
                ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, widths[4]);
                ImGui.TableSetupColumn("##actions", ImGuiTableColumnFlags.WidthFixed, 48);

                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableHeadersRow();

                JumpPuzzleData? favoriteToToggle = null;
                bool favoriteToToggleIsAdd = false;

                for (int i = 0; i < puzzles.Count; i++)
                {
                    var puzzle = puzzles[i];

                    ImGui.PushID(i);
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();

                    Vector2 cellStart = ImGui.GetCursorPos();
                    ImGui.Selectable("##row", false, ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowItemOverlap);
                    if (ImGui.IsItemHovered())
                    {
                        DrawPuzzleTooltip(puzzle);
                    }

                    ImGui.SetCursorPos(cellStart);
                    ImGui.TextColored(UiTheme.GetRatingColor(puzzle.Rating), puzzle.Rating);

                    ImGui.TableNextColumn();
                    using (new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.TextBright))
                    {
                        ImGui.TextWrapped(puzzle.PuzzleName);
                    }

                    ImGui.TableNextColumn();
                    using (new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.TextDim))
                    {
                        ImGui.TextWrapped(puzzle.Builder);
                    }

                    ImGui.TableNextColumn();
                    ImGui.TextUnformatted(puzzle.World);
                    ImGui.SameLine();
                    using (new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Gray))
                    {
                        ImGui.TextWrapped(puzzle.Address);
                    }

                    ImGui.TableNextColumn();
                    string combinedCodes = UiHelpers.CombineCodes(puzzle.M, puzzle.E, puzzle.S, puzzle.P, puzzle.V, puzzle.J, puzzle.G, puzzle.L, puzzle.X);
                    UiHelpers.RenderCodesWithTooltips(combinedCodes);

                    ImGui.TableNextColumn();
                    bool isFav = IsFavorite(puzzle);
                    using (new ImRaii.StyleVar(ImGuiStyleVar.FramePadding, new Vector2(3, 1)))
                    using (new ImRaii.StyleColor(
                        (ImGuiCol.Button, Vector4.Zero),
                        (ImGuiCol.ButtonHovered, new Vector4(1, 1, 1, 0.07f)),
                        (ImGuiCol.ButtonActive, new Vector4(1, 1, 1, 0.12f))))
                    {
                        using (new ImRaii.StyleColor(ImGuiCol.Text, isFav ? UiTheme.Error : new Vector4(0.42f, 0.44f, 0.49f, 1.0f)))
                        {
                            if (ImGuiComponents.IconButton("fav", FontAwesomeIcon.Heart))
                            {
                                favoriteToToggle = puzzle;
                                favoriteToToggleIsAdd = !isFav;
                            }
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip(isFav ? "Remove from favorites" : "Add to favorites");

                        ImGui.SameLine(0, 2);

                        using (new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Primary))
                        {
                            if (ImGuiComponents.IconButton("go", FontAwesomeIcon.LocationArrow))
                            {
                                OnTravelRequest(puzzle);
                            }
                        }
                        if (ImGui.IsItemHovered()) ImGui.SetTooltip($"Travel to {puzzle.World} {puzzle.Address}");
                    }

                    ImGui.PopID();
                }

                ImGui.EndTable();

                if (favoriteToToggle != null)
                {
                    if (favoriteToToggleIsAdd)
                    {
                        AddToFavorites(favoriteToToggle);
                        ShowNotification("Puzzle added to favorites", MessageType.Success);
                    }
                    else
                    {
                        RemoveFromFavorites(favoriteToToggle);
                        ShowNotification("Puzzle removed from favorites", MessageType.Info);
                    }
                }
            }

            UiTheme.EndTableStyle();
        }

        private static void DrawPuzzleTooltip(JumpPuzzleData puzzle)
        {
            using var tooltip = new ImRaii.Tooltip();

            ImGui.TextColored(UiTheme.TextBright, puzzle.PuzzleName);
            ImGui.TextColored(UiTheme.GetRatingColor(puzzle.Rating), puzzle.Rating);
            ImGui.SameLine();
            ImGui.TextColored(UiTheme.TextDim, $"by {puzzle.Builder}");

            ImGui.Separator();
            ImGui.TextUnformatted($"{puzzle.World} — {puzzle.Address}");

            string codes = UiHelpers.CombineCodes(puzzle.M, puzzle.E, puzzle.S, puzzle.P, puzzle.V, puzzle.J, puzzle.G, puzzle.L, puzzle.X);
            if (!string.IsNullOrEmpty(codes))
            {
                ImGui.Spacing();
                UiHelpers.DrawCodeDescriptions(codes);
            }

            if (!string.IsNullOrEmpty(puzzle.GoalsOrRules))
            {
                ImGui.Spacing();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 22f);
                ImGui.TextColored(UiTheme.TextDim, puzzle.GoalsOrRules);
                ImGui.PopTextWrapPos();
            }
        }

        private void DrawStatusBar()
        {
            ImGui.Separator();

            if (DateTime.UtcNow < notificationExpiry)
            {
                ImGui.TextColored(GetNotificationColor(notificationType), notificationMessage);
                return;
            }

            ImGui.TextColored(UiTheme.TextDim, statusMessage);
            ImGui.SameLine();
            ImGui.TextColored(UiTheme.Gray, $"·  {totalPuzzleCount} puzzles  ·  Updated {lastRefreshDate:yyyy-MM-dd HH:mm}");

            bool lifestream = lifestreamIpcHandler.IsAvailable;
            string lifestreamText = lifestream ? "Lifestream connected" : "Lifestream not detected";
            ImGui.SameLine(ImGui.GetWindowWidth() - ImGui.CalcTextSize(lifestreamText).X - 16);
            ImGui.TextColored(lifestream ? UiTheme.Success : UiTheme.Error, lifestreamText);
        }

        private void ShowNotification(string message, MessageType type, float durationSeconds = 3.0f)
        {
            notificationMessage = message;
            notificationType = type;
            notificationExpiry = DateTime.UtcNow.AddSeconds(durationSeconds);
        }

        private static Vector4 GetNotificationColor(MessageType type) => type switch
        {
            MessageType.Success => UiTheme.Success,
            MessageType.Warning => UiTheme.Warning,
            MessageType.Error => UiTheme.Error,
            _ => UiTheme.Primary
        };

        private void SelectView(string view)
        {
            selectedView = view;
            ratingFilter = "All";
            worldFilter = AllWorlds;
            settingsManager.Configuration.LastSelectedView = view;
        }

        private bool IsTableView() =>
            selectedView == ViewAll || selectedView == ViewFavorites || selectedView.StartsWith(ViewDcPrefix, StringComparison.Ordinal);

        private (string Title, string Subtitle) GetViewMeta()
        {
            switch (selectedView)
            {
                case ViewAll: return ("All Puzzles", $"{totalPuzzleCount} total");
                case ViewFavorites: return ("Favorites", $"{favoritePuzzles.Count} saved");
                case ViewOverview: return ("DC Overview", string.Empty);
                case ViewInformation: return ("Information", "ratings & codes");
                case ViewCommunity: return ("Strange Housing", "community");
                default:
                    string dataCenter = selectedView.Substring(ViewDcPrefix.Length);
                    int count = csvDataByDataCenter.TryGetValue(dataCenter, out var puzzles) ? puzzles.Count : 0;
                    return (dataCenter, $"{count} puzzles · {GetRegionForDataCenter(dataCenter)}");
            }
        }

        private static string GetRegionForDataCenter(string dataCenterName)
        {
            foreach (var region in Regions)
            {
                if (region.DataCenters.Contains(dataCenterName))
                {
                    return region.Name;
                }
            }
            return "Unknown";
        }

        private bool PassesRatingFilter(JumpPuzzleData puzzle)
        {
            if (ratingFilter == "All") return true;

            int stars = UiHelpers.CountStars(puzzle.Rating);
            if (ratingFilter == "Special") return stars == 0;
            return stars == ratingFilter.Length;
        }

        private List<JumpPuzzleData> GetBaseRows()
        {
            string key = $"{selectedView}|{searchQuery}|{worldFilter}|{districtFilter}|{dataVersion}|{favoritesVersion}";
            if (key == cachedBaseKey) return cachedBaseRows;

            IEnumerable<JumpPuzzleData> rows = selectedView switch
            {
                ViewFavorites => favoritePuzzles,
                var v when v.StartsWith(ViewDcPrefix, StringComparison.Ordinal) =>
                    csvDataByDataCenter.TryGetValue(v.Substring(ViewDcPrefix.Length), out var list)
                        ? list
                        : Enumerable.Empty<JumpPuzzleData>(),
                _ => csvDataByDataCenter.Values.SelectMany(x => x)
            };

            if (worldFilter != AllWorlds)
            {
                rows = rows.Where(p => p.World == worldFilter);
            }

            if (districtFilter != AllDistricts)
            {
                rows = rows.Where(p => p.Address.Contains(districtFilter, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                rows = rows.Where(p =>
                    p.PuzzleName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Builder.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.World.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.GoalsOrRules.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                    p.Address.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));
            }

            cachedBaseRows = rows
                .OrderByDescending(p => UiHelpers.ConvertRatingToInt(p.Rating))
                .ThenBy(p => p.PuzzleName, StringComparer.OrdinalIgnoreCase)
                .ToList();
            cachedBaseKey = key;
            cachedVisibleKey = null;

            return cachedBaseRows;
        }

        private List<JumpPuzzleData> GetVisibleRows()
        {
            var baseRows = GetBaseRows();

            string key = $"{cachedBaseKey}|{ratingFilter}";
            if (key == cachedVisibleKey) return cachedVisibleRows;

            cachedVisibleRows = ratingFilter == "All" ? baseRows : baseRows.Where(PassesRatingFilter).ToList();
            cachedVisibleKey = key;

            return cachedVisibleRows;
        }

        private float[] GetColumnWidths(List<JumpPuzzleData> rows)
        {
            if (cachedColumnWidths != null && cachedWidthsKey == cachedVisibleKey)
            {
                return cachedColumnWidths;
            }

            float rating = ImGui.CalcTextSize("Rating").X;
            float name = ImGui.CalcTextSize("Name").X;
            float builder = ImGui.CalcTextSize("Builder").X;
            float location = ImGui.CalcTextSize("Location").X;
            float type = ImGui.CalcTextSize("Type").X;

            foreach (var p in rows)
            {
                rating = Math.Max(rating, ImGui.CalcTextSize(p.Rating).X);
                name = Math.Max(name, ImGui.CalcTextSize(p.PuzzleName).X);
                builder = Math.Max(builder, ImGui.CalcTextSize(p.Builder).X);
                location = Math.Max(location, ImGui.CalcTextSize($"{p.World} {p.Address}").X + 6);
                type = Math.Max(type, ImGui.CalcTextSize(UiHelpers.CombineCodes(p.M, p.E, p.S, p.P, p.V, p.J, p.G, p.L, p.X)).X);
            }

            const float pad = 10f;
            cachedColumnWidths = new[]
            {
                Math.Min(rating, 90f) + pad,
                Math.Min(name, 250f) + pad,
                Math.Min(builder, 150f) + pad,
                Math.Min(location, 280f) + pad,
                Math.Min(type, 90f) + pad
            };
            cachedWidthsKey = cachedVisibleKey;

            return cachedColumnWidths;
        }

        private bool IsFavorite(JumpPuzzleData puzzle) => favoritePuzzles.Any(p => p.Id == puzzle.Id);

        private void AddToFavorites(JumpPuzzleData puzzle)
        {
            if (!IsFavorite(puzzle))
            {
                favoritePuzzles.Add(puzzle);
                favoritesVersion++;
                SaveFavorites();
            }
        }

        private void RemoveFromFavorites(JumpPuzzleData puzzle)
        {
            favoritePuzzles.RemoveAll(p => p.Id == puzzle.Id);
            favoritesVersion++;
            SaveFavorites();
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

        private void OnTravelRequest(JumpPuzzleData puzzle)
        {
            ExecuteTravel(UiTheme.FormatTravelCommand(puzzle));
        }

        private void ExecuteTravel(string travelCommand)
        {
            if (lifestreamIpcHandler.ExecuteLiCommand(travelCommand))
            {
                Plugin.ChatGui.Print($"[WahJumps] Executing: {travelCommand}");
                ShowNotification("Travel command executed", MessageType.Success);
            }
            else
            {
                ShowNotification("Lifestream not available - is it installed and enabled?", MessageType.Error);
            }
        }

        private void OnStatusUpdated(string message) => statusMessage = message;

        private void OnProgressUpdated(float progress) => currentProgress = progress;

        private void OnCsvProcessingCompleted()
        {
            dataReloadPending = true;
        }

        private void OnDataLoaded()
        {
            statusMessage = "Ready";
            isReady = true;
            dataVersion++;

            if (selectedView.StartsWith(ViewDcPrefix, StringComparison.Ordinal) &&
                !csvDataByDataCenter.ContainsKey(selectedView.Substring(ViewDcPrefix.Length)))
            {
                SelectView(ViewAll);
            }

            ShowNotification("Data loading completed successfully!", MessageType.Success);
        }

        private void RefreshData()
        {
            _ = csvManager.DownloadAndSaveIndividualCsvsAsync();
            statusMessage = "Refreshing data...";
            currentProgress = 0f;
            isReady = false;
        }

        private void LoadCsvData()
        {
            csvDataByDataCenter.Clear();
            worldsByDataCenter.Clear();

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
                        worldsByDataCenter[dataCenter.DataCenter] = data
                            .Select(p => p.World)
                            .Distinct()
                            .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
                            .ToList();

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

            totalPuzzleCount = csvDataByDataCenter.Values.Sum(v => v.Count);
            allWorlds = worldsByDataCenter.Values
                .SelectMany(w => w)
                .Distinct()
                .OrderBy(w => w, StringComparer.OrdinalIgnoreCase)
                .ToList();
            uniqueBuilderCount = csvDataByDataCenter.Values
                .SelectMany(v => v)
                .Where(p => !string.IsNullOrEmpty(p.Builder))
                .Select(p => p.Builder)
                .Distinct()
                .Count();
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
                        int ratingComparison = UiHelpers.ConvertRatingToInt(y.Rating).CompareTo(UiHelpers.ConvertRatingToInt(x.Rating));
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
    }
}
