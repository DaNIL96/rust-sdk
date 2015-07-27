#ifndef RUST_STANDARD_INCLUDED
#define RUST_STANDARD_INCLUDED

#define RUST_STD_NORMAL_WORLD
#define	RUST_STD_ENVREFL_OCC
#define RUST_STD_ENVREFL_HORIZFADE
#if _MICROFIBERFUZZLAYER_ON
	#define RUST_STD_FUZZ
#endif

#include "Assets/Content/Shaders/Include/Standard/RustPBSLighting.cginc"
#include "Assets/Content/Shaders/Include/Standard/StandardLayers.cginc"

struct Input
{
	float3 worldPos;		// built-in
	float3 worldNorm;
	float4 worldTangent;
	float4 texcoords;
	float3 viewDirForParallax;
};

void vert (inout appdata_full v, out Input o)
{
	UNITY_INITIALIZE_OUTPUT(Input, o);

#if _BASELAYER_ON
	o.texcoords.xy = TRANSFORM_TEX( v.texcoord, _MainTex );
	o.texcoords.xy += _MainTexScroll * _Time2.y;
#endif
#if _DETAILLAYER_ON
	o.texcoords.zw = TRANSFORM_TEX( ( ( _DetailUVSet == 0 ) ? v.texcoord1 : v.texcoord2 ), _DetailAlbedoMap );
	o.texcoords.zw += _DetailAlbedoMapScroll * _Time2.y;
#endif

	o.worldTangent = float4( UnityObjectToWorldNormal( v.tangent.xyz ), v.tangent.w );
	o.worldNorm = UnityObjectToWorldNormal( v.normal.xyz );

	TANGENT_SPACE_ROTATION;
	o.viewDirForParallax = mul( rotation, ObjSpaceViewDir( v.vertex ) );
}

void surf( Input IN, inout SurfaceOutputStandardRust o )
{
	// TANGENT-TO-WORLD BASIS TRANSFORM
	half3 worldNormal = normalize( IN.worldNorm.xyz );
	half3 worldTangent = normalize( IN.worldTangent.xyz );
	float3 worldBitangent = cross( worldNormal, worldTangent ) * IN.worldTangent.w;
	float3x3 tangentToWorld = float3x3( worldTangent, worldBitangent, worldNormal );

	// PREPARE SHARED PARAMS
	SharedParams param = ( SharedParams ) 0;
	param.worldPos = IN.worldPos;
	param.texcoords = IN.texcoords;
	param.viewDirForParallax = normalize( IN.viewDirForParallax );
	param.tangentToWorld = tangentToWorld;
#if _BASELAYER_ON
	param.occlusion = tex2D( _OcclusionMap, param.texcoords.xy ).g;
	param.detailMask = tex2D( _DetailMask, param.texcoords.xy ).g;
#else
	param.occlusion = 1;
	param.detailMask = 0;
#endif
#if _DETAILLAYER_ON
	param.detailOcclusion = tex2D( _DetailOcclusionMap, param.texcoords.zw ).g;
#else
	param.detailOcclusion = 1;
#endif
	param.combinedOcclusion = param.occlusion * param.detailOcclusion;

	// APPLY LAYERS; TANGENT-SPACE NORMAL
	ApplyBaseLayer( param, o );
	ApplyColorizeLayer(param, o);
	ApplyDetailLayer( param, o );
	ApplyMicrofiberFuzzLayer( param, o );
	ApplyParticleLayer( param, o );
	ApplyWeatherWetnessLayer( param, o );
	ApplyShoreWetnessLayer( param, o );
	ApplyReflectionOcclusion( param, o );

	// WORLD-NORMAL OUTPUT
	o.VertexNormalWorld = worldNormal;
	o.NormalWorld = mul( o.NormalTangent, tangentToWorld );
}

#endif // RUST_STANDARD_INCLUDED
