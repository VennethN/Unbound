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
    }
}
