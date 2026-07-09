namespace Gamelogic.Fx.Internal
{
	internal static class ToolTipStrings
	{
		public const string AutomaticallySet = "Automatically set by script.";
		public const string UpdateRenderListEachFrameInPlayMode 
			= "Whether to update each frame, or once only.\n"
			+ "Update each frame if the renderers change very frequently.\n"
			+ "Otherwise, set to false for better performance and update manually with RefreshRendererList.";

		public const string UpdateRenderListEachFrameInEditMode 
			= "Whether to update each frame in the editor, or once only.\n"
				+ "Update each frame you edit the scene or switch scenes frequently.\n"
				+ "Otherwise, set to false for better performance and update manually with RefreshRendererList.";
		
		public const string SourceCamera 
			= "The camera that renders the scene. The internal render camera will copy settings from this camera.";

		public const string UpdateCameraEachFrame
			= "Whether to upodate the camera properties each frame. " 
				+ "Set to true if your source camera properties change through animation or scripting."; 
		
		public const string RenderTarget = "The render target to render into.";
		public const string MapProperties = "The map properties that configure how the map is rendered.";
		public const string MapLayerMask = "Layer mask used to select which objects are rendered into the map.";
		public const string WhenToRender = "When in the render pipeline this pass is injected.";
		public const string CameraScope = "Which cameras should the render feature be applied to?";
		public const string PostEffectProperties = "Shader properties that configure this post-processing effect.";
	}
}
