# Unity 2020.1+ Texture Access API examples

Unity 2020.1 added `Texture2D.GetPixelData` and related APIs for C# Jobs/Burst compatible way of reading & writing texture pixels
(see [documentation](https://docs.unity3d.com/2020.1/Documentation/ScriptReference/Texture2D.GetPixelData.html)).

This repository contains a small example of that. Required Unity version is **2020.1** or later.

## Procedural Plasma Texture

An example where a "plasma effect" texture is updated on the CPU every frame.

![Plasma](/Images/Plasma.jpg?raw=true "Plasma")

`Assets/PlasmaSampleScene` is the sample scene and code.

Time it takes to update a 512x512 texture, on 2019 MacBookPro (Core i9 2.6GHz, 8 cores / 16 threads):

- `SetPixels`: **113ms**,
- `SetPixel`: 140ms,
- `SetPixelData` w/ Burst: 17ms,
- `SetPixelData` w/ Burst, parallel jobs: **1.7ms**.


