

//include the other goodies
#include "GrassStructsVars.cginc"
#include "GrassFunc.cginc"



#if defined(RENDERMODE_MESH)
v2g vertex_shader(v2g IN, uint inst : SV_InstanceID) {
	v2g o;
	o.pos.xyz = IN.pos.xyz;
	o.pos.w = inst;
	o.norm = IN.norm;
	o.uv = IN.uv;
	//UNITY_SETUP_INSTANCE_ID(IN);
	return o;
}
#else
v2g vertex_shader(v2g IN, uint inst : SV_InstanceID) {
	v2g o;
	o.pos.xyz = IN.pos.xyz;
	o.pos.w = inst;
	//o.uv = UNITY_ACCESS_INSTANCED_PROP(_chunkPos);
	o.uv = _chunkPos;
	return o;
}
#endif


//v2g vertex_depth(v2g IN, uint inst : SV_InstanceID) {
//	v2g o;
//	o.pos.xyz = IN.pos.xyz;
//	o.pos.w = inst;
//	//return 0;
//	return o;
//}


#ifdef LOWER_QUALITY 
[maxvertexcount(3)]
#elif !defined(MULTI_SEGMENT)
[maxvertexcount(4)]
#else
[maxvertexcount(MAX_BLADE_SEGMENTS * 2 + 2)]
#endif

void geometry_shader(triangle v2g IN[3], inout TriangleStream<g2f> outStream, uint primId : SV_PrimitiveID) {

	g2f o;

#define vd1 IN[0]
#define vd2 IN[1]
#define vd3 IN[2]

	//#define GetRndUV

	float instID = vd1.pos.w;
	rngfloat rndSeed = rngfloat(primId, instID) + 2.4378952;
	float2 rndUV = float2(rndm(rndSeed), rndm(rndSeed));
	

#ifndef RENDERMODE_MESH
	//TERRAIN MODE

	VertexData lvd; //terrain sample data
	GetHeightmapData(lvd, rndUV, vd1.uv, rndSeed);

	UNITY_BRANCH
	static const float edgeBound = 0.15;
	if (lvd.vertex.x < -edgeBound || lvd.vertex.z < -edgeBound || 
		lvd.vertex.x > terrainSize.x + edgeBound || lvd.vertex.z > terrainSize.z + edgeBound) {
		//cull blades that are off the edge of the terrain
		return;
	}

#else
	//MESH MODE

	VertexData lvd; //lerped vertex data
	LerpVertData(lvd, rndUV, vd1, vd2, vd3, rndSeed);
#endif

	UNITY_BRANCH
	if (rndm(rndSeed) > lvd.dhfParams.x) {
		return;
	}


	float3 worldNormal = GrassToWorldNormal(lvd.normal);
	float3 worldPos = mul(grassToWorld, float4(lvd.vertex.xyz, 1));


	float3 toTri = worldPos - _WorldSpaceCameraPos;

#ifdef DEFERRED
	//variate length to avoid artifacting in dithering
	toTri *= rndm(rndSeed) * 0.75 + 1.0;
#endif

	//calculate fade alpha
	float camDist = rsqrt(dot(toTri, toTri));
	half alphaBlendo = saturate(pow(camDist * grassFade, grassFadeSharpness));

	if (instID > (_instanceLod) - 1) {
		float fracFade = frac((_instanceLod));
		alphaBlendo *= (fracFade != 0 ? fracFade : 1);
	}


	UNITY_BRANCH
	if (alphaBlendo < 0.01) return;


	o.uv = 1;
#if defined(SHADOW_CASTER)
	o.uv.w = alphaBlendo;
#endif



#if !defined(BILLBOARD)
	//float3 camRight = float3(1, rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f);
	float3 camRight = normalize(float3(rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f));
#else

	//float3 camRight = mul((float3x3)unity_CameraToWorld, float3(1, rndm(rndSeed) * 0.5f - 0.25f, rndm(rndSeed) * 0.5f - 0.25f));

	//gets the camera-right vector
	float3 camRight = unity_WorldToCamera[0].xyz
		+ unity_WorldToCamera[1].xyz * (rndm(rndSeed) * 0.5f - 0.25f)
		+ unity_WorldToCamera[2].xyz * (rndm(rndSeed) * 0.5f - 0.25f)
		;

	//camRight = mul((float3x3)worldToGrass, camRight);
#endif



	camDist = 1.0 + widthLODscale / camDist;

	half noiseSamp;


	half3 widthMod = camRight * (1.0 + lvd.dhfParams.z * 0.5) * bladeWidth * Variate(rndm(rndSeed), variance.w) * camDist;

	float finalHeight = GET_FINAL_HEIGHT(lvd);
	float3 posVariance = GET_POS_VARIANCE(finalHeight);
	float3 tV = TP_Vert(lvd, posVariance, camDist, rndSeed, finalHeight, 1);
	float3 lV = worldPos - widthMod;
	float3 rV = worldPos + widthMod;

	float3 windAdd = GetWindAdd(tV, lvd, finalHeight, noiseSamp);
	tV = mul(grassToWorld, float4(tV, 1));
	float3 rippleForce = GetRippleForce(tV);

	//ApplyRipples(tV);
	tV += rippleForce;
	tV += windAdd;


	//push top vert away from camera when look down
	float3 topView = -unity_WorldToCamera[1].xyz * saturate(dot(unity_WorldToCamera[2].xyz, -worldNormal) - 0.5) * (-3 * topViewPush);
	tV += topView;


	

#if !defined(SHADOWS_DEPTH) || defined(SEMI_TRANSPARENT)
	#define _MaxTexAtlasSize 16.0
	float typeSamp = lvd.typeParams * _MaxTexAtlasSize;

	#define typeIdx floor(typeSamp)
	#define typePct frac(typeSamp)

	//since our type pct can only technically store 16 values and can never actually be "1"
	//our 16 values are actually 0-15
	//so we need this value to scale the rnd number into the space of our fractional type pct
	//to be clear, typePct will only ever be 0 to 0.9375 and occur in steps of (1 / 16 = 0.0625)
	const static float rndScale = 1.0 - (1.0 / 16.0); // = 0.9375

	float uvXL = (rndm(rndSeed) * rndScale) < typePct ? typeIdx * numTexturesPctUV : 0;
	//float uvXL = typeIdx * numTexturesPctUV;

	float uvXR = uvXL + numTexturesPctUV;
#else
	static float uvXL = 0;
	static float uvXR = 1;
#endif

#if !defined(SHADOW_CASTER) && !defined(FORWARD_ADD) && !defined(DEFERRED)

	half diffuseCO = 1.0 - ambientCO;
	half3 lightDirection = GET_LIGHT_DIR(o);
	//half lightAmnt = saturate(1.0 - lightDirection.y);
	half shade = ambientCO + diffuseCO * saturate(dot(lightDirection, worldNormal) + 0.25);
	shade *= 1 + saturate(dot(lightDirection, cross(cross(lightDirection, float3(0, -edgeLightSharp, 0)), worldNormal)) - (edgeLightSharp - edgeLight));
	//shade = saturate(dot(camFwd, lightDirection));
	//shade *= saturate(0.5 + lightDirection.y);

	//noiseSamp = 0.8f + noiseSamp * 0.25f;
	noiseSamp = noiseSamp * 1.5 - 0.5;
	float3 windTintAdd = float3(1, 1, 1) + windTint.rgb * windTint.a * noiseSamp;


	//TOP Vert - no AO on this one
	//ShadeVert(o, tV, lvd, shade, noiseSamp, alphaBlendo, rndSeed, float2(0.5, 1.0));
	float4 bladeCol = float4(lvd.color * pow(Variate(rndm(rndSeed), variance.z), 0.4), alphaBlendo);
	bladeCol.rgb *= windTintAdd;

	float3 lighting = GET_LIGHT_COL(o) * shade;
	lighting += max(0, GET_GI(float4(worldNormal, 1)) * ambientCO);


	//
	//SRP LIGHTING STUFF
	//
#if defined(SRP) && defined(_ADDITIONAL_LIGHTS) && !defined(SRP_FRAGMENT_ADDITIONAL_LIGHTS)
	int additionalLightsCount = GetAdditionalLightsCount();
	for (int lI = 0; lI < additionalLightsCount; ++lI) {
		Light light = GetAdditionalLight(lI, tV);
		shade = ambientCO + diffuseCO * saturate(dot(light.direction, worldNormal) + 0.25);
		lighting += (light.color * shade * light.shadowAttenuation * light.distanceAttenuation);
	}
#endif
#if defined(SRP_FRAGMENT_ADDITIONAL_LIGHTS)
	o.ogCol = bladeCol;
#endif

	bladeCol.rgb *= lighting;

	CHECK_PAINT_HIGHLIGHT;

	o.col = bladeCol;

#endif

#if defined(FORWARD_ADD)
	float4 bladeCol = float4(lvd.color, alphaBlendo);
	o.col = bladeCol;
#endif

#if defined(DEFERRED)

	noiseSamp = noiseSamp * 1.5 - 0.5;
	float3 windTintAdd = float3(1, 1, 1) + windTint.rgb * windTint.a * noiseSamp;
	float4 bladeCol = float4(lvd.color * pow(Variate(rndm(rndSeed), variance.z), 0.4) * windTintAdd, alphaBlendo);
	o.col = bladeCol;

	//this whole thing seems weird and im not really sure why i did it like this anymore oops
	float3 worldGroundNormal = worldNormal * 0.5 + 0.5;
	//o.normal.xyz = worldGroundNormal;

	//weird alternative normals situation give more variation and rough grass normals
	o.normal.xyz = lerp(worldGroundNormal, normalize(UnityObjectToWorldNormal(tV - lvd.vertex)) * 0.5 + 0.5, 0.3);

	o.normal.w = 1.0 * specularMult;
#endif

#ifdef SRP_FRAGMENT_ADDITIONAL_LIGHTS
	o.normal = float4(worldNormal, 1);
#endif





#if !defined(SHADOW_CASTER)
	//Reduce AO a bit on flattened grass and variate AO a smidge
	float3 startCol = bladeCol;
	float aoValue = lerp(
		lvd.dhfParams.z + rndm(rndSeed) * 0.2 + _AO,
		1.0,
		noiseSamp * lvd.dhfParams.x * 0.35 + (1.0 - lvd.dhfParams.x) * 0.5
	);
	bladeCol.rgb *= aoValue; // apply AO
	//bladeCol *= lvd.dhfParams.x * 0.5 + 0.5; // reduce AO based on grass density;
	bladeCol.a = alphaBlendo;

	CHECK_PAINT_HIGHLIGHT;
#endif


	//Top left Vert
	worldPos = float4(tV - widthMod * bladeSharp, 1.0);
	//o.normal = float4(normalize(lerp(worldPos - tV, worldNormal, 0.1)), 1);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXL, 1.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o, worldPos);
	outStream.Append(o);


	//Top right Vert
	worldPos = float4(tV + widthMod * bladeSharp, 1.0);
	//o.normal = float4(normalize(lerp(worldPos - tV, worldNormal, 0.1)), 1);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXR, 1.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o, worldPos);
	outStream.Append(o);

	

#ifdef MULTI_SEGMENT

	int bladeSegments = (saturate(_instancePct)) * BLADE_SEG_DIFF + MIN_BLADE_SEGMENTS;

	for (float i = (bladeSegments - 1); i >= 1 ; i--) {

		float t = i / bladeSegments;
		float t2 = t * t;

		tV = TP_Vert(lvd, posVariance * t, camDist, rndSeed, finalHeight * t, t2);
		tV = mul(grassToWorld, float4(tV, 1));
		tV += (windAdd + rippleForce) * lerp(t2, t, bladeStiffness);
		tV += topView * t;

#if !defined(SHADOW_CASTER)
		o.col.rgb = lerp(bladeCol.rgb, startCol, t);
#endif

		float sharpLerp = lerp(1, bladeSharp, t2);

		//Top left Vert
		worldPos = float4(tV - widthMod * sharpLerp, 1.0);
		SET_WORLDPOS(o, worldPos);
		o.pos = GetClipPos(worldPos);
		SET_UV(float3(uvXL, t, o.pos.z));
		TRANSFER_GRASS_SHADOW(o, worldPos);
		outStream.Append(o);


		//Top right Vert
		worldPos = float4(tV + widthMod * sharpLerp, 1.0);
		SET_WORLDPOS(o, worldPos);
		o.pos = GetClipPos(worldPos);
		SET_UV(float3(uvXR, t, o.pos.z));
		TRANSFER_GRASS_SHADOW(o, worldPos);
		outStream.Append(o);
	}
#endif
	


#if !defined(SHADOW_CASTER)
	o.col = bladeCol;
#endif


#if defined(DEFERRED)
	o.normal.w = aoValue;
#endif


	//BL Vert
	//v.vertex = float4(lV, 1.0);
	worldPos = lV;
	//worldPos = mul(grassToWorld, v.vertex);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXL, 0.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o, worldPos);
	outStream.Append(o);

	//BR Vert
	//v.vertex = float4(rV, 1.0);
	worldPos = rV;
	//worldPos = mul(grassToWorld, v.vertex);
	SET_WORLDPOS(o, worldPos);
	o.pos = GetClipPos(worldPos);
	SET_UV(float3(uvXR, 0.0, o.pos.z));
	TRANSFER_GRASS_SHADOW(o, worldPos);
	outStream.Append(o);



	//outStream.RestartStrip();
}


#if !defined(SHADOW_CASTER)

#if !defined(DEFERRED)
half4 fragment_shader(g2f i) : SV_Target{
#else
FragmentOutput fragment_shader(g2f i) {
#endif

	//return 0;
	#if defined(DEFERRED)
	//half alphaRef = tex3D(_DitherMaskLOD, float3((i.worldPos.xy + i.worldPos.z)*2, i.col.a*0.9375 + 0.0001)).a;
	half alphaRef = tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, i.col.a * 0.9375 + 0.0001)).a;
	clip(alphaRef - 0.01);
	i.col.a = 1;
	#endif

	half4 col = tex2D(_MainTex, i.uv);

#if defined(SEMI_TRANSPARENT)
	clip(col.a - alphaClip);
#endif


#ifdef FORWARD_ADD
	UNITY_LIGHT_ATTENUATION(atten, i, i.worldPos);
	col.rgb *= _LightColor0.rgb * atten;

#elif defined(SHADOWS_SCREEN)
	col.rgb *= ambientCOShadow + (1.0 - ambientCOShadow) * GRASS_SHADOW_ATTENUATION(i);
#endif


#if defined(SRP_FRAGMENT_ADDITIONAL_LIGHTS)
	half3 texCol = col;
	half diffuseCO = 1.0 - ambientCO;
	int additionalLightsCount = GetAdditionalLightsCount();
	float3 lighting = 0;
	for (int lI = 0; lI < additionalLightsCount; ++lI) {
		Light light = GetAdditionalLight(lI, i.worldPos);
		half shade = ambientCO + diffuseCO * saturate(dot(light.direction, i.normal) + 0.25);
		//col.rgb += light.color * shade * light.shadowAttenuation * light.distanceAttenuation;
		lighting += (light.color * shade * light.shadowAttenuation * light.distanceAttenuation);
	}

	col = saturate(col * i.col);
	col.rgb += lighting * i.ogCol * texCol;
#else
	col = saturate(col * i.col);
#endif



	UNITY_APPLY_FOG(i.uv.z, col);


#if defined(DEFERRED)

	FragmentOutput deferredData;

	half3 specular;
	half specularMonochrome;
	half3 diffuseColor = DiffuseAndSpecularFromMetallic(col.rgb, _Metallic, specular, specularMonochrome);

	float occSamp = lerp(1, tex2D(_OccMap, i.uv).r, occMult);
	float specSamp = tex2D(_SpecMap, i.uv).r * occSamp;

	deferredData.albedo.rgb = diffuseColor; //albedo	
	deferredData.albedo.a = 1 - occSamp; //occulusion


	deferredData.specular.rgb = specular * i.normal.w * specSamp; //specular tint
	deferredData.specular.a = _Gloss * i.normal.w * specSamp; //shinyness


	deferredData.normal = float4(i.normal.xyz, 1);

	//indirect lighting
	float3 sh9 = max(0, ShadeSH9(float4(i.normal.xyz, 1)) * ambientCO);

	deferredData.light.rgb = diffuseColor * sh9 * occSamp;

	#if !defined(UNITY_HDR_ON)
	deferredData.light.rgb = exp2(-deferredData.light.rgb);
	#endif

	deferredData.light.a = 0;

	return deferredData;

#else
	return col;
#endif

}
#endif

#if defined(SHADOW_CASTER)
void fragment_depth(g2f i) {
	half alpha = 1;

#if defined(SEMI_TRANSPARENT)
	alpha = tex2D(_MainTex, TRANSFORM_TEX(i.uv, _MainTex)).a;
#endif

	alpha *= tex3D(_DitherMaskLOD, float3(i.pos.xy * 0.25, i.uv.w * 0.9375 + 0.0001)).a;
	clip(alpha - alphaClip);

}
#endif