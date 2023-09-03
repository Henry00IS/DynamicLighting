// Copyright(C) David W. Jeske, 2013
// Released to the public domain.

using System;
using System.Collections.Generic;

using UnityEngine;

namespace AlpacaIT.DynamicLighting.Acceleration
{
    /// <summary>
    /// An adaptor for ssBVH to understand SSObject nodes.
    /// </summary>
    public class SSObjectBVHNodeAdaptor : SSBVHNodeAdaptor<DynamicLight>
    {
        private Dictionary<DynamicLight, ssBVHNode<DynamicLight>> ssToLeafMap = new Dictionary<DynamicLight, ssBVHNode<DynamicLight>>();

        public Vector3 objectpos(DynamicLight obj)
        {
            return obj.transform.position;
        }

        public float radius(DynamicLight obj)
        {
            if (obj.lightRadius >= 0f)
            {
                // extract the object scale...
                // use it to transform the object-space bounding-sphere radius into a world-space radius
                return obj.lightRadius;
            }
            else
            {
                return 1.0f;
            }
        }

        public void checkMap(DynamicLight obj)
        {
            if (!ssToLeafMap.ContainsKey(obj))
            {
                throw new Exception("missing map for shuffled child");
            }
        }

        public void unmapObject(DynamicLight obj)
        {
            ssToLeafMap.Remove(obj);
        }

        public void mapObjectToBVHLeaf(DynamicLight obj, ssBVHNode<DynamicLight> leaf)
        {
            // this allows us to be notified when an object moves, so we can adjust the BVH
            obj.OnPositionOrSizeChanged += obj_OnChanged;

            // TODO: add a hook to handle SSObject deletion... (either a weakref GC notify, or OnDestroy)

            ssToLeafMap[obj] = leaf;
        }

        public ssBVHNode<DynamicLight> getLeaf(DynamicLight obj)
        {
            return ssToLeafMap[obj];
        }

        // the SSObject has changed, so notify the BVH leaf to refit for the object
        protected void obj_OnChanged(DynamicLight sender)
        {
            ssToLeafMap[sender].refit_ObjectChanged(this, sender);
        }

        private ssBVH<DynamicLight> _BVH;

        public ssBVH<DynamicLight> BVH
        { get { return _BVH; } }

        public void setBVH(ssBVH<DynamicLight> BVH)
        {
            this._BVH = BVH;
        }

        public SSObjectBVHNodeAdaptor()
        { }
    }
}