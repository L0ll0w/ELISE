namespace Gamelogic.Fx
{
	/// <summary>
	/// Ways to select the source for outline edge detection.
	/// </summary>
	public enum OutlineSource
	{
		/// <summary>
		/// Use the camera color texture as the source.
		/// </summary>
		CameraColor = 0,
		
		/// <summary>
		/// Use an alternate texture as the source.
		/// </summary>
		AlternateTexture = 1,
		
		/// <summary>
		/// Use the normals texture as the source.
		/// </summary>
		NormalsTexture = 2,
		
		/// <summary>
		/// Use the depth texture as the source.
		/// </summary>
		DepthTexture = 3
	}
}
