using UnityEngine;

#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;

#endif

namespace AlpacaIT.DynamicLighting
{
    // implements a temporary scene for raytracing with custom physics scene.

    internal partial class DynamicLightingTracer
    {
#if UNITY_EDITOR
        private TemporaryScene temporaryScene;
        private PhysicsScene temporaryScenePhysics;
#endif

        /// <summary>Initialization of the DynamicLightingTracer.TemporaryScene partial class.</summary>
        private void TemporarySceneInitialize()
        {
#if UNITY_EDITOR
            temporaryScene = ScriptableObject.CreateInstance<TemporaryScene>();
            StageUtility.GoToStage(temporaryScene, true);

            // get the physics scene for raycasting.
            temporaryScenePhysics = temporaryScene.scene.GetPhysicsScene();
#endif
        }

        /// <summary>Cleanup of the DynamicLightingTracer.TemporaryScene partial class.</summary>
        private void TemporarySceneCleanup()
        {
#if UNITY_EDITOR
            StageUtility.GoBackToPreviousStage();
#endif
        }

        /// <summary>Adds a <see cref="MeshFilter"/> to the scene that must be raytraced.</summary>
        /// <param name="meshFilter">The mesh filter to add to the temporary scene.</param>
        private bool TemporarySceneAdd(MeshFilter meshFilter)
        {
            // we have to recreate simple meshes with a mesh collider.
            if (!meshFilter) return false;
            var originalGameObject = meshFilter.gameObject;

            // the game object must be marked as static.
            if (!originalGameObject.isStatic) return false;
#if UNITY_EDITOR
            // the game object must also have contributegi enabled.
            var editorStaticFlags = GameObjectUtility.GetStaticEditorFlags(originalGameObject);
            if (!editorStaticFlags.HasFlag(StaticEditorFlags.ContributeGI))
                return false;
#endif
            // make sure the mesh filter has a mesh assigned.
            var mesh = meshFilter.sharedMesh;
            if (!mesh) return false;

            // we only care about things we can actually render.
            if (!meshFilter.TryGetComponent<MeshRenderer>(out var meshRenderer))
                return false;
#if UNITY_EDITOR
            // get the transform of the original game object.
            var originalTransform = meshFilter.transform;

            // now we create a new game object that represents this object.
            var clone = ObjectFactory.CreateGameObject(temporaryScene.scene, HideFlags.None, originalTransform.name);
            var cloneTransform = clone.transform;

            // the clone will use the same layer as the original for raytracing.
            clone.layer = originalGameObject.layer;

            // place the clone at the same position, rotation and scale as the original.
            cloneTransform.position = originalTransform.position;
            cloneTransform.rotation = originalTransform.rotation;
            cloneTransform.localScale = originalTransform.lossyScale;

            // add a mesh filter with the same mesh.
            var cloneMeshFilter = clone.AddComponent<MeshFilter>();
            cloneMeshFilter.sharedMesh = mesh;

            // add a mesh renderer for photon cube rendering.
            var cloneMeshRenderer = clone.AddComponent<MeshRenderer>();
            cloneMeshRenderer.sharedMaterials = meshRenderer.sharedMaterials;

            // and the important mesh collider for raycasting.
            clone.AddComponent<MeshCollider>();
#endif
            return true;
        }

#if UNITY_EDITOR

        /// <summary>An isolated temporary scene used for raytracing only static renderers.</summary>
        public class TemporaryScene : PreviewSceneStage
        {
            protected override GUIContent CreateHeaderContent()
            {
                return new GUIContent("Dynamic Lighting");
            }
        }

#endif
    }
}