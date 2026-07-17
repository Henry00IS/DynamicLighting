#if UNITY_6000_7_OR_NEWER
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;

namespace AlpacaIT.DynamicLighting
{
    public partial class DynamicLightingRenderPipeline
    {
        private const int maxDirectionalCascades = 4;

        private readonly ShaderTagId shaderTagIdShadowCaster;
        private readonly int dirShadowAtlasId;
        private readonly int dirShadowMatricesId;
        private readonly int cascadeCountId;
        private readonly int cascadeSplitsId;
        private readonly int shadowDistanceFadeId;

        private readonly Matrix4x4[] dirShadowMatrices = new Matrix4x4[maxDirectionalCascades];
        private readonly Plane[] lightCullPlanes = new Plane[6];
        private readonly float[] cascadeSplitDistances = new float[maxDirectionalCascades];

        private void RenderDirectionalCascades(ScriptableRenderContext context, Camera camera, CullingResults culling, CommandBuffer cmd)
        {
            var light = FindDirectionalShadowLight();
            int cascadeCount = Mathf.Clamp(renderPipelineAsset.directionalShadowCascades, 1, maxDirectionalCascades);
            int atlasSize = Mathf.Max(renderPipelineAsset.directionalShadowAtlasSize, 1);
            float maxDistance = Mathf.Min(renderPipelineAsset.directionalShadowDistance, camera.farClipPlane);

            if (light == null || maxDistance <= 0f)
            {
                cmd.GetTemporaryRT(dirShadowAtlasId, 1, 1, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
                cmd.SetGlobalInt(cascadeCountId, 0);
                return;
            }

            int layerMask = DynamicLightManager.hasInstance ? DynamicLightManager.Instance.realtimeShadowLayers : ~0;
            int split = cascadeCount <= 1 ? 1 : 2;
            int tileSize = atlasSize / split;
            float cameraNear = Mathf.Max(camera.nearClipPlane, 0.01f);
            Vector3 lightDir = light.transform.forward.normalized;
            Quaternion lightRot = Quaternion.LookRotation(lightDir, Mathf.Abs(Vector3.Dot(lightDir, Vector3.up)) > 0.99f ? Vector3.forward : Vector3.up);

            // pssm: more cascades => tighter near cascade.
            BuildCascadeSplits(cascadeCount, cameraNear, maxDistance, cascadeSplitDistances);

            cmd.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
            cmd.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(true, false, Color.clear);

            // one light-volume cull on the largest cascade (covers all smaller ones).
            CullingResults shadowCulling = culling;
            {
                ComputeLightMatrices(camera, cameraNear, cascadeSplitDistances[cascadeCount - 1], lightDir, lightRot, tileSize, maxDistance, out Matrix4x4 cv, out Matrix4x4 cp);
                if (TryCullShadowCasters(context, camera, cv, cp, layerMask, out CullingResults culled))
                    shadowCulling = culled;
            }

            for (int i = 0; i < cascadeCount; i++)
            {
                // nested volume: camera near -> this cascade far.
                ComputeLightMatrices(camera, cameraNear, cascadeSplitDistances[i], lightDir, lightRot, tileSize, maxDistance, out Matrix4x4 view, out Matrix4x4 proj);

                Vector2 tile = new Vector2(i % split, i / split);
                cmd.SetViewport(new Rect(tile.x * tileSize, tile.y * tileSize, tileSize, tileSize));
                dirShadowMatrices[i] = ToAtlasMatrix(proj, view, tile, split);
                cmd.SetViewProjectionMatrices(view, proj);

                cmd.DrawRendererList(context.CreateRendererList(new RendererListDesc(shaderTagIdShadowCaster, shadowCulling, camera)
                {
                    renderQueueRange = RenderQueueRange.opaque,
                    sortingCriteria = SortingCriteria.CommonOpaque,
                    layerMask = layerMask,
                }));
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            // upload split depths for view-space cascade selection (stable under rotation).
            Vector4 splits = new Vector4(maxDistance, maxDistance, maxDistance, maxDistance);
            if (cascadeCount > 0) splits.x = cascadeSplitDistances[0];
            if (cascadeCount > 1) splits.y = cascadeSplitDistances[1];
            if (cascadeCount > 2) splits.z = cascadeSplitDistances[2];
            if (cascadeCount > 3) splits.w = cascadeSplitDistances[3];

            float fade = Mathf.Max(renderPipelineAsset.directionalShadowDistanceFade, 0.001f);
            cmd.SetGlobalInt(cascadeCountId, cascadeCount);
            cmd.SetGlobalVector(cascadeSplitsId, splits);
            cmd.SetGlobalMatrixArray(dirShadowMatricesId, dirShadowMatrices);
            cmd.SetGlobalVector(shadowDistanceFadeId, new Vector4(1f / maxDistance, 1f / fade, 0f, 0f));
            cmd.SetGlobalTexture(dirShadowAtlasId, dirShadowAtlasId);
        }

        private void ReleaseDirectionalShadowAtlas(CommandBuffer cmd) => cmd.ReleaseTemporaryRT(dirShadowAtlasId);

        private static void BuildCascadeSplits(int count, float near, float far, float[] splits)
        {
            const float lambda = 0.9f;
            float range = Mathf.Max(far - near, 0.01f);
            for (int i = 0; i < count; i++)
            {
                float p = (i + 1f) / count;
                splits[i] = Mathf.Lerp(near + range * p, near * Mathf.Pow(far / near, p), lambda);
            }
            splits[count - 1] = far;
        }

        /// <summary>
        /// stable cascade projection: sphere radius is derived in camera-local space (constant for
        /// fixed fov/splits), then only the center is texel-snapped. never re-fit radius after snap
        /// (that was making edges crawl).
        /// </summary>
        private static void ComputeLightMatrices(
            Camera camera, float near, float far, Vector3 lightDir, Quaternion lightRot, int tileSize, float maxDistance,
            out Matrix4x4 view, out Matrix4x4 proj)
        {
            // camera-local frustum sphere (radius independent of camera pose).
            FrustumSphere(camera.fieldOfView, camera.aspect, near, far, out Vector3 centerLocal, out float radius);

            // quantize radius so texel size only steps; pad by a fixed diagonal texel after that.
            radius = Mathf.Max(Mathf.Ceil(radius * 16f) / 16f, 0.001f);
            float texel = (radius * 2f) / tileSize;
            radius += texel * 1.41421356f;
            // keep pad inside the same quant grid (no continuous growth).
            radius = Mathf.Ceil(radius * 16f) / 16f;
            texel = (radius * 2f) / tileSize;

            // world-space sphere center along the camera axes.
            Vector3 center = camera.transform.position + camera.transform.rotation * centerLocal;

            // snap center to the light-xy texel grid (only translation moves under camera motion).
            Vector3 ls = Quaternion.Inverse(lightRot) * center;
            ls.x = Mathf.Floor(ls.x / texel + 0.5f) * texel;
            ls.y = Mathf.Floor(ls.y / texel + 0.5f) * texel;
            center = lightRot * ls;

            // deep near plane so distant casters still land in the near cascade.
            float backup = Mathf.Max(radius * 2f, maxDistance);
            Vector3 eye = center - lightDir * backup;
            Matrix4x4 m = Matrix4x4.TRS(eye, lightRot, Vector3.one).inverse;
            m.m20 = -m.m20; m.m21 = -m.m21; m.m22 = -m.m22; m.m23 = -m.m23;
            view = m;
            proj = Matrix4x4.Ortho(-radius, radius, -radius, radius, 0.01f, backup + radius);
        }

        /// <summary>bounding sphere of a perspective frustum in camera-local space (+z = forward).</summary>
        private static void FrustumSphere(float fovY, float aspect, float near, float far, out Vector3 centerLocal, out float radius)
        {
            float k = Mathf.Sqrt(1f + aspect * aspect) * Mathf.Tan(fovY * 0.5f * Mathf.Deg2Rad);
            // far plane dominates; use the standard minimal sphere along the view axis.
            if (k * k >= (far - near) / (far + near))
            {
                // sphere on the far plane corners.
                centerLocal = new Vector3(0f, 0f, far);
                radius = far * k;
            }
            else
            {
                centerLocal = new Vector3(0f, 0f, 0.5f * (far + near) * (1f + k * k));
                radius = 0.5f * Mathf.Sqrt((far - near) * (far - near) + 2f * (far * far + near * near) * k * k + (far * far - near * near) * k * k * k * k);
            }
        }

        // urp-style world->shadow + atlas tile (reverse-z on proj only).
        private static Matrix4x4 ToAtlasMatrix(Matrix4x4 proj, Matrix4x4 view, Vector2 tile, int split)
        {
            if (SystemInfo.usesReversedZBuffer)
            {
                proj.m20 = -proj.m20; proj.m21 = -proj.m21; proj.m22 = -proj.m22; proj.m23 = -proj.m23;
            }
            Matrix4x4 m = proj * view;
            float s = 1f / split;
            // clip [-1,1] -> uv [0,1], then into the atlas tile.
            m.m00 = (0.5f * (m.m00 + m.m30) + tile.x * m.m30) * s;
            m.m01 = (0.5f * (m.m01 + m.m31) + tile.x * m.m31) * s;
            m.m02 = (0.5f * (m.m02 + m.m32) + tile.x * m.m32) * s;
            m.m03 = (0.5f * (m.m03 + m.m33) + tile.x * m.m33) * s;
            m.m10 = (0.5f * (m.m10 + m.m30) + tile.y * m.m30) * s;
            m.m11 = (0.5f * (m.m11 + m.m31) + tile.y * m.m31) * s;
            m.m12 = (0.5f * (m.m12 + m.m32) + tile.y * m.m32) * s;
            m.m13 = (0.5f * (m.m13 + m.m33) + tile.y * m.m33) * s;
            m.m20 = 0.5f * (m.m20 + m.m30);
            m.m21 = 0.5f * (m.m21 + m.m31);
            m.m22 = 0.5f * (m.m22 + m.m32);
            m.m23 = 0.5f * (m.m23 + m.m33);
            return m;
        }

        private bool TryCullShadowCasters(
            ScriptableRenderContext context, Camera camera,
            Matrix4x4 view, Matrix4x4 proj, int layerMask, out CullingResults results)
        {
            if (!camera.TryGetCullingParameters(false, out ScriptableCullingParameters p))
            {
                results = default;
                return false;
            }
            Matrix4x4 vp = proj * view;
            p.cullingMatrix = vp;
            p.isOrthographic = true;
            p.cullingOptions = CullingOptions.None;
            p.cullingMask = (uint)layerMask;
            GeometryUtility.CalculateFrustumPlanes(vp, lightCullPlanes);
            p.cullingPlaneCount = 6;
            for (int i = 0; i < 6; i++)
                p.SetCullingPlane(i, lightCullPlanes[i]);
            results = context.Cull(ref p);
            return true;
        }

        private static DynamicLight FindDirectionalShadowLight()
        {
            if (!DynamicLightManager.hasInstance)
                return null;
            var manager = DynamicLightManager.Instance;
            var raycasted = manager.raycastedDynamicLights;
            var count = raycasted.Count;
            for (int i = 0; i < count; i++)
            {
                var light = raycasted[i].light;
                if (IsDirectionalShadowLight(light))
                    return light;
            }
            return manager.FindDirectionalRealtimeShadowLight();
        }

        private static bool IsDirectionalShadowLight(DynamicLight light) =>
            light && light.isActiveAndEnabled
            && light.lightType == DynamicLightType.Directional
            && light.lightShadows == DynamicLightShadowMode.RealtimeShadows
            && light.lightIntensity > 0f;

    }
}
#endif
