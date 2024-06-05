#version 330 core
in vec2 TexCoord;
out vec4 FragColor;

uniform sampler2D ourTexture;

void main()
{
	FragColor = texture(ourTexture, vec2(TexCoord.x, 1.0 - TexCoord.y));
}
