uniform mat4 ViewProjectionMatrix;
uniform mat4 WorldMatrix;
uniform vec3 CameraPosition;
uniform float FarPlane;

attribute vec4 position;

varying vec3 normal;
varying float depth;

void main()
{
	vec4 worldPos = WorldMatrix * position;
	gl_Position = ViewProjectionMatrix * worldPos;
	normal = (WorldMatrix * vec4(gl_Normal, 0.0)).xyz;
	depth = length(worldPos.xyz - CameraPosition) / FarPlane;
	gl_TexCoord[0] = gl_MultiTexCoord0;
}