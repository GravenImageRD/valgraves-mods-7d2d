using System;
using UnityEngine;

namespace Valgraves.Common.Extensions
{
    /// <summary>
    /// Extensions for the various vector classes. 
    /// </summary>
    public static class VectorExtensions
    {
        /// <summary>
        /// Uses TFP's FastFloor to floor the units of a <see cref="Vector3"/>, creating a <see cref="Vector3i"/>
        /// </summary>
        public static Vector3i FloorToInt(this Vector3 self)
        {
            return new Vector3i(
                Utils.Fastfloor(self.x),
                Utils.Fastfloor(self.y),
                Utils.Fastfloor(self.z)
            );
        }

        /// <summary>
        /// Calculates the vector magnitude for a <see cref="Vector3i"/>
        /// </summary>
        public static double Magnitude(this Vector3i vector)
        {
            return Math.Sqrt((vector.x * vector.x) + (vector.y * vector.y) + (vector.z * vector.z));
        }
    }
}