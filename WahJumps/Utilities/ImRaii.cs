using System;
using Dalamud.Bindings.ImGui;
using System.Numerics;

namespace WahJumps.Utilities
{
    public static class ImRaii
    {
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
    }
}
