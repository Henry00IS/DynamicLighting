using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>Should be attached to every dynamic object that receives dynamic lighting.</summary>
    [RequireComponent(typeof(MeshRenderer))]
    [ExecuteInEditMode]
    public class DynamicLightingReceiver : MonoBehaviour
    {
        /// <summary>
        /// The fade-in speed in seconds until the dynamic object is fully lit by a nearby light source.
        /// </summary>
        [Min(0f)]
        [Tooltip("The fade-in speed in seconds until the dynamic object is fully lit by a nearby light source.")]
        public float lightFadeInSpeed = 0.2f;

        /// <summary>
        /// The fade-out speed in seconds until the dynamic object is fully unlit from a previously
        /// visible light source (e.g. the light got blocked by a wall).
        /// </summary>
        [Min(0f)]
        [Tooltip("The fade-out speed in seconds until the dynamic object is fully unlit from a previously visible light source (e.g. the light got blocked by a wall).")]
        public float lightFadeOutSpeed = 0.2f;

        /// <summary>
        /// The last "dynamic_objects_index" assigned to the <see cref="MaterialPropertyBlock"/> of
        /// the <see cref="meshRenderer"/> or -1 if not set.
        /// </summary>
        [System.NonSerialized]
        internal int lastMaterialDynamicObjectsIndex = -1;

        /// <summary>Active light indices that are rendered on this object.</summary>
        [System.NonSerialized]
        internal DynamicLight[] activeLights = new DynamicLight[8];

        /// <summary>Handles fading in and out the active light indices.</summary>
        [System.NonSerialized]
        internal MathEx.LinearMotion[] fadeLinear = new MathEx.LinearMotion[8];

        /// <summary>Checks whether the given light source exists on this dynamic object.</summary>
        /// <param name="light">The light source to find.</param>
        /// <param name="index">The index in the <see cref="activeLights"/> array.</param>
        /// <returns>True when the light source exists else false.</returns>
        internal bool HasActiveLight(DynamicLight light, out int index)
        {
            for (index = 0; index < activeLights.Length; index++)
                if (activeLights[index] == light)
                    return true;
            return false;
        }

        /// <summary>Finds a free slot in the <see cref="activeLights"/> array.</summary>
        /// <param name="index">The free index in the <see cref="activeLights"/> array.</param>
        /// <returns>True when there is a free slot else false.</returns>
        internal bool GetFreeSlot(out int index)
        {
            for (index = 0; index < activeLights.Length; index++)
                if (fadeLinear[index].position == 0.0f)
                    return true;
            return false;
        }

        /// <summary>The cached <see cref="Transform"/> of this <see cref="GameObject"/>.</summary>
        [System.NonSerialized]
        private Transform transformInstance = null;

        /// <summary>
        /// Gets the <see cref="Transform"/> attached to this <see cref="GameObject"/>.
        /// <para>The <see cref="Transform"/> is cached automatically and fast to access.</para>
        /// </summary>
        public new Transform transform
        {
            get
            {
                if (ReferenceEquals(transformInstance, null))
                    transformInstance = base.transform;
                return transformInstance;
            }
        }

        /// <summary>The cached <see cref="MeshRenderer"/> of this <see cref="GameObject"/>.</summary>
        [System.NonSerialized]
        private MeshRenderer meshRendererInstance = null;

        /// <summary>
        /// Gets the <see cref="MeshRenderer"/> attached to this <see cref="GameObject"/>.
        /// <para>The <see cref="MeshRenderer"/> is cached automatically and fast to access.</para>
        /// </summary>
        public MeshRenderer meshRenderer
        {
            get
            {
                if (ReferenceEquals(meshRendererInstance, null))
                    meshRendererInstance = GetComponent<MeshRenderer>();
                return meshRendererInstance;
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < fadeLinear.Length; i++)
                fadeLinear[i] = new MathEx.LinearMotion();

            DynamicLightManager.Instance.RegisterDynamicObject(this);
        }

        private void OnDisable()
        {
            if (DynamicLightManager.hasInstance)
                DynamicLightManager.Instance.UnregisterDynamicObject(this);
        }
    }
}