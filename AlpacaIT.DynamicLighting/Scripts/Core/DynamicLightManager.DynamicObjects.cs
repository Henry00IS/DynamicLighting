using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    // implements occlusion for dynamic objects.

    public partial class DynamicLightManager
    {
        /// <summary>Collection of <see cref="DynamicLightingReceiver"/> objects in the scene.</summary>
        private List<DynamicLightingReceiver> dynamicObjects;

        /// <summary>The <see cref="ComputeBuffer"/> containing all of the dynamic object data.</summary>
        private ComputeBufferList<ShaderDynamicObject> dynamicShaderObjects;

        /// <summary>
        /// Handles thousands of raycasts on the job system to check occlusion of dynamic objects.
        /// </summary>
        private RaycastProcessor dynamicObjectsRaycastProcessor;

        /// <summary>The pool of <see cref="DynamicObjectsRaycastHandler"/> that get recycled.</summary>
        private RaycastHandlerPool<DynamicObjectsRaycastHandler> dynamicObjectsRaycastHandlerPool;

        /// <summary>Prepared raycast command that is recycled for raytracing.</summary>
        private RaycastCommand dynamicObjectsRaycastCommand;

        /// <summary>Initialization of the DynamicLightManager.DynamicObjects partial class.</summary>
        private void DynamicObjectsInitialize(bool reload)
        {
            dynamicObjects = new List<DynamicLightingReceiver>();

            if (reload)
                dynamicObjects.AddRange(FindObjectsOfType<DynamicLightingReceiver>());

            dynamicShaderObjects = new ComputeBufferList<ShaderDynamicObject>(512);
            ShadersSetGlobalDynamicObjects(dynamicShaderObjects.buffer);

            // prepare a raycast command that is recycled for raytracing.
#if UNITY_2021_2_OR_NEWER && !UNITY_2021_2_17 && !UNITY_2021_2_16 && !UNITY_2021_2_15 && !UNITY_2021_2_14 && !UNITY_2021_2_13 && !UNITY_2021_2_12 && !UNITY_2021_2_11 && !UNITY_2021_2_10 && !UNITY_2021_2_9 && !UNITY_2021_2_8 && !UNITY_2021_2_7 && !UNITY_2021_2_6 && !UNITY_2021_2_5 && !UNITY_2021_2_4 && !UNITY_2021_2_3 && !UNITY_2021_2_2 && !UNITY_2021_2_1 && !UNITY_2021_2_0
#if UNITY_EDITOR
            dynamicObjectsRaycastCommand.physicsScene = Physics.defaultPhysicsScene;
#endif
#else
            Debug.LogWarning("Dynamic Lighting only officially supports Unity Editor 2021.2.18f1 and beyond. Please try to upgrade your project for the best experience.");
#endif
#if UNITY_2022_2_OR_NEWER
            dynamicObjectsRaycastCommand.queryParameters = new QueryParameters(raytraceLayers, false, QueryTriggerInteraction.Ignore, true);
#else
            dynamicObjectsRaycastCommand.layerMask = raytraceLayers;
            dynamicObjectsRaycastCommand.maxHits = 1;
#endif
            dynamicObjectsRaycastProcessor = new RaycastProcessor(Allocator.Persistent);
            dynamicObjectsRaycastHandlerPool = new RaycastHandlerPool<DynamicObjectsRaycastHandler>(512);
        }

        /// <summary>Cleanup of the DynamicLightManager.DynamicObjects partial class.</summary>
        private void DynamicObjectsCleanup()
        {
            dynamicObjects = null;

            ShadersSetGlobalDynamicObjects(null);

            // destroy the buffer on the graphics card.
            dynamicShaderObjects.Release();
            dynamicShaderObjects = null;

            dynamicObjectsRaycastProcessor.Dispose();
            dynamicObjectsRaycastProcessor = null;

            dynamicObjectsRaycastHandlerPool = null;
        }

        internal void RegisterDynamicObject(DynamicLightingReceiver receiver)
        {
            Initialize();

            dynamicObjects.Add(receiver);

            objectRegistered?.Invoke(this, new DynamicObjectRegisteredEventArgs(receiver));
        }

        internal void UnregisterDynamicObject(DynamicLightingReceiver receiver)
        {
            dynamicObjects?.Remove(receiver);

            objectUnregistered?.Invoke(this, new DynamicObjectUnregisteredEventArgs(receiver));
        }

        private class DynamicObjectsRaycastHandler : RaycastHandler
        {
            private DynamicLightManager lightManager;
            private DynamicLightingReceiver dynamicObject;
            private List<DynamicLight> dynamicLights;
            private DynamicLight[] wishlist = new DynamicLight[8];
            private int wishlistCount;
            private float deltaTime;

            public DynamicObjectsRaycastHandler()
            {
                dynamicLights = new List<DynamicLight>(8);
            }

            public void Setup(DynamicLightManager lightManager, DynamicLightingReceiver dynamicObject, float deltaTime)
            {
                // reset:
                wishlistCount = 0;

                // setup:
                this.lightManager = lightManager;
                this.dynamicObject = dynamicObject;
                this.deltaTime = deltaTime;

                // todo: remember how many lights we encountered last frame and recreate the lists if they are too big.
                dynamicLights.Clear();
            }

            public void AddLight(DynamicLight light)
            {
                dynamicLights.Add(light);
            }

            public override void OnRaycastMiss()
            {
                if (wishlistCount >= 8) return;
                wishlist[wishlistCount++] = dynamicLights[raycastsIndex];
            }

            public override unsafe void OnRaycastHit(RaycastHit* hit)
            {
            }

            public override unsafe void OnHandlerFinished()
            {
                ShaderDynamicObject sdo; // (unsafe) skip initialization.

                lightManager.DynamicObjectsApplyWishlist(dynamicObject, &sdo, wishlist, wishlistCount, deltaTime);

                lightManager.dynamicShaderObjects.Add(sdo);
            }
        }

        /// <summary>Called after the lights have processed for rendering.</summary>
        private unsafe void DynamicObjectsUpdate()
        {
            var raycastedDynamicLightsCount = raycastedDynamicLights.Count;
            var deltaTime = Time.deltaTime;

            // clear the list of shader data.
            dynamicShaderObjects.Clear();

            // iterate over all dynamic objects in the scene:
            var dynamicObjectsCount = dynamicObjects.Count;
            for (int i = 0; i < dynamicObjectsCount; i++)
            {
                // fetch the required information as fast as possible.
                var dynamicObject = dynamicObjects[i];
                var dynamicObjectRenderer = dynamicObject.meshRenderer; // cached!
                var dynamicObjectBounds = dynamicObjectRenderer.bounds;
                var dynamicObjectIndex = dynamicObject.lastMaterialDynamicObjectsIndex;

                // store the index of this object in the material property block, this is -1 by
                // default and may change as dynamic objects are disabled in the scene.
                if (dynamicObjectIndex != i)
                {
                    SetDynamicObjectIndex(dynamicObjectRenderer, i);
                    dynamicObject.lastMaterialDynamicObjectsIndex = i;
                }

                // prepare to raytrace this dynamic object on the job system.
                var dynamicObjectRaycastHandler = dynamicObjectsRaycastHandlerPool.GetInstance();
                dynamicObjectRaycastHandler.Setup(this, dynamicObject, deltaTime);

                int counter = 0;

                // todo: replace this with the BVH, benchmark.
                // iterate over all light sources that were uploaded to the shader:
                for (int j = 0; j < raycastedDynamicLightsCount; j++)
                {
                    var raycastedDynamicLight = raycastedDynamicLights[j];
                    var lightAvailable = raycastedDynamicLight.lightAvailable;

                    // destroyed raycasted lights in the scene, must still exist in the shader.
                    if (!lightAvailable) continue;

                    // retrieving light fields to reduce overhead.
                    var light = raycastedDynamicLight.light;
                    var lightCache = light.cache;
                    var lightPosition = lightCache.transformPosition;
                    var lightRadius = light.lightRadius;
                    var lightRadiusSqr = lightRadius * lightRadius;

                    // we want to get as close as possible to a light source.
                    var closestPositionToLight = dynamicObjectBounds.ClosestPointFast(lightPosition);

                    // check whether this object can see the light source.
                    var normal = lightPosition;
                    UMath.Subtract(&normal, &closestPositionToLight);
                    var distanceSqr = Vector3.SqrMagnitude(normal);

                    // we can use the distance and guaranteed maximum light radius to early out.
                    if (distanceSqr > lightRadiusSqr)
                        continue;

                    // only normalize once necessary.
                    normal.Normalize();

                    // create a raycast command as fast as possible.
                    dynamicObjectsRaycastCommand.from = closestPositionToLight;
                    dynamicObjectsRaycastCommand.direction = normal;
                    dynamicObjectsRaycastCommand.distance = Mathf.Sqrt(distanceSqr);

                    // that should be plenty for this naive approach.
                    if (++counter >= 16)
                        break;

                    dynamicObjectRaycastHandler.AddLight(light);
                    dynamicObjectsRaycastProcessor.Add(dynamicObjectsRaycastCommand, dynamicObjectRaycastHandler);
                }

                dynamicObjectRaycastHandler.Ready();
            }

            // finish raycasting and processing the dynamic objects.
            dynamicObjectsRaycastProcessor.Complete();

            // upload the dynamic objects data to the graphics card.
            DynamicObjectsUpload();
        }

        /// <summary>
        /// Attempts to apply a sorted wishlist of light sources (index 0 is most desired). It will
        /// have to fade-out undesired light sources to free up a slot.
        /// </summary>
        /// <param name="wishlist">The sorted wishlist of light sources.</param>
        /// <param name="wishlistCount">The amount of items on the wishlist (maximum is 8).</param>
        private unsafe void DynamicObjectsApplyWishlist(DynamicLightingReceiver dynamicObject, ShaderDynamicObject* sdo, DynamicLight[] wishlist, int wishlistCount, float deltaTime)
        {
            var lightFadeInSpeed = dynamicObject.lightFadeInSpeed;
            var lightFadeOutSpeed = dynamicObject.lightFadeOutSpeed;

            // iterate over the wishlist:
            for (int i = 0; i < wishlistCount; i++)
            {
                var wish = wishlist[i];

                // if the wishlist light is already active then keep it active.
                if (dynamicObject.HasActiveLight(wish, out int index))
                {
                    var fader = dynamicObject.fadeLinear[index];
                    fader.Open(lightFadeInSpeed);
                }
                // try to find a free slot to activate the wishlist light.
                else if (dynamicObject.GetFreeSlot(out index))
                {
                    dynamicObject.activeLights[index] = wish;
                    var fader = dynamicObject.fadeLinear[index];
                    fader.CloseImmediately();
                    fader.Open(lightFadeInSpeed);
                }
            }

            // iterate over all active light sources:
            for (int i = 0; i < 8; i++)
            {
                var activeLight = dynamicObject.activeLights[i];

                // is it no longer on the wish list?
                bool found = false;
                for (int j = 0; j < wishlistCount; j++)
                {
                    if (ReferenceEquals(activeLight, wishlist[j]))
                    {
                        found = true;
                        break;
                    }
                }

                // then fade out the light source.
                var fader = dynamicObject.fadeLinear[i];

                if (!found)
                {
                    fader.Close(lightFadeOutSpeed);

                    // todo: if outside of light radius immediately close.
                }

                // updating all of the fading states.
                fader.Update(deltaTime);
            }

            // prepare fast memory access.
            var sdoActiveLights = (uint*)sdo;
            var sdoFadeLights = (float*)sdo;
            var fadeLightsOffset = 8;

            // copy the active lights into the shader data.
            for (int i = 0; i < 8; i++)
            {
                var fader = dynamicObject.fadeLinear[i];
                if (fader.position > 0.0f)
                {
                    var activeLight = dynamicObject.activeLights[i];
                    sdoActiveLights[i] = activeLight.cache.shaderIndex;
                }
                sdoFadeLights[fadeLightsOffset + i] = fader.position;
            }
        }

        /// <summary>Sets the material variable "dynamic_objects_index" to the specified index.</summary>
        /// <param name="renderer">The <see cref="MeshRenderer"/> of the dynamic object to be modified.</param>
        /// <param name="index">The index in the <see cref="dynamicShaderObjects"/> for this object.</param>
        private void SetDynamicObjectIndex(MeshRenderer renderer, int index)
        {
            var materialPropertyBlock = new MaterialPropertyBlock();

            // play nice with other scripts.
            if (renderer.HasPropertyBlock())
                renderer.GetPropertyBlock(materialPropertyBlock);

#if UNITY_2021_1_OR_NEWER
            materialPropertyBlock.SetInteger(shadersPropertyIdDynamicObjectsIndex, index);
#else
            materialPropertyBlock.SetInt(shadersPropertyIdDynamicObjectsIndex, index);
#endif

            renderer.SetPropertyBlock(materialPropertyBlock);
        }

        /// <summary>
        /// Uploads the <see cref="dynamicShaderObjects"/> to the graphics card.
        /// </summary>
        private void DynamicObjectsUpload()
        {
            // upload the data to the graphics card and if necessary change the compute buffer.
            if (dynamicShaderObjects.Upload())
                ShadersSetGlobalDynamicObjects(dynamicShaderObjects.buffer);
        }
    }
}