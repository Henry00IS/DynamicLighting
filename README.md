# Dynamic Lighting for Unity.

This package brings an old school lighting technique to Unity.

It is inspired by Tim Sweeney's lighting system in Unreal Gold and Unreal Tournament (1996-1999).

![Showcasing Dynamic Lighting in Unity with a classic Unreal map the Vortex Rikers](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-vortex2-unity.gif)

This lighting technique precomputes unique shadows for each light source, allowing dynamic adjustments such as color changes, flickering, or even water refraction. This level of realtime customization is not possible with Unity's baked lighting alone. It utilizes straightforward custom shaders similar to Unity's Standard shader and is compatible with the built-in render pipeline. The minimum Unity Editor requirement is 2021.2.18f1 up to and including Unity 6.

To raytrace the scene, game objects must be marked as static.

| High Quality Shadows | Volumetric Fog |
:---: | :---:
| ![Higher shadow quality than Unreal](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-shadow-detail.png) | ![Volumetric fog surrounding light sources](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-volumetric-fog.png) |
| **Bounce Lighting** | **Transparency** |
| ![Bounce lighting demo in Sponza](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-bounce-2.png) | ![Transparent texture raytracing demo with a works on my machine funny texture](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-transparency.png) |
| **Metallic PBR** | **Mixable with Progressive Lightmapper** |
| ![Metallic PBR workflow support](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-rendering-pbr.png) | ![Progressive Lightmapper using Dynamic Lighting](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-mixed-lighting.gif) |

#### Cons
The main limitation of this technique is that lights with shadows cannot change their position. If they have to move, they become real-time lights that cast no shadows and can potentially shine through walls, if their radius allows for it. Depending on the use case and level design, this may never be a problem at all.

However, real-time shadows are supported and can be used sparingly to overcome this limitation.

## Installation Instructions:

Add the following line to your Unity Package Manager:

![Unity Package Manager](https://user-images.githubusercontent.com/7905726/84954483-c82ba100-b0f5-11ea-9cd0-1cdc24ef2660.png)

`https://github.com/Henry00IS/DynamicLighting.git`

## Support:

Feel free to [join my Discord server](https://discord.gg/sKEvrBwHtq) and let's talk about it.

[![Join my Discord server](https://dcbadge.limes.pink/api/server/sKEvrBwHtq)](https://discord.gg/sKEvrBwHtq)

If you found this package useful, please consider making a donation or supporting me on Patreon. Your donations are a tremendous encouragement for the continued development and support of this package. 😁

[![Support me on Patreon](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/badges/patreon.svg)](https://patreon.com/henrydejongh) [![Support me on Ko-fi](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/badges/kofi.svg)](https://ko-fi.com/henry00) [![paypal](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/badges/paypal.svg)](https://paypal.me/henrydejongh)

## Partners:

The Dynamic Lighting system enhances _Gloomwood_, a professional game by New Blood Interactive.

[![Gloomwood Promotion](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/partner-gloomwood.png)](https://store.steampowered.com/app/1150760/Gloomwood/)

New Blood Interactive generously funded the development of custom features, including bounce lighting, to elevate Gloomwood's visuals. They permitted all features created for the game to be shared in this open-source project, empowering the Unity community and showcasing the system's capabilities in a commercial production.