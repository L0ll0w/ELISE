# Where to get help
Please write us at [support@gamelogic.co.za](mailto:support@gamelogic.co.za) if you have any issues. ]

Online documentation: [https://www.gamelogic.co.za/documentation/fx/dithering/](https://www.gamelogic.co.za/documentation/fx/dithering/)

# How to use the sample
This sample contains two main tezst scenes, depending on the rendering pipeline you are using:

> * Built-in render pipeline: `BuiltIn/Main`
> * URP: `URP/Main`

If you open the wrong scene, the materials will appear pink!

### BuiltIn/Main 
All the effects of the package are under the Effects game object in the hierarchy. 
You can enable / disable the game objects to switch them on or off. You can adjust the properties
of each component. 

### URP/Main
To test the asset with the URP, you need to have URP installed. See here how to do this:
https://docs.unity3d.com/6000.4/Documentation/Manual/urp/InstallingAndConfiguringURP.html

⚠️ Note: Currently URP post process shaders do not with with `RenderGraph`, and you have to enable **Compatibility 
Mode**:
> Project Settings → Graphics → URP → Compatibility Mode (Disable Render Graph)

Once you have it installed, you can open the URP/Main scene.

No effects will be applied at first. Effects are implemented as `ScriptableRendererFeature`s, and you need to add them to the 
renderer:

1. Select the renderer in your project.
2. In the inspector, click on **Add Renderer Feature**.
3. Select the effect you want to add. The effects that ship with this package all have a `GL_` prefix.

Some effects do not affect what you see with certain settings, so make sure to play around with them. (For example, 
if the `Gamma` is `1` on the `GL_AdjustGamma` render featrure, or the `PixelSize` is `(1, 1)` in the `GL_Pixelate`
render feature,)
