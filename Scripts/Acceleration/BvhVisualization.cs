using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    public class BvhVisualization : MonoBehaviour
    {
        private BvhNode hierarchy = new BvhNode() { boundingVolume = new BvhBoundingVolume() };

        private void Start()
        {
            var boundingVolumes = new List<BvhBoundingVolume>();
            foreach (var light in FindObjectsOfType<DynamicLight>())
            {
                boundingVolumes.Add(new BvhBoundingVolume(light.transform.position, light.lightRadius));
            }

            hierarchy = BvhNode.Build(boundingVolumes.ToArray());
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            DrawRecursive(hierarchy);

            //List<BvhBoundingVolume> overlappingVolumes = new List<BvhBoundingVolume>();
            //hierarchy.FindNodesOverlappingPosition(Camera.main.transform.position, overlappingVolumes);
            //
            //Debug.Log(overlappingVolumes.Count);
        }

        private void DrawRecursive(BvhNode node, int depth = 0)
        {
            Gizmos.DrawWireCube(node.boundingVolume.center, node.boundingVolume.size);

            if (node.leftChild != null)
            {
                DrawRecursive(node.leftChild, depth + 1);
            }
            if (node.rightChild != null)
            {
                DrawRecursive(node.rightChild, depth + 1);
            }
        }
    }
}