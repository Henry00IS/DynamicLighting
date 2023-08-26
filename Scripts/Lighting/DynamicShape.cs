using System.Collections;
using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    [ExecuteInEditMode]
    public class DynamicShape : MonoBehaviour
    {
        public Vector3 size
        {
            get
            {
                return GetComponent<Renderer>().bounds.size;
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
    }
}