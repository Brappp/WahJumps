using Dalamud.Bindings.ImGui;
using WahJumps.Utilities;
using WahJumps.Windows.Components;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WahJumps.Windows
{
    public class StrangeHousingTab
    {
        private readonly List<StickFigure> figures = new List<StickFigure>();
        private const int FigureCount = 2;

        private readonly List<UiElement> uiElements = new List<UiElement>();
        private readonly List<Particle> particles = new List<Particle>();
        private bool initialized = false;
        private float globalTime = 0f;
        private Random rand = new Random();

        private const float FigureHeight = 24f;

        private struct UiElement
        {
            public Vector2 Min;
            public Vector2 Max;
            public string Type;

            public Vector2 TopCenter => new Vector2((Min.X + Max.X) / 2, Min.Y);
            public float Width => Max.X - Min.X;
            public float Height => Max.Y - Min.Y;
        }

        private class StickFigure
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public FigureState State = FigureState.Idle;
            public float StateTimer = 0f;
            public bool FacingRight = true;
            public Vector4 Color;
            public int CurrentPlatformIndex = -1;

            public Vector2[] Trail = new Vector2[8];
            public int TrailIndex = 0;
            public float TrailTimer = 0f;

            public float ClimbProgress = 0f;
            public Vector2 ClimbStart;
            public Vector2 ClimbEnd;

            public float WallTimer = 0f;
            public int WallPlatformIndex = -1;

            public List<int> PlannedRoute = new List<int>();
            public int RouteIndex = 0;
            public bool HasRoute => PlannedRoute.Count > 0 && RouteIndex < PlannedRoute.Count;
        }

        private enum FigureState
        {
            Idle, Running, Jumping, Falling,
            WallSlide, Hanging, Climbing
        }

        private struct Particle
        {
            public Vector2 Pos;
            public Vector2 Vel;
            public float Life;
            public float Size;
        }

        public void Draw()
        {
            using var tabItem = new ImRaii.TabItem("Strange Housing");
            if (!tabItem.Success) return;

            var drawList = ImGui.GetWindowDrawList();
            var windowPos = ImGui.GetWindowPos();
            var windowSize = ImGui.GetWindowSize();
            float windowWidth = windowSize.X;
            float deltaTime = Math.Min(ImGui.GetIO().DeltaTime, 0.05f);
            globalTime += deltaTime;

            uiElements.Clear();
            DrawUIAndCollectElements(windowPos, windowWidth);

            if (!initialized && uiElements.Count > 0)
            {
                InitializeFigures();
                initialized = true;
            }

            UpdateParticles(deltaTime);
            UpdateFigures(deltaTime, windowPos, windowSize);

            DrawParticles(drawList);
            foreach (var fig in figures)
            {
                DrawStickFigure(drawList, fig);
            }
        }

        private void DrawUIAndCollectElements(Vector2 windowPos, float windowWidth)
        {
            ImGui.Spacing();
            var headerStart = ImGui.GetCursorScreenPos();
            UiTheme.CenteredText("Strange Housing Community", UiTheme.Primary);
            var headerEnd = ImGui.GetCursorScreenPos();
            AddElement(new Vector2(windowPos.X + 20, headerStart.Y),
                      new Vector2(windowPos.X + windowWidth - 20, headerEnd.Y), "header");

            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("Discover and explore creative jump puzzles built by the FFXIV community");
            ImGui.PopTextWrapPos();

            var sepPos = ImGui.GetCursorScreenPos();
            ImGui.Separator();
            AddElement(new Vector2(windowPos.X, sepPos.Y),
                      new Vector2(windowPos.X + windowWidth, sepPos.Y + 4), "separator");

            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Accent);
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("Thank you to the Strange Housing staff and community for their amazing work!");
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();

            ImGui.Spacing();

            float buttonWidth = Math.Min(200, (windowWidth - 60) / 2);
            float leftButtonX = (windowWidth - (buttonWidth * 2 + 20)) / 2;
            float rightButtonX = leftButtonX + buttonWidth + 20;

            AddButton(leftButtonX, buttonWidth, "Visit ffxiv.ju.mp", UiTheme.Primary, "https://ffxiv.ju.mp/");
            ImGui.SameLine();
            AddButton(rightButtonX, buttonWidth, "Join Discord Server", UiTheme.DiscordPrimary, "https://discord.gg/6agVYe6xYk");

            AddButton(leftButtonX, buttonWidth, "Jumping Guide", UiTheme.Success, "https://docs.google.com/document/d/1CrO9doADJAP1BbYq8uPAyFqzGU1fS4cemXat_YACtJI/edit");
            ImGui.SameLine();
            AddButton(rightButtonX, buttonWidth, "Puzzle Database", UiTheme.Warning, "https://docs.google.com/spreadsheets/d/1DyOqqECaNuAEntBxwv2NQ7p5rTrC1tDN9hHpcI_PNs4/edit?gid=1921920879#gid=1921920879");

            ImGui.Spacing();

            float centerX = (windowWidth - Math.Min(250, windowWidth - 40)) / 2;
            float lifestreamButtonWidth = Math.Min(250, windowWidth - 40);

            ImGui.PushStyleColor(ImGuiCol.Text, UiTheme.Warning);
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("LifeStream Plugin Required");
            ImGui.PopTextWrapPos();
            ImGui.PopStyleColor();

            var textPos = ImGui.GetCursorScreenPos();
            ImGui.PushTextWrapPos(windowWidth - 40);
            UiTheme.CenteredText("The LifeStream plugin is required for travel buttons to work properly.");
            ImGui.PopTextWrapPos();
            var textEnd = ImGui.GetCursorScreenPos();
            AddElement(new Vector2(windowPos.X + 40, textPos.Y),
                      new Vector2(windowPos.X + windowWidth - 40, textEnd.Y), "text");

            ImGui.SetCursorPosX(centerX);
            AddButton(centerX, lifestreamButtonWidth, "Download LifeStream", UiTheme.Primary, "https://github.com/NightmareXIV/Lifestream");

            ImGui.Spacing();
            ImGui.Spacing();

            var footerPos = ImGui.GetCursorScreenPos();
            UiTheme.CenteredText("Made with ♥ by wah", new Vector4(1.0f, 0.3f, 0.3f, 0.8f));
            var footerEnd = ImGui.GetCursorScreenPos();
            AddElement(new Vector2(windowPos.X + 60, footerPos.Y),
                      new Vector2(windowPos.X + windowWidth - 60, footerEnd.Y), "footer");
        }

        private void AddElement(Vector2 min, Vector2 max, string type)
        {
            uiElements.Add(new UiElement { Min = min, Max = max, Type = type });
        }

        private void AddButton(float xPos, float width, string label, Vector4 color, string url)
        {
            ImGui.SetCursorPosX(xPos);
            var btnStart = ImGui.GetCursorScreenPos();
            if (UiTheme.ColoredButton(label, color, new Vector2(width, 28)))
                OpenUrl(url);

            var btnEnd = btnStart + new Vector2(width, 28);
            uiElements.Add(new UiElement { Min = btnStart, Max = btnEnd, Type = "button" });
        }

        private void InitializeFigures()
        {
            var buttonIndices = new List<int>();
            for (int i = 0; i < uiElements.Count; i++)
            {
                if (uiElements[i].Type == "button")
                    buttonIndices.Add(i);
            }

            if (buttonIndices.Count == 0) return;

            Vector4[] colors = {
                new Vector4(0.3f, 0.75f, 1.0f, 1f),
                new Vector4(0.4f, 0.95f, 0.5f, 1f)
            };

            for (int i = 0; i < FigureCount; i++)
            {
                int btnIdx = buttonIndices[i % buttonIndices.Count];
                var btn = uiElements[btnIdx];
                figures.Add(new StickFigure
                {
                    Pos = new Vector2(btn.TopCenter.X, btn.Min.Y),
                    Color = colors[i % colors.Length],
                    StateTimer = i * 0.6f + 0.2f,
                    CurrentPlatformIndex = btnIdx
                });
            }
        }

        private void UpdateFigures(float deltaTime, Vector2 windowPos, Vector2 windowSize)
        {
            if (uiElements.Count == 0) return;

            foreach (var fig in figures)
            {
                fig.TrailTimer += deltaTime;
                if (fig.TrailTimer > 0.02f)
                {
                    fig.TrailTimer = 0f;
                    fig.Trail[fig.TrailIndex] = fig.Pos;
                    fig.TrailIndex = (fig.TrailIndex + 1) % fig.Trail.Length;
                }

                fig.StateTimer -= deltaTime;

                switch (fig.State)
                {
                    case FigureState.Idle:
                        UpdateIdle(fig, deltaTime);
                        break;
                    case FigureState.Running:
                        UpdateRunning(fig, deltaTime);
                        break;
                    case FigureState.Jumping:
                    case FigureState.Falling:
                        UpdateAirborne(fig, deltaTime, windowPos, windowSize);
                        break;
                    case FigureState.WallSlide:
                        UpdateWallSlide(fig, deltaTime);
                        break;
                    case FigureState.Hanging:
                        UpdateHanging(fig, deltaTime);
                        break;
                    case FigureState.Climbing:
                        UpdateClimbing(fig, deltaTime);
                        break;
                }
            }
        }

        private void UpdateIdle(StickFigure fig, float deltaTime)
        {
            if (fig.StateTimer <= 0)
            {
                if (fig.HasRoute)
                {
                    FollowRoute(fig);
                    return;
                }

                float r = (float)rand.NextDouble();

                if (r < 0.35f)
                {
                    if (fig.CurrentPlatformIndex >= 0 && fig.CurrentPlatformIndex < uiElements.Count)
                    {
                        var plat = uiElements[fig.CurrentPlatformIndex];
                        fig.FacingRight = fig.Pos.X < plat.TopCenter.X;
                        fig.State = FigureState.Running;
                        fig.StateTimer = 0.3f + (float)rand.NextDouble() * 0.5f;
                    }
                    else
                    {
                        fig.StateTimer = 0.5f;
                    }
                }
                else if (r < 0.55f)
                {
                    fig.FacingRight = rand.NextDouble() > 0.5;
                    fig.StateTimer = 1.0f + (float)rand.NextDouble() * 1.5f;
                }
                else if (r < 0.70f)
                {
                    int climbTarget = FindClimbTarget(fig);
                    if (climbTarget >= 0)
                    {
                        StartClimb(fig, climbTarget);
                    }
                    else
                    {
                        fig.StateTimer = 0.4f + (float)rand.NextDouble() * 0.4f;
                    }
                }
                else
                {
                    PlanRoute(fig);
                    if (!fig.HasRoute)
                    {
                        fig.StateTimer = 0.5f + (float)rand.NextDouble() * 0.5f;
                    }
                }
            }
        }

        private void PlanRoute(StickFigure fig)
        {
            fig.PlannedRoute.Clear();
            fig.RouteIndex = 0;

            var buttons = new List<int>();
            for (int i = 0; i < uiElements.Count; i++)
            {
                if (IsSolidPlatform(uiElements[i]) && i != fig.CurrentPlatformIndex)
                    buttons.Add(i);
            }

            if (buttons.Count == 0) return;

            int currentIdx = fig.CurrentPlatformIndex;
            Vector2 currentPos = fig.Pos;
            int routeLength = 1 + rand.Next(2);

            for (int step = 0; step < routeLength && buttons.Count > 0; step++)
            {
                int bestIdx = -1;
                float bestScore = float.MinValue;

                foreach (int idx in buttons)
                {
                    var plat = uiElements[idx];
                    float dx = plat.TopCenter.X - currentPos.X;
                    float dy = plat.Min.Y - currentPos.Y;
                    float dist = MathF.Sqrt(dx * dx + dy * dy);

                    if (dist > 300) continue;

                    float score = 100f - MathF.Abs(dist - 120f);

                    if (fig.PlannedRoute.Count > 0)
                    {
                        var lastPlat = uiElements[fig.PlannedRoute[fig.PlannedRoute.Count - 1]];
                        float lastDx = lastPlat.TopCenter.X - currentPos.X;
                        if (Math.Sign(dx) == Math.Sign(lastDx))
                            score += 30f;
                    }

                    if (dy > 0) score += 15f;
                    if (Math.Abs(dy) < 50) score += 20f;

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIdx = idx;
                    }
                }

                if (bestIdx >= 0)
                {
                    fig.PlannedRoute.Add(bestIdx);
                    buttons.Remove(bestIdx);
                    currentPos = uiElements[bestIdx].TopCenter;
                    currentIdx = bestIdx;
                }
                else
                {
                    break;
                }
            }
        }

        private void FollowRoute(StickFigure fig)
        {
            if (!fig.HasRoute) return;

            int targetIdx = fig.PlannedRoute[fig.RouteIndex];
            var target = uiElements[targetIdx];

            if (fig.CurrentPlatformIndex >= 0 && fig.CurrentPlatformIndex < uiElements.Count)
            {
                var currentPlat = uiElements[fig.CurrentPlatformIndex];
                float targetX = target.TopCenter.X;

                bool targetIsRight = targetX > currentPlat.TopCenter.X;
                float jumpEdgeX = targetIsRight ? currentPlat.Max.X - 8 : currentPlat.Min.X + 8;

                bool atJumpEdge = Math.Abs(fig.Pos.X - jumpEdgeX) < 12;

                if (!atJumpEdge)
                {
                    fig.State = FigureState.Running;
                    fig.FacingRight = targetIsRight;
                    fig.StateTimer = 2.0f;
                }
                else
                {
                    fig.FacingRight = targetX > fig.Pos.X;
                    float landX = target.Min.X + 15 + (float)rand.NextDouble() * Math.Max(0, target.Width - 30);
                    JumpTo(fig, new Vector2(landX, target.Min.Y));
                    fig.RouteIndex++;
                }
            }
            else
            {
                fig.PlannedRoute.Clear();
            }
        }

        private void UpdateRunning(StickFigure fig, float deltaTime)
        {
            float runSpeed = 80f;
            fig.Pos.X += (fig.FacingRight ? 1 : -1) * runSpeed * deltaTime;

            if (fig.CurrentPlatformIndex >= 0 && fig.CurrentPlatformIndex < uiElements.Count)
            {
                var plat = uiElements[fig.CurrentPlatformIndex];
                float edgeMargin = 8f;
                bool nearLeftEdge = fig.Pos.X <= plat.Min.X + edgeMargin;
                bool nearRightEdge = fig.Pos.X >= plat.Max.X - edgeMargin;

                if (nearLeftEdge && !fig.FacingRight)
                {
                    fig.Pos.X = plat.Min.X + edgeMargin;
                    fig.State = FigureState.Idle;
                    fig.StateTimer = 0.1f;
                    return;
                }
                else if (nearRightEdge && fig.FacingRight)
                {
                    fig.Pos.X = plat.Max.X - edgeMargin;
                    fig.State = FigureState.Idle;
                    fig.StateTimer = 0.1f;
                    return;
                }
            }

            if (fig.StateTimer <= 0)
            {
                fig.State = FigureState.Idle;
                fig.StateTimer = 0.1f;
            }
        }

        private void JumpTo(StickFigure fig, Vector2 targetFeet)
        {
            fig.State = FigureState.Jumping;
            fig.FacingRight = targetFeet.X > fig.Pos.X;

            float dx = targetFeet.X - fig.Pos.X;
            float dy = targetFeet.Y - fig.Pos.Y;

            const float gravity = 800f;
            float peakHeight = 60f + Math.Max(0, -dy) * 0.3f;
            float timeToPeak = MathF.Sqrt(2f * peakHeight / gravity);
            float vy0 = gravity * timeToPeak;

            float fallDist = peakHeight + dy;
            float timeToLand = fallDist > 0 ? MathF.Sqrt(2f * fallDist / gravity) : 0;

            float totalTime = timeToPeak + timeToLand;
            totalTime = Math.Max(0.25f, totalTime);

            float vx = dx / totalTime;

            fig.Vel = new Vector2(vx, -vy0);
            fig.CurrentPlatformIndex = -1;

            SpawnDust(fig.Pos, 3);
        }

        private bool IsSolidPlatform(UiElement el)
        {
            return el.Type == "button";
        }

        private void UpdateAirborne(StickFigure fig, float deltaTime, Vector2 windowPos, Vector2 windowSize)
        {
            float gravity = 800f;
            fig.Vel.Y += gravity * deltaTime;
            fig.Pos += fig.Vel * deltaTime;

            for (int i = 0; i < uiElements.Count; i++)
            {
                var plat = uiElements[i];
                if (!IsSolidPlatform(plat)) continue;

                if (fig.Vel.Y > 0 &&
                    fig.Pos.X >= plat.Min.X && fig.Pos.X <= plat.Max.X &&
                    fig.Pos.Y >= plat.Min.Y - 5 && fig.Pos.Y <= plat.Min.Y + 8)
                {
                    fig.Pos.Y = plat.Min.Y;
                    fig.CurrentPlatformIndex = i;
                    fig.State = FigureState.Idle;
                    fig.StateTimer = 0.5f + (float)rand.NextDouble() * 0.8f;
                    fig.Vel = Vector2.Zero;
                    SpawnDust(fig.Pos, 4);
                    return;
                }
            }

            if (fig.Vel.Y > 0)
            {
                for (int i = 0; i < uiElements.Count; i++)
                {
                    var plat = uiElements[i];
                    if (!IsSolidPlatform(plat)) continue;

                    bool nearLeftEdge = Math.Abs(fig.Pos.X - plat.Min.X) < 12 && Math.Abs(fig.Pos.Y - plat.Min.Y) < 15;
                    bool nearRightEdge = Math.Abs(fig.Pos.X - plat.Max.X) < 12 && Math.Abs(fig.Pos.Y - plat.Min.Y) < 15;

                    if (nearLeftEdge || nearRightEdge)
                    {
                        fig.State = FigureState.Hanging;
                        fig.Pos.X = nearLeftEdge ? plat.Min.X : plat.Max.X;
                        fig.Pos.Y = plat.Min.Y + 8;
                        fig.FacingRight = nearLeftEdge;
                        fig.CurrentPlatformIndex = i;
                        fig.Vel = Vector2.Zero;
                        fig.StateTimer = 0.3f + (float)rand.NextDouble() * 0.3f;
                        SpawnDust(fig.Pos + new Vector2(0, -5), 2);
                        return;
                    }
                }
            }

            if (fig.Vel.Y > 30)
            {
                for (int i = 0; i < uiElements.Count; i++)
                {
                    var plat = uiElements[i];
                    if (!IsSolidPlatform(plat)) continue;

                    bool touchLeft = Math.Abs(fig.Pos.X - plat.Min.X) < 8 && fig.Pos.Y > plat.Min.Y && fig.Pos.Y < plat.Max.Y;
                    bool touchRight = Math.Abs(fig.Pos.X - plat.Max.X) < 8 && fig.Pos.Y > plat.Min.Y && fig.Pos.Y < plat.Max.Y;

                    if (touchLeft || touchRight)
                    {
                        fig.State = FigureState.WallSlide;
                        fig.Pos.X = touchLeft ? plat.Min.X - 3 : plat.Max.X + 3;
                        fig.FacingRight = touchLeft;
                        fig.WallPlatformIndex = i;
                        fig.WallTimer = 0;
                        fig.Vel = Vector2.Zero;
                        SpawnDust(fig.Pos, 2);
                        return;
                    }
                }
            }

            if (fig.Vel.Y > 80 && fig.State == FigureState.Jumping)
                fig.State = FigureState.Falling;

            if (fig.Pos.X < windowPos.X + 15)
            {
                fig.Pos.X = windowPos.X + 15;
                fig.Vel.X = Math.Abs(fig.Vel.X) * 0.6f;
            }
            if (fig.Pos.X > windowPos.X + windowSize.X - 15)
            {
                fig.Pos.X = windowPos.X + windowSize.X - 15;
                fig.Vel.X = -Math.Abs(fig.Vel.X) * 0.6f;
            }
            if (fig.Pos.Y > windowPos.Y + windowSize.Y - 10)
            {
                fig.Pos.Y = windowPos.Y + windowSize.Y - 10;
                fig.State = FigureState.Idle;
                fig.StateTimer = 0.3f;
                fig.Vel = Vector2.Zero;
                SpawnDust(fig.Pos, 3);
            }
        }

        private void UpdateWallSlide(StickFigure fig, float deltaTime)
        {
            fig.WallTimer += deltaTime;
            fig.Pos.Y += 45f * deltaTime;

            if (rand.NextDouble() < deltaTime * 4)
                SpawnDust(fig.Pos + new Vector2(fig.FacingRight ? -5 : 5, -5), 1);

            if (fig.WallPlatformIndex >= 0)
            {
                var wall = uiElements[fig.WallPlatformIndex];
                if (fig.Pos.Y >= wall.Max.Y)
                {
                    fig.State = FigureState.Falling;
                    fig.Vel = new Vector2(fig.FacingRight ? -30 : 30, 50);
                    return;
                }
            }

            if (fig.WallTimer > 0.2f + (float)rand.NextDouble() * 0.2f)
            {
                fig.State = FigureState.Jumping;
                fig.Vel = new Vector2(fig.FacingRight ? -180f : 180f, -350f);
                fig.FacingRight = !fig.FacingRight;
                SpawnDust(fig.Pos, 4);
            }
        }

        private void UpdateHanging(StickFigure fig, float deltaTime)
        {
            if (fig.StateTimer <= 0)
            {
                if (rand.NextDouble() > 0.25f)
                {
                    if (fig.CurrentPlatformIndex >= 0)
                    {
                        var plat = uiElements[fig.CurrentPlatformIndex];
                        fig.Pos.Y = plat.Min.Y;
                        fig.Pos.X = fig.FacingRight ? plat.Min.X + 10 : plat.Max.X - 10;
                    }
                    fig.State = FigureState.Idle;
                    fig.StateTimer = 0.1f;
                    SpawnDust(fig.Pos, 2);
                }
                else
                {
                    fig.State = FigureState.Falling;
                    fig.Vel = new Vector2((fig.FacingRight ? -1 : 1) * 40, 20);
                    fig.CurrentPlatformIndex = -1;
                }
            }
        }

        private int FindClimbTarget(StickFigure fig)
        {
            for (int i = 0; i < uiElements.Count; i++)
            {
                if (i == fig.CurrentPlatformIndex) continue;
                if (!IsSolidPlatform(uiElements[i])) continue;

                var plat = uiElements[i];
                float dx = Math.Abs(fig.Pos.X - plat.TopCenter.X);
                float dy = plat.Min.Y - fig.Pos.Y;

                if (dx < 80 && Math.Abs(dy) > 25 && Math.Abs(dy) < 80)
                {
                    return i;
                }
            }
            return -1;
        }

        private void StartClimb(StickFigure fig, int targetIndex)
        {
            var target = uiElements[targetIndex];

            fig.State = FigureState.Climbing;
            fig.ClimbProgress = 0f;
            fig.ClimbStart = fig.Pos;

            float endX = target.TopCenter.X;
            endX = Math.Max(target.Min.X + 10, Math.Min(target.Max.X - 10, endX));
            fig.ClimbEnd = new Vector2(endX, target.Min.Y);
            fig.FacingRight = endX > fig.Pos.X;
            fig.CurrentPlatformIndex = targetIndex;

            SpawnDust(fig.Pos, 2);
        }

        private void UpdateClimbing(StickFigure fig, float deltaTime)
        {
            fig.ClimbProgress += deltaTime * 1.8f;

            float t = fig.ClimbProgress;
            t = t * t * (3f - 2f * t);
            fig.Pos = Vector2.Lerp(fig.ClimbStart, fig.ClimbEnd, t);

            if (fig.ClimbProgress >= 1f)
            {
                fig.Pos = fig.ClimbEnd;
                fig.State = FigureState.Idle;
                fig.StateTimer = 0.3f + (float)rand.NextDouble() * 0.4f;
                SpawnDust(fig.Pos, 3);
            }
        }

        private void SpawnDust(Vector2 pos, int count)
        {
            for (int i = 0; i < count; i++)
            {
                particles.Add(new Particle
                {
                    Pos = pos + new Vector2((float)(rand.NextDouble() - 0.5) * 8, 0),
                    Vel = new Vector2((float)(rand.NextDouble() - 0.5) * 35, -(float)(rand.NextDouble() * 25 + 10)),
                    Life = 0.2f + (float)rand.NextDouble() * 0.12f,
                    Size = 2f + (float)rand.NextDouble() * 1.5f
                });
            }
        }

        private void UpdateParticles(float deltaTime)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                p.Life -= deltaTime;
                if (p.Life <= 0)
                {
                    particles.RemoveAt(i);
                    continue;
                }
                p.Pos += p.Vel * deltaTime;
                p.Vel.Y += 70f * deltaTime;
                particles[i] = p;
            }
        }

        private void DrawParticles(ImDrawListPtr drawList)
        {
            foreach (var p in particles)
            {
                float a = (p.Life / 0.32f) * 0.5f;
                var c = ImGui.GetColorU32(new Vector4(0.7f, 0.7f, 0.7f, a));
                drawList.AddCircleFilled(p.Pos, p.Size * p.Life * 2.5f, c);
            }
        }

        private void DrawStickFigure(ImDrawListPtr drawList, StickFigure fig)
        {
            float speed = fig.Vel.Length();

            if (speed > 100)
            {
                for (int i = 0; i < fig.Trail.Length; i++)
                {
                    int idx = (fig.TrailIndex - i - 1 + fig.Trail.Length) % fig.Trail.Length;
                    if (fig.Trail[idx] == Vector2.Zero) continue;
                    float a = (1f - (float)i / fig.Trail.Length) * 0.15f;
                    var tc = ImGui.GetColorU32(new Vector4(fig.Color.X, fig.Color.Y, fig.Color.Z, a));
                    drawList.AddCircleFilled(fig.Trail[idx] + new Vector2(0, -12), 3f * (1f - (float)i / fig.Trail.Length), tc);
                }
            }

            var color = ImGui.GetColorU32(fig.Color);
            Vector2 feet = fig.Pos;
            float t = globalTime;

            float headBob = 0f;
            float armL = 0.3f, armR = -0.3f;
            float legL = 0.15f, legR = -0.15f;

            switch (fig.State)
            {
                case FigureState.Idle:
                    headBob = MathF.Sin(t * 2f) * 0.5f;
                    break;

                case FigureState.Running:
                    float run = t * 12f;
                    headBob = MathF.Abs(MathF.Sin(run)) * 1.5f;
                    legL = MathF.Sin(run) * 0.6f;
                    legR = MathF.Sin(run + MathF.PI) * 0.6f;
                    armL = MathF.Sin(run + MathF.PI) * 0.5f;
                    armR = MathF.Sin(run) * 0.5f;
                    break;

                case FigureState.Jumping:
                    if (fig.Vel.Y < 0)
                    {
                        armL = -0.7f; armR = -0.7f;
                        legL = -0.25f; legR = -0.25f;
                    }
                    else
                    {
                        armL = 0.5f; armR = 0.5f;
                        legL = 0.3f; legR = 0.3f;
                    }
                    break;

                case FigureState.Falling:
                    float flail = MathF.Sin(t * 10f) * 0.3f;
                    armL = 0.8f + flail;
                    armR = 0.8f - flail;
                    legL = 0.2f + flail * 0.5f;
                    legR = 0.2f - flail * 0.5f;
                    break;

                case FigureState.WallSlide:
                    if (fig.FacingRight)
                    { armL = 0.2f; armR = -1.2f; }
                    else
                    { armL = -1.2f; armR = 0.2f; }
                    legL = 0.15f; legR = 0.15f;
                    break;

                case FigureState.Hanging:
                    float swing = MathF.Sin(t * 2f) * 0.1f;
                    armL = -2.3f; armR = -2.3f;
                    legL = swing + 0.1f;
                    legR = -swing + 0.1f;
                    break;

                case FigureState.Climbing:
                    float climb = t * 8f;
                    armL = -1.2f + MathF.Sin(climb) * 0.4f;
                    armR = -1.0f + MathF.Sin(climb + MathF.PI) * 0.4f;
                    legL = MathF.Sin(climb + MathF.PI) * 0.4f;
                    legR = MathF.Sin(climb) * 0.4f;
                    break;
            }

            float dir = fig.FacingRight ? 1f : -1f;

            Vector2 hip = feet + new Vector2(0, -8);
            Vector2 shoulder = feet + new Vector2(0, -16);
            Vector2 head = feet + new Vector2(0, -21 - headBob);

            drawList.AddLine(hip, shoulder, color, 2f);
            drawList.AddCircleFilled(head, 4.5f, color);

            float armLen = 6.5f;
            Vector2 armEndL = shoulder + new Vector2(MathF.Sin(armL) * armLen * dir, MathF.Cos(armL) * armLen);
            Vector2 armEndR = shoulder + new Vector2(MathF.Sin(armR) * armLen * dir, MathF.Cos(armR) * armLen);
            drawList.AddLine(shoulder, armEndL, color, 1.5f);
            drawList.AddLine(shoulder, armEndR, color, 1.5f);

            float legLen = 7f;
            Vector2 legEndL = hip + new Vector2(MathF.Sin(legL) * legLen * dir - 2, MathF.Cos(legL) * legLen);
            Vector2 legEndR = hip + new Vector2(MathF.Sin(legR) * legLen * dir + 2, MathF.Cos(legR) * legLen);
            drawList.AddLine(hip, legEndL, color, 1.5f);
            drawList.AddLine(hip, legEndR, color, 1.5f);
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch { }
        }
    }
}
