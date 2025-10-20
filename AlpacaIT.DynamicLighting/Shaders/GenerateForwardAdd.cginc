#include "UnityCG.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"
#include "DynamicLighting.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float4 color : COLOR;
};

struct v2f
{
    float2 uv0 : TEXCOORD0;
    UNITY_FOG_COORDS(1)
    UNITY_SHADOW_COORDS(4)
    float4 pos : SV_POSITION;
    float4 color : COLOR;
    float3 world : TEXCOORD2;
    float3 normal : TEXCOORD3;
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;

v2f vert (appdata v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
    o.color = v.color;
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.world = mul(unity_ObjectToWorld, v.vertex).xyz;
    UNITY_TRANSFER_FOG(o, o.pos);
	UNITY_TRANSFER_SHADOW(o, v.uv1);
    return o;
}

// return the main light during this pass.
UnityLight CreateLight(v2f i) {
	UnityLight light;
	#if defined(POINT) || defined(POINT_COOKIE) || defined(SPOT)
		light.dir = normalize(_WorldSpaceLightPos0 - i.world);
	#else
		light.dir = _WorldSpaceLightPos0;
	#endif
	UNITY_LIGHT_ATTENUATION(attenuation, i, i.world);
	light.color = _LightColor0 * attenuation;
	return light;
}

// return an unused indirect light source.
UnityIndirect CreateIndirectLight() {
	UnityIndirect indirectLight;
	indirectLight.diffuse = 0;
	indirectLight.specular = 0;
	return indirectLight;
}

fixed4 frag (v2f i) : SV_Target
{
    float3 view_direction = normalize(_WorldSpaceCameraPos - i.world);

    float4 brdf = UNITY_BRDF_PBS(
	    tex2D(_MainTex, i.uv0), // albedo
	    float3(0, 0, 0), // specular
	    1, // oneMinusReflectivity
	    0, // smoothness
	    i.normal, // normal
	    view_direction, // view direction
	    CreateLight(i), // gi light
	    CreateIndirectLight() // gi indirect
    );

    // sample the main texture, multiply by the light and add vertex colors.
    float4 col = float4(_Color.rgb, 1) * brdf * i.color;

    // apply fog.
    UNITY_APPLY_FOG(i.fogCoord, col);

    return col;
}