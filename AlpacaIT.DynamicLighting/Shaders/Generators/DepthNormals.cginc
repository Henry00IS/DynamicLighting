// universal render pipeline - depth and normals pass for post processing effects.

#pragma vertex vert
#pragma fragment frag
#pragma shader_feature_local _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON

#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv0 : TEXCOORD0;
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float3 normal : TEXCOORD0;
    float2 uv0 : TEXCOORD1;
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;

#if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHATEST_ON)
    float _Cutoff;
#endif

v2f vert (appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
    return o;
}

float4 frag (v2f i) : SV_Target
{
	float alpha = _Color.a;
	alpha *= tex2D(_MainTex, i.uv0).a;
    #if defined(_ALPHAPREMULTIPLY_ON) || defined(_ALPHATEST_ON)
        clip(alpha - _Cutoff);
    #endif
    return float4(i.normal, 0);
}