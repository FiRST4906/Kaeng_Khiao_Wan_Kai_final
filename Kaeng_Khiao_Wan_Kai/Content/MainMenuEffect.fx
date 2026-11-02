#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler2D SpriteTextureSampler : register(s0);

float Time;
float DarkenAmount;
float WaveAmplitude;
float WaveSpeed;
float2 FlickerCenter;
float FlickerRadius;
float FlickerSpeed;
float FlickerIntensity;

float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
	float2 uv = texCoord;

	// คลื่นทะเลเบาๆ: เลื่อนแนวนอนตามตำแหน่งแนวตั้ง + เวลา (ขยับช้าๆ ไม่กระตุก)
	uv.x += sin(uv.y * 6.0 + Time * WaveSpeed) * WaveAmplitude;

	float4 texColor = tex2D(SpriteTextureSampler, uv);

	// ทำภาพให้มืดลงเล็กน้อย
	texColor.rgb *= (1.0 - DarkenAmount);

	// ไฟกระพริบช้าๆ เฉพาะจุด (บริเวณกระจกหมวกนักดำน้ำ) ผสมคลื่นไซน์ 2 ความถี่ให้ดูเป็นธรรมชาติ ไม่นิ่งเป๊ะ
	float dist = distance(texCoord, FlickerCenter);
	float falloff = saturate(1.0 - dist / FlickerRadius);
	falloff *= falloff;

	float flicker = (sin(Time * FlickerSpeed) * 0.5 + 0.5) * 0.65
	              + (sin(Time * FlickerSpeed * 2.7 + 1.3) * 0.5 + 0.5) * 0.35;

	texColor.rgb += falloff * flicker * FlickerIntensity;

	return texColor * color;
}

technique MainMenuBackground
{
	pass P0
	{
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
}
