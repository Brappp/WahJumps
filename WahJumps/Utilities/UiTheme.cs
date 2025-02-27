// File: WahJumps/Utilities/UiTheme.cs
// Status: UPDATED - Removed compact mode

using System.Numerics;
using ImGuiNET;
using System.Collections.Generic;

namespace WahJumps.Utilities
{
    public static class UiTheme
    {
        // Common Colors
        public static readonly Vector4 Primary = new Vector4(0.098f, 0.608f, 0.8f, 1.0f);        // #189BCC
        public static readonly Vector4 PrimaryLight = new Vector4(0.365f, 0.729f, 0.859f, 1.0f); // #5DBADB
        public static readonly Vector4 PrimaryLighter = new Vector4(0.639f, 0.843f, 0.922f, 1.0f); // #A3D7EB
        public static readonly Vector4 Secondary = new Vector4(0.624f, 0.529f, 0.718f, 1.0f);    // #9F87B7
        public static readonly Vector4 Accent = new Vector4(0.847f, 0.42f, 0.467f, 1.0f);        // #D86B77
        public static readonly Vector4 Success = new Vector4(0.149f, 0.651f, 0.427f, 1.0f);      // #26A66D
        public static readonly Vector4 Warning = new Vector4(0.902f, 0.784f, 0.420f, 1.0f);      // #E6C86B
        public static readonly Vector4 Error = new Vector4(0.902f, 0.325f, 0.325f, 1.0f);        // #E65353
        public static readonly Vector4 Dark = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);            // #262626
        public static readonly Vector4 Gray = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);               // #666666
        public static readonly Vector4 Light = new Vector4(0.9f, 0.9f, 0.9f, 1.0f);              // #E6E6E6

        // Discord Colors
        public static readonly Vector4 DiscordPrimary = new Vector4(0.29f, 0.33f, 0.86f, 1.0f);  // #4A54DB
        public static readonly Vector4 DiscordHover = new Vector4(0.39f, 0.43f, 0.96f, 1.0f);    // #636EF5
        public static readonly Vector4 DiscordActive = new Vector4(0.19f, 0.23f, 0.76f, 1.0f);   // #313BC2

        // Data Center Colors
        private static readonly Dictionary<string, (Vector4 Dark, Vector4 Medium, Vector4 Light)> DataCenterColors = new Dictionary<string, (Vector4, Vector4, Vector4)>
        {
            // NA Blues
            ["aether"] = (new Vector4(0.098f, 0.608f, 0.8f, 1.0f), new Vector4(0.365f, 0.729f, 0.859f, 1.0f), new Vector4(0.639f, 0.843f, 0.922f, 1.0f)),
            ["primal"] = (new Vector4(0.098f, 0.608f, 0.8f, 1.0f), new Vector4(0.365f, 0.729f, 0.859f, 1.0f), new Vector4(0.639f, 0.843f, 0.922f, 1.0f)),
            ["crystal"] = (new Vector4(0.098f, 0.608f, 0.8f, 1.0f), new Vector4(0.365f, 0.729f, 0.859f, 1.0f), new Vector4(0.639f, 0.843f, 0.922f, 1.0f)),
            ["dynamis"] = (new Vector4(0.098f, 0.608f, 0.8f, 1.0f), new Vector4(0.365f, 0.729f, 0.859f, 1.0f), new Vector4(0.639f, 0.843f, 0.922f, 1.0f)),

            // EU Purples
            ["light"] = (new Vector4(0.624f, 0.529f, 0.718f, 1.0f), new Vector4(0.773f, 0.718f, 0.831f, 1.0f), new Vector4(0.875f, 0.843f, 0.906f, 1.0f)),
            ["chaos"] = (new Vector4(0.624f, 0.529f, 0.718f, 1.0f), new Vector4(0.773f, 0.718f, 0.831f, 1.0f), new Vector4(0.875f, 0.843f, 0.906f, 1.0f)),

            // Materia Yellows
            ["materia"] = (new Vector4(1.0f, 0.764f, 0.509f, 1.0f), new Vector4(0.988f, 0.851f, 0.706f, 1.0f), new Vector4(0.996f, 0.941f, 0.882f, 1.0f)),

            // Japan Reds
            ["elemental"] = (new Vector4(0.847f, 0.42f, 0.467f, 1.0f), new Vector4(0.894f, 0.592f, 0.627f, 1.0f), new Vector4(0.953f, 0.827f, 0.839f, 1.0f)),
            ["gaia"] = (new Vector4(0.847f, 0.42f, 0.467f, 1.0f), new Vector4(0.894f, 0.592f, 0.627f, 1.0f), new Vector4(0.953f, 0.827f, 0.839f, 1.0f)),
            ["mana"] = (new Vector4(0.847f, 0.42f, 0.467f, 1.0f), new Vector4(0.894f, 0.592f, 0.627f, 1.0f), new Vector4(0.953f, 0.827f, 0.839f, 1.0f)),
            ["meteor"] = (new Vector4(0.847f, 0.42f, 0.467f, 1.0f), new Vector4(0.894f, 0.592f, 0.627f, 1.0f), new Vector4(0.953f, 0.827f, 0.839f, 1.0f))
        };

        // Get data center colors (case insensitive)
        public static (Vector4 Dark, Vector4 Medium, Vector4 Light) GetDataCenterColors(string dataCenterKey)
        {
            dataCenterKey = dataCenterKey.ToLower();
            if (DataCenterColors.TryGetValue(dataCenterKey, out var colors))
                return colors;

            // Default colors if no matching data center is found
            return (Primary, PrimaryLight, PrimaryLighter);
        }

        // Creates a button styled as a hyperlink
        public static bool Hyperlink(string text, string id = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Primary);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0, 0, 0, 0));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0, 0, 0, 0));

            bool clicked = ImGui.Button(id == null ? text : $"{text}##{id}");

            if (ImGui.IsItemHovered())
            {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                ImGui.SetTooltip("Click to follow link");

                // Draw an underline
                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                min.Y = max.Y;
                ImGui.GetWindowDrawList().AddLine(min, max, ImGui.GetColorU32(Primary), 1.0f);
            }

            ImGui.PopStyleColor(4);
            return clicked;
        }

        // Creates a centered heading
        public static void CenteredText(string text, Vector4? color = null)
        {
            float windowWidth = ImGui.GetWindowWidth();
            float textWidth = ImGui.CalcTextSize(text).X;

            ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);

            if (color.HasValue)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, color.Value);
                ImGui.Text(text);
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.Text(text);
            }
        }

        // Creates a header with styling
        public static void Header(string text, Vector4? color = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, color ?? Primary);

            ImGui.Text(text);
            ImGui.Separator();

            ImGui.PopStyleColor();
        }

        // Apply consistent table styling
        public static void StyleTable()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(4, 2));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 4));
            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.13f, 0.33f, 0.46f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.4f, 0.4f, 0.4f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
        }

        // End table styling
        public static void EndTableStyle()
        {
            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar(2);
        }

        // Apply default styling for consistent look
        public static void ApplyGlobalStyle()
        {
            // Set some reasonable default styling
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 3));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 3));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 3.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 4.0f);

            // Ensure table rows are easier to read
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.22f, 0.22f, 0.22f, 1.0f));
        }

        // End global styling
        public static void EndGlobalStyle()
        {
            ImGui.PopStyleColor();
            ImGui.PopStyleVar(5);
        }
    }
}
