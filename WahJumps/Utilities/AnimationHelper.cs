using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace WahJumps.Utilities
{
    public static class AnimationHelper
    {
        // Easing functions
        public enum EasingType
        {
            Linear,
            EaseInQuad,
            EaseOutQuad,
            EaseInOutQuad,
            EaseInCubic,
            EaseOutCubic,
            EaseInOutCubic,
            EaseInElastic,
            EaseOutElastic,
            EaseInOutElastic,
            EaseInBounce,
            EaseOutBounce,
            EaseInOutBounce
        }

        public static float ApplyEasing(float t, EasingType easingType)
        {
            switch (easingType)
            {
                case EasingType.Linear:
                    return t;
                case EasingType.EaseInQuad:
                    return t * t;
                case EasingType.EaseOutQuad:
                    return t * (2 - t);
                case EasingType.EaseInOutQuad:
                    return t < 0.5f ? 2 * t * t : -1 + (4 - 2 * t) * t;
                case EasingType.EaseInCubic:
                    return t * t * t;
                case EasingType.EaseOutCubic:
                    return (--t) * t * t + 1;
                case EasingType.EaseInOutCubic:
                    return t < 0.5f ? 4 * t * t * t : (t - 1) * (2 * t - 2) * (2 * t - 2) + 1;
                case EasingType.EaseInElastic:
                    return EaseInElastic(t);
                case EasingType.EaseOutElastic:
                    return EaseOutElastic(t);
                case EasingType.EaseInOutElastic:
                    return t < 0.5f ? 0.5f * EaseInElastic(t * 2) : 0.5f + 0.5f * EaseOutElastic((t - 0.5f) * 2);
                case EasingType.EaseInBounce:
                    return 1 - EaseOutBounce(1 - t);
                case EasingType.EaseOutBounce:
                    return EaseOutBounce(t);
                case EasingType.EaseInOutBounce:
                    return t < 0.5f ? 0.5f * (1 - EaseOutBounce(1 - t * 2)) : 0.5f + 0.5f * EaseOutBounce((t - 0.5f) * 2);
                default:
                    return t;
            }
        }

        // Animate a float value over time
        public static float AnimateFloat(float current, float target, float speed, EasingType easingType = EasingType.Linear)
        {
            if (Math.Abs(current - target) < 0.001f)
                return target;

            float deltaTime = ImGui.GetIO().DeltaTime;
            float step = speed * deltaTime;

            if (easingType == EasingType.Linear)
            {
                // Simple linear interpolation
                return current + Math.Sign(target - current) * Math.Min(step, Math.Abs(target - current));
            }
            else
            {
                // Calculate progress from current to target
                float distance = Math.Abs(target - current);
                float totalDistance = distance / step;
                float progress = 1 - Math.Min(distance / totalDistance, 1);

                float easedProgress = ApplyEasing(progress, easingType);

                // Use eased progress to calculate new value
                return current + Math.Sign(target - current) * Math.Min(step * (1 - easedProgress), distance);
            }
        }

        // Animate a Vector2 value over time
        public static Vector2 AnimateVector2(Vector2 current, Vector2 target, float speed, EasingType easingType = EasingType.Linear)
        {
            if (Vector2.Distance(current, target) < 0.001f)
                return target;

            float deltaTime = ImGui.GetIO().DeltaTime;

            if (easingType == EasingType.Linear)
            {
                // Simple linear interpolation
                return Vector2.Lerp(current, target, Math.Min(speed * deltaTime, 1.0f));
            }
            else
            {
                // Calculate progress
                float distance = Vector2.Distance(current, target);
                float totalDistance = distance / (speed * deltaTime);
                float progress = 1 - Math.Min(distance / totalDistance, 1);

                float easedProgress = ApplyEasing(progress, easingType);

                // Interpolate based on eased progress
                return Vector2.Lerp(current, target, Math.Min(speed * deltaTime * (1 - easedProgress), 1.0f));
            }
        }

        // Animate a color value over time
        public static Vector4 AnimateColor(Vector4 current, Vector4 target, float speed)
        {
            float r = AnimateFloat(current.X, target.X, speed);
            float g = AnimateFloat(current.Y, target.Y, speed);
            float b = AnimateFloat(current.Z, target.Z, speed);
            float a = AnimateFloat(current.W, target.W, speed);

            return new Vector4(r, g, b, a);
        }

        // Helper elastic easing functions
        private static float EaseInElastic(float t)
        {
            const float c4 = (2 * MathF.PI) / 3;

            return t == 0
                ? 0
                : t == 1
                ? 1
                : -MathF.Pow(2, 10 * t - 10) * MathF.Sin((t * 10 - 10.75f) * c4);
        }

        private static float EaseOutElastic(float t)
        {
            const float c4 = (2 * MathF.PI) / 3;

            return t == 0
                ? 0
                : t == 1
                ? 1
                : MathF.Pow(2, -10 * t) * MathF.Sin((t * 10 - 0.75f) * c4) + 1;
        }

        private static float EaseOutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;

            if (t < 1 / d1)
            {
                return n1 * t * t;
            }
            else if (t < 2 / d1)
            {
                return n1 * (t -= 1.5f / d1) * t + 0.75f;
            }
            else if (t < 2.5 / d1)
            {
                return n1 * (t -= 2.25f / d1) * t + 0.9375f;
            }
            else
            {
                return n1 * (t -= 2.625f / d1) * t + 0.984375f;
            }
        }

        public static void DrawSpinner(string id, float radius, float thickness, Vector4 color, float speed = 2.0f)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();
            Vector2 center = new Vector2(pos.X + radius, pos.Y + radius);

            // Calculate rotation angle based on time
            float t = (float)ImGui.GetTime() * speed;

            int segments = 8;
            for (int i = 0; i < segments; i++)
            {
                float segmentAngle = (MathF.PI * 2.0f / segments);
                float startAngle = t + i * segmentAngle;
                float endAngle = startAngle + segmentAngle * 0.8f;

                float alpha = 0.1f + 0.9f * (i / (float)segments);
                Vector4 segmentColor = new Vector4(color.X, color.Y, color.Z, color.W * alpha);

                drawList.PathArcTo(center, radius, startAngle, endAngle, 12);
                drawList.PathStroke(ImGui.GetColorU32(segmentColor), ImDrawFlags.None, thickness);
            }

            // Advance cursor
            ImGui.Dummy(new Vector2(radius * 2, radius * 2));
        }

        public static void DrawGradientProgressBar(float fraction, Vector2 size, Vector4 startColor, Vector4 endColor)
        {
            ImDrawListPtr drawList = ImGui.GetWindowDrawList();
            Vector2 pos = ImGui.GetCursorScreenPos();

            // Background
            drawList.AddRectFilled(pos, new Vector2(pos.X + size.X, pos.Y + size.Y),
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)),
                4.0f);

            // Gradient bar
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
            }

            // Advance cursor
            ImGui.Dummy(size);
        }
    }
}
