// VignetteEffect.fx
// ทำจอมืดรอบขอบ (vignette) แบบวงกลม สว่างตรงกลาง (โฟกัสตัวละคร) ไล่มืดไปทางขอบจอ
// ใช้เป็น post-process: วาดทับ "รูปภาพทั้งจอ" (render target ของฉากเกมที่ render เสร็จแล้ว)
// ไม่ใช่ใส่ตรงๆ ตอนวาด sprite แต่ละชิ้น เพราะ texCoord ของ sprite แต่ละอันอ้างอิงพื้นที่ของตัวเอง
// ไม่ใช่พิกัดหน้าจอจริง ถ้าใส่ตรงๆ จะเกิด vignette ซ้อนกันหลายวงตามจำนวน sprite ที่วาด

#if OPENGL
    #define SV_POSITION POSITION
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

sampler2D SpriteTextureSampler : register(s0);

// ความละเอียดหน้าจอ (pixel) ใช้แก้ปัญหาวงรีเพี้ยนตามอัตราส่วนจอที่ไม่ใช่สี่เหลี่ยมจัตุรัส
float2 Resolution;

// ระยะจากจุดกึ่งกลางจอ (ค่า normalized ประมาณ 0-1) ที่ความมืดเริ่มปรากฏ
float VignetteRadius;

// ความนุ่มของการไล่โทนจากขอบ (ยิ่งค่ามาก ยิ่งไล่เนียน ค่าน้อยจะเป็นขอบคมชัด)
float VignetteSoftness;

// ความเข้มของความมืดที่ขอบจอ (0 = ไม่มืดเลย, 1 = มืดสนิทที่ขอบ)
float VignetteIntensity;

float4 MainPS(float2 texCoord : TEXCOORD0, float4 color : COLOR0) : COLOR0
{
    float4 texColor = tex2D(SpriteTextureSampler, texCoord) * color;

    // ย้าย origin ไปกึ่งกลางจอ (0,0) แล้วปรับ aspect ratio ตามความกว้าง/สูงจริง
    // กันไม่ให้วงกลม vignette กลายเป็นวงรีบิดเบี้ยวเวลาจอไม่ใช่สี่เหลี่ยมจัตุรัส
    float2 uv = texCoord - 0.5;
    uv.x *= Resolution.x / Resolution.y;

    float dist = length(uv);

    // t = 0 ตรงกลางจอ (ไม่มืด), ไล่เป็น 1 เมื่อถึงขอบ (มืดเต็มที่ตาม VignetteIntensity)
    float t = smoothstep(VignetteRadius - VignetteSoftness, VignetteRadius, dist);
    float darkness = lerp(1.0, 1.0 - VignetteIntensity, t);

    texColor.rgb *= darkness;
    return texColor;
}

technique SpriteVignette
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
}
