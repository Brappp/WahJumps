using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using WahJumps.Utilities;

namespace WahJumps.Windows
{
    public class StrangeHousingTab
    {
        private readonly Func<bool> lifestreamAvailable;
        private readonly Func<(int Puzzles, int Builders, int Worlds)> statsProvider;

        private static readonly (FontAwesomeIcon Icon, string Title, string Description, string Url, Vector4 Color)[] Links =
        {
            (FontAwesomeIcon.Globe, "ffxiv.ju.mp", "The community hub", "https://ffxiv.ju.mp/", UiTheme.Primary),
            (FontAwesomeIcon.Comments, "Discord Server", "Events, help, and new puzzle drops", "https://discord.gg/6agVYe6xYk", UiTheme.DiscordPrimary),
            (FontAwesomeIcon.GraduationCap, "Jumping Guide", "Techniques from first hop to expert", "https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit", UiTheme.Success),
            (FontAwesomeIcon.Database, "Puzzle Database", "The source spreadsheet", "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879", UiTheme.Warning),
        };

        public StrangeHousingTab(Func<bool> lifestreamAvailable, Func<(int Puzzles, int Builders, int Worlds)> statsProvider)
        {
            this.lifestreamAvailable = lifestreamAvailable;
            this.statsProvider = statsProvider;
        }

        public void Draw()
        {
            float avail = ImGui.GetContentRegionAvail().X;
            float columnWidth = Math.Min(560f, avail);
            float indent = Math.Max(0f, (avail - columnWidth) * 0.5f);
            if (indent > 0) ImGui.SetCursorPosX(ImGui.GetCursorPosX() + indent);

            using var column = new ImRaii.Child("CommunityColumn", new Vector2(columnWidth, 0));
            var drawList = ImGui.GetWindowDrawList();

            ImGui.Spacing();

            ImGui.TextColored(UiTheme.TextBright, "Strange Housing");

            string credits = "made with ♥ by wah";
            if (ImGui.CalcTextSize("Strange Housing").X + ImGui.CalcTextSize(credits).X + 24 < columnWidth)
            {
                ImGui.SameLine(columnWidth - ImGui.CalcTextSize(credits).X - 8);
                ImGui.TextColored(UiTheme.Accent, credits);
            }
            else
            {
                ImGui.TextColored(UiTheme.Accent, credits);
            }

            ImGui.TextColored(UiTheme.TextDim, Ellipsize("Creative jump puzzles built by the FFXIV community", columnWidth - 8));

            Vector2 underline = ImGui.GetCursorScreenPos() + new Vector2(0, 3);
            drawList.AddRectFilled(underline, underline + new Vector2(46, 3), ImGui.GetColorU32(UiTheme.Primary), 1.5f);
            ImGui.Dummy(new Vector2(0, 14));

            var (puzzles, builders, worlds) = statsProvider();
            if (puzzles > 0)
            {
                ImGui.TextColored(UiTheme.Gray, Ellipsize($"{puzzles:N0} puzzles  ·  {builders:N0} builders  ·  {worlds:N0} worlds — and counting", columnWidth - 8));
                ImGui.Dummy(new Vector2(0, 6));
            }

            DrawLinkTiles(columnWidth);

            if (!lifestreamAvailable())
            {
                ImGui.Dummy(new Vector2(0, 6));
                DrawLifestreamWarning();
            }

            ImGui.Dummy(new Vector2(0, 10));
            ImGui.Separator();

            string thanks = "Thanks to the Strange Housing staff & community!";
            string repo = "GitHub: wahtf/WahJumps";
            float linkWidth = ImGui.CalcTextSize(repo).X + ImGui.GetStyle().FramePadding.X * 2;

            ImGui.TextColored(UiTheme.Gray, thanks);
            if (ImGui.CalcTextSize(thanks).X + linkWidth + 24 < columnWidth)
            {
                ImGui.SameLine(columnWidth - linkWidth - 4);
            }
            if (UiTheme.Hyperlink(repo, "githubLink"))
            {
                UiHelpers.OpenUrl("https://github.com/wahtf/WahJumps");
            }
        }

        private static void DrawLinkTiles(float columnWidth)
        {
            var drawList = ImGui.GetWindowDrawList();
            const float spacing = 8f;
            int columns = columnWidth >= 500f ? 2 : 1;
            float tileWidth = columns == 2
                ? (ImGui.GetContentRegionAvail().X - spacing) / 2f
                : ImGui.GetContentRegionAvail().X;
            float lineHeight = ImGui.GetTextLineHeight();
            var tileSize = new Vector2(tileWidth, lineHeight * 2 + 24);
            float textX = 46f;
            float maxTextWidth = tileWidth - textX - 10f;

            for (int i = 0; i < Links.Length; i++)
            {
                var (icon, title, description, url, color) = Links[i];

                if (columns == 2 && i % 2 == 1) ImGui.SameLine(0, spacing);

                Vector2 pos = ImGui.GetCursorScreenPos();
                bool clicked = ImGui.InvisibleButton($"##communityLink{i}", tileSize);
                bool hovered = ImGui.IsItemHovered();

                drawList.AddRectFilled(pos, pos + tileSize,
                    ImGui.GetColorU32(hovered ? UiTheme.PanelHover : UiTheme.PanelBg2), 5.0f);
                drawList.AddRect(pos, pos + tileSize,
                    ImGui.GetColorU32(hovered ? color : UiTheme.SoftBorder), 5.0f);

                string iconText = icon.ToIconString();
                using (Plugin.PluginInterface.UiBuilder.IconFontHandle.Push())
                {
                    Vector2 iconSize = ImGui.CalcTextSize(iconText);
                    drawList.AddText(
                        new Vector2(pos.X + 14, pos.Y + (tileSize.Y - iconSize.Y) * 0.5f),
                        ImGui.GetColorU32(color), iconText);
                }

                drawList.AddText(new Vector2(pos.X + textX, pos.Y + 10),
                    ImGui.GetColorU32(UiTheme.TextBright), Ellipsize(title, maxTextWidth));
                drawList.AddText(new Vector2(pos.X + textX, pos.Y + 12 + lineHeight),
                    ImGui.GetColorU32(UiTheme.Gray), Ellipsize(description, maxTextWidth));

                if (hovered)
                {
                    ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                    ImGui.SetTooltip(url);
                }

                if (clicked)
                {
                    UiHelpers.OpenUrl(url);
                }
            }
        }

        private static string Ellipsize(string text, float maxWidth)
        {
            if (ImGui.CalcTextSize(text).X <= maxWidth) return text;

            while (text.Length > 1 && ImGui.CalcTextSize(text + "…").X > maxWidth)
            {
                text = text.Substring(0, text.Length - 1);
            }

            return text + "…";
        }

        private static void DrawLifestreamWarning()
        {
            ImGui.TextColored(UiTheme.Warning, "LifeStream not detected — travel buttons are disabled.");
            ImGui.SameLine();
            if (UiTheme.ColoredButton("Download LifeStream", UiTheme.Primary, tooltip: "https://github.com/NightmareXIV/Lifestream"))
            {
                UiHelpers.OpenUrl("https://github.com/NightmareXIV/Lifestream");
            }
        }
    }
}
