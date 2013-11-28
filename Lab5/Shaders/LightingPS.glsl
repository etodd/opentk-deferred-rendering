varying vec2 clipSpacePosition;
varying vec3 viewRay;

uniform vec3 Color;
uniform float Radius;
uniform vec3 Position;
uniform vec3 CameraPosition;
uniform float FarPlane;

uniform sampler2D DepthBuffer;
uniform sampler2D NormalBuffer;

void main()
{
	float depth = texture2D(DepthBuffer, clipSpacePosition).x * FarPlane;
	vec3 pixelPosition = CameraPosition + (normalize(viewRay) * depth);
	vec3 toLight = Position - pixelPosition;
	float lightDistance = length(toLight);
	toLight /= lightDistance;
	vec3 normal = (texture2D(NormalBuffer, clipSpacePosition).xyz - 0.5) * 2.0;
    gl_FragColor = vec4(Color * clamp(dot(toLight, normal), 0.0, 1.0) * clamp(1.0 - (lightDistance / Radius), 0.0, 1.0) * 0.5, 1.0);
}