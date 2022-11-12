using Unity.Mathematics;
using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public class MeshRaycastHit
    {
        /// <summary>The impact point in world space where the ray hit the mesh.</summary>
        public float3 point;
        /// <summary>The normal of the surface the ray hit.</summary>
        public float3 normal;
        /// <summary>The distance from the ray's origin to the impact point.</summary>
        public float distance;
        /// <summary>The index of the triangle that was hit.</summary>
        public int triangleIndex;
        /// <summary>The mesh that was hit.</summary>
        public MeshRaycast mesh;
        /// <summary>The first vertex index of the triangle that was hit.</summary>
        public int vertexIndex1;
        /// <summary>The second vertex index of the triangle that was hit.</summary>
        public int vertexIndex2;
        /// <summary>The third vertex index of the triangle that was hit.</summary>
        public int vertexIndex3;
        /// <summary>The first vertex position of the triangle that was hit.</summary>
        public Vector3 vertex1;
        /// <summary>The second vertex position of the triangle that was hit.</summary>
        public Vector3 vertex2;
        /// <summary>The third vertex position of the triangle that was hit.</summary>
        public Vector3 vertex3;
    }

    public class MeshRaycast
    {
        /// <summary>An array containing all triangles in the mesh.</summary>
        private int[] triangles;

        /// <summary>An array containing all vertices in the mesh.</summary>
        private Vector3[] vertices;

        /// <summary>Gets an array containing all triangles in the mesh.</summary>
        public int[] Triangles => triangles;

        /// <summary>Gets an array containing all vertices in the mesh.</summary>
        public Vector3[] Vertices => vertices;

        public MeshRaycast(Mesh mesh)
        {
            triangles = mesh.triangles;
            vertices = mesh.vertices;
        }

        public MeshRaycast(int[] triangles, Vector3[] vertices)
        {
            this.triangles = triangles;
            this.vertices = vertices;
        }

        /// <summary>
        /// Casts a ray from the origin point in the specified direction against the mesh.
        /// </summary>
        /// <returns>Returns true if the ray intersects with the mesh, otherwise false.</returns>
        public bool Raycast(float3 origin, float3 direction, out MeshRaycastHit hit)
        {
            var ray = new Ray(origin, direction);

            var closestHitDistance = float.MaxValue;
            var closestHit = float3.zero;
            var closestHitNormal = float3.zero;
            var closestTriangleIndex = -1;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];

                if (Intersect(v1, v2, v3, ray))
                {
                    var plane = new Plane(v1, v2, v3);
                    if (plane.Raycast(ray, out float enter))
                    {
                        var point = origin + direction * enter;
                        var distance = math.distance(origin, point);
                        if (distance < closestHitDistance)
                        {
                            closestHitDistance = distance;
                            closestHit = point;
                            closestHitNormal = plane.normal;
                            closestTriangleIndex = i;
                        }
                    }
                }
            }

            if (!closestHit.Equals(float3.zero))
            {
                hit = new MeshRaycastHit()
                {
                    point = closestHit,
                    normal = closestHitNormal,
                    distance = closestHitDistance,
                    triangleIndex = closestTriangleIndex,
                    mesh = this,
                    vertexIndex1 = triangles[closestTriangleIndex],
                    vertexIndex2 = triangles[closestTriangleIndex + 1],
                    vertexIndex3 = triangles[closestTriangleIndex + 2],
                    vertex1 = vertices[triangles[closestTriangleIndex]],
                    vertex2 = vertices[triangles[closestTriangleIndex + 1]],
                    vertex3 = vertices[triangles[closestTriangleIndex + 2]],
                };
                return true;
            }

            hit = default;
            return false;
        }

        /// <summary>
        /// https://answers.unity.com/questions/215678/raycast-without-colliders.html Checks if the
        /// specified ray hits the triangle descibed by p1, p2 and p3. Möller–Trumbore ray-triangle
        /// intersection algorithm implementation.
        /// </summary>
        /// <param name="p1">Vertex 1 of the triangle.</param>
        /// <param name="p2">Vertex 2 of the triangle.</param>
        /// <param name="p3">Vertex 3 of the triangle.</param>
        /// <param name="ray">The ray to test hit for.</param>
        /// <returns><c>true</c> when the ray hits the triangle, otherwise <c>false</c></returns>
        private bool Intersect(Vector3 p1, Vector3 p2, Vector3 p3, Ray ray)
        {
            // vectors from p1 to p2/p3 (edges).
            Vector3 e1, e2;

            Vector3 p, q, t;
            float det, invDet, u, v;

            // find vectors for two edges sharing vertex/point p1.
            e1 = p2 - p1;
            e2 = p3 - p1;

            // calculating determinant.
            p = Vector3.Cross(ray.direction, e2);

            // calculate determinant.
            det = Vector3.Dot(e1, p);

            // if determinant is near zero, ray lies in plane of triangle otherwise not.
            if (det > -Mathf.Epsilon && det < Mathf.Epsilon) { return false; }
            invDet = 1.0f / det;

            // calculate distance from p1 to ray origin.
            t = ray.origin - p1;

            // calculate u parameter.
            u = Vector3.Dot(t, p) * invDet;

            // check for ray hit.
            if (u < 0 || u > 1) { return false; }

            // prepare to test v parameter.
            q = Vector3.Cross(t, e1);

            // calculate v parameter.
            v = Vector3.Dot(ray.direction, q) * invDet;

            // check for ray hit.
            if (v < 0 || u + v > 1) { return false; }

            if ((Vector3.Dot(e2, q) * invDet) > Mathf.Epsilon)
            {
                // ray does intersect.
                return true;
            }

            // no hit at all.
            return false;
        }
    }
}