#ifndef PSMESHBLINNPHONG_HLSL
#define PSMESHBLINNPHONG_HLSL

#define CLIPPLANE
#define MESH

#include"..\Common\Common.hlsl"
#include"..\Common\DataStructs.hlsl"
#include"psCommon.hlsl"
//--------------------------------------------------------------------------------------
// normal mapping
//--------------------------------------------------------------------------------------
// This function returns the normal in world coordinates.
// The input struct contains tangent (t1), bitangent (t2) and normal (n) of the
// unperturbed surface in world coordinates. The perturbed normal in tangent space
// can be read from texNormalMap.
// The RGB values in this texture need to be normalized from (0, +1) to (-1, +1).
float3 calcNormal(PSInput input)
{
    float3 normal = normalize(input.n);
    if (bHasNormalMap)
    {        
        if (bAutoTengent)
        {
            float3 localNormal = BiasX2(texNormalMap.Sample(samplerSurface, input.t).xyz);
            normal = PeturbNormal(localNormal, input.wp.xyz, normal, input.t);
        }
        else
        {
		    // Normalize the per-pixel interpolated tangent-space
            float3 tangent = normalize(input.t1);
            float3 biTangent = normalize(input.t2);

		    // Sample the texel in the bump map.
            float3 bumpMap = texNormalMap.Sample(samplerSurface, input.t);
		    // Expand the range of the normal value from (0, +1) to (-1, +1).
            bumpMap = mad(2.0f, bumpMap, -1.0f);
		    // Calculate the normal from the data in the bump map.
            normal += mad(bumpMap.x, tangent, bumpMap.y * biTangent);
            normal = normalize(normal);
        }
    }
    return normal;
}


//--------------------------------------------------------------------------------------
// Blinn-Phong Lighting Reflection Model
//--------------------------------------------------------------------------------------
// Returns the sum of the diffuse and specular terms in the Blinn-Phong reflection model.
float4 calcBlinnPhongLighting(float4 LColor, float4 vMaterialTexture, float3 N, float4 diffuse, float3 L, float3 H, float4 specular, float shininess)
{
    //float4 Id = vMaterialTexture * diffuse * saturate(dot(N, L));
    //float4 Is = vMaterialSpecular * pow(saturate(dot(N, H)), sMaterialShininess);
    float4 f = lit(dot(N, L), dot(N, H), shininess);
    float4 Id = f.y * vMaterialTexture * diffuse;
    float4 Is = min(f.z, vMaterialTexture.w) * specular;
    return (Id + Is) * LColor;
}


//--------------------------------------------------------------------------------------
// reflectance mapping
//--------------------------------------------------------------------------------------
float3 cubeMapReflection(PSInput input, const in float3 I, const in float3 reflectColor)
{
    float3 v = normalize((float3) input.wp - vEyePos);
    float3 r = reflect(v, input.n);
    return (1.0f - reflectColor) * I + reflectColor * texCubeMap.Sample(samplerCube, r);
}

//--------------------------------------------------------------------------------------
// PER PIXEL LIGHTING - BLINN-PHONG
//--------------------------------------------------------------------------------------
float4 main(PSInput input) : SV_Target
{    
	// renormalize interpolated vectors
    input.n = calcNormal(input);

    // get per pixel vector to eye-position
    float3 eye = input.vEye.xyz;

    // light emissive intensity and add ambient light
    float4 I = input.c2;
    if (SSAOEnabled)
    {
        float2 quadTex = input.p.xy * vViewport.zw;
        I.rgb *= texSSAOMap.SampleLevel(samplerSurface, quadTex, 0).r;
    }

    if (bHasEmissiveMap)
    {
        I.rgb += texEmissiveMap.Sample(samplerSurface, input.t);
    }
    // get shadow color
    float s = 1;
    if (bHasShadowMap)
    {
        if(bRenderShadowMap)
            s = shadowStrength(input.sp);
    }
    // add diffuse sampling
    float4 vMaterialTexture = 1.0f;
    if (bHasDiffuseMap)
    {
	    // SamplerState is defined in Common.fx.
        vMaterialTexture *= texDiffuseMap.Sample(samplerSurface, input.t);
    }

    float alpha = 1;
    if (bHasAlphaMap)
    {
        float4 color = texAlphaMap.Sample(samplerSurface, input.t);
        alpha = color[3];
        color[3] = 1;
        vMaterialTexture *= color;
    }
    float4 DI = float4(0, 0, 0, 0);
    float4 specular = vMaterialSpecular;
    float shininess = sMaterialShininess;
    float4 reflectColor = vMaterialReflect;
    if (bBatched)
    {
        specular = FloatToRGB(input.c.z);
        shininess = input.c.x;
        reflectColor = FloatToRGB(input.c.w);
    }
    if (bHasSpecularMap)
    {
        specular *= texSpecularMap.Sample(samplerSurface, input.t);
    }
    // compute lighting
    for (int i = 0; i < NumLights; ++i)
    {
        if (Lights[i].iLightType == 1) // directional
        {
            float3 d = normalize((float3) Lights[i].vLightDir); // light dir	
            float3 h = normalize(eye + d);
            DI += calcBlinnPhongLighting(Lights[i].vLightColor, vMaterialTexture, input.n, input.cDiffuse, d, h, specular, shininess);
        }
        else if (Lights[i].iLightType == 2)  // point
        {
            float3 d = (float3) (Lights[i].vLightPos - input.wp); // light dir
            float dl = length(d); // light distance
            if (Lights[i].vLightAtt.w < dl)
            {
                continue;
            }
            d = d / dl; // normalized light dir						
            float3 h = normalize(eye + d); // half direction for specular
            float att = 1.0f / (Lights[i].vLightAtt.x + Lights[i].vLightAtt.y * dl + Lights[i].vLightAtt.z * dl * dl);
            DI = mad(att, calcBlinnPhongLighting(Lights[i].vLightColor, vMaterialTexture, input.n, input.cDiffuse, d, h, specular, shininess), DI);
        }
        else if (Lights[i].iLightType == 3)  // spot
        {
            float3 d = (float3) (Lights[i].vLightPos - input.wp); // light dir
            float dl = length(d); // light distance
            if (Lights[i].vLightAtt.w < dl)
            {
                continue;
            }
            d = d / dl; // normalized light dir					
            float3 h = normalize(eye + d); // half direction for specular
            float3 sd = normalize((float3) Lights[i].vLightDir); // missuse the vLightDir variable for spot-dir

													/* --- this is the OpenGL 1.2 version (not so nice) --- */
													//float spot = (dot(-d, sd));
													//if(spot > cos(vLightSpot[i].x))
													//	spot = pow( spot, vLightSpot[i].y );
													//else
													//	spot = 0.0f;	
													/* --- */

													/* --- this is the  DirectX9 version (better) --- */
            float rho = dot(-d, sd);
            float spot = pow(saturate((rho - Lights[i].vLightSpot.x) / (Lights[i].vLightSpot.y - Lights[i].vLightSpot.x)), Lights[i].vLightSpot.z);
            float att = spot / (Lights[i].vLightAtt.x + Lights[i].vLightAtt.y * dl + Lights[i].vLightAtt.z * dl * dl);
            DI = mad(att, calcBlinnPhongLighting(Lights[i].vLightColor, vMaterialTexture, input.n, input.cDiffuse, d, h, specular, shininess), DI);
        }
    }
    DI.rgb *= s;
    I += DI;
    I.a = input.cDiffuse.a * vMaterialTexture.a;
    if (bHasAlphaMap)
    {
        I.a *= alpha;
    }
	// multiply by vertex colors
    //I = I * input.c;
    // get reflection-color
    if (bHasCubeMap)
    {
        I.rgb = cubeMapReflection(input, I.rgb, reflectColor.rgb);
    }

    return saturate(I);
}

#endif