using System;
using UnityEditor;
using UnityEngine;

internal class RustStandardShaderGUI : ShaderGUI
{
	private enum WorkflowMode
	{
		Specular,
		Metallic,
		Dielectric
	}

	public enum BlendMode
	{
		Opaque,
		Cutout,
		Fade,		// Old school alpha-blending mode, fresnel does not affect amount of transparency
		Transparent // Physically plausible transparency mode, implemented as alpha pre-multiply
	}

	private static class Styles
	{
		public static GUIStyle optionsButton = "PaneOptions";
		public static GUIContent uvSetLabel = new GUIContent( "UV Set" );
		public static GUIContent[] uvSetOptions = new GUIContent[] { new GUIContent( "UV channel 0" ), new GUIContent( "UV channel 1" ) };

		public static string emptyTootip = "";
		public static GUIContent albedoText = new GUIContent( "Albedo", "Albedo (RGB) and Transparency (A)" );
		public static GUIContent alphaCutoffText = new GUIContent( "Alpha Cutoff", "Threshold for alpha cutoff" );
		public static GUIContent specularMapText = new GUIContent( "Specular", "Specular (RGB) and Smoothness (A)" );
		public static GUIContent metallicMapText = new GUIContent( "Metallic", "Metallic (R) and Smoothness (A)" );
		public static GUIContent smoothnessText = new GUIContent( "Smoothness", "" );
		public static GUIContent normalMapText = new GUIContent( "Normal Map", "Normal Map" );
		public static GUIContent heightMapText = new GUIContent( "Height Map", "Height Map (G)" );
		public static GUIContent occlusionText = new GUIContent( "Occlusion", "Occlusion (G)" );
		public static GUIContent emissionText = new GUIContent( "Emission", "Emission (RGB)" );
		public static GUIContent detailMaskText = new GUIContent( "Detail Mask", "Mask for Secondary Maps (A)" );
		public static GUIContent detailAlbedoText = new GUIContent( "Albedo", "Albedo (RGB) multiplied by 2" );
		public static GUIContent detailNormalMapText = new GUIContent( "Normal Map", "Normal Map" );

		public static string whiteSpaceString = " ";
		public static string primaryMapsText = "Base Layer";
		public static string blendLayerText = "Detail Layer";
		public static string particleLayerText = "Particle Accum. Layer";
		public static string wetnessLayerText = "Weather Wetness Layer";
		public static string renderingMode = "Rendering Mode";
		public static GUIContent emissiveWarning = new GUIContent( "Emissive value is animated but the material has not been configured to support emissive. Please make sure the material itself has some amount of emissive." );
		public static GUIContent emissiveColorWarning = new GUIContent( "Ensure emissive color is non-black for emission to have effect." );
		public static readonly string[] blendNames = Enum.GetNames( typeof( BlendMode ) );
	}

	// BASE LAYER
	private MaterialProperty blendMode;
	private MaterialProperty cullMode;

	private MaterialProperty albedoMap;
	private MaterialProperty albedoMapScroll;
	private MaterialProperty albedoColor;
	private MaterialProperty alphaCutoff;
	private MaterialProperty specularMap;
	private MaterialProperty specularColor;
	private MaterialProperty metallicMap;
	private MaterialProperty metallic;
	private MaterialProperty smoothness;
	private MaterialProperty bumpScale;
	private MaterialProperty bumpMap;
	private MaterialProperty occlusionStrength;
	private MaterialProperty occlusionMap;
	private MaterialProperty heigtMapScale;
	private MaterialProperty heightMap;
	private MaterialProperty emissionScaleUI;
	private MaterialProperty emissionColorUI;
	private MaterialProperty emissionColorForRendering;
	private MaterialProperty emissionMap;
	private MaterialProperty detailMask;
	private MaterialProperty envReflOcclusionStrength;
	private MaterialProperty envReflHorizonFade;

	// DETAIL LAYER
	private MaterialProperty detailLayerEnabled;

	private MaterialProperty detailAlbedoColor;
	private MaterialProperty detailAlbedoMap;
	private MaterialProperty detailAlbedoMapScroll;
	private MaterialProperty detailOverlaySpecular;
	private MaterialProperty detailOverlayMetallic;
	private MaterialProperty detailOverlaySmoothness;
	private MaterialProperty detailNormalMapScale;
	private MaterialProperty detailNormalMap;
	private MaterialProperty detailOcclusionStrength;
	private MaterialProperty detailOcclusionMap;
	private MaterialProperty detailUVSet;

	// MICROFIBER FUZZ LAYER
	private MaterialProperty microfiberFuzzLayerEnabled;

	private MaterialProperty microfiberFuzzLayerMask;
	private MaterialProperty microfiberFuzzLayerColor;
	private MaterialProperty microfiberFuzzLayerIntensity;
	private MaterialProperty microfiberFuzzLayerScatter;
	private MaterialProperty microfiberFuzzLayerOcclusion;
	private MaterialProperty microfiberFuzzLayerMaskWithGloss;

	// COLOURIZE LAYER
	private MaterialProperty colorizeLayerEnabled;
	private MaterialProperty colorizeLayerMask;
	private MaterialProperty colorizeLayerColorR;
	private MaterialProperty colorizeLayerColorG;
	private MaterialProperty colorizeLayerColorB;
	private MaterialProperty colorizeLayerColorA;

	// PARTICLE ACCUMULATION LAYER
	private MaterialProperty particleLayerEnabled;

	private MaterialProperty particleLayerThickness;
	private MaterialProperty particleLayerBlendFactor;
	private MaterialProperty particleLayerBlendFalloff;
	private MaterialProperty particleLayerWorldDirection;
	private MaterialProperty particleLayerMapTiling;
	private MaterialProperty particleLayerAlbedoColor;
	private MaterialProperty particleLayerAlbedoMap;
	private MaterialProperty particleLayerSmoothness;
	private MaterialProperty particleLayerSpecularColor;
	private MaterialProperty particleLayerSpecularMap;
	private MaterialProperty particleLayerMetallic;
	private MaterialProperty particleLayerMetallicMap;
	private MaterialProperty particleLayerNormalScale;
	private MaterialProperty particleLayerNormalMap;

	// TERRAIN LAYER
	private MaterialProperty terrainLayerEnabled;

	private MaterialProperty terrainLayerDistanceFadeStart;
	private MaterialProperty terrainLayerDistanceFadeRange;
	private MaterialProperty terrainLayerSteepnessFadeStart;
	private MaterialProperty terrainLayerSteepnessFadeRange;
	private MaterialProperty terrainLayerHeightFadeStart;
	private MaterialProperty terrainLayerHeightFadeRange;
	private MaterialProperty terrainLayerBlendFactor;
	private MaterialProperty terrainLayerBlendFalloff;
	private MaterialProperty terrainLayerWorldDirection;
	private MaterialProperty terrainLayerCoatThickness;
	private MaterialProperty terrainLayerCoatBlendFactor;
	private MaterialProperty terrainLayerCoatBlendFalloff;

	// WEATHER WETNESS LAYER
	private MaterialProperty wetnessLayerEnabled;

	private MaterialProperty wetnessLayerIntensity;
	private MaterialProperty wetnessLayerTint;
	private MaterialProperty wetnessLayerMask;
	private MaterialProperty wetnessLayerSmoothness;
	private MaterialProperty wetnessLayerSpecularColor;
	private MaterialProperty wetnessLayerSpecularMap;
	private MaterialProperty wetnessLayerMetallic;
	private MaterialProperty wetnessLayerMetallicMap;
	private MaterialProperty wetnessLayerOcclusionStrength;

	// SHORE WETNESS LAYER
	private MaterialProperty shoreWetnessLayerEnabled;

	private MaterialEditor m_MaterialEditor;
	private WorkflowMode m_WorkflowMode = WorkflowMode.Specular;

	private bool m_FirstTimeApply = true;

	public void FindProperties( MaterialProperty[] props )
	{
		blendMode = FindProperty( "_Mode", props );
		cullMode = FindProperty( "_Cull", props );

		// BASE LAYER
		albedoMap = FindProperty( "_MainTex", props );
		albedoMapScroll = FindProperty( "_MainTexScroll", props );
		albedoColor = FindProperty( "_Color", props );
		alphaCutoff = FindProperty( "_Cutoff", props );
		specularMap = FindProperty( "_SpecGlossMap", props, false );
		specularColor = FindProperty( "_SpecColor", props, false );
		metallicMap = FindProperty( "_MetallicGlossMap", props, false );
		metallic = FindProperty( "_Metallic", props, false );
		if ( specularMap != null && specularColor != null )
			m_WorkflowMode = WorkflowMode.Specular;
		else if ( metallicMap != null && metallic != null )
			m_WorkflowMode = WorkflowMode.Metallic;
		else
			m_WorkflowMode = WorkflowMode.Dielectric;
		smoothness = FindProperty( "_Glossiness", props );
		bumpScale = FindProperty( "_BumpScale", props );
		bumpMap = FindProperty( "_BumpMap", props );
		heigtMapScale = FindProperty( "_Parallax", props, false );
		heightMap = FindProperty( "_ParallaxMap", props, false );
		occlusionStrength = FindProperty( "_OcclusionStrength", props, false );
		occlusionMap = FindProperty( "_OcclusionMap", props, false );
		emissionScaleUI = FindProperty( "_EmissionScaleUI", props );
		emissionColorUI = FindProperty( "_EmissionColorUI", props );
		emissionColorForRendering = FindProperty( "_EmissionColor", props );
		emissionMap = FindProperty( "_EmissionMap", props, false );
		detailMask = FindProperty( "_DetailMask", props, false );
		envReflOcclusionStrength = FindProperty( "_EnvReflOcclusionStrength", props );
		envReflHorizonFade = FindProperty( "_EnvReflHorizonFade", props );

		// DETAIL LAYER
		detailLayerEnabled = FindProperty( "_DetailLayer", props, false );
		if ( detailLayerEnabled != null )
		{
			detailAlbedoColor = FindProperty( "_DetailColor", props );
			detailAlbedoMap = FindProperty( "_DetailAlbedoMap", props );
			detailAlbedoMapScroll = FindProperty( "_DetailAlbedoMapScroll", props );
			detailOverlaySpecular = FindProperty( "_DetailOverlaySpecular", props, false );
			detailOverlayMetallic = FindProperty( "_DetailOverlayMetallic", props, false );
			detailOverlaySmoothness = FindProperty( "_DetailOverlaySmoothness", props );
			detailNormalMapScale = FindProperty( "_DetailNormalMapScale", props );
			detailNormalMap = FindProperty( "_DetailNormalMap", props );
			detailOcclusionStrength = FindProperty( "_DetailOcclusionStrength", props );
			detailOcclusionMap = FindProperty( "_DetailOcclusionMap", props, false );
			detailUVSet = FindProperty( "_DetailUVSet", props );
		}

		// MICROFIBER FUZZ LAYER
		microfiberFuzzLayerEnabled = FindProperty( "_MicrofiberFuzzLayer", props, false );
		if ( microfiberFuzzLayerEnabled != null )
		{
			microfiberFuzzLayerMask = FindProperty( "_MicrofiberFuzzMask", props );
			microfiberFuzzLayerColor = FindProperty( "_MicrofiberFuzzColor", props );
			microfiberFuzzLayerIntensity = FindProperty( "_MicrofiberFuzzIntensity", props );
			microfiberFuzzLayerScatter = FindProperty( "_MicrofiberFuzzScatter", props );
			microfiberFuzzLayerOcclusion = FindProperty( "_MicrofiberFuzzOcclusion", props );
			microfiberFuzzLayerMaskWithGloss = FindProperty( "_MicrofiberFuzzMaskWithGloss", props );
		}

		// COLOURIZE LAYER
		colorizeLayerEnabled = FindProperty( "_ColorizeLayer", props, false );
		if ( colorizeLayerEnabled != null )
		{
			colorizeLayerMask = FindProperty( "_ColorizeMask", props );
			colorizeLayerColorR = FindProperty( "_ColorizeColorR", props );
			colorizeLayerColorG = FindProperty( "_ColorizeColorG", props );
			colorizeLayerColorB = FindProperty( "_ColorizeColorB", props );
			colorizeLayerColorA = FindProperty( "_ColorizeColorA", props );
		}

		// PARTICLE ACCUMULATION LAYER
		particleLayerEnabled = FindProperty( "_ParticleLayer", props, false );
		if ( particleLayerEnabled != null )
		{
			particleLayerThickness = FindProperty( "_ParticleLayer_Thickness", props );
			particleLayerBlendFactor = FindProperty( "_ParticleLayer_BlendFactor", props );
			particleLayerBlendFalloff = FindProperty( "_ParticleLayer_BlendFalloff", props );
			particleLayerWorldDirection = FindProperty( "_ParticleLayer_WorldDirection", props );
			particleLayerMapTiling = FindProperty( "_ParticleLayer_MapTiling", props );
			particleLayerAlbedoColor = FindProperty( "_ParticleLayer_AlbedoColor", props );
			particleLayerAlbedoMap = FindProperty( "_ParticleLayer_AlbedoMap", props );
			particleLayerSmoothness = FindProperty( "_ParticleLayer_Glossiness", props );
			particleLayerSpecularColor = FindProperty( "_ParticleLayer_SpecColor", props, false );
			particleLayerSpecularMap = FindProperty( "_ParticleLayer_SpecGlossMap", props, false );
			particleLayerMetallic = FindProperty( "_ParticleLayer_Metallic", props, false );
			particleLayerMetallicMap = FindProperty( "_ParticleLayer_MetallicGlossMap", props, false );
			particleLayerNormalScale = FindProperty( "_ParticleLayer_NormalScale", props );
			particleLayerNormalMap = FindProperty( "_ParticleLayer_NormalMap", props );
		}

		// TERRAIN LAYER
		terrainLayerEnabled = FindProperty( "_TerrainLayer", props, false );
		if ( terrainLayerEnabled != null )
		{
			terrainLayerDistanceFadeStart = FindProperty( "_TerrainLayer_DistanceFadeStart", props );
			terrainLayerDistanceFadeRange = FindProperty( "_TerrainLayer_DistanceFadeRange", props );
			terrainLayerSteepnessFadeStart = FindProperty( "_TerrainLayer_SteepnessFadeStart", props );
			terrainLayerSteepnessFadeRange = FindProperty( "_TerrainLayer_SteepnessFadeRange", props );
			terrainLayerHeightFadeStart = FindProperty( "_TerrainLayer_HeightFadeStart", props );
			terrainLayerHeightFadeRange = FindProperty( "_TerrainLayer_HeightFadeRange", props );
			terrainLayerBlendFactor = FindProperty( "_TerrainLayer_BlendFactor", props );
			terrainLayerBlendFalloff = FindProperty( "_TerrainLayer_BlendFalloff", props );
			terrainLayerWorldDirection = FindProperty( "_TerrainLayer_WorldDirection", props );
			terrainLayerCoatThickness = FindProperty( "_TerrainLayer_CoatThickness", props );
			terrainLayerCoatBlendFactor = FindProperty( "_TerrainLayer_CoatBlendFactor", props );
			terrainLayerCoatBlendFalloff = FindProperty( "_TerrainLayer_CoatBlendFalloff", props );
		}

		// WEATHER WETNESS LAYER
		wetnessLayerEnabled = FindProperty( "_WetnessLayer", props, false );
		if ( wetnessLayerEnabled != null )
		{
			wetnessLayerIntensity = FindProperty( "_WetnessLayer_Intensity", props );
			wetnessLayerTint = FindProperty( "_WetnessLayer_Tint", props );
			wetnessLayerMask = FindProperty( "_WetnessLayer_Mask", props, false );
			wetnessLayerSmoothness = FindProperty( "_WetnessLayer_Glossiness", props );
			wetnessLayerSpecularColor = FindProperty( "_WetnessLayer_SpecColor", props, false );
			wetnessLayerSpecularMap = FindProperty( "_WetnessLayer_SpecGlossMap", props, false );
			wetnessLayerMetallic = FindProperty( "_WetnessLayer_Metallic", props, false );
			wetnessLayerMetallicMap = FindProperty( "_WetnessLayer_MetallicMap", props, false );
			wetnessLayerOcclusionStrength = FindProperty( "_WetnessLayer_OcclusionStrength", props );
		}

		// WEATHER WETNESS LAYER
		shoreWetnessLayerEnabled = FindProperty( "_ShoreWetnessLayer", props, false );
		if ( shoreWetnessLayerEnabled != null )
		{
		}
	}

	public override void OnGUI( MaterialEditor materialEditor, MaterialProperty[] props )
	{
		FindProperties( props ); // MaterialProperties can be animated so we do not cache them but fetch them every event to ensure animated values are updated correctly
		m_MaterialEditor = materialEditor;
		Material material = materialEditor.target as Material;

		ShaderPropertiesGUI( material );

		// Make sure that needed keywords are set up if we're switching some existing
		// material to a standard shader.
		if ( m_FirstTimeApply )
		{
			SetMaterialKeywords( material, m_WorkflowMode );
			m_FirstTimeApply = false;
		}
	}

	private bool LayerToggleFoldout( Material material, string name, MaterialProperty toggleProperty )
	{
		const float foldoutOffset = -17;

		string label = " " + name;

		GUILayout.BeginHorizontal();
		GUILayout.Space( 3 );

		bool wasEnabled = true;
		bool isEnabled = true;

		if ( toggleProperty != null )
		{
			wasEnabled = toggleProperty.floatValue != 0;
			isEnabled = GUILayout.Toggle( toggleProperty.floatValue != 0, label );
			toggleProperty.floatValue = isEnabled ? 1 : 0;

			string keyword = toggleProperty.name.ToUpper() + "_ON";

			if ( !material.IsKeywordEnabled( keyword ) && isEnabled )
				material.EnableKeyword( keyword );
			else if ( material.IsKeywordEnabled( keyword ) && !isEnabled )
				material.DisableKeyword( keyword );
		}
		else
			GUILayout.Toggle( true, label );

		GUILayout.EndHorizontal();
		GUILayout.Space( foldoutOffset );

		bool wasOpen = EditorPrefs.GetBool( "RustStandardShaderGUI." + name, true );
		bool isOpen = EditorGUILayout.Foldout( wasOpen, "" );

		isOpen = ( toggleProperty != null && toggleProperty.floatValue == 0 ) ? false : isOpen;
		isOpen = ( toggleProperty != null && !isOpen && !wasEnabled && isEnabled ) ? true : isOpen;

		if ( isOpen != wasOpen )
			EditorPrefs.SetBool( "RustStandardShaderGUI." + name, isOpen );
		return isOpen;
	}

	public void ShaderPropertiesGUI( Material material )
	{
		const float indentOffset = 15;

		// Use default labelWidth
		EditorGUIUtility.labelWidth = 0f;

		// Detect any changes to the material
		EditorGUI.BeginChangeCheck();
		{
			BlendModePopup();

			EditorGUILayout.Space();

			if ( LayerToggleFoldout( material, "BASE LAYER", null ) )
			{
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				GUILayout.Space( indentOffset );
				GUILayout.BeginVertical();

				DoAlbedoArea( material, albedoMap, albedoColor, true );
				DoSpecularMetallicArea( specularMap, specularColor, metallicMap, metallic, smoothness, true );
				m_MaterialEditor.TexturePropertySingleLine( Styles.normalMapText, bumpMap, bumpMap.textureValue != null ? bumpScale : null );

				if ( heightMap != null )
					m_MaterialEditor.TexturePropertySingleLine( Styles.heightMapText, heightMap, heightMap.textureValue != null ? heigtMapScale : null );

				if ( occlusionMap != null )
					m_MaterialEditor.TexturePropertySingleLine( Styles.occlusionText, occlusionMap, occlusionMap.textureValue != null ? occlusionStrength : null );

				if ( emissionMap != null )
					DoEmissionArea( material );

				if ( detailMask != null )
					m_MaterialEditor.TexturePropertySingleLine( Styles.detailMaskText, detailMask );

				m_MaterialEditor.ShaderProperty( envReflOcclusionStrength, "Env. Refl. Occlusion Strength" );
				m_MaterialEditor.ShaderProperty( envReflHorizonFade, "Env. Refl. Horizon Fade" );

				EditorGUI.BeginChangeCheck();
				m_MaterialEditor.TextureScaleOffsetProperty( albedoMap );
				if ( EditorGUI.EndChangeCheck() )
					emissionMap.textureScaleAndOffset = albedoMap.textureScaleAndOffset; // Apply the main texture scale and offset to the emission texture as well, for Enlighten's sake
				DoMapScroll( albedoMapScroll );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			if ( detailLayerEnabled != null && LayerToggleFoldout( material, "DETAIL LAYER", detailLayerEnabled ) )
			{
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				GUILayout.Space( indentOffset );
				GUILayout.BeginVertical();

				DoAlbedoArea( material, detailAlbedoMap, detailAlbedoColor, false );
				m_MaterialEditor.TexturePropertySingleLine( Styles.normalMapText, detailNormalMap, detailNormalMapScale );
				if ( detailOcclusionMap != null )
					m_MaterialEditor.TexturePropertySingleLine( Styles.occlusionText, detailOcclusionMap, detailOcclusionMap.textureValue != null ? detailOcclusionStrength : null );
				else
					m_MaterialEditor.ShaderProperty( detailOcclusionStrength, detailOcclusionStrength.displayName );

				if ( m_WorkflowMode == WorkflowMode.Specular )
					m_MaterialEditor.ShaderProperty( detailOverlaySpecular, "Overlay Specular" );
				else if ( m_WorkflowMode == WorkflowMode.Metallic )
					m_MaterialEditor.ShaderProperty( detailOverlayMetallic, "Overlay Metallic" );

				m_MaterialEditor.ShaderProperty( detailOverlaySmoothness, "Overlay Smoothness" );
				m_MaterialEditor.TextureScaleOffsetProperty( detailAlbedoMap );
				DoMapScroll( detailAlbedoMapScroll );
				m_MaterialEditor.ShaderProperty( detailUVSet, "UV Set" );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			if ( microfiberFuzzLayerEnabled != null && LayerToggleFoldout( material, "MICROFIBER FUZZ LAYER", microfiberFuzzLayerEnabled ) )
			{
				GUILayout.Space( 6 );
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				GUILayout.BeginVertical();

				m_MaterialEditor.TexturePropertySingleLine( new GUIContent( "Mask" ), microfiberFuzzLayerMask );
				m_MaterialEditor.ShaderProperty( microfiberFuzzLayerColor, "Color" );
				m_MaterialEditor.ShaderProperty( microfiberFuzzLayerIntensity, "Intensity" );
				m_MaterialEditor.ShaderProperty( microfiberFuzzLayerScatter, "Scatter" );
				m_MaterialEditor.ShaderProperty( microfiberFuzzLayerOcclusion, "Occlusion" );
				m_MaterialEditor.ShaderProperty( microfiberFuzzLayerMaskWithGloss, "Mask With Gloss" );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			if ( colorizeLayerEnabled != null && LayerToggleFoldout( material, "COLORIZE LAYER", colorizeLayerEnabled ) )
			{
				GUILayout.Space( 6 );
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				GUILayout.BeginVertical();

				m_MaterialEditor.TexturePropertySingleLine( new GUIContent( "Mask" ), colorizeLayerMask );
				m_MaterialEditor.ShaderProperty( colorizeLayerColorR, "Mask R Color" );
				m_MaterialEditor.ShaderProperty( colorizeLayerColorG, "Mask G Color" );
				m_MaterialEditor.ShaderProperty( colorizeLayerColorB, "Mask B Color" );
				m_MaterialEditor.ShaderProperty( colorizeLayerColorA, "Mask A Color" );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			if ( particleLayerEnabled != null && LayerToggleFoldout( material, "PARTICLE ACCUM. LAYER", particleLayerEnabled ) )
			{
				GUILayout.Space( 6 );
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				GUILayout.BeginVertical();

				DoAlbedoArea( material, particleLayerAlbedoMap, particleLayerAlbedoColor, false );
				DoSpecularMetallicArea( particleLayerSpecularMap, particleLayerSpecularColor, particleLayerMetallicMap, particleLayerMetallic, particleLayerSmoothness, true );
				m_MaterialEditor.TexturePropertySingleLine( Styles.normalMapText, particleLayerNormalMap, particleLayerNormalMap.textureValue != null ? particleLayerNormalScale : null );
				m_MaterialEditor.ShaderProperty( particleLayerMapTiling, "Map Tiling (World)" );
				m_MaterialEditor.ShaderProperty( particleLayerThickness, "Thickness" );
				m_MaterialEditor.ShaderProperty( particleLayerBlendFactor, "Blend Factor" );
				m_MaterialEditor.ShaderProperty( particleLayerBlendFalloff, "Blend Falloff" );
				m_MaterialEditor.ShaderProperty( particleLayerWorldDirection, "Direction (World)" );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			if ( terrainLayerEnabled != null && LayerToggleFoldout( material, "TERRAIN LAYER", terrainLayerEnabled ) )
			{
				GUILayout.Space( 6 );
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				GUILayout.BeginVertical();

				m_MaterialEditor.ShaderProperty( terrainLayerDistanceFadeStart, "Meld Dist. Fade Start" );
				m_MaterialEditor.ShaderProperty( terrainLayerDistanceFadeRange, "Meld Dist. Fade Range" );
				m_MaterialEditor.ShaderProperty( terrainLayerSteepnessFadeStart, "Steepness Fade Start" );
				m_MaterialEditor.ShaderProperty( terrainLayerSteepnessFadeRange, "Steepness Fade Range" );
				m_MaterialEditor.ShaderProperty( terrainLayerHeightFadeStart, "Height Fade Start" );
				m_MaterialEditor.ShaderProperty( terrainLayerHeightFadeRange, "Height Fade Range" );
				m_MaterialEditor.ShaderProperty( terrainLayerBlendFactor, "Blend Factor" );
				m_MaterialEditor.ShaderProperty( terrainLayerBlendFalloff, "Blend Falloff" );
				m_MaterialEditor.ShaderProperty( terrainLayerWorldDirection, "Direction (World)" );
				m_MaterialEditor.ShaderProperty( terrainLayerCoatThickness, "Coat Thickness" );
				m_MaterialEditor.ShaderProperty( terrainLayerCoatBlendFactor, "Coat Blend Factor" );
				m_MaterialEditor.ShaderProperty( terrainLayerCoatBlendFalloff, "Coat Blend Falloff" );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				EditorGUILayout.Space();
			}

			if ( wetnessLayerEnabled != null && LayerToggleFoldout( material, "WEATHER WETNESS LAYER", wetnessLayerEnabled ) )
			{
				GUILayout.Space( 6 );
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				GUILayout.BeginVertical();

				m_MaterialEditor.ShaderProperty( wetnessLayerIntensity, "Wetness" );
				m_MaterialEditor.ShaderProperty( wetnessLayerOcclusionStrength, "Occlusion Strength" );

				if ( wetnessLayerMask != null )
					m_MaterialEditor.TexturePropertySingleLine( new GUIContent( "Soak Mask/Tint" ), wetnessLayerMask, wetnessLayerTint );

				if ( wetnessLayerSpecularMap != null )
					DoSpecularMetallicArea( wetnessLayerSpecularMap, wetnessLayerSpecularColor, wetnessLayerMetallic, wetnessLayerMetallicMap, wetnessLayerSmoothness, true );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}

			if ( shoreWetnessLayerEnabled != null && LayerToggleFoldout( material, "SHORE WETNESS LAYER", shoreWetnessLayerEnabled ) )
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space( 20 );
				GUILayout.BeginVertical();

				GUILayout.Label( "Shore Wetness parameters are set globally via Terrain Material.", EditorStyles.miniLabel );

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}

			m_MaterialEditor.ShaderProperty( cullMode, "Cull Mode" );
		}
		if ( EditorGUI.EndChangeCheck() )
		{
			foreach ( var obj in blendMode.targets )
				MaterialChanged( ( Material ) obj, m_WorkflowMode );
		}
	}

	internal void DetermineWorkflow( MaterialProperty[] props )
	{
		if ( FindProperty( "_SpecGlossMap", props, false ) != null && FindProperty( "_SpecColor", props, false ) != null )
			m_WorkflowMode = WorkflowMode.Specular;
		else if ( FindProperty( "_MetallicGlossMap", props, false ) != null && FindProperty( "_Metallic", props, false ) != null )
			m_WorkflowMode = WorkflowMode.Metallic;
		else
			m_WorkflowMode = WorkflowMode.Dielectric;
	}

	public override void AssignNewShaderToMaterial( Material material, Shader oldShader, Shader newShader )
	{
		base.AssignNewShaderToMaterial( material, oldShader, newShader );

		if ( oldShader == null || !oldShader.name.Contains( "Legacy Shaders/" ) )
			return;

		BlendMode blendMode = BlendMode.Opaque;
		if ( oldShader.name.Contains( "/Transparent/Cutout/" ) )
		{
			blendMode = BlendMode.Cutout;
		}
		else if ( oldShader.name.Contains( "/Transparent/" ) )
		{
			// NOTE: legacy shaders did not provide physically based transparency
			// therefore Fade mode
			blendMode = BlendMode.Fade;
		}
		material.SetFloat( "_Mode", ( float ) blendMode );

		DetermineWorkflow( MaterialEditor.GetMaterialProperties( new Material[] { material } ) );
		MaterialChanged( material, m_WorkflowMode );
	}

	private void BlendModePopup()
	{
		EditorGUI.showMixedValue = blendMode.hasMixedValue;
		var mode = ( BlendMode ) blendMode.floatValue;

		EditorGUI.BeginChangeCheck();
		mode = ( BlendMode ) EditorGUILayout.Popup( Styles.renderingMode, ( int ) mode, Styles.blendNames );
		if ( EditorGUI.EndChangeCheck() )
		{
			m_MaterialEditor.RegisterPropertyChangeUndo( "Rendering Mode" );
			blendMode.floatValue = ( float ) mode;
		}

		EditorGUI.showMixedValue = false;
	}

	private void DoAlbedoArea( Material material, MaterialProperty map, MaterialProperty color, bool showCutoff )
	{
		m_MaterialEditor.TexturePropertySingleLine( new GUIContent( map.displayName ), map, color );
		if ( ( ( BlendMode ) material.GetFloat( "_Mode" ) == BlendMode.Cutout ) && showCutoff )
		{
			m_MaterialEditor.ShaderProperty( alphaCutoff, Styles.alphaCutoffText.text, MaterialEditor.kMiniTextureFieldLabelIndentLevel + 1 );
		}
	}

	private void DoEmissionArea( Material material )
	{
		bool showEmissionColorAndGIControls = emissionScaleUI.floatValue > 0f;
		bool hadEmissionTexture = emissionMap.textureValue != null;

		// Do controls
		m_MaterialEditor.TexturePropertySingleLine( Styles.emissionText, emissionMap, showEmissionColorAndGIControls ? emissionColorUI : null, emissionScaleUI );

		// Set default emissionScaleUI if texture was assigned
		if ( emissionMap.textureValue != null && !hadEmissionTexture && emissionScaleUI.floatValue <= 0f )
			emissionScaleUI.floatValue = 1.0f;

		// Dynamic Lightmapping mode
		if ( showEmissionColorAndGIControls )
		{
			bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled( EvalFinalEmissionColor( material ) );
			EditorGUI.BeginDisabledGroup( !shouldEmissionBeEnabled );

			m_MaterialEditor.LightmapEmissionProperty();

			EditorGUI.EndDisabledGroup();
		}

		if ( !HasValidEmissiveKeyword( material ) )
		{
			EditorGUILayout.HelpBox( Styles.emissiveWarning.text, MessageType.Warning );
		}
	}

	private void DoSpecularMetallicArea( MaterialProperty specMap, MaterialProperty specColor, MaterialProperty metalMap, MaterialProperty metal, MaterialProperty smooth, bool showValues )
	{
		if ( m_WorkflowMode == WorkflowMode.Specular )
		{
			if ( specMap.textureValue == null || showValues )
				m_MaterialEditor.TexturePropertySingleLine( Styles.specularMapText, specMap, specColor, smooth );
			else
				m_MaterialEditor.TexturePropertySingleLine( Styles.specularMapText, specMap );
		}
		else if ( m_WorkflowMode == WorkflowMode.Metallic )
		{
			if ( metalMap.textureValue == null || showValues )
				m_MaterialEditor.TexturePropertySingleLine( Styles.metallicMapText, metalMap, metal, smooth );
			else
				m_MaterialEditor.TexturePropertySingleLine( Styles.metallicMapText, metalMap );
		}
	}

	private void DoMapScroll( MaterialProperty prop )
	{
		EditorGUILayout.BeginHorizontal();
		EditorGUILayout.LabelField( "Offset Scroll" );
		GUILayout.Space( -66 );
		Vector2 v = new Vector2( prop.vectorValue.x, prop.vectorValue.y );
		EditorGUILayout.LabelField( "X", GUILayout.Width( 11 ) );
		v.x = EditorGUILayout.FloatField( v.x );
		EditorGUILayout.LabelField( "Y", GUILayout.Width( 11 ) );
		v.y = EditorGUILayout.FloatField( v.y );
		prop.vectorValue = new Vector4( v.x, v.y, 0, 0 );
		EditorGUILayout.EndHorizontal();
	}

	public static void SetupMaterialWithBlendMode( Material material, BlendMode blendMode )
	{
		switch ( blendMode )
		{
			case BlendMode.Opaque:
			material.SetInt( "_SrcBlend", ( int ) UnityEngine.Rendering.BlendMode.One );
			material.SetInt( "_DstBlend", ( int ) UnityEngine.Rendering.BlendMode.Zero );
			material.SetInt( "_ZWrite", 1 );
			material.DisableKeyword( "_ALPHATEST_ON" );
			material.DisableKeyword( "_ALPHABLEND_ON" );
			material.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
			material.renderQueue = -1;
			break;

			case BlendMode.Cutout:
			material.SetInt( "_SrcBlend", ( int ) UnityEngine.Rendering.BlendMode.One );
			material.SetInt( "_DstBlend", ( int ) UnityEngine.Rendering.BlendMode.Zero );
			material.SetInt( "_ZWrite", 1 );
			material.EnableKeyword( "_ALPHATEST_ON" );
			material.DisableKeyword( "_ALPHABLEND_ON" );
			material.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
			material.renderQueue = 2450;
			break;

			case BlendMode.Fade:
			material.SetInt( "_SrcBlend", ( int ) UnityEngine.Rendering.BlendMode.SrcAlpha );
			material.SetInt( "_DstBlend", ( int ) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
			material.SetInt( "_ZWrite", 0 );
			material.DisableKeyword( "_ALPHATEST_ON" );
			material.EnableKeyword( "_ALPHABLEND_ON" );
			material.DisableKeyword( "_ALPHAPREMULTIPLY_ON" );
			material.renderQueue = 3000;
			break;

			case BlendMode.Transparent:
			material.SetInt( "_SrcBlend", ( int ) UnityEngine.Rendering.BlendMode.One );
			material.SetInt( "_DstBlend", ( int ) UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha );
			material.SetInt( "_ZWrite", 0 );
			material.DisableKeyword( "_ALPHATEST_ON" );
			material.DisableKeyword( "_ALPHABLEND_ON" );
			material.EnableKeyword( "_ALPHAPREMULTIPLY_ON" );
			material.renderQueue = 3000;
			break;
		}
	}

	// Calculate final HDR _EmissionColor (gamma space) from _EmissionColorUI (LDR, gamma) & _EmissionScaleUI (gamma)
	private static Color EvalFinalEmissionColor( Material material )
	{
		return material.GetColor( "_EmissionColorUI" ) * material.GetFloat( "_EmissionScaleUI" );
	}

	private static bool ShouldEmissionBeEnabled( Color color )
	{
		return color.grayscale > ( 0.1f / 255.0f );
	}

	private static void SetMaterialKeywords( Material material, WorkflowMode workflowMode )
	{
		// Note: keywords must be based on Material value not on MaterialProperty due to multi-edit & material animation
		// (MaterialProperty value might come from renderer material property block)
		SetKeyword( material, "_NORMALMAP", material.GetTexture( "_BumpMap" ) || material.GetTexture( "_DetailNormalMap" ) );
		if ( workflowMode == WorkflowMode.Specular )
			SetKeyword( material, "_SPECGLOSSMAP", material.GetTexture( "_SpecGlossMap" ) );
		else if ( workflowMode == WorkflowMode.Metallic )
			SetKeyword( material, "_METALLICGLOSSMAP", material.GetTexture( "_MetallicGlossMap" ) );

		SetKeyword( material, "_PARALLAXMAP", material.GetTexture( "_ParallaxMap" ) );

		bool shouldEmissionBeEnabled = ShouldEmissionBeEnabled( material.GetColor( "_EmissionColor" ) );
		SetKeyword( material, "_EMISSION", shouldEmissionBeEnabled );

		// Setup lightmap emissive flags
		MaterialGlobalIlluminationFlags flags = material.globalIlluminationFlags;
		if ( ( flags & ( MaterialGlobalIlluminationFlags.BakedEmissive | MaterialGlobalIlluminationFlags.RealtimeEmissive ) ) != 0 )
		{
			flags &= ~MaterialGlobalIlluminationFlags.EmissiveIsBlack;
			if ( !shouldEmissionBeEnabled )
				flags |= MaterialGlobalIlluminationFlags.EmissiveIsBlack;

			material.globalIlluminationFlags = flags;
		}
	}

	private bool HasValidEmissiveKeyword( Material material )
	{
		// Material animation might be out of sync with the material keyword.
		// So if the emission support is disabled on the material, but the property blocks have a value that requires it, then we need to show a warning.
		// (note: (Renderer MaterialPropertyBlock applies its values to emissionColorForRendering))
		bool hasEmissionKeyword = material.IsKeywordEnabled( "_EMISSION" );
		if ( !hasEmissionKeyword && ShouldEmissionBeEnabled( emissionColorForRendering.colorValue ) )
			return false;
		else
			return true;
	}

	private static void MaterialChanged( Material material, WorkflowMode workflowMode )
	{
		// Clamp EmissionScale to always positive
		if ( material.GetFloat( "_EmissionScaleUI" ) < 0.0f )
			material.SetFloat( "_EmissionScaleUI", 0.0f );

		// Apply combined emission value
		Color emissionColorOut = EvalFinalEmissionColor( material );
		material.SetColor( "_EmissionColor", emissionColorOut );

		// Handle Blending modes
		SetupMaterialWithBlendMode( material, ( BlendMode ) material.GetFloat( "_Mode" ) );

		SetMaterialKeywords( material, workflowMode );
	}

	private static void SetKeyword( Material m, string keyword, bool state )
	{
		if ( state )
			m.EnableKeyword( keyword );
		else
			m.DisableKeyword( keyword );
	}
}
