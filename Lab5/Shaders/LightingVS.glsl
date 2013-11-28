uniform mat4 ViewProjectionMatrix;
uniform mat4 InverseViewProjectionMatrix;
uniform mat4 WorldMatrix;
uniform vec3 CameraPosition;
uniform float FarPlane;

attribute vec4 position;

varying vec2 clipSpacePosition;
varying vec3 viewRay;

void main()
{
	vec4 pos = ViewProjectionMatrix * WorldMatrix * position;
	gl_Position = pos;
	pos.xyz /= pos.w;
	clipSpacePosition.x = pos.x * 0.5 + 0.5;
    clipSpacePosition.y = pos.y * 0.5 + 0.5;
	viewRay = (WorldMatrix * position).xyz - CameraPosition;
}