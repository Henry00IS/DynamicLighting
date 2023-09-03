// Copyright(C) David W. Jeske, 2014, and released to the public domain.
//
// Dynamic BVH (Bounding Volume Hierarchy) using incremental refit and tree-rotations
//
// initial BVH build based on: Bounding Volume Hierarchies (BVH) – A brief tutorial on what they are and how to implement them
//              http://www.3dmuve.com/3dmblog/?p=182
//
// Dynamic Updates based on: "Fast, Effective BVH Updates for Animated Scenes" (Kopta, Ize, Spjut, Brunvand, David, Kensler)
//              https://github.com/jeske/SimpleScene/blob/master/SimpleScene/Util/ssBVH/docs/BVH_fast_effective_updates_for_animated_scenes.pdf
//
// see also:  Space Partitioning: Octree vs. BVH
//            http://thomasdiewald.com/blog/?p=1488
//
//

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

// TODO: handle merge/split when LEAF_OBJ_MAX > 1 and objects move
// TODO: add sphere traversal

namespace AlpacaIT.DynamicLighting.Acceleration
{
    public enum Axis
    {
        X, Y, Z,
    }

    public interface SSBVHNodeAdaptor<GO>
    {
        ssBVH<GO> BVH { get; }

        void setBVH(ssBVH<GO> bvh);

        Vector3 objectpos(GO obj);

        float radius(GO obj);

        void mapObjectToBVHLeaf(GO obj, ssBVHNode<GO> leaf);

        void unmapObject(GO obj);

        void checkMap(GO obj);

        ssBVHNode<GO> getLeaf(GO obj);
    }

    public class ssBVH<GO>
    {
        public ssBVHNode<GO> rootBVH;
        public SSBVHNodeAdaptor<GO> nAda;
        public readonly int LEAF_OBJ_MAX;
        public int nodeCount = 0;
        public int maxDepth = 0;

        public HashSet<ssBVHNode<GO>> refitNodes = new HashSet<ssBVHNode<GO>>();

        public delegate bool NodeTest(SSAABB box);

        /// <summary>
        /// Call this to batch-optimize any object-changes notified through
        /// ssBVHNode.refit_ObjectChanged(..). For example, in a game-loop,
        /// call this once per frame.
        /// </summary>

        public void optimize()
        {
            if (LEAF_OBJ_MAX != 1)
            {
                throw new Exception("In order to use optimize, you must set LEAF_OBJ_MAX=1");
            }

            while (refitNodes.Count > 0)
            {
                int maxdepth = refitNodes.Max(n => n.depth);

                var sweepNodes = refitNodes.Where(n => n.depth == maxdepth).ToList();
                sweepNodes.ForEach(n => refitNodes.Remove(n));

                sweepNodes.ForEach(n => n.tryRotate(this));
            }
        }

        public void addObject(GO newOb)
        {
            SSAABB box = SSAABB.FromSphere(nAda.objectpos(newOb), nAda.radius(newOb));
            float boxSAH = ssBVHNode<GO>.SA(ref box);
            rootBVH.addObject(nAda, newOb, ref box, boxSAH);
        }

        public void removeObject(GO newObj)
        {
            var leaf = nAda.getLeaf(newObj);
            leaf.removeObject(nAda, newObj);
        }

        public int countBVHNodes()
        {
            return rootBVH.countBVHNodes();
        }

        /// <summary>
        /// initializes a BVH with a given nodeAdaptor, and object list.
        /// </summary>
        /// <param name="nodeAdaptor"></param>
        /// <param name="objects"></param>
        /// <param name="LEAF_OBJ_MAX">WARNING! currently this must be 1 to use dynamic BVH updates</param>
        public ssBVH(SSBVHNodeAdaptor<GO> nodeAdaptor, List<GO> objects, int LEAF_OBJ_MAX = 1)
        {
            this.LEAF_OBJ_MAX = LEAF_OBJ_MAX;
            nodeAdaptor.setBVH(this);
            this.nAda = nodeAdaptor;

            if (objects.Count > 0)
            {
                rootBVH = new ssBVHNode<GO>(this, objects);
            }
            else
            {
                rootBVH = new ssBVHNode<GO>(this);
                rootBVH.gobjects = new List<GO>(); // it's a leaf, so give it an empty object list
            }
        }
    }
}