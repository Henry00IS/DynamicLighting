// Copyright(C) David W. Jeske, 2013
// Released to the public domain.

using System;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    public struct SSAABB : IEquatable<SSAABB>
    {
        public Vector3 Min;
        public Vector3 Max;

        public SSAABB(float min = float.PositiveInfinity, float max = float.NegativeInfinity)
        {
            Min = new Vector3(min, min, min);
            Max = new Vector3(max, max, max);
        }

        public SSAABB(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }

        public void Combine(ref SSAABB other)
        {
            Min = Extensions.ComponentMin(Min, other.Min);
            Max = Extensions.ComponentMax(Max, other.Max);
        }

        public bool IntersectsSphere(Vector3 origin, float radius)
        {
            if (
                (origin.x + radius < Min.x) ||
                (origin.y + radius < Min.y) ||
                (origin.z + radius < Min.z) ||
                (origin.x - radius > Max.x) ||
                (origin.y - radius > Max.y) ||
                (origin.z - radius > Max.z)
               )
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public bool IntersectsAABB(SSAABB box)
        {
            return ((Max.x > box.Min.x) && (Min.x < box.Max.x) &&
                     (Max.y > box.Min.y) && (Min.y < box.Max.y) &&
                     (Max.z > box.Min.z) && (Min.z < box.Max.z));
        }

        public bool Equals(SSAABB other)
        {
            return
                (Min.x == other.Min.x) &&
                (Min.y == other.Min.y) &&
                (Min.z == other.Min.z) &&
                (Max.x == other.Max.x) &&
                (Max.y == other.Max.y) &&
                (Max.z == other.Max.z);
        }

        public void UpdateMin(Vector3 localMin)
        {
            Min = Extensions.ComponentMin(Min, localMin);
        }

        public void UpdateMax(Vector3 localMax)
        {
            Max = Extensions.ComponentMax(Max, localMax);
        }

        public Vector3 Center()
        {
            return (Min + Max) / 2f;
        }

        public Vector3 Diff()
        {
            return Max - Min;
        }

        internal void ExpandToFit(SSAABB b)
        {
            if (b.Min.x < this.Min.x) { this.Min.x = b.Min.x; }
            if (b.Min.y < this.Min.y) { this.Min.y = b.Min.y; }
            if (b.Min.z < this.Min.z) { this.Min.z = b.Min.z; }

            if (b.Max.x > this.Max.x) { this.Max.x = b.Max.x; }
            if (b.Max.y > this.Max.y) { this.Max.y = b.Max.y; }
            if (b.Max.z > this.Max.z) { this.Max.z = b.Max.z; }
        }

        public SSAABB ExpandedBy(SSAABB b)
        {
            SSAABB newbox = this;
            if (b.Min.x < newbox.Min.x) { newbox.Min.x = b.Min.x; }
            if (b.Min.y < newbox.Min.y) { newbox.Min.y = b.Min.y; }
            if (b.Min.z < newbox.Min.z) { newbox.Min.z = b.Min.z; }

            if (b.Max.x > newbox.Max.x) { newbox.Max.x = b.Max.x; }
            if (b.Max.y > newbox.Max.y) { newbox.Max.y = b.Max.y; }
            if (b.Max.z > newbox.Max.z) { newbox.Max.z = b.Max.z; }

            return newbox;
        }

        public void ExpandBy(SSAABB b)
        {
            this = this.ExpandedBy(b);
        }

        public static SSAABB FromSphere(Vector3 pos, float radius)
        {
            SSAABB box;
            box.Min.x = pos.x - radius;
            box.Max.x = pos.x + radius;
            box.Min.y = pos.y - radius;
            box.Max.y = pos.y + radius;
            box.Min.z = pos.z - radius;
            box.Max.z = pos.z + radius;

            return box;
        }
    }
}