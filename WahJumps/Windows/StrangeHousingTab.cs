// File: WahJumps/Windows/StrangeHousingTab.cs
// Status: COMPACTED VERSION - More professional

using ImGuiNET;
using WahJumps.Utilities;
using WahJumps.Windows.Components;
using System.Numerics;

namespace WahJumps.Windows
{
    public class StrangeHousingTab
    {
        public void Draw()
        {
            // Fixed TabItem usage
            using var tabItem = new ImRaii.TabItem("Strange Housing");
            if (!tabItem.Success) return;

            // Thank you message in a nice card
            using (var thanksCard = new ImRaii.Child("ThankYouSection", new Vector2(-1, -1)))
            {
                float windowWidth = ImGui.GetWindowWidth();
                float textWidth;

                // Header
                using (var headerColor = new ImRaii.StyleColor(ImGuiCol.Text, UiTheme.Secondary))
                {
                    textWidth = ImGui.CalcTextSize("Strange Housing Community").X;
                    ImGui.SetCursorPosX((windowWidth - textWidth) / 2);
                    ImGui.TextUnformatted("Strange Housing Community");
                }

                ImGui.Spacing();

                // Description
                textWidth = ImGui.CalcTextSize("Find and explore creative jump puzzles built by the community").X;
                ImGui.SetCursorPosX((windowWidth - textWidth) / 2);
                ImGui.TextUnformatted("Find and explore creative jump puzzles built by the community");

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Links Section
                UiTheme.CenteredText("Useful Links", UiTheme.Primary);
                ImGui.Spacing();

                // Two-column layout for links
                float buttonWidth = windowWidth * 0.4f;
                float leftCol = (windowWidth - (buttonWidth * 2 + 20)) / 2;

                // Row 1
                ImGui.SetCursorPosX(leftCol);
                DrawLinkButton("Visit ffxiv.ju.mp", "https://ffxiv.ju.mp/", buttonWidth);

                ImGui.SameLine(leftCol + buttonWidth + 20);
                DrawDiscordButton("Join Discord", "https://discord.gg/6agVYe6xYk", buttonWidth);

                ImGui.Spacing();
                ImGui.Spacing();

                // Row 2
                ImGui.SetCursorPosX(leftCol);
                DrawLinkButton("Jumping Guide",
                    "https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit",
                    buttonWidth);

                ImGui.SameLine(leftCol + buttonWidth + 20);
                DrawLinkButton("Jump Puzzle Spreadsheet",
                    "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879",
                    buttonWidth);

                ImGui.Spacing();
                ImGui.Spacing();

                // LifeStream Notice
                UiComponents.NotificationBox(
                    "⚠️ The LifeStream plugin is required for travel buttons to work\n" +
                    "Download it from GitHub: NightmareXIV/Lifestream",
                    UiTheme.Warning
                );

                ImGui.Spacing();

                ImGui.SetCursorPosX((windowWidth - buttonWidth) / 2);
                DrawLinkButton("LifeStream Plugin", "https://github.com/NightmareXIV/Lifestream", buttonWidth);
            }
        }

        private void DrawLinkButton(string label, string url, float width)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(0.12f, 0.15f, 0.2f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new Vector4(0.17f, 0.21f, 0.28f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(0.25f, 0.30f, 0.4f, 1.0f));

            bool clicked = ImGui.Button($"{label}##link", new Vector2(width, 0));

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();

            if (clicked)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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

        private void DrawDiscordButton(string label, string url, float width)
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);
            ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.DiscordPrimary);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.DiscordHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, UiTheme.DiscordActive);

            bool clicked = ImGui.Button($"{label}##discord", new Vector2(width, 0));

            ImGui.PopStyleColor(3);
            ImGui.PopStyleVar();

            if (clicked)
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
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
    }
}
