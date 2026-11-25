#version 330 core

in vec3 vColor;
in vec2 vUV;
in vec3 vLocalPos;
out vec4 FragColor;

uniform float uTime;
uniform bool uShowUV;
uniform vec3 uCloudSeed;
uniform float uCloudScale;
uniform float uCloudOpacity;
uniform float uCloudFade;

float hash(vec3 p)
{
    p = fract(p * 0.3183099 + vec3(0.13, 0.27, 0.41));
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float a = mix(hash(i), hash(i + vec3(1,0,0)), f.x);
    float b = mix(hash(i + vec3(0,1,0)), hash(i + vec3(1,1,0)), f.x);
    float c = mix(hash(i + vec3(0,0,1)), hash(i + vec3(1,0,1)), f.x);
    float d = mix(hash(i + vec3(0,1,1)), hash(i + vec3(1,1,1)), f.x);
    return mix(mix(a,b,f.y), mix(c,d,f.y), f.z);
}

float fbm(vec3 p)
{
    float s = 0.0;
    float a = 0.5;
    for (int i = 0; i < 4; ++i)
    {
        s += a * noise3(p);
        p = p * 2.02 + vec3(3.1, 7.2, 5.4);
        a *= 0.5;
    }
    return s / 0.9375;
}

void main()
{
    if (uShowUV) { FragColor = vec4(vUV, 0.5, 1.0); return; }
    vec3 n = normalize(vLocalPos);
    vec3 drift = vec3(uTime * 0.025, 0.0, uTime * 0.012);
    float cloudNoise = fbm(n * uCloudScale + uCloudSeed + drift);
    float coverage = smoothstep(0.53, 0.68, cloudNoise);
    float wisps = smoothstep(0.48, 0.72, fbm(n * (uCloudScale * 1.8) + uCloudSeed * 1.7 - drift));
    float alpha = clamp((coverage * 0.78 + wisps * 0.22) * uCloudOpacity * uCloudFade, 0.0, 0.92);
    float light = 0.72 + 0.28 * max(dot(n, normalize(vec3(-0.45, 0.35, 0.82))), 0.0);
    vec3 cloudColor = vec3(0.83, 0.91, 1.0) * light * max(vColor, vec3(0.5));
    FragColor = vec4(cloudColor, alpha);
}
