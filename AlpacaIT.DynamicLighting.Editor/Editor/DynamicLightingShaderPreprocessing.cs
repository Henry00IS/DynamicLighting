using System.Collections.Generic;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace AlpacaIT.DynamicLighting.Editor
{
    /// <summary>Handles removing unnecessary shader variations.</summary>
    internal class DynamicLightingShaderPreprocessing : IPreprocessShaders
    {
        private ShaderKeyword shaderKeywordDynamicLightingQualityLow;
        private ShaderKeyword shaderKeywordDynamicLightingQualityHigh;
        private ShaderKeyword shaderKeywordDynamicLightingQualityIntegratedGraphics;
        private ShaderKeyword shaderKeywordDynamicLightingLit;
        private ShaderKeyword shaderKeywordDynamicLightingBvh;
        private ShaderKeyword shaderKeywordDynamicLightingBounce;
        private ShaderKeyword shaderKeywordDynamicLightingDynamicGeometryDistanceCubes;

        public int callbackOrder => 0;

        public DynamicLightingShaderPreprocessing()
        {
            shaderKeywordDynamicLightingQualityLow = new ShaderKeyword("DYNAMIC_LIGHTING_QUALITY_LOW");
            shaderKeywordDynamicLightingQualityHigh = new ShaderKeyword("DYNAMIC_LIGHTING_QUALITY_HIGH");
            shaderKeywordDynamicLightingQualityIntegratedGraphics = new ShaderKeyword("DYNAMIC_LIGHTING_INTEGRATED_GRAPHICS");
            shaderKeywordDynamicLightingLit = new ShaderKeyword("DYNAMIC_LIGHTING_LIT");
            shaderKeywordDynamicLightingBvh = new ShaderKeyword("DYNAMIC_LIGHTING_BVH");
            shaderKeywordDynamicLightingBounce = new ShaderKeyword("DYNAMIC_LIGHTING_BOUNCE");
            shaderKeywordDynamicLightingDynamicGeometryDistanceCubes = new ShaderKeyword("DYNAMIC_LIGHTING_DYNAMIC_GEOMETRY_DISTANCE_CUBES");
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            var removed = 0;

            var dataCount = data.Count;
            for (int i = dataCount; i-- > 0;)
            {
                var keywords = data[i].shaderKeywordSet;

                // the unlit mode has many variations that can be stripped from the build:
                if (keywords.IsDisabled(shaderKeywordDynamicLightingLit))
                {
                    if (keywords.AnyEnabled(
                        shaderKeywordDynamicLightingBvh,
                        shaderKeywordDynamicLightingQualityLow,
                        shaderKeywordDynamicLightingQualityHigh,
                        shaderKeywordDynamicLightingQualityIntegratedGraphics,
                        shaderKeywordDynamicLightingBvh,
                        shaderKeywordDynamicLightingBounce,
                        shaderKeywordDynamicLightingDynamicGeometryDistanceCubes
                    ))
                    {
                        removed++;
                        data.RemoveAt(i);
                        continue;
                    }
                }
            }
        }
    }
}