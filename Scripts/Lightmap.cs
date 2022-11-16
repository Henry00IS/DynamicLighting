using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AlpacaIT.VertexTracer
{
    public class Lightmap : MonoBehaviour
    {
        [HideInInspector]
        [SerializeField]
        public uint[] pixels;
        
        [HideInInspector]
        [SerializeField]
        public int resolution;

        public ComputeBuffer buffer;
    }
}
