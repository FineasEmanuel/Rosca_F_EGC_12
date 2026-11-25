#version 330 core

in vec3 vColor;
in vec2 vUV;
in vec3 vLocalPos;
out vec4 FragColor;

uniform float uTime;
uniform bool uShowUV;
uniform vec3 uSeed;
uniform float uSeaLevel;
uniform float uTerrainScale;
uniform float uIceLatitude;
uniform float uBeachWidth;

float hash(vec3 p)
{
    p = fract(p * 0.3183099 + vec3(0.1, 0.2, 0.3));
    p *= 17.0;
    return fract(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float noise3(vec3 p)
{
    vec3 i = floor(p);
    vec3 f = fract(p);
    f = f * f * (3.0 - 2.0 * f);
    float n000 = hash(i + vec3(0,0,0));
    float n100 = hash(i + vec3(1,0,0));
    float n010 = hash(i + vec3(0,1,0));
    float n110 = hash(i + vec3(1,1,0));
    float n001 = hash(i + vec3(0,0,1));
    float n101 = hash(i + vec3(1,0,1));
    float n011 = hash(i + vec3(0,1,1));
    float n111 = hash(i + vec3(1,1,1));
    float x00 = mix(n000, n100, f.x);
    float x10 = mix(n010, n110, f.x);
    float x01 = mix(n001, n101, f.x);
    float x11 = mix(n011, n111, f.x);
    return mix(mix(x00, x10, f.y), mix(x01, x11, f.y), f.z);
}

float fbm(vec3 p)
{
    float sum = 0.0;
    float amp = 0.5;
    for (int i = 0; i < 5; ++i)
    {
        sum += amp * noise3(p);
        p = p * 2.03 + vec3(7.1, 3.7, 5.2);
        amp *= 0.5;
    }
    return sum / 0.96875;
}

void main()
{
    vec3 n = normalize(vLocalPos);
    if (uShowUV) { FragColor = vec4(vUV, 0.5, 1.0); return; }

    vec3 q = n * uTerrainScale + uSeed;
    float continental = fbm(q * 0.72);
    float detail = fbm(q * 2.1 + vec3(4.0, 9.0, 2.0));
    float height = continental * 0.78 + detail * 0.22;
    float sea = uSeaLevel;
    float shore = smoothstep(sea - uBeachWidth, sea + uBeachWidth, height);
    float landHeight = smoothstep(sea + 0.015, sea + 0.20, height);

    vec3 oceanDeep = vec3(0.015, 0.10, 0.30);
    vec3 oceanShallow = vec3(0.04, 0.48, 0.68);
    vec3 beach = vec3(0.83, 0.72, 0.43);
    vec3 plain = vec3(0.36, 0.62, 0.22);
    vec3 forest = vec3(0.055, 0.30, 0.13);
    vec3 mountain = vec3(0.40, 0.36, 0.32);
    vec3 snow = vec3(0.91, 0.96, 1.0);

    vec3 color = mix(oceanDeep, oceanShallow, smoothstep(sea - 0.13, sea + 0.03, height));
    color = mix(color, beach, smoothstep(sea - 0.01, sea + uBeachWidth, height) * (1.0 - smoothstep(sea + uBeachWidth, sea + 0.10, height)));
    color = mix(color, plain, smoothstep(sea + 0.04, sea + 0.16, height));
    color = mix(color, forest, smoothstep(sea + 0.16, sea + 0.30, height));
    color = mix(color, mountain, smoothstep(sea + 0.31, sea + 0.43, height));
    color = mix(color, snow, smoothstep(sea + 0.42, sea + 0.54, height));

    // Polar ice caps with irregular boundaries.
    float polarNoise = fbm(n * 8.0 + uSeed * 0.4) * 0.10;
    float latitude = abs(n.y) + polarNoise;
    float ice = smoothstep(uIceLatitude - 0.035, uIceLatitude + 0.035, latitude);
    color = mix(color, snow, ice);

    // Coastal foam / brighter shallow water.
    float coast = 1.0 - smoothstep(0.0, 0.045, abs(height - sea));
    color = mix(color, vec3(0.30, 0.78, 0.86), coast * (1.0 - shore) * 0.65);

    // Subtle procedural night-side shading; vColor is the mesh's diffuse grayscale.
    float light = 0.52 + 0.48 * max(dot(n, normalize(vec3(-0.45, 0.35, 0.82 + 0.035 * sin(uTime * 0.12)))), 0.0);
    color *= light * max(vColor, vec3(0.35));
    FragColor = vec4(color, 1.0);
}
