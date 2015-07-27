Shader "Hidden/Dummy-AddPass" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		CGPROGRAM
		#pragma surface surf Lambert decal:add

		sampler2D _MainTex;

		struct Input {
			float2 uv_MainTex;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			o.Albedo = 0;
			o.Alpha = 0;
		}
		ENDCG
	}
	FallBack Off
}
