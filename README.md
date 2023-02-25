# Dynamic Lighting for Unity.

This package brings an old school lighting technique to Unity.

It is inspired by Tim Sweeney's lighting system in Unreal Gold and Unreal Tournament (1996-1999).

![Showcasing Dynamic Lighting and Reactive Logic by switching some light sources](https://raw.githubusercontent.com/wiki/Henry00IS/ReactiveLogic/images/DynamicLightingAndReactiveLogic.gif)

This system allows you to have an almost unlimited number of light sources in your scene, all with ray traced shadows. Unlike light baking techniques, where you can't change any light source, or shadow mapping, where performance drops off after 4 lights or so, this technique allows real-time adjustments to all lights, such as flickering and changing colours, or even water refraction, among many other effects. This requires using relatively simple custom shaders, including Metallic PBR, which is almost identical to Unity's Standard shader, to work. It doesn't rely on Unity's custom render pipelines, it's just the good old built-in render pipeline.

#### Cons
The main limitation of this technique is that lights with shadows cannot change their position. If they have to move, they become real-time lights that cast no shadows and can potentially shine through walls, if their radius allows for it. Depending on the use case and level design, this may never be a problem at all.

## Installation Instructions:

Add the following line to your Unity Package Manager:

![Unity Package Manager](https://user-images.githubusercontent.com/7905726/84954483-c82ba100-b0f5-11ea-9cd0-1cdc24ef2660.png)

`https://github.com/Henry00IS/DynamicLighting.git`

## Reflection Probes

When you bake reflection probes, Unity may also create a small lightmap that messes up the UV1 coordinates when you enter play mode. You can fix this by disabling the baked global illumination in the lighting settings:

![Unchecking baked global illumination in the lighting settings window](https://github.com/Henry00IS/DynamicLighting/wiki/images/home/baked-global-illumination.png)

## Donations:

If you found this package useful, please consider making a donation or supporting me on Patreon. Your donations are a tremendous encouragement for the continued development and support of this package. 😁

[![Support me on Patreon](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Fshieldsio-patreon.vercel.app%2Fapi%3Fusername%3Dhenrydejongh%26type%3Dpatrons&style=for-the-badge)](https://patreon.com/henrydejongh)

[![paypal](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://paypal.me/henrydejongh)
