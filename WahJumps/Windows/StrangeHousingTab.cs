using ImGuiNET;
using WahJumps.Utilities;
using WahJumps.Windows.Components;
using System.Numerics;
using System;

namespace WahJumps.Windows
{
    public class StrangeHousingTab
    {
        private float animationTime = 0f;
        private float stickFigureX = 0f;
        private bool isJumping = false;
        private float jumpStartTime = 0f;
        private float lastObstacleX = 0f;

        public void Draw()
        {
            using var tabItem = new ImRaii.TabItem("Strange Housing");
            if (!tabItem.Success) return;

            float windowWidth = ImGui.GetWindowWidth();
            float deltaTime = ImGui.GetIO().DeltaTime;
            animationTime += deltaTime;

            // Header
            ImGui.Spacing();
            UiTheme.CenteredText("Strange Housing Community", UiTheme.Primary);
            
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("Discover and explore creative jump puzzles built by the FFXIV community");
            ImGui.PopTextWrapPos();
            
            ImGui.Separator();

            // Thank you message
            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Accent);
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("Thank you to the Strange Housing staff and community for their amazing work!");
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();

            ImGui.Spacing();

            // 2x2 button grid
            float buttonWidth = Math.Min(200, (windowWidth - 60) / 2);
            float leftButtonX = (windowWidth - (buttonWidth * 2 + 20)) / 2;
            float rightButtonX = leftButtonX + buttonWidth + 20;

            ImGui.SetCursorPosX(leftButtonX);
            if (DrawCleanButton("Visit ffxiv.ju.mp", buttonWidth, UiTheme.Primary))
            {
                OpenUrl("https://ffxiv.ju.mp/");
            }
            
            ImGui.SameLine();
            ImGui.SetCursorPosX(rightButtonX);
            if (DrawCleanButton("Join Discord Server", buttonWidth, UiTheme.DiscordPrimary))
            {
                OpenUrl("https://discord.gg/6agVYe6xYk");
            }

            // Second row
            ImGui.SetCursorPosX(leftButtonX);
            if (DrawCleanButton("Jumping Guide", buttonWidth, UiTheme.Success))
            {
                OpenUrl("https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit");
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(rightButtonX);
            if (DrawCleanButton("Puzzle Database", buttonWidth, UiTheme.Warning))
            {
                OpenUrl("https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879");
            }

            ImGui.Spacing();

            // LifeStream notice
            float centerX = (windowWidth - Math.Min(250, windowWidth - 40)) / 2;
            float lifestreamButtonWidth = Math.Min(250, windowWidth - 40);

            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Warning);
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("LifeStream Plugin Required");
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();

            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("The LifeStream plugin is required for travel buttons to work properly.");
            ImGui.PopTextWrapPos();

            ImGui.SetCursorPosX(centerX);
            if (DrawCleanButton("Download LifeStream", lifestreamButtonWidth, UiTheme.Primary))
            {
                OpenUrl("https://github.com/NightmareXIV/Lifestream");
            }

            ImGui.Spacing();

            // Credits
            UiTheme.CenteredText("Made with ♥", UiTheme.Error);
            UiTheme.CenteredText("wah", UiTheme.Accent);

            ImGui.Spacing();

            // Animation
            DrawAnimatedStickFigure(windowWidth, deltaTime);
        }

        private void DrawAnimatedStickFigure(float windowWidth, float deltaTime)
        {
            var drawList = ImGui.GetWindowDrawList();
            var windowPos = ImGui.GetWindowPos();
            var cursorPos = ImGui.GetCursorPos();
            var sceneHeight = 120f;
            var groundY = windowPos.Y + cursorPos.Y + sceneHeight - 15f;

            float speed = 80f;
            stickFigureX += speed * deltaTime;

            if (stickFigureX > windowWidth + 50)
            {
                stickFigureX = -50f;
                lastObstacleX = 0f;
            }

            float[] obstaclePositions = { 
                windowWidth * 0.15f,
                windowWidth * 0.3f,
                windowWidth * 0.45f,
                windowWidth * 0.6f,
                windowWidth * 0.75f,
                windowWidth * 0.9f
            };
            
            foreach (var obstacleX in obstaclePositions)
            {
                if (Math.Abs(stickFigureX - obstacleX) < 35f && !isJumping && obstacleX != lastObstacleX)
                {
                    isJumping = true;
                    jumpStartTime = animationTime;
                    lastObstacleX = obstacleX;
                    break;
                }
            }

            if (isJumping)
            {
                float jumpDuration = 1.0f;
                if (animationTime - jumpStartTime > jumpDuration)
                {
                    isJumping = false;
                }
            }

            drawList.AddLine(
                new Vector2(windowPos.X, groundY),
                new Vector2(windowPos.X + windowWidth, groundY),
                ImGui.GetColorU32(new Vector4(0.4f, 0.4f, 0.4f, 1.0f)),
                3.0f
            );

            for (int i = 0; i < obstaclePositions.Length; i++)
            {
                var obstacleX = obstaclePositions[i];
                
                if (i % 3 == 0)
                {
                    drawList.AddRectFilled(
                        new Vector2(windowPos.X + obstacleX - 45, groundY - 20),
                        new Vector2(windowPos.X + obstacleX - 15, groundY),
                        ImGui.GetColorU32(UiTheme.Primary)
                    );

                    drawList.AddLine(
                        new Vector2(windowPos.X + obstacleX - 15, groundY),
                        new Vector2(windowPos.X + obstacleX + 25, groundY),
                        ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)),
                        6.0f
                    );

                    drawList.AddRectFilled(
                        new Vector2(windowPos.X + obstacleX + 25, groundY - 15),
                        new Vector2(windowPos.X + obstacleX + 55, groundY),
                        ImGui.GetColorU32(UiTheme.Success)
                    );
                }
                else if (i % 3 == 1)
                {
                    float platformHeight = 35f;
                    drawList.AddRectFilled(
                        new Vector2(windowPos.X + obstacleX - 25, groundY - platformHeight),
                        new Vector2(windowPos.X + obstacleX + 25, groundY),
                        ImGui.GetColorU32(UiTheme.Warning)
                    );
                    
                    drawList.AddRectFilled(
                        new Vector2(windowPos.X + obstacleX - 25, groundY - platformHeight),
                        new Vector2(windowPos.X + obstacleX + 25, groundY - platformHeight + 3),
                        ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 0.6f, 1.0f))
                    );
                }
                else
                {
                    drawList.AddRectFilled(
                        new Vector2(windowPos.X + obstacleX - 15, groundY - 25),
                        new Vector2(windowPos.X + obstacleX + 15, groundY),
                        ImGui.GetColorU32(UiTheme.Secondary)
                    );
                    
                    drawList.AddLine(
                        new Vector2(windowPos.X + obstacleX - 15, groundY - 25),
                        new Vector2(windowPos.X + obstacleX + 15, groundY - 25),
                        ImGui.GetColorU32(new Vector4(0.8f, 0.7f, 0.9f, 1.0f)),
                        2.0f
                    );
                }
            }

            DrawBackgroundElements(drawList, windowPos, windowWidth, groundY, sceneHeight);

            float figureX = windowPos.X + stickFigureX;
            float figureY = groundY;

            if (isJumping)
            {
                float jumpProgress = (animationTime - jumpStartTime) / 1.0f;
                float jumpHeight = (float)(Math.Sin(jumpProgress * Math.PI) * 50f);
                figureY -= jumpHeight;
            }

            DrawStickFigure(drawList, figureX, figureY, isJumping, animationTime);
            ImGui.Dummy(new Vector2(windowWidth, sceneHeight));
        }

        private void DrawBackgroundElements(ImDrawListPtr drawList, Vector2 windowPos, float windowWidth, float groundY, float sceneHeight)
        {
            var bgColor = ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.35f, 0.6f));
            
            drawList.AddRectFilled(
                new Vector2(windowPos.X + windowWidth * 0.1f, groundY - 60),
                new Vector2(windowPos.X + windowWidth * 0.25f, groundY - 45),
                bgColor
            );
            
            drawList.AddRectFilled(
                new Vector2(windowPos.X + windowWidth * 0.7f, groundY - 70),
                new Vector2(windowPos.X + windowWidth * 0.85f, groundY - 50),
                bgColor
            );

            drawList.AddRectFilled(
                new Vector2(windowPos.X + windowWidth * 0.4f, groundY - 80),
                new Vector2(windowPos.X + windowWidth * 0.5f, groundY - 75),
                ImGui.GetColorU32(new Vector4(0.2f, 0.4f, 0.6f, 0.4f))
            );

            for (int i = 0; i < 4; i++)
            {
                float buildingX = windowPos.X + (windowWidth / 4) * i + (windowWidth * 0.05f);
                float buildingHeight = 40f + (i * 10f);
                drawList.AddRectFilled(
                    new Vector2(buildingX, groundY - buildingHeight),
                    new Vector2(buildingX + 30, groundY - sceneHeight + 20),
                    ImGui.GetColorU32(new Vector4(0.15f, 0.15f, 0.2f, 0.3f))
                );
            }
        }

        private void DrawStickFigure(ImDrawListPtr drawList, float x, float y, bool jumping, float time)
        {
            var color = ImGui.GetColorU32(UiTheme.Accent);
            float headRadius = 6f;
            float bodyHeight = 20f;
            float legLength = 15f;
            float armLength = 12f;

            drawList.AddCircle(new Vector2(x, y - bodyHeight - headRadius), headRadius, color, 12, 2.0f);

            drawList.AddLine(
                new Vector2(x, y - bodyHeight),
                new Vector2(x, y),
                color, 2.0f
            );

            float legOffset = jumping ? 0f : (float)(Math.Sin(time * 8) * 8);
            float armOffset = jumping ? 0f : (float)(Math.Cos(time * 8) * 6);

            if (jumping)
            {
                drawList.AddLine(
                    new Vector2(x, y),
                    new Vector2(x - 8, y + 8),
                    color, 2.0f
                );
                drawList.AddLine(
                    new Vector2(x, y),
                    new Vector2(x + 8, y + 8),
                    color, 2.0f
                );
            }
            else
            {
                drawList.AddLine(
                    new Vector2(x, y),
                    new Vector2(x - 6 + legOffset, y + legLength),
                    color, 2.0f
                );
                drawList.AddLine(
                    new Vector2(x, y),
                    new Vector2(x + 6 - legOffset, y + legLength),
                    color, 2.0f
                );
            }

            if (jumping)
            {
                drawList.AddLine(
                    new Vector2(x, y - bodyHeight + 5),
                    new Vector2(x - 10, y - bodyHeight - 5),
                    color, 2.0f
                );
                drawList.AddLine(
                    new Vector2(x, y - bodyHeight + 5),
                    new Vector2(x + 10, y - bodyHeight - 5),
                    color, 2.0f
                );
            }
            else
            {
                drawList.AddLine(
                    new Vector2(x, y - bodyHeight + 5),
                    new Vector2(x - 8 + armOffset, y - bodyHeight + 5 + armLength),
                    color, 2.0f
                );
                drawList.AddLine(
                    new Vector2(x, y - bodyHeight + 5),
                    new Vector2(x + 8 - armOffset, y - bodyHeight + 5 + armLength),
                    color, 2.0f
                );
            }
        }

        private bool DrawCleanButton(string label, float width, Vector4 color)
        {
            ImGui.PushStyleColor(ImGuiCol.Button, new Vector4(color.X * 0.8f, color.Y * 0.8f, color.Z * 0.8f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new Vector4(color.X * 0.6f, color.Y * 0.6f, color.Z * 0.6f, 1.0f));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);

            bool clicked = ImGui.Button(label, new Vector2(width, 28));

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
