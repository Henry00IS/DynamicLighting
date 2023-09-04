using System.Collections.Generic;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    public partial class DynamicLightManager : MonoBehaviour
    {
        /// <summary>
        /// The number of dynamic shapes that can be active at the same time. If this budget is
        /// exceeded, shapes that are out of view or furthest away from the camera will
        /// automatically fade out in a way that the player will hopefully not notice. A
        /// conservative budget as required by the level design will help older graphics hardware
        /// when there are hundreds of shapes in the scene. Budgeting does not begin until the
        /// number of active dynamic shapes actually exceeds this number.
        /// </summary>
        [Tooltip("The number of dynamic shapes that can be active at the same time. If this budget is exceeded, shapes that are out of view or furthest away from the camera will automatically fade out in a way that the player will hopefully not notice. A conservative budget as required by the level design will help older graphics hardware when there are hundreds of shapes in the scene. Budgeting does not begin until the number of active dynamic shapes actually exceeds this number.")]
        [Min(0)]
        public int dynamicShapeBudget = 2048;

        /// <summary>The memory size in bytes of the <see cref="ShaderDynamicShape"/> struct.</summary>
        private int dynamicShapeStride;
        private List<DynamicShape> sceneDynamicShapes;

        private List<DynamicShape> activeDynamicShapes;
        private ShaderDynamicShape[] shaderDynamicShapes;
        private ComputeBuffer dynamicShapesBuffer;

        private void Initialize_Shapes(bool reload = false)
        {
            dynamicShapeStride = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShaderDynamicShape));

            // prepare to store dynamic shapes that will register themselves to us.
            sceneDynamicShapes = new List<DynamicShape>(dynamicShapeBudget);

            if (reload)
            {
                // manually register all shapes - this is used after raytracing.
                sceneDynamicShapes = new List<DynamicShape>(FindObjectsOfType<DynamicShape>());
            }

            // allocate the required arrays and buffers according to our budget.
            activeDynamicShapes = new List<DynamicShape>(dynamicShapeBudget);
            shaderDynamicShapes = new ShaderDynamicShape[dynamicShapeBudget];
            dynamicShapesBuffer = new ComputeBuffer(shaderDynamicShapes.Length, dynamicShapeStride, ComputeBufferType.Default);
            Shader.SetGlobalBuffer("dynamic_shapes", dynamicShapesBuffer);
            Shader.SetGlobalInt("dynamic_shapes_count", 0);
        }

        private void Cleanup_Shapes()
        {
            if (dynamicShapesBuffer != null && dynamicShapesBuffer.IsValid())
            {
                dynamicShapesBuffer.Release();
                dynamicShapesBuffer = null;
            }

            sceneDynamicShapes = null;
            activeDynamicShapes = null;
        }

        internal void RegisterDynamicShape(DynamicShape shape)
        {
            Initialize();
            sceneDynamicShapes.Add(shape);
        }

        internal void UnregisterDynamicShape(DynamicShape shape)
        {
            if (sceneDynamicShapes != null)
            {
                sceneDynamicShapes.Remove(shape);
                activeDynamicShapes.Remove(shape);
            }
        }

        /// <summary>
        /// Whenever the dynamic shapes budget changes we must update the shader buffer.
        /// </summary>
        private void ReallocateShaderShapeBuffer()
        {
            Debug.Log("REALLOC");

            // properly release any old buffer.
            if (dynamicShapesBuffer != null && dynamicShapesBuffer.IsValid())
                dynamicShapesBuffer.Release();

            shaderDynamicShapes = new ShaderDynamicShape[dynamicShapeBudget];
            dynamicShapesBuffer = new ComputeBuffer(shaderDynamicShapes.Length, dynamicShapeStride, ComputeBufferType.Default);
            Shader.SetGlobalBuffer("dynamic_shapes", dynamicShapesBuffer);
        }

        private void Update_Shapes(Camera camera)
        {
            // if the budget changed we must recreate the shader buffers.
            if (dynamicShapeBudget == 0) return; // sanity check.
            if (shaderDynamicShapes.Length != dynamicShapeBudget)
                ReallocateShaderShapeBuffer();

            // if we exceed the dynamic shape budget we sort the dynamic shapes by distance every
            // frame, as we will assume they are moving around.
            var cameraPosition = camera.transform.position;
            if (sceneDynamicShapes.Count > dynamicShapeBudget)
            {
                SortSceneDynamicShapes(cameraPosition);
            }

            // fill the active shapes back up with the closest shapes.
            activeDynamicShapes.Clear();

            var activeDynamicLightsCount = activeDynamicLights.Count;
            var activeRealtimeLightsCount = activeRealtimeLights.Count;
            var sceneDynamicShapesCount = sceneDynamicShapes.Count;
            for (int i = 0; i < sceneDynamicShapesCount; i++)
            {
                if (activeDynamicShapes.Count < dynamicShapeBudget)
                {
                    var shape = sceneDynamicShapes[i];
                    var shapePosition = shape.transform.position;

                    // unlike lights, shadow shapes outside of the camera frustum may still throw a
                    // shadow in front of the camera (e.g. a statue off-camera), but a shadow can
                    // only be thrown by a light that is currently active, which we just finished
                    // calculating. by design all of the shadow shapes are scaled in the same obb so
                    // if we know the maximum enclosing radius (as they can be oriented which
                    // increases their size) we can use that to determine whether an active light
                    // can even interact with a shadow shape before uploading it to the gpu.
                    var maxRadius = MathEx.CalculateLargestObbRadius(shape.size);

                    bool uploadShape = false;
                    for (int j = 0; j < activeDynamicLightsCount; j++)
                    {
                        var light = activeDynamicLights[j];
                        if (MathEx.SpheresIntersect(shapePosition, maxRadius, light.transform.position, light.lightRadius))
                        { uploadShape = true; break; }
                    }

                    if (!uploadShape)
                        for (int j = 0; j < activeRealtimeLightsCount; j++)
                        {
                            var light = activeRealtimeLights[j];
                            if (MathEx.SpheresIntersect(shapePosition, maxRadius, light.transform.position, light.lightRadius))
                            { uploadShape = true; break; }
                        }

                    if (uploadShape)
                        activeDynamicShapes.Add(sceneDynamicShapes[i]);
                }
            }

            // write the active shapes into the shader data.

            var idx = 0;
            var activeDynamicShapesCount = activeDynamicShapes.Count;
            for (int i = 0; i < activeDynamicShapesCount; i++)
            {
                var shape = activeDynamicShapes[i];
                SetShaderDynamicShape(idx, shape);
                idx++;
            }

            // upload the active shape data to the graphics card.
            if (dynamicShapesBuffer != null && dynamicShapesBuffer.IsValid())
                dynamicShapesBuffer.SetData(shaderDynamicShapes);
            Shader.SetGlobalInt("dynamic_shapes_count", activeDynamicShapesCount);
        }

        /// <summary>
        /// Sorts the scene dynamic shape lists by the distance from the specified origin. The
        /// closests shapes will appear first in the list.
        /// </summary>
        /// <param name="origin">The origin (usually the camera world position).</param>
        private void SortSceneDynamicShapes(Vector3 origin)
        {
            sceneDynamicShapes.Sort((a, b) => (origin - a.transform.position).sqrMagnitude
            .CompareTo((origin - b.transform.position).sqrMagnitude));
        }

        private void SetShaderDynamicShape(int idx, DynamicShape shape)
        {
            var shapeTransform = shape.transform;

            shaderDynamicShapes[idx].position = shapeTransform.position;
            shaderDynamicShapes[idx].size = shape.size * 0.5f;
            shaderDynamicShapes[idx].rotation = MathEx.ShaderLookAtMatrix(shapeTransform.forward, shapeTransform.up);
            shaderDynamicShapes[idx].type = 0;

            switch (shape.shapeType)
            {
                case DynamicShapeType.Box:
                    shaderDynamicShapes[idx].type |= 1;
                    break;

                case DynamicShapeType.Sphere:
                    shaderDynamicShapes[idx].type |= (uint)1 << 1;
                    break;

                case DynamicShapeType.Cylinder:
                    shaderDynamicShapes[idx].type |= (uint)1 << 2;
                    break;

                case DynamicShapeType.Capsule:
                    shaderDynamicShapes[idx].type |= (uint)1 << 3;
                    break;
            }

            if (shape.skipInnerSelfShadows)
            {
                shaderDynamicShapes[idx].type |= (uint)1 << 4;
            }
        }
    }
}