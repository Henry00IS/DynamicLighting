using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    public class BvhVisualization : MonoBehaviour
    {
        private BvhNode hierarchy = new BvhNode() { boundingVolume = new BoundingBox() };

        private void Start()
        {
            var boundingVolumes = new List<BoundingBox>();
            foreach (var light in FindObjectsOfType<DynamicLight>())
            {
                boundingVolumes.Add(new BoundingBox() { center = light.transform.position, bounds = new Bounds(light.transform.position, Vector3.one * light.lightRadius * 2f) });
            }

            hierarchy = BvhNode.Build(boundingVolumes.ToArray());
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;
            DrawRecursive(hierarchy);

            //List<BoundingVolume> overlappingVolumes = new List<BoundingVolume>();
            //hierarchy.FindNodesOverlappingPosition(Camera.main.transform.position, overlappingVolumes);
            //
            //Debug.Log(overlappingVolumes.Count);
        }

        private void DrawRecursive(BvhNode node, int depth = 0)
        {
            if (node.leftChild != null)
            {
                Gizmos.DrawWireCube(node.boundingVolume.center, ((BoundingBox)node.boundingVolume).bounds.size);
                DrawRecursive(node.leftChild, depth + 1);
            }
            if (node.rightChild != null)
            {
                Gizmos.DrawWireCube(node.boundingVolume.center, ((BoundingBox)node.boundingVolume).bounds.size);
                DrawRecursive(node.rightChild, depth + 1);
            }
            else
            {
                //Gizmos.color = Color.red;
                Gizmos.DrawWireCube(node.boundingVolume.center, ((BoundingBox)node.boundingVolume).bounds.size);
            }
        }
    }
}