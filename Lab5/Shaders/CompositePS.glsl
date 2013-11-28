varying vec2 texCoord;

uniform sampler2D ColorBuffer;
uniform sampler2D LightingBuffer;

void main()
{
    gl_FragColor = vec4(texture2D(LightingBuffer, texCoord).xyz * 2.0 * texture2D(ColorBuffer, texCoord).xyz, 1.0);
}