using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    public class BvhVisualization : MonoBehaviour
    {
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.white;

            var bvh = DynamicLightManager.Instance.dynamicShapesBvh;

            if (bvh == null) return;
            if (bvh.rootBVH == null) return;

            var stack = new Stack<ssBVHNode<DynamicShape>>();
            stack.Push(bvh.rootBVH);
            while (stack.Count > 0)
            {
                var node = stack.Pop();

                Gizmos.DrawWireCube(node.box.Center(), node.box.Diff());

                if (!node.IsLeaf)
                {
                    stack.Push(node.left);
                    stack.Push(node.right);
                }
            }
        }

        /*
        private BvhHierarchy hierarchy = new BvhHierarchy();

        private void Start()
        {
            var boundingVolumes = new List<BvhNode>();
            foreach (var light in FindObjectsOfType<DynamicLight>())
            {
                boundingVolumes.Add(new BvhNode(light.transform.position, light.lightRadius));
            }

            hierarchy = new BvhHierarchy();
            hierarchy.Build(boundingVolumes.ToArray());
        }

        private void OnDrawGizmos()
        {
            if (hierarchy.IsEmpty) return;

            Gizmos.color = Color.white;

            var stack = new Stack<BvhNode>();
            stack.Push(hierarchy.GetNode(0));
            while (stack.Count > 0)
            {
                var node = stack.Pop();

                Gizmos.DrawWireCube(node.center, node.size);

                if (node.leftChild != 0)
                {
                    stack.Push(hierarchy.GetNode(node.leftChild));
                }
                if (node.rightChild != 0)
                {
                    stack.Push(hierarchy.GetNode(node.rightChild));
                }
            }

            //var overlappingVolumes = new List<BvhNode>();
            //hierarchy.FindNodesOverlappingPosition(Camera.main.transform.position, overlappingVolumes);
            //
            //Debug.Log(overlappingVolumes.Count);
        }*/
    }
}