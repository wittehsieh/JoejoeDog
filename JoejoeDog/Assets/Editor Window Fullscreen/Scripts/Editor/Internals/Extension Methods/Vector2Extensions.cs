/* 
 * Author:  Johanan Round
 */

using UnityEngine;
using System;

namespace EditorWindowFullscreen
{
    public static partial class Vector2Extensions
    {

        /// <summary>
        /// Get the closest distance from this Vector2 point to a line
        /// </summary>
        public static float DistanceToLine(this Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            return (point.ClosestPointOnLine(linePoint1, linePoint2) - point).magnitude;
        }

        /// <summary>
        /// Get the closest distance from this Vector2 point to a line segment
        /// </summary>
        public static float DistanceToLineSegment(this Vector2 point, Vector2 lineSegmentStart, Vector2 lineSegmentEnd)
        {
            return (point.ClosestPointOnLineSegment(lineSegmentStart, lineSegmentEnd) - point).magnitude;
        }

        /// <summary>
        /// Get the point on a line which is closest to this Vector2 point
        /// </summary>
        public static Vector2 ClosestPointOnLine(this Vector2 point, Vector2 linePoint1, Vector2 linePoint2)
        {
            if (linePoint1 == linePoint2) return linePoint1;
            Vector2 segment = (linePoint2 - linePoint1);
            Vector2 relativePoint = point - linePoint1;
            return linePoint1 + (Vector2.Dot(relativePoint, segment) / segment.magnitude * segment / segment.magnitude);
        }

        /// <summary>
        /// Get the point on a line segment which is closest to this Vector2 point
        /// </summary>
        public static Vector2 ClosestPointOnLineSegment(this Vector2 point, Vector2 lineSegmentStart, Vector2 lineSegmentEnd)
        {
            if (lineSegmentStart == lineSegmentEnd) return lineSegmentStart;
            Vector2 lineSegmentAtZero = lineSegmentEnd - lineSegmentStart;
            Vector2 relativePoint = point - lineSegmentStart;

            float projectionLength = Mathf.Max(0, Mathf.Min(lineSegmentAtZero.magnitude, Vector2.Dot(relativePoint, lineSegmentAtZero) / lineSegmentAtZero.magnitude));
            return lineSegmentStart + (projectionLength * lineSegmentAtZero / lineSegmentAtZero.magnitude);
        }
    }
}