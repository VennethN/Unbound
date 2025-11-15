using UnityEngine;

namespace Unbound.Camera
{
    /// <summary>
    /// Defines the boundaries within which the camera can move.
    /// This prevents the camera from showing areas outside the game map.
    /// </summary>
    [System.Serializable]
    public struct CameraBounds
    {
        [Tooltip("Minimum X position the camera can reach")]
        public float minX;

        [Tooltip("Maximum X position the camera can reach")]
        public float maxX;

        [Tooltip("Minimum Y position the camera can reach")]
        public float minY;

        [Tooltip("Maximum Y position the camera can reach")]
        public float maxY;

        /// <summary>
        /// Creates a new CameraBounds with the specified values.
        /// </summary>
        public CameraBounds(float minX, float maxX, float minY, float maxY)
        {
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
        }

        /// <summary>
        /// Clamps a position to stay within these bounds.
        /// </summary>
        /// <param name="position">The position to clamp</param>
        /// <returns>The clamped position</returns>
        public Vector3 ClampPosition(Vector3 position)
        {
            return new Vector3(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY),
                position.z
            );
        }

        /// <summary>
        /// Checks if a position is within these bounds.
        /// </summary>
        /// <param name="position">The position to check</param>
        /// <returns>True if the position is within bounds</returns>
        public bool Contains(Vector3 position)
        {
            return position.x >= minX && position.x <= maxX &&
                   position.y >= minY && position.y <= maxY;
        }

        /// <summary>
        /// Gets the center point of these bounds.
        /// </summary>
        public Vector3 Center => new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0f);

        /// <summary>
        /// Gets the size of these bounds.
        /// </summary>
        public Vector3 Size => new Vector3(maxX - minX, maxY - minY, 0f);

        /// <summary>
        /// Creates a union of multiple bounds (the bounds that encompasses all given bounds).
        /// </summary>
        /// <param name="boundsArray">Array of bounds to union</param>
        /// <returns>The union bounds, or empty bounds if array is null/empty</returns>
        public static CameraBounds Union(params CameraBounds[] boundsArray)
        {
            if (boundsArray == null || boundsArray.Length == 0)
                return new CameraBounds(0f, 0f, 0f, 0f);

            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            bool hasValidBounds = false;
            foreach (var bounds in boundsArray)
            {
                if (bounds.Size == Vector3.zero)
                    continue;

                minX = Mathf.Min(minX, bounds.minX);
                maxX = Mathf.Max(maxX, bounds.maxX);
                minY = Mathf.Min(minY, bounds.minY);
                maxY = Mathf.Max(maxY, bounds.maxY);
                hasValidBounds = true;
            }

            if (!hasValidBounds)
                return new CameraBounds(0f, 0f, 0f, 0f);

            return new CameraBounds(minX, maxX, minY, maxY);
        }
    }
}
