using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Specialized drawing methods for <see cref="Gizmos"/>.</summary>
    internal static class GizmosEx
    {
        /// <summary>Draws a wireframe circle outline.</summary>
        /// <param name="position">The center position of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="direction">The plane the circle will be drawn on.</param>
        /// <param name="segments">The amount of segments the circle uses.</param>
        public static void DrawWireCircle(Vector3 position, float radius, Vector3 direction, int segments = 32)
        {
            float angle = 0f;
            Quaternion rot = Quaternion.LookRotation(direction, Vector3.up);
            Vector3 lastPoint = Vector3.zero;
            Vector3 thisPoint = Vector3.zero;

            for (int i = 0; i < segments + 1; i++)
            {
                thisPoint.x = Mathf.Sin(Mathf.Deg2Rad * angle) * radius;
                thisPoint.y = Mathf.Cos(Mathf.Deg2Rad * angle) * radius;

                if (i > 0)
                {
                    Gizmos.DrawLine(rot * lastPoint + position, rot * thisPoint + position);
                }

                lastPoint = thisPoint;
                angle += 360f / segments;
            }
        }

        /// <summary>Draws a wireframe spotlight outline.</summary>
        /// <param name="position">The center position of the light source.</param>
        /// <param name="lightRadius">The radius of the light source.</param>
        /// <param name="spotCutoff">The spot cutoff angle (0-180).</param>
        /// <param name="forward">The forward direction of the light.</param>
        /// <param name="up">The up direction of the light.</param>
        public static void DrawWireSpot(Vector3 position, float lightRadius, float spotCutoff, Vector3 forward, Vector3 up)
        {
            var color = Gizmos.color;

            // hack for angles beyond 180 degrees.
            if (spotCutoff > 90f)
                forward = -forward;

            var forwardRadiusCenter = position + forward * lightRadius;

            float halfAngleRad = spotCutoff * Mathf.Deg2Rad;
            float circleRadius = lightRadius * Mathf.Tan(halfAngleRad);
            DrawWireCircle(forwardRadiusCenter, circleRadius, forward);

            var right = Vector3.Cross(forward, up);

            var ro = right * circleRadius;
            var pl = forwardRadiusCenter - ro;
            var pr = forwardRadiusCenter + ro;
            var uo = up * circleRadius;
            var pu = forwardRadiusCenter + uo;
            var pd = forwardRadiusCenter - uo;

            Gizmos.DrawIcon(forwardRadiusCenter, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingWhite4x4.png", false, color);
            Gizmos.DrawIcon(pl, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingWhite4x4.png", false, color);
            Gizmos.DrawIcon(pr, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingWhite4x4.png", false, color);
            Gizmos.DrawIcon(pu, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingWhite4x4.png", false, color);
            Gizmos.DrawIcon(pd, "Packages/de.alpacait.dynamiclighting/AlpacaIT.DynamicLighting/Gizmos/DynamicLightingWhite4x4.png", false, color);

            Gizmos.DrawLine(position, pl);
            Gizmos.DrawLine(position, pr);
            Gizmos.DrawLine(position, pu);
            Gizmos.DrawLine(position, pd);
        }
    }
}