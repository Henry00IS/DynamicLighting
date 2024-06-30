using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Samples a <see cref="Material"/> color and texture and provides fast access.</summary>
    internal class MaterialSampler
    {
        /// <summary>The main color of the material.</summary>
        private Color materialColor = Color.white;

        /// <summary>The one-dimensional array of main texture colors.</summary>
        private Color[] textureColors = null;

        /// <summary>The width of the main texture or zero.</summary>
        private int width = 0;

        /// <summary>The height of the main texture or zero.</summary>
        private int height = 0;

        public MaterialSampler(Material material)
        {
            Initialize(material);
        }

        public MaterialSampler(MeshFilter meshFilter)
        {
            if (meshFilter == null) return;
            if (!meshFilter.TryGetComponent<MeshRenderer>(out var meshRenderer)) return;
            var material = meshRenderer.sharedMaterial;
            if (material == null) return;
            Initialize(material);
        }

        private void Initialize(Material material)
        {
            materialColor = material.color;

            var texture = material.mainTexture;

            // read the texture data.
            if (texture != null)
            {
                width = texture.width;
                height = texture.height;

                var rt = RenderTexture.GetTemporary(width, height);
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;
                var readableTexture = new Texture2D(width, height);
                readableTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                readableTexture.Apply();
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);

                textureColors = readableTexture.GetPixels();
            }
        }

        public Color Sample(float x, float y)
        {
            return materialColor;
        }
    }
}