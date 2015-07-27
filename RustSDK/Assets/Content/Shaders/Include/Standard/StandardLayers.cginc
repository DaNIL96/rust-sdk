#ifndef RUST_STANDARD_LAYERS_INCLUDED
#define RUST_STANDARD_LAYERS_INCLUDED

#ifdef RUST_STD_ENVREFL_OCC
	half _EnvReflOcclusionStrength;
#endif

#if _BASELAYER_ON
	half4 _Color;
	sampler2D _MainTex;
	float4 _MainTex_ST;
	float2 _MainTexScroll;
	#if _ALPHATEST_ON
		half _Cutoff;
	#endif
	half _Glossiness;
	#ifdef RUST_STD_SPECULAR
		sampler2D _SpecGlossMap;
	#else
		half _Metallic;
		sampler2D _MetallicGlossMap;
	#endif
	half _BumpScale;
	sampler2D _BumpMap;
	half _Parallax;
	sampler2D _ParallaxMap;
	half _OcclusionStrength;
	sampler2D _OcclusionMap;
	half4 _EmissionColor;
	sampler2D _EmissionMap;
	sampler2D _DetailMask;				// uses Base Layer UVs; only shows up if Detail Layer enabled	
#endif

#if _DETAILLAYER_ON
	half4 _DetailColor;
	sampler2D _DetailAlbedoMap;
	#ifdef RUST_STD_SPECULAR
		half _DetailOverlaySpecular;
	#else
		half _DetailOverlayMetallic;
	#endif
	half _DetailOverlaySmoothness;
	float4 _DetailAlbedoMap_ST;
	float2 _DetailAlbedoMapScroll;
	half _DetailNormalMapScale;
	sampler2D _DetailNormalMap;
	half _DetailOcclusionStrength;
	sampler2D _DetailOcclusionMap;
	half _DetailUVSet;
#endif

#if _MICROFIBERFUZZLAYER_ON
	sampler2D _MicrofiberFuzzMask;
	half3 _MicrofiberFuzzColor;
	half _MicrofiberFuzzIntensity;
	half _MicrofiberFuzzScatter;
	half _MicrofiberFuzzOcclusion;
	half _MicrofiberFuzzMaskWithGloss;
#endif

#if _COLORIZELAYER_ON
	sampler2D _ColorizeMask;
	half4 _ColorizeColorR;
	half4 _ColorizeColorG;
	half4 _ColorizeColorB;
	half4 _ColorizeColorA;
#endif

struct SharedParams
{
	float3 worldPos;
	float3 worldView;
	float4 texcoords;
	float3 viewDirForParallax;
	float3x3 tangentToWorld;
	half height;
	half occlusion;
	half detailMask;
	half detailOcclusion;
	half combinedOcclusion;
	half camDist;
};

half3 SampleCombinedNormalLOD( inout SharedParams param, float lod )
{
#if _BASELAYER_ON
	half3 normal = UnpackScaleNormal( tex2Dlod( _BumpMap, float4( param.texcoords.xy, 0, lod ) ), _BumpScale );
	#if _DETAILLAYER_ON
		half3 detailNormal = UnpackScaleNormal( tex2Dlod( _DetailNormalMap, float4( param.texcoords.zw, 0, lod ) ), _DetailNormalMapScale );
		normal = lerp( normal, BlendNormals( normal, detailNormal ), param.detailMask );
	#endif
	return normal;
#else
	return half3( 0, 0, 1 );
#endif
}

void ApplyBaseLayer( inout SharedParams param, inout SurfaceOutputStandardRust o )
{
#if _BASELAYER_ON
	fixed alpha = tex2D( _MainTex, param.texcoords.xy ).a;
	#if _ALPHATEST_ON
		clip( alpha - _Cutoff );
	#endif

	#if _PARALLAXMAP
		half h = tex2D( _ParallaxMap, param.texcoords.xy ).g;
		param.texcoords += ParallaxOffset( h, _Parallax, param.viewDirForParallax ).xyxy;
	#endif
	half3 albedo = tex2D( _MainTex, param.texcoords.xy ).rgb * _Color.rgb;
	#ifdef RUST_STD_SPECULAR
		half4 specGlossConst = half4( _SpecColor.rgb, _Glossiness );
		half4 specGloss = tex2D( _SpecGlossMap, param.texcoords.xy ) * specGlossConst;
	#else
		half2 metalGlossConst = half2( _Metallic, _Glossiness );
		half2 metalGloss = tex2D( _MetallicGlossMap, param.texcoords.xy ).ra * metalGlossConst;
	#endif
	half3 normal = UnpackScaleNormal( tex2D( _BumpMap, param.texcoords.xy ), _BumpScale );
	half occlusion = LerpOneTo( param.occlusion, _OcclusionStrength );
	half3 emission = _EmissionColor.rgb;
	#if _EMISSION
		emission *= tex2D( _EmissionMap, param.texcoords.xy ).rgb;
	#endif

	// OUTPUT
	o.Albedo = albedo;
	#ifdef RUST_STD_SPECULAR
		o.Specular = specGloss.rgb;
		o.Smoothness = specGloss.a;
	#else
		o.Metallic = metalGloss.x;
		o.Smoothness = metalGloss.y;
	#endif
	o.NormalTangent = normal;
	o.Occlusion = occlusion;
	o.Emission = emission;
	o.Alpha = alpha;	
#endif
}

void ApplyDetailLayer( inout SharedParams param, inout SurfaceOutputStandardRust o )
{
#if _DETAILLAYER_ON
	half3 albedo = tex2D( _DetailAlbedoMap, param.texcoords.zw ).rgb * _DetailColor.rgb;
	half occlusion = LerpOneTo( param.detailOcclusion, _DetailOcclusionStrength );
	half3 normal = UnpackScaleNormal( tex2D( _DetailNormalMap, param.texcoords.zw ), _DetailNormalMapScale );
	half t = param.detailMask;

	half3 detailAlbedo = LerpWhiteTo( albedo * unity_ColorSpaceDouble.rgb, t );

	// OUTPUT
	o.Albedo = o.Albedo * detailAlbedo;
	o.NormalTangent = normalize( lerp( o.NormalTangent, BlendNormals( o.NormalTangent, normal ), t ) );
	o.Occlusion = o.Occlusion * LerpOneTo( occlusion, t );
	#ifdef RUST_STD_SPECULAR
		o.Specular = lerp( o.Specular, o.Specular * detailAlbedo.rgb, _DetailOverlaySpecular );
	#else
		o.Metallic = lerp( o.Metallic, o.Metallic * detailAlbedo.g, _DetailOverlayMetallic );
	#endif
	o.Smoothness = lerp( o.Smoothness, o.Smoothness * detailAlbedo.g, _DetailOverlaySmoothness );
#endif
}

void ApplyMicrofiberFuzzLayer(inout SharedParams param, inout SurfaceOutputStandardRust o)
{	 
#ifdef _MICROFIBERFUZZLAYER_ON
	const half MaxPower = 4;
	half power = 1 + ( MaxPower - 1 ) * ( 1 - _MicrofiberFuzzScatter );
	half attenuation = 1 / ( ( MaxPower + 1 ) - power );

	half NdotV = DotClamped( o.NormalTangent, param.viewDirForParallax );
	
	half fuzzMask = tex2D( _MicrofiberFuzzMask, param.texcoords.xy );
	half fuzzGlossMask = LerpOneTo( 1 - o.Smoothness, _MicrofiberFuzzMaskWithGloss );
	half fuzzOcclusion = LerpOneTo( o.Occlusion, _MicrofiberFuzzOcclusion );

	half fuzz = saturate( pow( ( 1 - NdotV ), power ) * attenuation ) * fuzzMask * fuzzGlossMask * fuzzOcclusion;

	o.Fuzz = fuzz * _MicrofiberFuzzColor * _MicrofiberFuzzIntensity;
#endif
}

void ApplyColorizeLayer(inout SharedParams param, inout SurfaceOutputStandardRust o)
{
#ifdef _COLORIZELAYER_ON
	half4 mask = tex2D( _ColorizeMask, param.texcoords.xy );
	half3 rgb = o.Albedo.rgb;
	o.Albedo.rgb = lerp( o.Albedo.rgb, rgb * _ColorizeColorR, mask.r * _ColorizeColorR.a );
	o.Albedo.rgb = lerp( o.Albedo.rgb, rgb * _ColorizeColorG, mask.g * _ColorizeColorG.a );
	o.Albedo.rgb = lerp( o.Albedo.rgb, rgb * _ColorizeColorB, mask.b * _ColorizeColorB.a );
	o.Albedo.rgb = lerp( o.Albedo.rgb, rgb * _ColorizeColorA, mask.a * _ColorizeColorA.a );
#endif
}

void ApplyParticleLayer( inout SharedParams param, inout SurfaceOutputStandardRust o )
{
}

// Terrain Layer Requirement
void ApplyTerrainLayer( inout SharedParams param, inout SurfaceOutputStandardRust o, bool blend )
{
}

void ApplyWeatherWetnessLayer( inout SharedParams param, inout SurfaceOutputStandardRust o )
{
}

void ApplyShoreWetnessLayer( inout SharedParams param, inout SurfaceOutputStandardRust o )
{
}

void ApplyPaintLayer( inout SharedParams param, inout SurfaceOutputStandardRust o )
{
}

void ApplyReflectionOcclusion( inout SharedParams param, inout SurfaceOutputStandardRust o )
{
#ifdef RUST_STD_ENVREFL_OCC
	o.EnvReflOcclusion = LerpOneTo( param.combinedOcclusion, _EnvReflOcclusionStrength );
#endif
}

#endif // RUST_STANDARD_LAYERS_INCLUDED
