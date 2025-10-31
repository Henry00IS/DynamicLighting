using UnityEngine;

namespace AlpacaIT.DynamicLighting
{
    /// <summary>The different shadow modes that can be applied to a dynamic light.</summary>
    public enum DynamicLightShadowMode
    {
        /// <summary>
        /// This is the technique used in Unreal Gold and Unreal Tournament (1996-1999). The main
        /// limitation is that lights with shadows cannot change their position. If they have to
        /// move, they become real-time lights that cast no shadows and can potentially shine
        /// through walls, if their radius allows for it. Depending on the use case and level
        /// design, this may never be a problem at all. This is the technique used in Unreal Gold
        /// and Unreal Tournament (1996-1999). Note that real-time light sources have a global
        /// performance impact on the entire scene so it is better to never move raytraced light
        /// sources (default).
        /// </summary>
        [Tooltip("This is the technique used in Unreal Gold and Unreal Tournament (1996-1999). The main limitation is that lights with shadows cannot change their position. If they have to move, they become real-time lights that cast no shadows and can potentially shine through walls, if their radius allows for it. Depending on the use case and level design, this may never be a problem at all. Note that real-time light sources have a global performance impact on the entire scene so it is better to never move raytraced light sources (default).")]
        RaytracedShadows = 0,

        /// <summary>
        /// Think of this as a 'real-time shadows enabled' flag on top of what the default setting does.
        /// <para>
        /// The light will cast shadows in real-time, similar to normal Unity lights. Use this
        /// option sparingly as it requires additional render passes. You can currently have up to
        /// 16 of these lights in your scene (performance limitations). You might want to limit this
        /// to the most important and obvious light sources in the scene, and disable them when the
        /// player leaves the area. The quality of the shadows decreases with distance, so don't
        /// expect to cover an entire level with one of these light sources.
        /// </para>
        /// <para>
        /// If you raytrace the scene and never move the light, the existing acceleration techniques
        /// will greatly reduce the global performance impact, identical to default lights.
        /// Raytraced shadows match real-time shadows, providing accurate details over long
        /// distances. The advantage of this mixed technique is that dynamic meshes that come within
        /// the radius of the light will cast shadows. If you want to move the light and don't want
        /// raytraced shadows, always place it on channel 32 to make it a real-time light.
        /// </para>
        /// <para>
        /// Tip: Standard Unity light sources are supported, and their real-time shadows render much
        /// faster because they are deeply integrated into the engine.
        /// </para>
        /// </summary>
        [Tooltip("Think of this as a 'real-time shadows enabled' flag on top of what the default setting does.\n\nThe light will cast shadows in real-time, similar to normal Unity lights. Use this option sparingly as it requires additional render passes. You can currently have up to 16 of these lights in your scene (performance limitations). You might want to limit this to the most important and obvious light sources in the scene, and disable them when the player leaves the area. The quality of the shadows decreases with distance, so don't expect to cover an entire level with one of these light sources.\n\nIf you raytrace the scene and never move the light, the existing acceleration techniques will greatly reduce the global performance impact, identical to default lights. Raytraced shadows match real-time shadows, providing accurate details over long distances. The advantage of this mixed technique is that dynamic meshes that come within the radius of the light will cast shadows. If you want to move the light and don't want raytraced shadows, always place it on channel 32 to make it a real-time light.\n\nTip: Standard Unity light sources are supported, and their real-time shadows render much faster because they are deeply integrated into the engine.")]
        RealtimeShadows = 1,

        /// <summary>
        /// Todo: This is going to be awesome.
        /// </summary>
        //BakedMixedShadows = 2,
    }
}