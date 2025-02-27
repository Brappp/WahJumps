// File: WahJumps/Windows/Components/UiComponents.cs
// Status: CONDENSED VERSION - More professional and compact

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using WahJumps.Data;
using WahJumps.Utilities;

namespace WahJumps.Windows.Components
{
    public static class UiComponents
    {
        // Creates a button styled for external links
        public static void ExternalLinkButton(string label, string url, float width = 0)
        {
            if (width > 0)
            {
                float windowWidth = ImGui.GetWindowWidth();
                ImGui.SetCursorPosX((windowWidth - width) / 2);
                ImGui.SetNextItemWidth(width);
            }
            else
            {
                float windowWidth = ImGui.GetWindowWidth();
                float textWidth = ImGui.CalcTextSize(label).X + 20; // Add padding
                ImGui.SetCursorPosX((windowWidth - textWidth) / 2);
            }

            if (ImGui.Button(label))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(url);
            }
        }

        // Creates a Discord-styled button for Discord links
        public static void DiscordButton(string label, string url)
        {
            using var colors = new ImRaii.StyleColor(
                (ImGuiCol.Button, UiTheme.DiscordPrimary),
                (ImGuiCol.ButtonHovered, UiTheme.DiscordHover),
                (ImGuiCol.ButtonActive, UiTheme.DiscordActive)
            );

            ExternalLinkButton(label, url);
        }

        // Creates a colored notification box
        public static void NotificationBox(string text, Vector4 color, bool centered = true)
        {
            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(color.X, color.Y, color.Z, 0.1f));
            using var child = new ImRaii.Child("##notificationBox", new Vector2(-1, 0), true);

            ImGui.PushStyleColor(ImGuiCol.Text, color);

            if (centered)
            {
                UiTheme.CenteredText(text);
            }
            else
            {
                ImGui.TextWrapped(text);
            }

            ImGui.PopStyleColor();
            ImGui.PopStyleColor();
        }

        // Warning box with icon
        public static void WarningBox(string text)
        {
            NotificationBox("⚠️ " + text, UiTheme.Warning);
        }

        // Creates a filter combo box
        public static bool FilterCombo<T>(string label, ref T selectedItem, IReadOnlyList<T> items, Func<T, string> getDisplayString)
        {
            bool changed = false;
            string currentItem = selectedItem != null ? getDisplayString(selectedItem) : "All";

            if (ImGui.BeginCombo(label, currentItem))
            {
                foreach (var item in items)
                {
                    string displayName = getDisplayString(item);
                    bool isSelected = EqualityComparer<T>.Default.Equals(selectedItem, item);

                    if (ImGui.Selectable(displayName, isSelected))
                    {
                        selectedItem = item;
                        changed = true;
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }

                ImGui.EndCombo();
            }

            return changed;
        }

        // Creates a styled, compact search box
        public static bool SearchBox(ref string query, string hint = "Search...", float width = 0)
        {
            string temp = query;

            if (width > 0)
            {
                ImGui.SetNextItemWidth(width);
            }
            else
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            }

            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 4));
            bool changed = ImGui.InputTextWithHint("##searchBox", hint, ref temp, 100);
            ImGui.PopStyleVar();

            if (changed)
            {
                query = temp;
            }

            return changed;
        }

        // Creates a compact filter header section
        public static void FilterHeader(string title, bool isSearching = false)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);

            if (isSearching)
            {
                ImGui.Text($"{title} - Filtering...");
            }
            else
            {
                ImGui.Text(title);
            }

            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();
        }

        // Helper function to combine codes into a single string
        public static string CombineCodes(params string[] codes)
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

        // Create an iconified button that takes minimal space
        public static bool IconButton(string icon, string tooltip, Vector4 color, string id = null)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            bool clicked = ImGui.Button(id != null ? $"{icon}##{id}" : icon);
            ImGui.PopStyleColor();

            if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(tooltip))
            {
                ImGui.SetTooltip(tooltip);
            }

            return clicked;
        }

        // Draw a compact card with title and content
        public static void Card(string title, string content, float width = 0)
        {
            float cardWidth = width > 0 ? width : ImGui.GetContentRegionAvail().X;

            ImGui.PushStyleColor(ImGuiCol.ChildBg, new Vector4(0.15f, 0.15f, 0.15f, 1.0f));
            ImGui.BeginChild($"##card_{title}", new Vector2(cardWidth, 0), true);

            // Title with color
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Primary);
            ImGui.Text(title);
            ImGui.PopStyleColor();

            ImGui.Separator();
            ImGui.Spacing();

            // Content
            ImGui.TextWrapped(content);

            ImGui.EndChild();
            ImGui.PopStyleColor();
        }

        // Draw a loading spinner
        public static void LoadingSpinner(string label, float radius = 10.0f, float thickness = 2.0f)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 center = new Vector2(pos.X + radius, pos.Y + radius);
            float time = (float)ImGui.GetTime();

            // Calculate rotation angles for segments
            const int segmentCount = 8;
            for (int i = 0; i < segmentCount; i++)
            {
                float t = time * 2.0f + i * 2.0f * MathF.PI / segmentCount;
                float segmentLength = 0.8f * MathF.PI / segmentCount;
                float alpha = 0.15f + 0.85f * (i / (float)segmentCount);

                drawList.PathArcTo(
                    center,
                    radius,
                    t,
                    t + segmentLength,
                    12
                );

                drawList.PathStroke(
                    ImGui.GetColorU32(new Vector4(UiTheme.Primary.X, UiTheme.Primary.Y, UiTheme.Primary.Z, alpha)),
                    ImDrawFlags.None,
                    thickness
                );
            }

            // Move cursor past the spinner and add label if provided
            ImGui.Dummy(new Vector2(radius * 2 + 4, radius * 2));

            if (!string.IsNullOrEmpty(label))
            {
                ImGui.SameLine();
                ImGui.Text(label);
            }
        }
    }
}
