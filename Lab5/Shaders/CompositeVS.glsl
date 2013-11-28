uniform mat4 ViewProjectionMatrix;
uniform mat4 InverseViewProjectionMatrix;
uniform mat4 WorldMatrix;
uniform vec3 CameraPosition;
uniform float FarPlane;

attribute vec4 position;

varying vec2 texCoord;
varying vec3 viewRay;

void main()
{
	gl_Position = position;
	vec4 pos = position;
	pos.xyz /= pos.w;
	texCoord = pos.xy * 0.5 + 0.5;
}