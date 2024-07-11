// * * * * * * * * * * * * * * * * * * * * * *
//  Author:  Lindsey Keene (nukeandbeans)
//  Contact: Twitter @nukeandbeans, Discord @nukeandbeans
//
//  Description:
//
//  * * * * * * * * * * * * * * * * * * * * * *

#if REALTIME_CSG
using RealtimeCSG;
#endif

using AlpacaIT.DynamicLighting.Internal;
using UnityEditor;
using UnityEngine;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace AlpacaIT.DynamicLighting.Editor {
    /// <summary>A collection of utilities used for toolbar buttons and such.</summary>
    internal static class ToolbarUtilities {
        /// <summary>Places a transform with a max placement distance from the <see cref="SceneView"/> camera.</summary>
        /// <param name="t">The transform to place.</param>
        /// <param name="maxDistance">The maximum distance from the camera the transform can be placed.</param>
        public static void PlaceInScene( Transform t, float maxDistance = 10.0f ) {
            Camera    camera          = Utilities.GetSceneViewCamera();
            Ray       ray             = camera.ViewportPointToRay( new Vector3( 0.5f, 0.5f, 0.0f ) );
            Transform cameraTransform = camera.transform;

            Vector3 hitPoint;

            if( Physics.Raycast( ray, out RaycastHit hit, maxDistance ) ) {
                hitPoint = hit.point + hit.normal * 1.25f; // place 0.25m off the surface
            }
            else {
                hitPoint = cameraTransform.position + cameraTransform.forward * maxDistance;
            }

            float snap =
#if REALTIME_CSG // use RealtimeCSG's snap settings instead of Unity's, if it is present, otherwise use Unity's snap scale.
                CSGSettings.SnapScale;
#else
                EditorSnapSettings.scale;
#endif
            hitPoint = new Vector3( snap * Mathf.Round( hitPoint.x / snap ), snap * Mathf.Round( hitPoint.y / snap ), snap * Mathf.Round( hitPoint.z / snap ) );

            t.position = hitPoint;
        }

        /// <summary>Sets the current scene selection, and optionally pings it in the hierarchy.</summary>
        /// <param name="o">The object to select, and optionally ping.</param>
        /// <param name="ping">Should the selected object be pinged? Defaults to true.</param>
        public static void Select( Object o, bool ping = false ) {
            Selection.activeObject = o;

            if( ping ) {
                EditorGUIUtility.PingObject( o );
            }
        }

        public static string GetPackageVersion(string packageName) {
            PackageInfo[] packages = PackageInfo.GetAllRegisteredPackages();

            foreach( PackageInfo package in packages ) {
                if( package.name == packageName ) {
                    return package.version;
                }
            }

            return "<?>";
        }
    }
}
