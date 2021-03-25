Shader "GrassFlow/Grass Material Shader" {
	Properties {

		[Space(15)]
		[HideInInspector] _CollapseStart("Grass Properties", Float) = 1
		[HDR]_Color("Grass Color", Color) = (1,1,1,1)
		bladeHeight("Blade Height", Float) = 1.0
		bladeWidth("Blade Width", Float) = 0.05
		bladeSharp("Blade Sharpness", Float) = 0.3
		seekSun("Seek Sun", Float) = 0.6
		topViewPush("Top View Adjust", Float) = 0.5
		flatnessMult("Flatness Adjust", Float) = 1.25
		[Toggle(BILLBOARD)]
		_BILLBOARD("Billboard", Float) = 1
		variance("Variances (p,h,c,w)", Vector) = (0.4, 0.4, 0.4, 0.4)
		_CollapseEnd("Grass Properties", Float) = 0

		[HideInInspector] _CollapseStart("Lighting Properties", Float) = 0
		_AO("AO", Float) = 0.25
		ambientCO("Ambient", Float) = 0.5
		ambientCOShadow("Shadow Ambient", Float) = 0.5
		edgeLight("Edge On Light", Float) = 0.4
		edgeLightSharp("Edge On Light Sharpness", Float) = 8
		_CollapseEnd("Lighting Properties", Float) = 0

		[Space(15)]
		[HideInInspector] _CollapseStart("LOD Properties", Float) = 0
		[Toggle(ALPHA_TO_MASK)]
		_ALPHA_TO_MASK("Alpha To Mask", Float) = 0
		widthLODscale("Width LOD Scale", Float) = 0.04
		grassFade("Grass Fade", Float) = 120
		grassFadeSharpness("Fade Sharpness", Float) = 8
		[HideInInspector]_LOD("LOD Params", Vector) = (20, 1.1, 0.2, 0.0)
		_CollapseEnd("LOD Properties", Float) = 0

		[Space(15)]
		[HideInInspector]_CollapseStart("Wind Properties", Float) = 0
		[HDR]windTint("windTint", Color) = (1,1,1, 0.15)
		_noiseScale("Noise Scale", Vector) = (1,1,.7)
		_noiseSpeed("Noise Speed", Vector) = (1.5,1,0.35)
		windDir  ("Wind Direction", Vector) = (-0.7,-0.6,0.1)
		windDir2 ("Secondary Wind Direction", Vector) = (0.5,0.5,1.2)
		_CollapseEnd("Wind Properties", Float) = 0

		[Space(15)]
		[HideInInspector]_CollapseStart("Bendable Settings", Float) = 0
		[Toggle(MULTI_SEGMENT)]
		_MULTI_SEGMENT("Enable Bending", Float) = 0
		bladeLateralCurve("Curvature", Float) = 0
		bladeVerticalCurve("Droop", Float) = 0
		bladeStiffness("Stiffness", Float) = 0
		_CollapseEnd("Bendable Settings", Float) = 0


		[Space(15)]
		[HideInInspector]_CollapseStart("Maps and Textures", Float) = 0
		[Toggle(SEMI_TRANSPARENT)]
		_SEMI_TRANSPARENT("Enable Alpha Clip", Float) = 0
		alphaClip("Alpha Clip", Float) = 0.25
		numTextures("Number of Textures", Int) = 1
		_MainTex("Grass Texture", 2D) = "white"{}
		[NoScaleOffset] colorMap("Grass Color Map", 2D) = "white"{}
		[NoScaleOffset] dhfParamMap("Grass Parameter Map", 2D) = "white"{}
		[NoScaleOffset] typeMap("Grass Type Map", 2D) = "black"{}
		_CollapseEnd("Maps and Textures", Float) = 0

		//[PerRendererData] _chunkPos("ChunkPos", Vector)= (0,0,0,0)
		[NoScaleOffset][HideInInspector] terrainNormalMap("Terrain Normal Map", 2D) = "black"{}
	}


	SubShader{

		Tags { "Queue" = "AlphaTest" }

		pass {
			Name "ForwardBasePass"

			Blend SrcAlpha OneMinusSrcAlpha
			Tags { "RenderType" = "Transparent" "LightMode" = "ForwardBase" }

			AlphaToMask [_ALPHA_TO_MASK]
			Cull Off

			CGPROGRAM

			#include "UnityCG.cginc"
			#include "AutoLight.cginc"


			#pragma target 4.0
			#pragma vertex vertex_shader
			#pragma geometry geometry_shader
			#pragma fragment fragment_shader			

			//this might look stupid but its better to use local keywords
			//but earlier unity versions dont support them so we need the global ones as fallback
			//unity ALLEGEDLY prioritizes local keywords if both are defined with same name
			//so hopefully on never version of unity it just ignores the global ones, thankfully itll still compile fine on older ones
			#pragma shader_feature_local BILLBOARD
			#pragma shader_feature_local SEMI_TRANSPARENT
			#pragma shader_feature_local RENDERMODE_MESH
			#pragma shader_feature_local GRASS_EDITOR
			#pragma shader_feature_local BAKED_HEIGHTMAP
			#pragma shader_feature_local MULTI_SEGMENT


			#pragma shader_feature BILLBOARD
			#pragma shader_feature SEMI_TRANSPARENT
			#pragma shader_feature RENDERMODE_MESH
			#pragma shader_feature GRASS_EDITOR
			#pragma shader_feature BAKED_HEIGHTMAP
			#pragma shader_feature MULTI_SEGMENT

			#pragma multi_compile_instancing
			#pragma multi_compile_fwdbase
			#pragma multi_compile_fog

			#include "GrassPrograms.cginc"


			ENDCG
		}// base pass
	}
	CustomEditor "GrassFlow.GrassShaderGUI"
}
