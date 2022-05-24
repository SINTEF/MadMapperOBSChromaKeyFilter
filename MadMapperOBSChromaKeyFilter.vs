void fxVsFunc()
{
	// Initialize Surface 2D fragment shader inputs
	mm_FragNormCoord = (mm_TextureMatrix*vec4(mm_TexCoord0.xy,0,1)).xy;
    

	// Tells OpenGL where this vertex should be on the output view
	gl_Position = mm_ModelViewProjectionMatrix * vec4(mm_Vertex.xy,0,1);
}
