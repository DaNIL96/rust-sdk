Shader "Rust/Standard Cloth (Specular setup)"
{
	Properties
	{ 
		// BASE LAYER
		_Color("Color", Color) = (1,1,1,1)
		_MainTex("Albedo", 2D) = "white" {}
		_MainTexScroll( "Scroll", Vector ) = ( 0, 0, 0, 0 )
		_Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
		_Glossiness("Smoothness", Range(0.0, 1.0)) = 0.5
		_SpecColor("Specular", Color) = (0.2,0.2,0.2)
		_SpecGlossMap("Specular", 2D) = "white" {}
		_BumpScale("Scale", Float) = 1.0
		_BumpMap("Normal Map", 2D) = "bump" {}
		_Parallax ("Height Scale", Range (0.005, 0.08)) = 0.02
		_ParallaxMap ("Height Map", 2D) = "black" {}
		_OcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_OcclusionMap("Occlusion", 2D) = "white" {}
		_EmissionColor("Color", Color) = (0,0,0)
		_EmissionMap("Emission", 2D) = "white" {}
		
		_DetailMask("Detail Mask", 2D) = "white" {} // uses Base Layer UVs; only shows up if Detail Layer enabled
		
		_EnvReflOcclusionStrength("Env. Refl. Occlusion Strength", Range(0.0, 1.0)) = 0.0
		_EnvReflHorizonFade("Env. Refl. Horizon Fade", Range(0.0, 2.0)) = 0.0
		
		// DETAIL LAYER
		[Toggle] _DetailLayer("Enabled", Float) = 1
		_DetailColor("Color", Color) = (1,1,1,1)
		_DetailAlbedoMap("Albedo", 2D) = "grey" {}
		_DetailAlbedoMapScroll( "Scroll", Vector ) = ( 0, 0, 0, 0 )
		_DetailOverlaySpecular("Overlay Specular", Range(0,1)) = 0
		_DetailOverlaySmoothness("Overlay Smoothness", Range(0,1)) = 0
		_DetailNormalMapScale("Scale", Float) = 1.0
		_DetailNormalMap("Normal Map", 2D) = "bump" {}
		_DetailOcclusionStrength("Strength", Range(0.0, 1.0)) = 1.0
		_DetailOcclusionMap("Occlusion", 2D) = "white" {}
		[Enum(UV0,0,UV1,1)] _DetailUVSet ("UV Set for Detail Layer", Float) = 0
		
		// MICROFIBER FUZZ LAYER
		[Toggle] _MicrofiberFuzzLayer("Enabled", Float) = 0
		_MicrofiberFuzzMask("Mask", 2D) = "white" {}
		_MicrofiberFuzzColor("Color", Color) = (1,1,1,1)
		_MicrofiberFuzzIntensity("Intensity", Range(0,1)) = 1
		_MicrofiberFuzzScatter("Scatter", Range(0,1)) = 0.5
		_MicrofiberFuzzOcclusion("Occlusion", Range(0,1)) = 1
		[Toggle] _MicrofiberFuzzMaskWithGloss("MaskWithGloss", Float) = 0

		[Toggle] _ColorizeLayer( "Enabled", Float ) = 0
		_ColorizeMask("Mask", 2D) = "white" {} 
		_ColorizeColorR("ColorR", Color) = (1,0,0,0)
		_ColorizeColorG("ColorG", Color) = (0,1,0,0)
		_ColorizeColorB("ColorB", Color) = (0,0,1,0)
		_ColorizeColorA("ColorA", Color) = (1,1,1,0) 
		
		// UI-only data
		[HideInInspector] _EmissionScaleUI("Scale", Float) = 0.0
		[HideInInspector] _EmissionColorUI("Color", Color) = (1,1,1)
		
		// Blending state
		[HideInInspector] _Mode ("__mode", Float) = 0.0
		[HideInInspector] _SrcBlend ("__src", Float) = 1.0
		[HideInInspector] _DstBlend ("__dst", Float) = 0.0
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0
		
		[MaterialEnum(Off,0,Front,1,Back,2)] _Cull ("Cull", Int) = 2
	}

	CGINCLUDE
		#pragma target 3.0
		#pragma exclude_renderers gles d3d11_9x		

		#define RUST_STD_SPECULAR

		#pragma multi_compile _BASELAYER_ON
	ENDCG

	SubShader // BEAUTIFUL, FANTASTIC
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 500
		Cull [_Cull]

		CGPROGRAM
			#pragma surface surf StandardRust vertex:vert nolightmap nodirlightmap			

			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION
			#pragma shader_feature _PARALLAXMAP

			#pragma shader_feature _DETAILLAYER_ON
			#pragma shader_feature _MICROFIBERFUZZLAYER_ON 
			#pragma shader_feature _COLORIZELAYER_ON

			#include "Assets/Content/Shaders/Include/Standard/Standard.cginc"
		ENDCG
	}
	SubShader // SIMPLE, GOOD
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 300
		Cull [_Cull]
	
		CGPROGRAM
			#pragma surface surf StandardRust vertex:vert nolightmap nodirlightmap
						
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION

			#pragma shader_feature _DETAILLAYER_ON
			#pragma shader_feature _COLORIZELAYER_ON

			#include "Assets/Content/Shaders/Include/Standard/Standard.cginc"
		ENDCG
	}	
	SubShader // FAST, FASTEST
	{
		Tags { "RenderType"="Opaque" "PerformanceChecks"="False" }
		LOD 0
		Cull [_Cull]
	
		CGPROGRAM
			#pragma surface surf StandardRust vertex:vert nolightmap nodirlightmap addshadow
			
			#pragma shader_feature _ _ALPHATEST_ON _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON
			#pragma shader_feature _EMISSION

			#pragma shader_feature _COLORIZELAYER_ON

			#include "Assets/Content/Shaders/Include/Standard/Standard.cginc"
		ENDCG
	}

	FallBack Off
	CustomEditor "RustStandardShaderGUI"
}
