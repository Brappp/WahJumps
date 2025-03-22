// File: WahJumps/Utilities/UiTheme.cs
using System.Numerics;
using ImGuiNET;
using System.Collections.Generic;
using System;

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

        // Improved table styling with more whitespace and better colors
        public static void StyleTable()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(8, 4)); // More padding
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6)); // More space between rows

            // Better header colors for contrast
            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.15f, 0.35f, 0.5f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.5f, 0.5f, 0.5f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));

            // Better row colors for readability
            ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.18f, 0.18f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.25f, 0.25f, 0.28f, 1.0f));
        }

        // End table styling - updated to match the number of pushed styles
        public static void EndTableStyle()
        {
            ImGui.PopStyleColor(5); // Match the number of PushStyleColor calls
            ImGui.PopStyleVar(2);   // Match the number of PushStyleVar calls
        }

        // Apply default styling for consistent look
        public static void ApplyGlobalStyle()
        {
            // Enhanced spacing and padding
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(12, 12));

            // Better rounding for visual appeal
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.TabRounding, 6.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 3.0f);

            // Improved colors
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.25f, 0.25f, 0.27f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, new Vector4(0.18f, 0.18f, 0.22f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.12f, 0.12f, 0.15f, 0.98f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.1f, 0.2f, 0.3f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.15f, 0.3f, 0.5f, 1.0f));
        }

        // End global styling - updated to match the pushed styles
        public static void EndGlobalStyle()
        {
            ImGui.PopStyleColor(5); // Match number of PushStyleColor calls
            ImGui.PopStyleVar(6);   // Match number of PushStyleVar calls 
        }

        // Create a tooltip with standardized styling
        public static void Tooltip(string text)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.15f, 0.15f, 0.18f, 1.0f));
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 20.0f);
                ImGui.TextUnformatted(text);
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
                ImGui.PopStyleColor();
            }
        }

        // Create a button with standardized styling
        public static bool StandardButton(string label, Vector2? size = null)
        {
            Vector2 buttonSize = size ?? new Vector2(0, 0);

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.2f, 0.4f, 0.6f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.3f, 0.5f, 0.7f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.15f, 0.35f, 0.55f, 1.0f));

            bool clicked = ImGui.Button(label, buttonSize);

            ImGui.PopStyleColor(3);

            return clicked;
        }

        // Enhanced section header with optional help tooltip
        public static void SectionHeader(string text, string tooltipText = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, Primary);
            ImGui.TextUnformatted(text);
            ImGui.PopStyleColor();

            if (!string.IsNullOrEmpty(tooltipText) && ImGui.IsItemHovered())
            {
                Tooltip(tooltipText);
            }

            ImGui.Separator();
            ImGui.Spacing();
        }

        // Draw a stylish header with gradient background
        public static void GradientHeader(string text, Vector4 startColor, Vector4 endColor)
        {
            var drawList = ImGui.GetWindowDrawList();
            var pos = ImGui.GetCursorScreenPos();
            var size = new Vector2(ImGui.GetWindowWidth() - ImGui.GetStyle().WindowPadding.X * 2, 40);

            // Draw gradient background
            drawList.AddRectFilledMultiColor(
                pos,
                new Vector2(pos.X + size.X, pos.Y + size.Y),
                ImGui.GetColorU32(startColor),
                ImGui.GetColorU32(endColor),
                ImGui.GetColorU32(endColor),
                ImGui.GetColorU32(startColor)
            );

            // Draw text centered
            var textSize = ImGui.CalcTextSize(text);
            var textPos = new Vector2(
                pos.X + (size.X - textSize.X) * 0.5f,
                pos.Y + (size.Y - textSize.Y) * 0.5f
            );

            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), text);

            // Advance cursor
            ImGui.Dummy(size);
            ImGui.Spacing();
        }

        // Loading spinner with improved visuals
        public static void LoadingSpinner(string label, float radius = 10.0f, float thickness = 2.0f, Vector4? color = null)
        {
            Vector4 spinnerColor = color ?? Primary;

            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 center = new Vector2(pos.X + radius, pos.Y + radius);
            float time = (float)ImGui.GetTime() * 1.5f; // Slightly faster animation

            // Draw a subtle background circle
            drawList.AddCircleFilled(
                center,
                radius * 0.8f,
                ImGui.GetColorU32(new Vector4(spinnerColor.X * 0.2f, spinnerColor.Y * 0.2f, spinnerColor.Z * 0.2f, 0.2f)),
                12
            );

            // Draw spinning segments
            for (int i = 0; i < 6; i++) // Reduced number of segments for cleaner look
            {
                float a1 = time + i * MathF.PI / 3.0f;
                float a2 = a1 + MathF.PI / 6.0f;

                // Calculate alpha based on position (fade effect)
                float alpha = 0.1f + 0.9f * ((i + time * 0.954f) % 6) / 6.0f;

                drawList.PathArcTo(
                    center,
                    radius,
                    a1,
                    a2,
                    12
                );

                drawList.PathStroke(
                    ImGui.GetColorU32(new Vector4(spinnerColor.X, spinnerColor.Y, spinnerColor.Z, alpha)),
                    ImDrawFlags.None,
                    thickness
                );
            }

            // Advance cursor
            ImGui.Dummy(new Vector2(radius * 2 + 4, radius * 2));

            // Add label if provided
            if (!string.IsNullOrEmpty(label))
            {
                ImGui.SameLine();
                ImGui.Text(label);
            }
        }

        // Create a progress bar with gradient
        public static void GradientProgressBar(float fraction, Vector2 size, Vector4 startColor, Vector4 endColor, string overlay = null)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();

            // Background
            drawList.AddRectFilled(
                pos,
                new Vector2(pos.X + size.X, pos.Y + size.Y),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)),
                4.0f
            );

            // Progress gradient
            if (fraction > 0)
            {
                float width = size.X * Math.Clamp(fraction, 0, 1);
                drawList.AddRectFilledMultiColor(
                    pos,
                    new Vector2(pos.X + width, pos.Y + size.Y),
                    ImGui.GetColorU32(startColor),
                    ImGui.GetColorU32(endColor),
                    ImGui.GetColorU32(endColor),
                    ImGui.GetColorU32(startColor)
                );

                // Add rounded corners with small overlaid circles
                float radius = Math.Min(4.0f, size.Y / 2);
                drawList.AddCircleFilled(
                    new Vector2(pos.X + width - radius, pos.Y + radius),
                    radius,
                    ImGui.GetColorU32(endColor)
                );
                drawList.AddCircleFilled(
                    new Vector2(pos.X + width - radius, pos.Y + size.Y - radius),
                    radius,
                    ImGui.GetColorU32(endColor)
                );
            }

            // Overlay text
            if (!string.IsNullOrEmpty(overlay))
            {
                var textSize = ImGui.CalcTextSize(overlay);
                drawList.AddText(
                    new Vector2(
                        pos.X + (size.X - textSize.X) * 0.5f,
                        pos.Y + (size.Y - textSize.Y) * 0.5f
                    ),
                    ImGui.GetColorU32(new Vector4(1, 1, 1, 1)),
                    overlay
                );
            }

            // Advance cursor
            ImGui.Dummy(size);
        }

        // Create a button with an icon
        public static bool IconButton(string icon, string label, Vector4 color, float width = 0)
        {
            float buttonWidth = width > 0 ? width : ImGui.CalcTextSize(label).X + 30;

            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.15f, 0.15f, 0.18f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.25f, 0.25f, 0.28f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Text, color);

            bool clicked = ImGui.Button($"{icon} {label}", new Vector2(buttonWidth, 0));

            ImGui.PopStyleColor(3);

            return clicked;
        }

        // Draw a card with title and content
        public static void Card(string title, string content, Vector4 titleColor, float width = 0)
        {
            float cardWidth = width > 0 ? width : ImGui.GetContentRegionAvail().X;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.15f, 0.18f, 1.0f));
            ImGui.BeginChild($"##card_{title}", new Vector2(cardWidth, 0), true);

            // Title
            ImGui.PushStyleColor(ImGuiCol.Text, titleColor);
            ImGui.Text(title);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            // Content
            ImGui.TextWrapped(content);

            ImGui.EndChild();
            ImGui.PopStyleColor();
        }
    }
}
