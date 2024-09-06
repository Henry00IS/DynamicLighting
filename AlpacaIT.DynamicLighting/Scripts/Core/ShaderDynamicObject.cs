namespace AlpacaIT.DynamicLighting
{
    /// <summary>
    /// A dynamic object (this struct is mirrored in the shader and can not be modified).
    /// <para>This struct uses 16-byte alignment for performance on the GPU.</para>
    /// </summary>
    internal struct ShaderDynamicObject
    {
        /// <summary>Active light index 1 that is rendered on this object.</summary>
        public uint activeLight1;

        /// <summary>Active light index 2 that is rendered on this object.</summary>
        public uint activeLight2;

        /// <summary>Active light index 3 that is rendered on this object.</summary>
        public uint activeLight3;

        /// <summary>Active light index 4 that is rendered on this object.</summary>
        public uint activeLight4;

        // -- 16 byte boundary --

        /// <summary>Active light index 5 that is rendered on this object.</summary>
        public uint activeLight5;

        /// <summary>Active light index 6 that is rendered on this object.</summary>
        public uint activeLight6;

        /// <summary>Active light index 7 that is rendered on this object.</summary>
        public uint activeLight7;

        /// <summary>Active light index 8 that is rendered on this object.</summary>
        public uint activeLight8;

        // -- 16 byte boundary --

        /// <summary>Fades <see cref="activeLight1"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight1;

        /// <summary>Fades <see cref="activeLight2"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight2;

        /// <summary>Fades <see cref="activeLight3"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight3;

        /// <summary>Fades <see cref="activeLight4"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight4;

        // -- 16 byte boundary --

        /// <summary>Fades <see cref="activeLight5"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight5;

        /// <summary>Fades <see cref="activeLight6"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight6;

        /// <summary>Fades <see cref="activeLight7"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight7;

        /// <summary>Fades <see cref="activeLight8"/> where (1.0 is fully lit and 0.0 is off).</summary>
        public float fadeLight8;

        // -- 16 byte boundary --
    };
}