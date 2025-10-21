#include "UnityCG.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv0 : TEXCOORD0;
};

struct v2f
{
    float2 uv0 : TEXCOORD0;
    float4 vertex : SV_POSITION;
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;

v2f vert(appdata v)
{
	v2f o;
	o.vertex = UnityClipSpaceShadowCasterPos(v.vertex.xyz, v.normal);
	o.vertex = UnityApplyLinearShadowBias(o.vertex);
	o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
	return o;
}

float4 frag(v2f i) : SV_Target
{
	float alpha = _Color.a;
	alpha *= tex2D(_MainTex, i.uv0).a;
	clip(alpha - 0.5);
	return 0;
}