#ifndef RUST_PBS_LIGHTING_INCLUDED
#define RUST_PBS_LIGHTING_INCLUDED

#include "UnityShaderVariables.cginc"
#include "UnityStandardConfig.cginc"
#include "UnityLightingCommon.cginc"
#include "UnityGlobalIllumination.cginc"
#include "UnityPBSLighting.cginc"

#include "Assets/Content/Shaders/Include/Encoding.cginc"

float4 _Time2;

//-------------------------------------------------------------------------------------------------------------
// Rust's custom BRDF; similar to BRDF1_Unity_PBS, except:
// 1) micro-occlusion aware Fresnel for direct specular term
// 2) cloud shadowing
// 3) transmission
// 4) microfiber fuzz

half3 F_Schlick( half3 f0, float u )
{
	// Reference: "An Efficient and Physically Plausible Real Time Shading Model" by C.Schuler, ShaderX7, 2.5
	// Schlick fresnel approximation adapted to baked micro-occlusion awareness

	// @diogo: retrieve micro-occlusion embedded in reflectance (e.g. cavities, cracks, etc..); reflectance values below a certain
	// threshold (f0=[0.02,0.08]) are considered impossible materials and assumed to be pre-baked occlusion

	// @diogo: there's something fishy about these reflectance values; f0 should be within [0.02,0.08];
	// so I'll just scale micro-occlusion to represent 25% of the allowed [0,1] range
	half f90 = saturate( 8.0 * dot( f0, 0.33 ) );

	return f0 + ( f90 - f0 ) * Pow5( abs( 1.0 - u ) );
}

half4 BRDF1_Rust_PBS (half3 diffColor, half3 specColor, half oneMinusReflectivity, half oneMinusRoughness, half occlusion, half transmission,
	half3 fuzz, float3 position, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi, bool cloudShadows )
{
	half roughness = 1-oneMinusRoughness;
	half3 halfDir = normalize (light.dir + viewDir);

	half nl = light.ndotl;
	half nh = BlinnTerm (normal, halfDir);
	half nv = DotClamped (normal, viewDir);
	half lv = DotClamped (light.dir, viewDir);
	half lh = DotClamped (light.dir, halfDir);

#if UNITY_BRDF_GGX
	half V = SmithGGXVisibilityTerm (nl, nv, roughness);
	half D = GGXTerm (nh, roughness);
#else
	half V = SmithBeckmannVisibilityTerm (nl, nv, roughness);
	half D = NDFBlinnPhongNormalizedTerm (nh, RoughnessToSpecPower (roughness));
#endif

	half nlPow5 = Pow5 (1-nl);
	half nvPow5 = Pow5 (1-nv);
	half Fd90 = 0.5 + 2 * lh * lh * roughness;
	half disneyDiffuse = (1 + (Fd90-1) * nlPow5) * (1 + (Fd90-1) * nvPow5);

	// HACK: theoretically we should divide by Pi diffuseTerm and not multiply specularTerm!
	// BUT 1) that will make shader look significantly darker than Legacy ones
	// and 2) on engine side "Non-important" lights have to be divided by Pi to in cases when they are injected into ambient SH
	// NOTE: multiplication by Pi is part of single constant together with 1/4 now

	half specularTerm = max(0, (V * D * nl) * unity_LightGammaCorrectionConsts_PIDiv4);// Torrance-Sparrow model, Fresnel is applied later (for optimization reasons)
	half diffuseTerm = disneyDiffuse * nl;

	half shadowTerm = 1;

#ifdef RUST_STD_SPEC_OCCLUSION
	specularTerm *= occlusion;
#endif

	half grazingTerm = saturate(oneMinusRoughness + (1-oneMinusReflectivity));

	half3 diffuseLight = gi.diffuse + light.color * diffuseTerm * shadowTerm;

    half3 color =	diffColor * diffuseLight
                    + specularTerm * light.color * F_Schlick (specColor, lh) * shadowTerm
					+ gi.specular * FresnelLerp (specColor, grazingTerm, nv);

#ifdef RUST_STD_TRANSMISSION
	color += diffColor * light.color * transmission * LambertTerm( -normal, light.dir );
#endif

#ifdef RUST_STD_FUZZ
	color += fuzz * diffuseLight;
#endif

	return half4( color, 1 );
}

half4 BRDF1_Rust_PBS( half3 diffColor, half3 specColor, half oneMinusReflectivity, half oneMinusRoughness, half occlusion, half transmission,
	half3 fuzz, float3 position, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi )
{
	return BRDF1_Rust_PBS( diffColor, specColor, oneMinusReflectivity, oneMinusRoughness, occlusion, transmission, fuzz, position, normal, viewDir, light, gi, true );
}

half4 BRDF1_Rust_PBS( half3 diffColor, half3 specColor, half oneMinusReflectivity, half oneMinusRoughness, half occlusion, half transmission,
	half3 fuzz, half3 normal, half3 viewDir, UnityLight light, UnityIndirect gi )
{
	return BRDF1_Rust_PBS( diffColor, specColor, oneMinusReflectivity, oneMinusRoughness, occlusion, transmission, fuzz, ( 0 ).xxx, normal, viewDir, light, gi, false );
}

// Functions sampling light environment data (lightmaps, light probes, reflection probes), which is then returned as the UnityGI struct.
#ifdef RUST_STD_ENVREFL_HORIZFADE
half _EnvReflHorizonFade;
#endif

inline UnityGI UnityGlobalIlluminationRust(
	UnityGIInput data,
	half occlusion,
#ifdef RUST_STD_ENVREFL_OCC
	half specularOcclusion,
#endif
	half oneMinusRoughness,
#ifdef RUST_STD_ENVREFL_HORIZFADE
	half3 vertexNormalWorld,
#endif
	half3 normalWorld,
	bool reflections )
{
	UnityGI o_gi;
	UNITY_INITIALIZE_OUTPUT(UnityGI, o_gi);

	// Explicitly reset all members of UnityGI
	ResetUnityGI(o_gi);

	#if UNITY_SHOULD_SAMPLE_SH
		#if UNITY_SAMPLE_FULL_SH_PER_PIXEL
			half3 sh = ShadeSH9(half4(normalWorld, 1.0));
		#elif (SHADER_TARGET >= 30)
			half3 sh = data.ambient + ShadeSH12Order(half4(normalWorld, 1.0));
		#else
			half3 sh = data.ambient;
		#endif

		o_gi.indirect.diffuse += sh;
	#endif

	#if !defined(LIGHTMAP_ON)
		o_gi.light = data.light;
		o_gi.light.color *= data.atten;

	#else
		// Baked lightmaps
		fixed4 bakedColorTex = UNITY_SAMPLE_TEX2D(unity_Lightmap, data.lightmapUV.xy);
		half3 bakedColor = DecodeLightmap(bakedColorTex);

		#ifdef DIRLIGHTMAP_OFF
			o_gi.indirect.diffuse = bakedColor;

			#ifdef SHADOWS_SCREEN
				o_gi.indirect.diffuse = MixLightmapWithRealtimeAttenuation (o_gi.indirect.diffuse, data.atten, bakedColorTex);
			#endif // SHADOWS_SCREEN

		#elif DIRLIGHTMAP_COMBINED
			fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER (unity_LightmapInd, unity_Lightmap, data.lightmapUV.xy);
			o_gi.indirect.diffuse = DecodeDirectionalLightmap (bakedColor, bakedDirTex, normalWorld);

			#ifdef SHADOWS_SCREEN
				o_gi.indirect.diffuse = MixLightmapWithRealtimeAttenuation (o_gi.indirect.diffuse, data.atten, bakedColorTex);
			#endif // SHADOWS_SCREEN

		#elif DIRLIGHTMAP_SEPARATE
			// Left halves of both intensity and direction lightmaps store direct light; right halves - indirect.

			// Direct
			fixed4 bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, data.lightmapUV.xy);
			o_gi.indirect.diffuse += DecodeDirectionalSpecularLightmap (bakedColor, bakedDirTex, normalWorld, false, 0, o_gi.light);

			// Indirect
			half2 uvIndirect = data.lightmapUV.xy + half2(0.5, 0);
			bakedColor = DecodeLightmap(UNITY_SAMPLE_TEX2D(unity_Lightmap, uvIndirect));
			bakedDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_LightmapInd, unity_Lightmap, uvIndirect);
			o_gi.indirect.diffuse += DecodeDirectionalSpecularLightmap (bakedColor, bakedDirTex, normalWorld, false, 0, o_gi.light2);
		#endif
	#endif

	#ifdef DYNAMICLIGHTMAP_ON
		// Dynamic lightmaps
		fixed4 realtimeColorTex = UNITY_SAMPLE_TEX2D(unity_DynamicLightmap, data.lightmapUV.zw);
		half3 realtimeColor = DecodeRealtimeLightmap (realtimeColorTex);

		#ifdef DIRLIGHTMAP_OFF
			o_gi.indirect.diffuse += realtimeColor;

		#elif DIRLIGHTMAP_COMBINED
			half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, data.lightmapUV.zw);
			o_gi.indirect.diffuse += DecodeDirectionalLightmap (realtimeColor, realtimeDirTex, normalWorld);

		#elif DIRLIGHTMAP_SEPARATE
			half4 realtimeDirTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicDirectionality, unity_DynamicLightmap, data.lightmapUV.zw);
			half4 realtimeNormalTex = UNITY_SAMPLE_TEX2D_SAMPLER(unity_DynamicNormal, unity_DynamicLightmap, data.lightmapUV.zw);
			o_gi.indirect.diffuse += DecodeDirectionalSpecularLightmap (realtimeColor, realtimeDirTex, normalWorld, true, realtimeNormalTex, o_gi.light3);
		#endif
	#endif
	o_gi.indirect.diffuse *= occlusion;

	half3 R = reflect( -data.worldViewDir, normalWorld );

	if (reflections)
	{
		half3 worldNormal = R;

		#if UNITY_SPECCUBE_BOX_PROJECTION
			half3 worldNormal0 = BoxProjectedCubemapDirection (worldNormal, data.worldPos, data.probePosition[0], data.boxMin[0], data.boxMax[0]);
		#else
			half3 worldNormal0 = worldNormal;
		#endif

		half3 env0 = Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(unity_SpecCube0), data.probeHDR[0], worldNormal0, 1-oneMinusRoughness);
		#if UNITY_SPECCUBE_BLENDING
			const float kBlendFactor = 0.99999;
			float blendLerp = data.boxMin[0].w;
			UNITY_BRANCH
			if (blendLerp < kBlendFactor)
			{
				#if UNITY_SPECCUBE_BOX_PROJECTION
					half3 worldNormal1 = BoxProjectedCubemapDirection (worldNormal, data.worldPos, data.probePosition[1], data.boxMin[1], data.boxMax[1]);
				#else
					half3 worldNormal1 = worldNormal;
				#endif

				half3 env1 = Unity_GlossyEnvironment (UNITY_PASS_TEXCUBE(unity_SpecCube1), data.probeHDR[1], worldNormal1, 1-oneMinusRoughness);
				o_gi.indirect.specular = lerp(env1, env0, blendLerp);
			}
			else
			{
				o_gi.indirect.specular = env0;
			}
		#else
			o_gi.indirect.specular = env0;
		#endif
	}

	#ifdef RUST_STD_ENVREFL_HORIZON_FADE
		half horizOcc = saturate( 1 + _EnvReflHorizonFade * dot( R, vertexNormalWorld ) );
		o_gi.indirect.specular *= horizOcc * horizOcc;
	#endif

	#ifdef RUST_STD_ENVREFL_OCC
		o_gi.indirect.specular *= specularOcclusion;
	#else
		o_gi.indirect.specular *= occlusion;
	#endif

	return o_gi;
}

//-------------------------------------------------------------------------------------------------------------
// Surface shader output structure to be used with physically based shading model.
// Includes both Specular and Metallic workflows with a few extra toggable options.

struct SurfaceOutputStandardRust
{
	fixed3 Albedo;			// diffuse color
#ifdef RUST_STD_SPECULAR
	fixed3 Specular;		// specular color
#else
	half Metallic;			// metallic
#endif
	fixed3 Normal;			// tangent space normal, if written
#ifdef RUST_STD_NORMAL_WORLD
	half3 NormalTangent;	// only used as temp; not output
	half3 NormalWorld;
#endif
#ifdef RUST_STD_ENVREFL_HORIZFADE
	half3 VertexNormalWorld;
#endif
	half3 Emission;
#ifdef RUST_STD_TRANSMISSION
	half3 Transmission;
#endif
#ifdef RUST_STD_FUZZ
	half3 Fuzz;
#endif
	half Smoothness;	// 0=rough, 1=smooth
	half Occlusion;
#ifdef RUST_STD_ENVREFL_OCC
	half EnvReflOcclusion;
#endif
	fixed Alpha;
};

#ifdef RUST_STD_NORMAL_WORLD
	#define NORMAL NormalWorld
#else
	#define NORMAL Normal
#endif

inline half4 LightingStandardRust (SurfaceOutputStandardRust s, half3 viewDir, UnityGI gi)
{
	s.NORMAL = normalize( s.NORMAL );

	// energy conservation
	half3 specColor;
	half oneMinusReflectivity;
#ifdef RUST_STD_SPECULAR
	s.Albedo = EnergyConservationBetweenDiffuseAndSpecular (s.Albedo, s.Specular, /*out*/ oneMinusReflectivity);
	specColor = s.Specular;
#else
	s.Albedo = DiffuseAndSpecularFromMetallic (s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity);
#endif

	// shader relies on pre-multiply alpha-blend (_SrcBlend = One, _DstBlend = OneMinusSrcAlpha)
	// this is necessary to handle transparency in physically correct way - only diffuse component gets affected by alpha
	half outputAlpha;
	s.Albedo = PreMultiplyAlpha (s.Albedo, s.Alpha, oneMinusReflectivity, /*out*/ outputAlpha);

	half3 albedo = s.Albedo;

#ifdef RUST_STD_TRANSMISSION
	half transmission = s.Transmission;
#else
	half transmission = 0;
#endif

#ifdef RUST_STD_FUZZ
	half3 fuzz = s.Fuzz;
#else
	half3 fuzz = 0;
#endif

	half4 c = BRDF1_Rust_PBS( albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Occlusion, transmission, fuzz, s.NORMAL, viewDir, gi.light, gi.indirect );
	c.rgb += BRDF_Unity_Indirect( albedo, specColor, oneMinusReflectivity, s.Smoothness, s.NORMAL, viewDir, s.Occlusion, gi );
	c.a = outputAlpha;

	return c;
}

inline half4 LightingStandardRust_Deferred( SurfaceOutputStandardRust s, half3 viewDir, UnityGI gi, out half4 outDiffuseOcclusion, out half4 outSpecSmoothness, out half4 outNormal )
{
	// energy conservation
	half3 specColor;
	half oneMinusReflectivity;
#ifdef RUST_STD_SPECULAR
	s.Albedo = EnergyConservationBetweenDiffuseAndSpecular( s.Albedo, s.Specular, /*out*/ oneMinusReflectivity );
	specColor = s.Specular;
#else
	s.Albedo = DiffuseAndSpecularFromMetallic( s.Albedo, s.Metallic, /*out*/ specColor, /*out*/ oneMinusReflectivity );
#endif

	half3 albedo = s.Albedo;

#ifdef RUST_STD_TRANSMISSION
	half transmission = s.Transmission;
#else
	half transmission = 0;
#endif

#ifdef RUST_STD_FUZZ
	half3 fuzz = s.Fuzz;
#else
	half3 fuzz = 0;
#endif

	half4 c = BRDF1_Rust_PBS( albedo, specColor, oneMinusReflectivity, s.Smoothness, s.Occlusion, transmission, fuzz, s.NORMAL, viewDir, gi.light, gi.indirect );
	c.rgb += BRDF_Unity_Indirect( albedo, specColor, oneMinusReflectivity, s.Smoothness, s.NORMAL, viewDir, s.Occlusion, gi );

	outDiffuseOcclusion = half4( s.Albedo, s.Occlusion );

#ifdef RUST_STD_FUZZ
	outSpecSmoothness = half4( s.Fuzz, s.Smoothness );
#else
	outSpecSmoothness = half4( specColor, s.Smoothness );
#endif

#if defined( RUST_STD_TRANSMISSION ) || defined( RUST_STD_FUZZ )
	outNormal.rg = oct_normal_encode( s.NORMAL );
#if defined( RUST_STD_TRANSMISSION )
	outNormal.b = transmission;
	outNormal.a = 0;				// transmission ext ON
#elif defined( RUST_STD_FUZZ )
	outNormal.b = Luminance( specColor );
	outNormal.a = 0.25;				// cloth ext ON
#endif
#else
	outNormal.rgb = half3( s.NORMAL * 0.5 + 0.5 );
	outNormal.a = 1;					// default, ext OFF
#endif

	half4 emission = half4( s.Emission + c.rgb, 1 );
	return emission;
}

inline void LightingStandardRust_GI (
	SurfaceOutputStandardRust s,
	UnityGIInput data,
	inout UnityGI gi)
{
	gi = UnityGlobalIlluminationRust (
		data,
		s.Occlusion,
	#ifdef RUST_STD_ENVREFL_OCC
		s.EnvReflOcclusion,
	#endif
		s.Smoothness,
	#ifdef RUST_STD_ENVREFL_HORIZFADE
		s.VertexNormalWorld,
	#endif
		s.NORMAL,
		true);
}

#endif // RUST_PBS_LIGHTING_INCLUDED
