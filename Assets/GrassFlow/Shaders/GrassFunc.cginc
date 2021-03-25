
#ifdef INDIRECT_INSTANCING

#ifndef SRP
#define grassToWorld objToWorldMatrix
#define worldToGrass worldToObjMatrix
#else
#define grassToWorld UNITY_MATRIX_M
#define worldToGrass UNITY_MATRIX_I_M
#endif

//#define GetClipPos(localPos) mul(UNITY_MATRIX_VP, mul(objToWorldMatrix, localPos))
#define GetClipPos(worldPos) mul(UNITY_MATRIX_VP, float4(worldPos, 1.0))

#else

#ifndef SRP
#define grassToWorld unity_ObjectToWorld
#define worldToGrass unity_WorldToObject
#define GetClipPos(worldPos)  mul(UNITY_MATRIX_VP, float4(worldPos, 1.0))
#else
#define grassToWorld UNITY_MATRIX_M
#define worldToGrass UNITY_MATRIX_I_M

#ifdef SRP_SHADOWCASTER
#if UNITY_REVERSED_Z
#define GetClipPos(worldPos)  mul(UNITY_MATRIX_VP, float4(worldPos, 1.0));\
				o.pos.z = min(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE)
#else
#define GetClipPos(worldPos)  mul(UNITY_MATRIX_VP, float4(worldPos, 1.0))\
				o.pos.z = max(o.pos.z, o.pos.w * UNITY_NEAR_CLIP_VALUE)
#endif
#else
#define GetClipPos(worldPos)  mul(UNITY_MATRIX_VP, float4(worldPos, 1.0))
#endif
#endif
//#define GetClipPos(localPos)  UnityObjectToClipPos(localPos.xyz)

#endif


//SRP STUFF
#ifndef SRP
#define GET_GI(worldNormal) ShadeSH9(worldNormal)
#define GET_LIGHT_DIR(light) _WorldSpaceLightPos0
#define GET_LIGHT_COL(light) _LightColor0.rgb
#else
#define GET_GI(worldNormal) SampleSH(worldNormal)
#define GET_LIGHT_DIR(light) _MainLightPosition.xyz
// unity_LightData.z is 1 when not culled by the culling mask, otherwise 0.
// unity_ProbesOcclusion.x is the mixed light probe occlusion data
#define GET_LIGHT_COL(light) _MainLightColor.rgb * unity_LightData.z * unity_ProbesOcclusion.x
#endif




#define Variate(rnd, variance) (1.0f - rnd * variance)
#define VariateBalanced(rnd, variance) (rnd * variance - variance * 0.5)
#define RndFloat3 float3(rndm(rndSeed), rndm(rndSeed), rndm(rndSeed))
#define Float4(val) float4(val,val,val,1.0f)
#define Float3(val) float3(val,val,val)

#define lerp3(v1, v2, v3, UV) (lerp(lerp(v1, v2, UV.x), v3, UV.y))

#if !defined(SHADOWS_DEPTH) || defined(SEMI_TRANSPARENT)
#if defined(FOG_ON)
#define SET_UV(inuv) o.uv.xyz = float3(TRANSFORM_TEX(inuv.xy, _MainTex), inuv.z)
#else
#define SET_UV(inuv) o.uv.xy = TRANSFORM_TEX(inuv.xy, _MainTex)
#endif
#else
#define SET_UV(uv)
#endif

#if defined(FRAGMENT_REQUIRES_WORLDPOS)
#define SET_WORLDPOS(o, inPos) o.worldPos = inPos
#else
#define SET_WORLDPOS(o, inPos)
#endif


#if !defined(SHADOWS_DEPTH)
#ifndef SRP
	//Non-SRP

#if defined(SHADOW_CASTER)
	#define TRANSFER_GRASS_SHADOW(o, wpos)
#else
	#if defined(SHADOWS_SCREEN) && defined (UNITY_NO_SCREENSPACE_SHADOWS)
	#define TRANSFER_GRASS_SHADOW(o, wpos) o._ShadowCoord = mul(unity_WorldToShadow[0], wpos);
	#else
	#define TRANSFER_GRASS_SHADOW(o, wpos) TRANSFER_SHADOW(o)
	#endif
#endif

#else
	//SRP
#ifdef _MAIN_LIGHT_SHADOWS
#if SHADOWS_SCREEN
#define TRANSFER_GRASS_SHADOW(o, wpos) o.shadowCoord = ComputeScreenPos(o.pos)
#else
#define TRANSFER_GRASS_SHADOW(o, wpos) o.shadowCoord = (wpos)
#endif
#else
#define TRANSFER_GRASS_SHADOW(o, wpos)
#endif
#endif
#else
#define TRANSFER_GRASS_SHADOW(o, wpos)
#endif

#if defined(GRASS_EDITOR)
#define CHECK_PAINT_HIGHLIGHT bladeCol = GetPaintHighlight(bladeCol, rndUV)
#else
#define CHECK_PAINT_HIGHLIGHT 
#endif


#ifndef SRP
#define GRASS_SHADOW_ATTENUATION(i) SHADOW_ATTENUATION(i)
#else
#if defined(_MAIN_LIGHT_SHADOWS)
#define GRASS_SHADOW_ATTENUATION(i) MainLightRealtimeShadow(TransformWorldToShadowCoord(i.shadowCoord.xyz))
#else
#define GRASS_SHADOW_ATTENUATION(i) 1.0
#endif
#endif


//float3 VorHash(float3 x)
//{
//	x = float3(dot(x, float3(127.1, 311.7, 74.7)),
//		dot(x, float3(269.5, 183.3, 246.1)),
//		dot(x, float3(113.5, 271.9, 124.6)));
//
//	return frac(sin(x)*43758.5453123);
//}

#define rngfloat float2


//inline float rndm(inout rngfloat x)
//{
//	x = rngfloat(dot(x, rngfloat(127.1, 311.7, 74.7, 211.3)),
//				 dot(x, rngfloat(269.5, 183.3, 246.1, 69.2)),
//				 dot(x, rngfloat(113.5, 271.9, 124.6, 301.3)),
//				 dot(x, rngfloat(308.2, 143.6, 53.4, 192.1)));
//
//	x = frac(sin(x)*43758.5453123);
//
//	return x;
//}

//this seems to be about the bare minimum i can get away with on rng before noticeable artifacts start showing up
inline float rndm(inout rngfloat x)
{
	//ok so, in my testing this is somehow faster than the one below
	//even though it has one more dot(), idk, nothing makes sense anymore
	//i wanna go home
	x = frac(sin(rngfloat(
		dot(x, rngfloat(127.1, 311.7)),
		dot(x, rngfloat(269.5, 183.3))
	)) * 43758.5453123);

	//x = dot(x, rngfloat(127.1, 311.7));
	//x = frac(sin(x) * 43758.5453123);

	return x.x + 0.00000001;
}

//in my testing at least, somehow this is slower than the above method
//despite the other one having a sin() in it
//perhaps this varies on different hardware but man idk what to do
//inline float rndm(inout rngfloat p)
//{
//	float3 p3 = frac(float3(p.xyx) * .1031);
//	p3 += dot(p3, p3.yzx + 33.33);
//	p = frac((p3.x + p3.y) * p3.z);
//	return p + 0.00000001;
//}

//older rnd method, i think its technically better quality but the rng quality isnt super vital for this application
//static float4 _q = float4(1225.0, 1585.0, 2457.0, 2098.0);
//static float4 _r = float4(1112.0, 367.0, 92.0, 265.0);
//static float4 _a = float4(3423.0, 2646.0, 1707.0, 1999.0);
//static float4 _m = float4(4194287.0, 4194277.0, 4194191.0, 4194167.0);

//inline float rndm(inout rngfloat n) {
//	rngfloat beta = floor(n / _q);
//	rngfloat p = _a * (n - beta * _q) - beta * _r;
//	beta = (sign(-p) + rngfloat(1.0, 1.0, 1.0, 1.0)) * rngfloat(0.5, 0.5, 0.5, 0.5) * _m;
//	n = (p + beta);
//
//	return frac(dot(n / _m, rngfloat(1.0, -1.0, 1.0, -1.0)));
//}


//#define PI 3.14159265358979323846264
//#define stddev 0.3
//#define mean 0.5
//float gaussrand(float2 co)
//{
//	// Box-Muller method for sampling from the normal distribution
//	// http://en.wikipedia.org/wiki/Normal_distribution#Generating_values_from_normal_distribution
//	// This method requires 2 uniform random inputs and produces 2 
//	// Gaussian random outputs.  We'll take a 3rd random variable and use it to
//	// switch between the two outputs.
//
//	float U, V, R, Z;
//	// Add in the CPU-supplied random offsets to generate the 3 random values that
//	// we'll use.
//	U = frac(sin(dot(co.xy, float2(12.9898, 78.233))) * 43758.5453) + 0.000000000001;
//	V = frac(sin(dot(co.yx, float2(52.9898, 18.233))) * 13758.5453) + 0.000000000001;
//	R = frac(sin(dot(co.xy, float2(62.9898, 38.233))) * 93758.5453) + 0.000000000001;
//	// Switch between the two random outputs.
//	if (R < 0.5)
//		Z = sqrt(-2.0 * log(U)) * sin(2.0 * PI * V);
//	else
//		Z = sqrt(-2.0 * log(U)) * cos(2.0 * PI * V);
//
//	// Apply the stddev and mean.
//	return Z * stddev + mean;
//}

#ifdef GRASS_EDITOR
#define map(value, min1, max1, min2, max2) (value - min1) / (max1 - min1) * (max2 - min2) + min2

sampler2D paintHighlightBrushTex;
float4 paintHighlightBrushParams; //xy = brushPos, z = size, w = lerp value (on or off)
float4 paintHightlightColor;

inline float4 GetPaintHighlight(float4 inCol, float2 uv) {
	float2 topLeft = paintHighlightBrushParams.xy - paintHighlightBrushParams.z;
	float2 topRight = paintHighlightBrushParams.xy + paintHighlightBrushParams.z;
	float4 brushTexCoord = float4(map(uv, topLeft, topRight, float2(0, 0), float2(1, 1)), 0, 0);

	float brushPct = tex2Dlod(paintHighlightBrushTex, brushTexCoord).a * paintHighlightBrushParams.w;
	brushPct *= (brushTexCoord.x >= 0 && brushTexCoord.y >= 0 && brushTexCoord.x <= 1 && brushTexCoord.y <= 1);

	return float4(lerp(inCol, paintHightlightColor, brushPct).rgb, inCol.a);
}

#endif

inline float3 GrassToWorldNormal(in float3 norm)
{
#ifdef UNITY_ASSUME_UNIFORM_SCALING
	return (mul((float3x3)grassToWorld, norm));
#else
	return (mul(norm, (float3x3)worldToGrass));
#endif
}

inline float3 WorldToGrassDir(in float3 dir)
{
	return normalize(mul((float3x3)worldToGrass, dir));
}

#ifdef RENDERMODE_MESH
float3 Barycentric(float3 p, float3 a, float3 b, float3 c)
{
	float3 v0 = b - a, v1 = c - a, v2 = p - a;
	float d00 = dot(v0, v0);
	float d01 = dot(v0, v1);
	float d11 = dot(v1, v1);
	float d20 = dot(v2, v0);
	float d21 = dot(v2, v1);
	float denom = d00 * d11 - d01 * d01;
	float3 uvw;
	uvw.y = (d11 * d20 - d01 * d21) / denom;
	uvw.z = (d00 * d21 - d01 * d20) / denom;
	uvw.x = 1.0f - uvw.y - uvw.z;
	return uvw;
}

float3 rndPoint(in float2 UV, in float3 v1, in float3 v2, in float3 v3) {
	float sqrtR = 1.0 / rsqrt(UV.x);
	float a = 1.0 - sqrtR;
	float b = sqrtR * (1.0 - UV.y);
	float c = sqrtR * UV.y;
	return v1 * a + v2 * b + v3 * c;
}



inline void LerpVertData(out VertexData ovd, inout float2 UV, v2g v1, v2g v2, v2g v3, inout rngfloat rndSeed) {
	//ovd.test = 1;

#ifdef LOWER_QUALITY
	ovd.vertex = lerp3(v2.pos, v1.pos, v3.pos, UV);
	UV = lerp3(v1.uv, v2.uv, v3.uv, UV);
	ovd.normal = lerp3(v3.norm, v1.norm, v2.norm, UV);
#else
	ovd.vertex = rndPoint(UV, v1.pos, v2.pos, v3.pos);
	float3 bary = Barycentric(ovd.vertex, v1.pos, v2.pos, v3.pos);
	ovd.normal = bary.x * v1.norm + bary.y * v2.norm + bary.z * v3.norm;
	UV = bary.x * v1.uv + bary.y * v2.uv + bary.z * v3.uv;
#endif



	half4 colSamp = tex2Dlod(colorMap, float4(UV, 0, 0));
	ovd.color = lerp(colSamp.rgb, colSamp.rgb * _Color, colSamp.a);

	//ovd.color = colSamp.rgb * colSamp.a;
	//ovd.color = lerp(ovd.color, _Color, ovd.color.x + ovd.color.y + ovd.color.z == 0);

	ovd.dhfParams = tex2Dlod(dhfParamMap, float4(UV, 0, 0));
	ovd.dhfParams.z = saturate(1.0 - ovd.dhfParams.z) * 0.75;

	ovd.typeParams = tex2Dlod(typeMap, float4(UV, 0, 0)).r;

	//ovd.color = lerp3(v1.color, v2.color, v3.color, UV);
	//ovd.dhfParams = lerp3(v1.dhfParams, v2.dhfParams, v3.dhfParams, UV);
}
#else

#if !defined(SRP)
float ReadHeightmap(float4 height) {
#if (API_HAS_GUARANTEED_R16_SUPPORT)
	return height.r;
#else
	return (height.r + height.g * 256.0f) / 257.0f; // (255.0f * height.r + 255.0f * 256.0f * height.g) / 65535.0f
#endif
}
#endif

inline void GetHeightmapData(inout VertexData ovd, inout float2 rndUV, float2 chunkPos, inout rngfloat rndSeed) {
	float3 bladePos;

	float tmpo = rndm(rndSeed) - 0.5;
	float2 chunkVariance = float2(tmpo, tmpo) * rndm(rndSeed) * terrainChunkSize * terrainExpansion;

	bladePos.xz = chunkPos + terrainChunkSize * rndUV + chunkVariance;

#ifdef BAKED_HEIGHTMAP
	//older baked heightmap mode
	rndUV = bladePos.xz / terrainSize.xz + terrainMapOffset;

	float heightSamp = tex2Dlod(terrainHeightMap, float4(rndUV, 0, 0));

#else 

	//separate stuff for unity 2018.3 or newer when using the heightmap directly from the terrain object
	rndUV = bladePos.xz / terrainSize.xz * (1 - terrainMapOffset * 2) + terrainMapOffset;

	float heightSamp = ReadHeightmap(tex2Dlod(terrainHeightMap, float4(rndUV, 0, 0))) * 2;
#endif

	float3 normalSamp = tex2Dlod(terrainNormalMap, float4(rndUV, 0, 0));

	bladePos.y = heightSamp * terrainSize.y;


	//bladePos.y += (bladePos.x > terrainSize.x || bladePos.x < 0 || bladePos.z > terrainSize.z || bladePos.z < 0) * -999999.0;

	ovd.vertex = bladePos;

	half4 colSamp = tex2Dlod(colorMap, float4(rndUV, 0, 0));
	//ovd.color = colSamp.rgb * colSamp.a * _Color;
	ovd.color = lerp(colSamp.rgb, colSamp.rgb * _Color, colSamp.a);

	ovd.dhfParams = tex2Dlod(dhfParamMap, float4(rndUV, 0, 0));
	ovd.dhfParams.z = saturate(1.0 - ovd.dhfParams.z) * 0.75;

	ovd.typeParams = tex2Dlod(typeMap, float4(rndUV, 0, 0)).r;

	//not super happy about this calc but i tested it on a super heavy scene and it made
	//no appreciable difference +- 0.1 ms
	//basically just checks if the normal is empty and replaces it if so
	ovd.normal = lerp(normalSamp, float3(0, 1, 0), (normalSamp.x + normalSamp.y + normalSamp.z) == 0);
}
#endif

static float3 upVec = float3(0, 1, 0);
#if defined(RENDERMODE_MESH)
#define UP_VEC normalize(mul((float3x3)worldToGrass, upVec))
#else
#define UP_VEC upVec
#endif


#define GET_FINAL_HEIGHT(vd) bladeHeight * vd.dhfParams.y * Variate(rndm(rndSeed), variance.y)

#define GET_POS_VARIANCE(finalHeight) VariateBalanced(RndFloat3, variance.x) * finalHeight

//GEOMETRY 
inline float3 TP_Vert(VertexData vd, float3 posVariance, float camDist, inout rngfloat rndSeed, float finalHeight, float t) {

	float3 pos = vd.vertex + lerp(vd.normal, UP_VEC, seekSun) * finalHeight +
		posVariance +
		vd.dhfParams.z * finalHeight * flatnessMult * -vd.normal;

#ifdef MULTI_SEGMENT
	float3 curveDir = (windDir * 3 + cross(vd.normal, posVariance));
	pos += (
		((bladeLateralCurve * curveDir * t) +
		(bladeVerticalCurve * -vd.normal * t)) * (1 - vd.dhfParams.z) +
		(cross(vd.normal, float3(1, 0, 0)) * bladeLateralCurve * 10 * t) * vd.dhfParams.z * vd.dhfParams.y
		) * finalHeight;
#endif

	//extend length of flattened grass
	pos += (pos - vd.vertex) * vd.dhfParams.z;	


	return pos;
}

inline float3 GetWindAdd(in float3 pos, in VertexData vd, in float finalHeight, out half noiseSamp) {

	float4 noiseScale = _noiseScale.xzyw * 0.01;
	float4 noiseOffset = _Time.x * float4(_noiseSpeed.xzy, 0);

	noiseSamp = tex3Dlod(_NoiseTex, float4(pos.xzy, 0) * noiseScale + noiseOffset).r * vd.dhfParams.w;
	//noiseSamp = 1.0;

	//apply main wind dir and wind strength from the param map
	float3 windAdd = windDir * noiseSamp;

#ifndef LOWER_QUALITY
	half noiseSamp2 = tex3Dlod(_NoiseTex, float4(pos.zyx, 0) * noiseScale + noiseOffset).r - 0.5;
	//half noiseSamp2 = 0.0;

	windAdd += lerp(Float3(0.0), windDir2 * noiseSamp2, noiseSamp);
#endif

	//dampen wind effet on flattened grass
	windAdd *= 1.0 - vd.dhfParams.z * 0.9;

#ifndef LOWER_QUALITY
	//increase wind effects on taller grass
	windAdd *= pow(finalHeight, 0.5);
#endif

	return windAdd;
}

inline float3 GetRippleForce(in float3 pos) {
	uint ripCount = rippleCount[0].val.x; // get current ripple count
	RippleData rip;

	float totalStrength;
	float3 forceDir = 0.001;

	for (uint i = 0; i < ripCount; i++) {
		rip = rippleBuffer[i];
		//float3 toPos = pos - mul(worldToGrass, float4(rip.pos.xyz, 1.0));
		float3 toPos = pos - rip.pos.xyz;
		float localStrength = rip.pos.w * (1.0 - saturate(dot(toPos, toPos) / (rip.drssParams.y * rip.drssParams.y)));
		totalStrength += localStrength;

		forceDir += toPos * localStrength;
	}

	for (uint i = 0; i < forcesCount; i++) {
		rip = forcesBuffer[i];
		//float3 toPos = pos - mul(worldToGrass, float4(rip.pos.xyz, 1.0));
		float3 toPos = pos - rip.pos.xyz;
		float localStrength = rip.drssParams.w * (1.0 - saturate(dot(toPos, toPos) / (rip.drssParams.y * rip.drssParams.y)));
		totalStrength += localStrength;

		forceDir += toPos * localStrength;
	}

	return normalize(forceDir) * saturate(totalStrength);
}