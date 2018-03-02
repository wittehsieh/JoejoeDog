/* 
 * Author:  Johanan Round
 */

using UnityEngine;

namespace EditorWindowFullscreen
{
    public static partial class RectExtensions
    {
        /// <summary> Returns true if a rectangle contains another rectangle </summary>
        public static bool Contains(this Rect containerRect, Rect innerRect)
        {
            return containerRect.xMin <= innerRect.xMin && containerRect.xMax >= innerRect.xMax && containerRect.yMin <= innerRect.yMin && containerRect.yMax >= innerRect.yMax;
        }

        /// <summary>
        /// The border point on the Rect which is closest to the specified point.
        /// </summary>
        public static Vector2 ClosestBorderPoint(this Rect rect, Vector2 closestToPoint)
        {
            Vector2 topLeft = new Vector2(rect.xMin, rect.yMin);
            Vector2 topRight = new Vector2(rect.xMax, rect.yMin);
            Vector2 bottomLeft = new Vector2(rect.xMin, rect.yMax);
            Vector2 bottomRight = new Vector2(rect.xMax, rect.yMax);

            Vector2 topPoint = closestToPoint.ClosestPointOnLineSegment(topLeft, topRight);
            Vector2 rightPoint = closestToPoint.ClosestPointOnLineSegment(topRight, bottomRight);
            Vector2 bottomPoint = closestToPoint.ClosestPointOnLineSegment(bottomLeft, bottomRight);
            Vector2 leftPoint = closestToPoint.ClosestPointOnLineSegment(topLeft, bottomLeft);

            float topDistance = (topPoint - closestToPoint).magnitude;
            float rightDistance = (rightPoint - closestToPoint).magnitude;
            float bottomDistance = (bottomPoint - closestToPoint).magnitude;
            float leftDistance = (leftPoint - closestToPoint).magnitude;

            if (topDistance <= rightDistance && topDistance <= bottomDistance && topDistance <= leftDistance)
            {
                /*Top distance is least*/
                return topPoint;
            }
            else if (rightDistance <= bottomDistance && rightDistance <= leftDistance)
            {
                /*Right distance is least*/
                return rightPoint;
            }
            else if (bottomDistance <= leftDistance)
            {
                /*Bottom distance is least*/
                return bottomPoint;
            }
            else
            {
                /*Left distance is least*/
                return leftPoint;
            }
        }

        /// <summary>
        /// The distance from the Rect to a point. Returns 0 if the point is inside the Rect.
        /// </summary>
        public static float DistanceToPoint(this Rect rect, Vector2 point)
        {
            if (rect.Contains(point)) return 0;
            else return (rect.ClosestBorderPoint(point) - point).magnitude;
        }
    }
}