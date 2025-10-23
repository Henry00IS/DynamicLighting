# Dynamic Lighting for Unity.

This package brings an old school lighting technique to Unity.

It is inspired by Tim Sweeney's lighting system in Unreal Gold and Unreal Tournament (1996-1999).

![Showcasing Dynamic Lighting in Unity with a classic Unreal map the Vortex Rikers](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/demo-vortex2-unity.gif)

This lighting technique precomputes unique shadows for each light source, allowing dynamic adjustments such as color changes, flickering, volumetric effects, rotating spot lights, animated cookies, or even water refraction; all after baking the scene has already finished. This level of realtime customization is not possible with Unity's baked lighting alone (mixing the Progressive Lightmapper and this technique is supported). It utilizes straightforward custom shaders similar to Unity's Standard shader and is compatible with the built-in render pipeline (URP is supported with some limitations). The minimum Unity Editor requirement is 2021.2.18f1 up to and including Unity 6.1.

To raytrace the scene, game objects must be marked as static.

![High Shadow Quality](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/home-01.png)
![Bounce Lighting](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/home-02.png)
![Transparency](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/home-03.png)
![Volumetric Fog](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/home-04.png)
![Physically Based Rendering](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/home-05.png)

#### Cons

The main limitation of this technique is that lights with shadows cannot change their position. If they have to move, they become real-time lights that cast no shadows and can potentially shine through walls, if their radius allows for it. Depending on the use case and level design, this may never be a problem at all.

However, real-time shadows are supported and can be used sparingly to overcome this limitation.

## Installation Instructions:

Add the following line to your Unity Package Manager:

![Unity Package Manager](https://user-images.githubusercontent.com/7905726/84954483-c82ba100-b0f5-11ea-9cd0-1cdc24ef2660.png)

`https://github.com/Henry00IS/DynamicLighting.git`

## Quick Start Guide

After installing the package (via the Git URL in the Unity Package Manager), follow these steps to add and bake dynamic lights.

1. **Add dynamic lights**  
   From Unity’s menu: **GameObject > Light > Dynamic Point Light**, **Dynamic Spot Light**, **Dynamic Directional Light**, or **Dynamic Discoball Light**.  
   Or press Space in the Scene view to open Overlays, enable the “Dynamic Lighting” overlay toolbar, and use the Point/Spot/Special buttons to add lights.  
   (Mark relevant GameObjects as Static for accurate raytracing.)
  
   ![GameObject Menu for Dynamic Lights](https://github.com/user-attachments/assets/c45df1c6-e3f1-41bf-8a14-05d029ec61e5)  
   *Select from Dynamic Lighting options under GameObject > Light.*

2. **Bake the lights (no live coverage overlay)**  
   Save your scene first.  
   Use the “Dynamic Lighting” overlay toolbar: **Preview Bake** (quick bake without bounce lighting), **Bake** (full bake with current settings), and **Options** (set bake resolution, delete baked lightmaps, defaults for new lights like bounce/transparency).  
   Alternatively use the main menu: **Dynamic Lighting > Raytrace Scene: <size>** or **Dynamic Lighting > Preview Scene > Skip Bounce Lighting: <size>**.  
  
   ![Dynamic Lighting Toolbar](https://github.com/user-attachments/assets/f3587f1c-f5db-4448-98b9-0ccbd93b4c19)  
   *Overlay toolbar with Light/Preview/Bake/Options buttons. Tip: Press Space in Scene view to open Overlays and enable the toolbar.*

3. **Iterate**  
   After baking, adjust light properties (color, intensity, effects, transparency, volumetrics) and re-run Preview/Bake as needed.  
   The overlay does not visualize light/shadow coverage in real-time; results appear after a bake or preview bake.
  
   ![Baking in Progress](https://github.com/user-attachments/assets/3c01df83-161d-4184-b01b-6afcf2f25e28)  
   *Click Bake to process shadows and effects.*



## Support:

Feel free to [join my Discord server](https://discord.gg/sKEvrBwHtq) and let's talk about it.

[![Join my Discord server](https://dcbadge.limes.pink/api/server/sKEvrBwHtq)](https://discord.gg/sKEvrBwHtq)

If you found this package useful, please consider making a donation or supporting me on Patreon. Your donations are a tremendous encouragement for the continued development and support of this package. 😁

[![Support me on Patreon](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/badges/patreon.svg)](https://patreon.com/henrydejongh) [![Support me on Ko-fi](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/badges/kofi.svg)](https://ko-fi.com/henry00) [![paypal](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/badges/paypal.svg)](https://paypal.me/henrydejongh)

## Partners:

The Dynamic Lighting system enhances _Gloomwood_, a professional game by New Blood Interactive.

[![Gloomwood Promotion](https://raw.githubusercontent.com/wiki/Henry00IS/DynamicLighting/images/home/partner-gloomwood.png)](https://store.steampowered.com/app/1150760/Gloomwood/)

New Blood Interactive generously funded the development of custom features, including bounce lighting, to elevate Gloomwood's visuals. They permitted all features created for the game to be shared in this open-source project, empowering the Unity community and showcasing the system's capabilities in a commercial production.
