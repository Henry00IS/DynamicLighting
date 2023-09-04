using System;
using System.Collections;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    public class DynamicShape : MonoBehaviour
    {
        public DynamicShapeType shapeType = DynamicShapeType.Box;

        public bool skipInnerSelfShadows = false;

        public Vector3 size
        {
            get
            {
                return transform.lossyScale;
            }
        }

        private void OnEnable()
        {
            DynamicLightManager.Instance.RegisterDynamicShape(this);
        }

        private void OnDisable()
        {
            if (DynamicLightManager.hasInstance)
                DynamicLightManager.Instance.UnregisterDynamicShape(this);
        }

        private bool selected;

        private void OnDrawGizmos()
        {
            var previousGizmosColor = Gizmos.color;
            Gizmos.color = Color.gray;

            if (selected)
            {
                Gizmos.color = new Color(1.0f, 0.5f, 0.0f);
            }

            switch (shapeType)
            {
                case DynamicShapeType.Box:
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                    break;

                case DynamicShapeType.Sphere:
                    Gizmos.DrawWireSphere(transform.position, size.x * 0.5f);
                    break;

                case DynamicShapeType.Cylinder:
                    break;

                case DynamicShapeType.Capsule:
                    break;

                default:
                    break;
            }

            Gizmos.color = previousGizmosColor;
            selected = false;
        }

        private void OnDrawGizmosSelected()
        {
            selected = true;
        }

        private Vector3 previousPosition;
        private Quaternion previousRotation;
        private Vector3 previousScale;
        public event Action<DynamicShape> OnTransformChanged;

        private void Update()
        {
            if (transform.position != previousPosition || transform.rotation != previousRotation || transform.lossyScale != previousScale)
            {
                previousPosition = transform.position;
                previousRotation = transform.rotation;
                previousScale = transform.lossyScale;
                OnTransformChanged?.Invoke(this);
            }
        }
    }
}