varying vec3 normal;
varying float depth;

uniform sampler2D DiffuseTexture;

void main()
{
    gl_FragData[0] = vec4(texture2D(DiffuseTexture, gl_TexCoord[0].st).xyz, 1.0);
    gl_FragData[1] = vec4(normalize(normal) * 0.5 + 0.5, 1.0);
    gl_FragData[2] = vec4(depth, 0, 0, 1);
}