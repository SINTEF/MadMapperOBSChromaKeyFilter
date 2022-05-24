/*

	This file is licensed under the GPL version 2 license.

	This is a port from OBS (Open Broadcaster Software).
	https://obsproject.com https://github.com/obsproject/obs-studio
*/

/*{
    "CREDIT": "OBS (Open Broadcaster Software), ported by SINTEF",
    "TAGS": ["graphics"],
    "VSN": 1.0,
    "DESCRIPTION": "Port of the OBS chroma key filter to MadMapper",
    "MEDIA": {
        "REQUIRES_TEXTURE": false,
        "GL_TEXTURE_MIN_FILTER": "LINEAR",
        "GL_TEXTURE_MAG_FILTER": "LINEAR",
        "GL_TEXTURE_WRAP": "CLAMP_TO_EDGE",
    },
    "INPUTS": [
        { "LABEL": "Contrast", "NAME": "contrast", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0 },
        { "LABEL": "Brightness", "NAME": "brightness", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0 },
        { "LABEL": "Gamma", "NAME": "gamma", "TYPE": "float", "DEFAULT": 0.0, "MIN": -1.0, "MAX": 1.0 },
        { "LABEL": "Similarity", "NAME": "similarity", "TYPE": "float", "DEFAULT": 0.4, "MIN": 0.001, "MAX": 1.0 },
        { "LABEL": "Smoothnes", "NAME": "smoothness", "TYPE": "float", "DEFAULT": 0.08, "MIN": 0.001, "MAX": 1.0 },
        { "LABEL": "Spill", "NAME": "spill", "TYPE": "float", "DEFAULT": 0.1, "MIN": 0.001, "MAX": 1.0 },
    ],
    "GENERATORS": []
}*/

vec4 cb_v4 = vec4(-0.100644, -0.338572,  0.439216, 0.501961);
vec4 cr_v4 = vec4(0.439216, -0.398942, -0.040274, 0.501961);

vec2 img_size = FX_IMG_SIZE();
vec2 pixel_size = vec2(1.0/img_size.x, 1.0/img_size.y);

vec4 chroma_green = vec4(0.0, 1.0, 0.0, 1.0);
float chroma_key_x = dot(chroma_green, cb_v4);
float chroma_key_y = dot(chroma_green, cr_v4);
vec2 chroma_key = vec2(chroma_key_x, chroma_key_y);

float saturate(float v) {
		return clamp(v, 0.0, 1.0);
}

vec2 RGBToCC(vec4 rgba) {
    float Y = 0.299 * rgba.r + 0.587 * rgba.g + 0.114 * rgba.b;
    return vec2((rgba.b - Y) * 0.565, (rgba.r - Y) * 0.713);
}

vec4 CalcColor(vec4 rgba)
{
	float contrastA = (contrast < 0.0) ? (1.0 / (-contrast + 1.0))
				    : (contrast + 1.0);

	float gammaA = (gamma < 0.0) ? (-gamma + 1.0) : (1.0 / (gamma + 1.0));

	return vec4(pow(rgba.rgb, vec3(gammaA, gammaA, gammaA)) * contrastA + brightness, rgba.a);
}

float GetChromaDist(vec3 rgb)
{
	float cb = dot(rgb.rgb, cb_v4.xyz) + cb_v4.w;
	float cr = dot(rgb.rgb, cr_v4.xyz) + cr_v4.w;
	return distance(chroma_key, vec2(cr, cb));
}

float GetNonlinearChannel(float u)
{
	return (u <= 0.0031308) ? (12.92 * u) : ((1.055 * pow(u, 1.0 / 2.4)) - 0.055);
}

vec3 GetNonlinearColor(vec3 rgb)
{
	return vec3(GetNonlinearChannel(rgb.r), GetNonlinearChannel(rgb.g), GetNonlinearChannel(rgb.b));
}

vec3 SampleTexture(vec2 uv)
{
	vec3 rgb = FX_NORM_PIXEL(uv).rgb;
	return GetNonlinearColor(rgb);
}

float GetBoxFilteredChromaDist(vec3 rgb, vec2 texCoord)
{
	vec2 h_pixel_size = pixel_size / 2.0;
	vec2 point_0 = vec2(pixel_size.x, h_pixel_size.y);
	vec2 point_1 = vec2(h_pixel_size.x, -pixel_size.y);
	float distVal = GetChromaDist(SampleTexture(texCoord-point_0));
	distVal += GetChromaDist(SampleTexture(texCoord+point_0));
	distVal += GetChromaDist(SampleTexture(texCoord-point_1));
	distVal += GetChromaDist(SampleTexture(texCoord+point_1));
	distVal *= 2.0;
	distVal += GetChromaDist(GetNonlinearColor(rgb));
	return distVal / 9.0;
}

vec4 ProcessChromaKey(vec4 rgba, vec2 uv)
{
	float chromaDist = GetBoxFilteredChromaDist(rgba.rgb, uv);
	float baseMask = chromaDist - similarity;
	float fullMask = pow(saturate(baseMask / smoothness), 1.5);
	float spillVal = pow(saturate(baseMask / spill), 1.5);

	rgba.a *= fullMask;

	float desat = dot(rgba.rgb, vec3(0.2126, 0.7152, 0.0722));
	rgba.rgb = mix(vec3(desat, desat, desat), rgba.rgb, spillVal);

	return CalcColor(rgba);
}

vec4 PSChromaKeyRGBA(vec2 uv)
{
	vec4 rgba = FX_NORM_PIXEL(uv);
	rgba.rgb = max(vec3(0.0, 0.0, 0.0), rgba.rgb / rgba.a);
	return ProcessChromaKey(rgba, uv);
}

vec4 fxColorForPixel(vec2 mm_FragNormCoord)
{
	vec2 uv;
	uv.x = mm_FragNormCoord.x;
	uv.y = mm_FragNormCoord.y;
	return PSChromaKeyRGBA(uv);
}
