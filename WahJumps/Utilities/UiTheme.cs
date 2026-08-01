using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using WahJumps.Models;

namespace WahJumps.Utilities
{
    public static class UiTheme
    {
        public static readonly Vector4 Primary = new Vector4(0.30f, 0.62f, 0.84f, 1.0f);
        public static readonly Vector4 Accent = new Vector4(0.847f, 0.42f, 0.467f, 1.0f);
        public static readonly Vector4 Success = new Vector4(0.35f, 0.72f, 0.51f, 1.0f);
        public static readonly Vector4 Warning = new Vector4(0.88f, 0.73f, 0.38f, 1.0f);
        public static readonly Vector4 Error = new Vector4(0.85f, 0.40f, 0.40f, 1.0f);
        public static readonly Vector4 Gray = new Vector4(0.55f, 0.57f, 0.61f, 1.0f);
        public static readonly Vector4 DiscordPrimary = new Vector4(0.35f, 0.40f, 0.90f, 1.0f);

        public static readonly Vector4 TextBright = new Vector4(0.88f, 0.90f, 0.94f, 1.0f);
        public static readonly Vector4 TextDim = new Vector4(0.70f, 0.73f, 0.78f, 1.0f);

        public static readonly Vector4 PanelBg = new Vector4(0.115f, 0.13f, 0.165f, 1.0f);
        public static readonly Vector4 PanelBg2 = new Vector4(0.14f, 0.16f, 0.20f, 1.0f);
        public static readonly Vector4 PanelHover = new Vector4(0.18f, 0.21f, 0.26f, 1.0f);
        public static readonly Vector4 PanelActive = new Vector4(0.21f, 0.24f, 0.30f, 1.0f);
        public static readonly Vector4 SelectionBg = new Vector4(0.14f, 0.25f, 0.36f, 1.0f);
        public static readonly Vector4 SelectionActive = new Vector4(0.17f, 0.30f, 0.44f, 1.0f);
        public static readonly Vector4 SidebarBg = new Vector4(0.095f, 0.105f, 0.135f, 1.0f);
        public static readonly Vector4 SoftBorder = new Vector4(0.17f, 0.19f, 0.24f, 0.9f);
        public static readonly Vector4 SearchBg = new Vector4(0.165f, 0.19f, 0.245f, 1.0f);
        public static readonly Vector4 SearchBorder = new Vector4(0.26f, 0.31f, 0.39f, 0.9f);

        public static readonly Vector4 Rating1Star = new Vector4(0.50f, 0.78f, 0.54f, 1.0f);
        public static readonly Vector4 Rating2Star = new Vector4(0.39f, 0.69f, 0.89f, 1.0f);
        public static readonly Vector4 Rating3Star = new Vector4(0.89f, 0.79f, 0.37f, 1.0f);
        public static readonly Vector4 Rating4Star = new Vector4(0.88f, 0.57f, 0.36f, 1.0f);
        public static readonly Vector4 Rating5Star = new Vector4(0.88f, 0.42f, 0.42f, 1.0f);
        public static readonly Vector4 RatingSpecial = new Vector4(0.71f, 0.55f, 0.88f, 1.0f);

        public static Vector4 GetRatingColor(string rating)
        {
            if (string.IsNullOrEmpty(rating)) return RatingSpecial;

            return rating switch
            {
                "1★" or "★" => Rating1Star,
                "2★" or "★★" => Rating2Star,
                "3★" or "★★★" => Rating3Star,
                "4★" or "★★★★" => Rating4Star,
                "5★" or "★★★★★" => Rating5Star,
                _ when rating.Contains("★★★★★") => Rating5Star,
                _ when rating.Contains("★★★★") => Rating4Star,
                _ when rating.Contains("★★★") => Rating3Star,
                _ when rating.Contains("★★") => Rating2Star,
                _ when rating.Contains("★") => Rating1Star,
                _ => RatingSpecial
            };
        }

        public static Vector4 GetSizeBucketColor(int count) => count switch
        {
            < 10 => new Vector4(0.6f, 0.6f, 0.6f, 1.0f),
            < 50 => new Vector4(0.7f, 0.6f, 0.5f, 1.0f),
            < 100 => new Vector4(0.4f, 0.8f, 0.8f, 1.0f),
            _ => new Vector4(0.4f, 0.8f, 0.4f, 1.0f)
        };

        public static string FormatTravelCommand(JumpPuzzleData puzzle)
        {
            if (puzzle == null) return string.Empty;

            var world = puzzle.World;
            var address = puzzle.Address;

            if (string.IsNullOrEmpty(address)) return $"/travel {world}";

            if (address.Contains("Room"))
            {
                var roomIndex = address.IndexOf("Room", StringComparison.Ordinal);
                if (roomIndex > 0)
                    address = address.Substring(0, roomIndex).Trim();
            }
            else if (address.Contains("Apartment"))
            {
                var apartmentIndex = address.IndexOf("Apartment", StringComparison.Ordinal);
                if (apartmentIndex > 0)
                {
                    var apartmentPart = address.Substring(apartmentIndex + 9).Trim();
                    address = address.Substring(0, apartmentIndex).Trim();

                    if (address.Contains("Wing 2"))
                    {
                        address = address.Replace("Wing 2", "subdivision").Trim();
                    }
                    else if (address.Contains("Wing 1"))
                    {
                        address = address.Replace("Wing 1", "").Trim();
                    }

                    address = $"{address} Apartment {apartmentPart}";
                }
            }

            return $"/travel {world} {address}";
        }

        public static bool Hyperlink(string text, string? id = null)
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

                var min = ImGui.GetItemRectMin();
                var max = ImGui.GetItemRectMax();
                min.Y = max.Y;
                ImGui.GetWindowDrawList().AddLine(min, max, ImGui.GetColorU32(Primary), 1.0f);
            }

            ImGui.PopStyleColor(4);
            return clicked;
        }

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

        public static void StyleTable()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(6, 3));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 3));

            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0.13f, 0.15f, 0.19f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, new Vector4(0.20f, 0.22f, 0.27f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableBorderLight, new Vector4(0.16f, 0.18f, 0.22f, 1.0f));

            ImGui.PushStyleColor(ImGuiCol.TableRowBg, new Vector4(0.105f, 0.115f, 0.145f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TableRowBgAlt, new Vector4(0.125f, 0.14f, 0.17f, 1.0f));
        }

        public static void EndTableStyle()
        {
            ImGui.PopStyleColor(5);
            ImGui.PopStyleVar(2);
        }

        public static void ApplyGlobalStyle()
        {
            ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(6, 3));
            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(6, 4));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(10, 10));
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, 4.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.GrabRounding, 3.0f);
            ImGui.PushStyleVar(ImGuiStyleVar.ScrollbarSize, 10.0f);

            ImGui.PushStyleColor(ImGuiCol.FrameBg, PanelBg);
            ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, PanelHover);
            ImGui.PushStyleColor(ImGuiCol.FrameBgActive, PanelActive);
            ImGui.PushStyleColor(ImGuiCol.Button, PanelBg2);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, PanelHover);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, SelectionBg);
            ImGui.PushStyleColor(ImGuiCol.Header, SelectionBg);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, PanelHover);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, SelectionActive);
            ImGui.PushStyleColor(ImGuiCol.PopupBg, new Vector4(0.10f, 0.11f, 0.14f, 0.98f));
            ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.08f, 0.09f, 0.12f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, new Vector4(0.11f, 0.13f, 0.17f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.Border, SoftBorder);
        }

        public static void EndGlobalStyle()
        {
            ImGui.PopStyleColor(13);
            ImGui.PopStyleVar(7);
        }

        public static bool ColoredButton(string label, Vector4 color, Vector2? size = null, string? tooltip = null)
        {
            Vector2 buttonSize = size ?? Vector2.Zero;

            var baseColor = new Vector4(color.X * 0.8f, color.Y * 0.8f, color.Z * 0.8f, 1.0f);
            var activeColor = new Vector4(color.X * 0.6f, color.Y * 0.6f, color.Z * 0.6f, 1.0f);

            ImGui.PushStyleColor(ImGuiCol.Button, baseColor);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, color);
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, activeColor);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4.0f);

            bool clicked = ImGui.Button(label, buttonSize);

            ImGui.PopStyleVar();
            ImGui.PopStyleColor(3);

            if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(tooltip);
            }

            return clicked;
        }
    }
}
