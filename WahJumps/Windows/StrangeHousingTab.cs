// File: WahJumps/Windows/StrangeHousingTab.cs
// Status: COMPLETE - Clean single-column design without broken icons

using ImGuiNET;
using WahJumps.Utilities;
using WahJumps.Windows.Components;
using System.Numerics;
using System;

namespace WahJumps.Windows
{
    public class StrangeHousingTab
    {
        public void Draw()
        {
            using var tabItem = new ImRaii.TabItem("Strange Housing");
            if (!tabItem.Success) return;

            float windowWidth = ImGui.GetWindowWidth();

            // Header section
            ImGui.Spacing();
            UiTheme.CenteredText("Strange Housing Community", UiTheme.Primary);
            ImGui.Spacing();
            
            // Description
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("Discover and explore creative jump puzzles built by the FFXIV community");
            ImGui.PopTextWrapPos();
            
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Thank you message
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Accent);
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("Thank you to the Strange Housing staff and community for their amazing work!");
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();

            ImGui.Spacing();
            ImGui.Spacing();

            // Main links section
            float buttonWidth = Math.Min(300, windowWidth - 40);
            float centerX = (windowWidth - buttonWidth) / 2;

            // Website button
            ImGui.SetCursorPosX(centerX);
            if (DrawStyledButton("Visit ffxiv.ju.mp", buttonWidth, UiTheme.Primary))
            {
                OpenUrl("https://ffxiv.ju.mp/");
            }

            ImGui.Spacing();

            // Discord button
            ImGui.SetCursorPosX(centerX);
            ImGui.PushStyleColor(ImGuiCol.Button, UiTheme.DiscordPrimary);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, UiTheme.DiscordHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, UiTheme.DiscordActive);
            
            if (ImGui.Button("Join Discord Server", new Vector2(buttonWidth, 35)))
            {
                OpenUrl("https://discord.gg/6agVYe6xYk");
            }
            
            ImGui.PopStyleColor(3);

            ImGui.Spacing();
            ImGui.Spacing();

            // Resources section
            UiTheme.CenteredText("Learning Resources", UiTheme.Secondary);
            ImGui.Spacing();

            // Jumping guide
            ImGui.SetCursorPosX(centerX);
            if (DrawStyledButton("Jumping Guide", buttonWidth, UiTheme.Success))
            {
                OpenUrl("https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit");
            }

            ImGui.Spacing();

            // Puzzle database
            ImGui.SetCursorPosX(centerX);
            if (DrawStyledButton("Puzzle Database", buttonWidth, UiTheme.Warning))
            {
                OpenUrl("https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879");
            }

            ImGui.Spacing();
            ImGui.Spacing();

            // LifeStream notice
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Warning);
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("LifeStream Plugin Required");
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();

            ImGui.Spacing();

            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("The LifeStream plugin is required for travel buttons to work properly.");
            ImGui.PopTextWrapPos();

            ImGui.Spacing();

            // LifeStream download
            ImGui.SetCursorPosX(centerX);
            if (DrawStyledButton("Download LifeStream", buttonWidth, UiTheme.Primary))
            {
                OpenUrl("https://github.com/NightmareXIV/Lifestream");
            }

            ImGui.Spacing();
            ImGui.Spacing();
            ImGui.Spacing();

            // Credits
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Error);
            UiTheme.CenteredText("Made with ♥");
            ImGui.PopStyleColor();
            
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Accent);
            UiTheme.CenteredText("wah");
            ImGui.PopStyleColor();
        }

        private bool DrawStyledButton(string label, float width, Vector4 color)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(color.X * 0.7f, color.Y * 0.7f, color.Z * 0.7f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(color.X * 0.5f, color.Y * 0.5f, color.Z * 0.5f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 6.0f);

            bool clicked = ImGui.Button(label, new Vector2(width, 35));

            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);

            return clicked;
        }

        private void OpenUrl(string url)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
