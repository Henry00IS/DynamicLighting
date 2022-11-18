using System.Collections;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public class MathEx
    {
        public static bool SpheresIntersect(Vector3 spherePosition1, float sphereRadius1, Vector3 spherePosition2, float sphereRadius2)
        {
            return Vector3.Distance(spherePosition1, spherePosition2) <= (sphereRadius1 + sphereRadius2);
        }
    }
}