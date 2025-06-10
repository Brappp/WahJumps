using System;
using ImGuiNET;
using System.Numerics;
using WahJumps.Data;

namespace WahJumps.Utilities
{
    /// <summary>
    /// RAII pattern for ImGui to ensure proper Begin/End pairing
    /// </summary>
    public static class ImRaii
    {
        public class Table : IDisposable
        {
            private readonly bool _success;

            public Table(string id, int columns, ImGuiTableFlags flags = ImGuiTableFlags.None)
            {
                _success = ImGui.BeginTable(id, columns, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndTable();
            }
        }

        public class TabBar : IDisposable
        {
            private readonly bool _success;

            public TabBar(string id, ImGuiTabBarFlags flags = ImGuiTabBarFlags.None)
            {
                _success = ImGui.BeginTabBar(id, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndTabBar();
            }
        }

        public class TabItem : IDisposable
        {
            private readonly bool _success;

            public TabItem(string label)
            {
                _success = ImGui.BeginTabItem(label);
            }

            public TabItem(string label, ref bool open)
            {
                _success = ImGui.BeginTabItem(label, ref open);
            }

            public TabItem(string label, ImGuiTabItemFlags flags)
            {
                bool dummy = true;
                _success = ImGui.BeginTabItem(label, ref dummy, flags);
            }

            public TabItem(string label, ref bool open, ImGuiTabItemFlags flags)
            {
                _success = ImGui.BeginTabItem(label, ref open, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndTabItem();
            }
        }

        public class Child : IDisposable
        {
            private readonly bool _success;

            public Child(string id, Vector2 size = default, bool border = false, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            {
                _success = ImGui.BeginChild(id, size, border, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndChild();
            }
        }

        public class Popup : IDisposable
        {
            private readonly bool _success;

            public Popup(string id, ImGuiWindowFlags flags = ImGuiWindowFlags.None)
            {
                _success = ImGui.BeginPopup(id, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndPopup();
            }
        }

        public class StyleVar : IDisposable
        {
            private readonly int _count;

            public StyleVar(ImGuiStyleVar styleVar, Vector2 value)
            {
                ImGui.PushStyleVar(styleVar, value);
                _count = 1;
            }

            public StyleVar(ImGuiStyleVar styleVar, float value)
            {
                ImGui.PushStyleVar(styleVar, value);
                _count = 1;
            }

            public StyleVar(params (ImGuiStyleVar Idx, float Val)[] vars)
            {
                _count = vars.Length;
                foreach (var (idx, val) in vars)
                    ImGui.PushStyleVar(idx, val);
            }

            public StyleVar(params (ImGuiStyleVar Idx, Vector2 Val)[] vars)
            {
                _count = vars.Length;
                foreach (var (idx, val) in vars)
                    ImGui.PushStyleVar(idx, val);
            }

            public void Dispose()
            {
                ImGui.PopStyleVar(_count);
            }
        }

        public class StyleColor : IDisposable
        {
            private readonly int _count;

            public StyleColor(ImGuiCol idx, Vector4 color)
            {
                ImGui.PushStyleColor(idx, color);
                _count = 1;
            }

            public StyleColor(params (ImGuiCol, Vector4)[] colors)
            {
                foreach (var (idx, color) in colors)
                    ImGui.PushStyleColor(idx, color);
                _count = colors.Length;
            }

            public void Dispose()
            {
                ImGui.PopStyleColor(_count);
            }
        }

        public class Group : IDisposable
        {
            public Group()
            {
                ImGui.BeginGroup();
            }

            public void Dispose()
            {
                ImGui.EndGroup();
            }
        }

        public class Combo : IDisposable
        {
            private readonly bool _success;

            public Combo(string label, string previewValue)
            {
                _success = ImGui.BeginCombo(label, previewValue);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.EndCombo();
            }
        }

        public class TreeNode : IDisposable
        {
            private readonly bool _success;

            public TreeNode(string label, ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.None)
            {
                _success = ImGui.TreeNodeEx(label, flags);
            }

            public bool Success => _success;

            public void Dispose()
            {
                if (_success)
                    ImGui.TreePop();
            }
        }

        public class Tooltip : IDisposable
        {
            public Tooltip()
            {
                ImGui.BeginTooltip();
            }

            public void Dispose()
            {
                ImGui.EndTooltip();
            }
        }

        // Static methods for quick access
        public static StyleColor PushColor(ImGuiCol idx, Vector4 color) => new StyleColor(idx, color);
        public static StyleVar PushStyle(ImGuiStyleVar idx, float value) => new StyleVar(idx, value);
        public static StyleVar PushStyle(ImGuiStyleVar idx, Vector2 value) => new StyleVar(idx, value);

        public class FontScale : IDisposable
        {
            private readonly float originalScale;

            public FontScale(float scale)
            {
                originalScale = ImGui.GetFont().Scale;
                ImGui.SetWindowFontScale(scale);
            }

            public void Dispose()
            {
                ImGui.SetWindowFontScale(originalScale);
            }
        }

        public class Id : IDisposable
        {
            public Id(string id)
            {
                ImGui.PushID(id);
            }

            public Id(int id)
            {
                ImGui.PushID(id);
            }

            public void Dispose()
            {
                ImGui.PopID();
            }
        }

        public class ConditionalStyle : IDisposable
        {
            private readonly StyleVar? styleVar;
            private readonly StyleColor? styleColor;

            public ConditionalStyle(ImGuiStyleVar idx, float value, bool condition)
            {
                styleVar = condition ? new StyleVar(idx, value) : null;
            }

            public ConditionalStyle(ImGuiStyleVar idx, Vector2 value, bool condition)
            {
                styleVar = condition ? new StyleVar(idx, value) : null;
            }

            public ConditionalStyle(ImGuiCol idx, Vector4 color, bool condition)
            {
                styleColor = condition ? new StyleColor(idx, color) : null;
            }

            public void Dispose()
            {
                styleVar?.Dispose();
                styleColor?.Dispose();
            }
        }

        public class Disabled : IDisposable
        {
            private readonly bool wasDisabled;

            public Disabled(bool condition = true)
            {
                wasDisabled = condition;
                if (condition)
                    ImGui.BeginDisabled();
            }

            public void Dispose()
            {
                if (wasDisabled)
                    ImGui.EndDisabled();
            }
        }

        public static bool StyledButton(string text, Vector2 size, Vector4 normalColor, Vector4 hoverColor, Vector4? activeColor = null)
        {
            using var colors = new StyleColor(
                (ImGuiCol.Button, normalColor),
                (ImGuiCol.ButtonHovered, hoverColor),
                (ImGuiCol.ButtonActive, activeColor ?? new Vector4(hoverColor.X * 1.3f, hoverColor.Y * 1.3f, hoverColor.Z * 1.3f, 1.0f)),
                (ImGuiCol.Text, Vector4.One)
            );
            using var roundingStyle = new StyleVar(ImGuiStyleVar.FrameRounding, 10.0f);
            using var paddingStyle = new StyleVar(ImGuiStyleVar.FramePadding, new Vector2(12, 8));

            return ImGui.Button(text, size);
        }

        public class TimerWindowStyle : IDisposable
        {
            private readonly StyleVar roundingStyle;
            private readonly StyleVar paddingStyle;
            private readonly StyleVar spacingStyle;
            private readonly StyleVar frameRoundingStyle;
            private readonly StyleVar buttonAlignStyle;
            private readonly StyleColor colors;

            public TimerWindowStyle(SpeedrunManager.SpeedrunState state)
            {
                roundingStyle = new StyleVar(ImGuiStyleVar.WindowRounding, 12.0f);
                paddingStyle = new StyleVar(ImGuiStyleVar.WindowPadding, new Vector2(20, 16));
                spacingStyle = new StyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(10, 10));
                frameRoundingStyle = new StyleVar(ImGuiStyleVar.FrameRounding, 8.0f);
                buttonAlignStyle = new StyleVar(ImGuiStyleVar.ButtonTextAlign, new Vector2(0.5f, 0.5f));

                Vector4 bgColor = state switch
                {
                    SpeedrunManager.SpeedrunState.Running => new Vector4(0.05f, 0.15f, 0.05f, 0.98f),
                    SpeedrunManager.SpeedrunState.Countdown => new Vector4(0.15f, 0.08f, 0.03f, 0.98f),
                    SpeedrunManager.SpeedrunState.Finished => new Vector4(0.15f, 0.12f, 0.03f, 0.98f),
                    _ => new Vector4(0.06f, 0.06f, 0.15f, 0.98f)
                };

                colors = new StyleColor(ImGuiCol.WindowBg, bgColor);
            }

            public void Dispose()
            {
                colors.Dispose();
                buttonAlignStyle.Dispose();
                frameRoundingStyle.Dispose();
                spacingStyle.Dispose();
                paddingStyle.Dispose();
                roundingStyle.Dispose();
            }
        }

        public static void CenteredText(string text, Vector4? color = null, float fontSize = 1.0f)
        {
            var textSize = ImGui.CalcTextSize(text) * fontSize;
            var contentWidth = ImGui.GetContentRegionAvail().X;
            var centerX = (contentWidth - textSize.X) * 0.5f;

            ImGui.SetCursorPosX(centerX);

            using var colorStyle = color.HasValue ? new StyleColor(ImGuiCol.Text, color.Value) : null;
            using var fontScale = fontSize != 1.0f ? new FontScale(fontSize) : null;

            ImGui.Text(text);
        }

        public class StyledTable : IDisposable
        {
            private readonly bool success;
            private readonly StyleVar cellPaddingStyle;
            private readonly StyleVar itemSpacingStyle;
            private readonly StyleColor colors;

            public StyledTable(string id, int columns, ImGuiTableFlags flags = ImGuiTableFlags.None)
            {
                cellPaddingStyle = new StyleVar(ImGuiStyleVar.CellPadding, new Vector2(8, 4));
                itemSpacingStyle = new StyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(8, 6));

                colors = new StyleColor(
                    (ImGuiCol.TableHeaderBg, new Vector4(0.15f, 0.35f, 0.5f, 1.0f)),
                    (ImGuiCol.TableBorderStrong, new Vector4(0.5f, 0.5f, 0.5f, 1.0f)),
                    (ImGuiCol.TableBorderLight, new Vector4(0.3f, 0.3f, 0.3f, 1.0f)),
                    (ImGuiCol.TableRowBg, new Vector4(0.18f, 0.18f, 0.2f, 1.0f)),
                    (ImGuiCol.TableRowBgAlt, new Vector4(0.25f, 0.25f, 0.28f, 1.0f))
                );

                success = ImGui.BeginTable(id, columns, flags);
            }

            public bool Success => success;

            public void Dispose()
            {
                if (success)
                    ImGui.EndTable();
                colors.Dispose();
                itemSpacingStyle.Dispose();
                cellPaddingStyle.Dispose();
            }
        }
    }
}
