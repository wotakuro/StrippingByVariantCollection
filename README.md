# StrippingByVariantCollection
[日本語はコチラ](README.ja.md)

Find the ShaderVariantCollection and GraphicsStateCollection in the project and exclude unregistered variants from the build.
You can change the behaiour in the settings window (  "UTJ/ShaderVariantStrip"  ).

# If you are using Unity 2022 or earlier
Please use [version 2](https://github.com/wotakuro/StrippingByVariantCollection/tree/version2).

# about Setting Window
## CommonTab
![alt text](Documentation~/Config_StripCommon.png)

### Enable Stripping
Set to perform strip processing or not.

### Log Variants
Specify whether to log which variants are included and which are excluded during the build process. Specify whether you want to keep a log of which variants were included and which were excluded.<br />
It will be written to the directory "ShaderVariants/Builds/Timestamps" under the project.<br />
This can be done even if Stripping is disabled.

### 「Reset Timestamp」Button
There was a possibility that the log timestamp was not reset properly when building consecutively. <br />
If you find that the log timestamps are not updated properly during continuous build, please press this button.

### Strict Variant Stripping
When enabled, Shaders that are not in the ShaderVariantCollection/GraphicsStateCollection will be deleted all Variants.
When disabled, shaders that are not in the ShaderVariantCollection/GraphicsStateCollection is not performed by any special strip processing.

### SafeMode
If all variants in Pass are deleted, at least one variant will be retained.
If Pass becomes empty, Fallback will not be triggered and drawing will be skipped, so we have provided an option for this case.


### Disable Unity Stripping
Enabling this feature will remove the IPreprocessShaders under the "Unity." or "UnityEngine.".( such as Universal RenderPipeline. )<br />
If Strict Variant Stripping is not enabled, you cannot use this feature.
*It's implemented by rewriting IL code.

### Script Execute Order
Setting the order of "IPreprocessShaders" in this asset.

### [Debug] List IPreprocessShaders
List the all classes that implments IPreprocessShaders.

### [Debug]List ShaderKeywords
![alt text](Documentation~/ShaderKeywordDebug.png)
You can debug which Shader keywords are enabled at which Stage.

## ShaderVariantCollection Tab
![alt text](Documentation~/Config_StripSVC.png)

### Use ShaderVariantCollection
If disabled, ShaderVariantCollection within the project will be ignored.

### Exclude Stripping Rule
The ShaderVariantCollection asset specified here will be ignored.


## GraphicsStateCollection Tab
![alt text](Documentation~/Config_StripGSC.png)

### UseGraphicsStateCollection
If disabled, GraphicsStateCollection within the project will be ignored.

### Match Graphis API Only
Only consider GraphicsStateCollections created with the same Graphics API as the build target.

### Match Target Platfomr Only
Only consider GraphicsStateCollection created on the same platform as the build target.

### Exclude Stripping Rule
The GraphicsStateCollection assets specified here will be excluded and ignored.

## Connect Runtime  Tab
![alt text](Documentation~/Config_Strip_ConnectRuntime.png) <br />
This option collects mismatched ShaderVariants from DevelopmentBuild with “Strict shader variant matching” enabled in PlayerSettings and creates a dummy GraphicsStateCollection.

### TargetPlayer
Connect to the target for DevelopmentBuild.

### Create GraphicsStatesCollection from Miss Match Variant
Press this button to generate GraphicsStateCollection under Assets/GraphicsStateCollection/MissMatchVarint.
<br />

### Recieve GraphicsStateCollection from Player
To display this button, you need to add “STRIP_ENABLE_AUTO_GSC” to Define. <br/ >
When the button is pressed, the GraphicsStateCollection that has been traced since the connected Player started up is transferred to the Editor. <br />

![alt text](Documentation~/PlayerSettings.png) <br />
GraphicsStateCollection cannot trace multiple objects simultaneously, so we have introduced options using Define.


# Reference：<br />

## About Strip Processing
This uses the removal of scriptable shader variants. <br />
https://blogs.unity3d.com/jp/2018/05/14/stripping-scriptable-shader-variants/

## When Strip Processing Is Not Performed
In incremental builds, IPreprocessShaders.OnPorocessShader may not be called. <br />
https://docs.unity3d.com/6000.0/Documentation/Manual/incremental-build-pipeline.html <br />
<br />
If this occurs, please try CleanBuild.

![alt text](Documentation~/CleanBuild.png) <br />


## Proposed workflow
This is a proposal for building a GraphicsStateCollection to improve strip processing. <br />

<pre>
1. First, disable Strip in this tool, create a Development build with “STRIP_ENABLE_AUTO_GSC” defined.
2. After running for a while, retrieve the GraphicsStateCollection from the Player.
3. Then, enable PlayerSettings.strictShaderVariantMatching, enable this tool, enable StrictVariantStripping and SafeMode in Common settings, and create a Development Build.
4. Collect Shader Variant mismatches using “Create GraphicsStatesCollection from Miss Match Variant.”

We recommend collecting and building the GraphicsStateCollection in this manner.
For the final build, enable SafeMode in this tool for safety considerations.
</pre>