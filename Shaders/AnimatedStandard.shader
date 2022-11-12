Shader "Game/AnimatedStandard"
{
	Properties
	{
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
		_Emission("Emission (Lightmapper)", Float) = 1.0
		_ScrollX("Scroll X (Animation)", Float) = 0.0
		_ScrollY("Scroll Y (Animation)", Float) = 0.0
		_SmallWavy("Small Wavy (Unreal Gold)", Float) = 0.0
	}

	CGINCLUDE
	sampler2D _MainTex;
	sampler2D _BumpMap;
	fixed4 _Color;
	fixed _Emission;

	fixed _ScrollX;
	fixed _ScrollY;
	fixed _SmallWavy;

	struct Input
	{
		float2 uv_MainTex;
		float2 uv_BumpMap;
	};

	void surf(Input IN, inout SurfaceOutput o)
	{
		fixed2 uv = IN.uv_MainTex;

		// horizontal and vertical scrolling.
		uv.x += _Time.y * _ScrollX;
		uv.y += _Time.y * _ScrollY;

		uv.x += (8.0f * _SinTime + 4.0f * cos(2.3f * _Time.x)) * _SmallWavy;// * 0.0001220703125f; // division by 8192.0f;
		uv.y += (8.0f * _CosTime + 4.0f * sin(2.3f * _Time.x)) * _SmallWavy;// * 0.0001220703125f; // division by 8192.0f;

		fixed4 tex = tex2D(_MainTex, uv);
		fixed4 c = tex * _Color;
		o.Albedo = c.rgb;
		o.Emission = c.rgb;
		#if defined (UNITY_PASS_META)
		o.Emission *= _Emission.rrr;
		#endif
		o.Alpha = c.a;
		o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
	}
	ENDCG

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 300

		CGPROGRAM
		#pragma surface surf Lambert
		#pragma target 3.0
		ENDCG
	}

	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 300

		CGPROGRAM
		#pragma surface surf Lambert nodynlightmap
		ENDCG
	}

	FallBack "Legacy Shaders/Self-Illumin/Diffuse"
}