#include "UnityCG.cginc"
#include "UnityPBSLighting.cginc"
#include "AutoLight.cginc"

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float4 color : COLOR;
    float4 tangent : TANGENT;
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
    float3 tspace0 : TEXCOORD5;
    float3 tspace1 : TEXCOORD6;
    float3 tspace2 : TEXCOORD7;
};

sampler2D _MainTex;
float4 _MainTex_ST;
float4 _Color;
sampler2D _MetallicGlossMap;
float _Metallic;
float _GlossMapScale;
sampler2D _BumpMap;
float _BumpScale;

#ifdef _ALPHATEST_ON
    float _Cutoff;
#endif

v2f vert (appdata v)
{
    v2f o;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.uv0 = TRANSFORM_TEX(v.uv0, _MainTex);
    o.color = v.color;
    o.normal = UnityObjectToWorldNormal(v.normal);
    o.world = mul(unity_ObjectToWorld, v.vertex).xyz;

    float3 wTangent = UnityObjectToWorldDir(v.tangent.xyz);
    // compute bitangent from cross product of normal and tangent
    float tangentSign = v.tangent.w * unity_WorldTransformParams.w;
    float3 wBitangent = cross(o.normal, wTangent) * tangentSign;
    // output the tangent space matrix
    o.tspace0 = float3(wTangent.x, wBitangent.x, o.normal.x);
    o.tspace1 = float3(wTangent.y, wBitangent.y, o.normal.y);
    o.tspace2 = float3(wTangent.z, wBitangent.z, o.normal.z);

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
		light.dir = _WorldSpaceLightPos0.xyz;
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
    
    // material parameters.
    float4 albedo = tex2D(_MainTex, i.uv0) * _Color * i.color;
    
    #if METALLIC_TEXTURE_UNASSIGNED // todo: this is not working right (it looks very dull).
        float metallic = _Metallic;
        float smoothness = _GlossMapScale;
    #else
        float4 metallicmap = tex2D(_MetallicGlossMap, i.uv0);
        float metallic = metallicmap.r;
        float smoothness = metallicmap.a * _GlossMapScale;
    #endif

    float3 bumpmap = UnpackNormalWithScale(tex2D(_BumpMap, i.uv0), _BumpScale);
    // transform normal from tangent to world space.
    float3 worldNormal;
    worldNormal.x = dot(i.tspace0, bumpmap);
    worldNormal.y = dot(i.tspace1, bumpmap);
    worldNormal.z = dot(i.tspace2, bumpmap);
    
    float3 N = normalize(worldNormal);

    // pbr terms for metallic workflow.
    float3 color = albedo.rgb * (1.0h - metallic);
    
    // calculate reflectance at normal incidence; if dia-electric (like plastic) use F0
    // of 0.04 and if it's a metal, use the albedo color as F0 (metallic workflow).
    float3 F0 = lerp(0.04, albedo, metallic);
    float oneMinusReflectivity = 1.0 - max(max(F0.r, F0.g), F0.b);
    
    float4 brdf = UNITY_BRDF_PBS(
	    color, // albedo
	    F0, // specular
	    oneMinusReflectivity, // oneMinusReflectivity
	    smoothness, // smoothness
	    N, // normal
	    view_direction, // view direction
	    CreateLight(i), // gi light
	    CreateIndirectLight() // gi indirect
    );
    
    #if defined(_ALPHAPREMULTIPLY_ON)
        brdf.rgb *= albedo.a;
    #elif defined(_ALPHATEST_ON)
        clip(albedo.a - _Cutoff);
    #endif
    
    // apply fog.
    UNITY_APPLY_FOG(i.fogCoord, brdf);

    return brdf;
}