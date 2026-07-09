namespace Gamelogic.Fx.URP
{
	/// <summary>
	/// Experimental settings for Gamelogic Fx URP effects.
	/// </summary>
	public static class ExperimentalSettings
	{
		/// <summary>
		/// Post-processing settings.
		/// </summary>
		public static class PostProcessing
		{
			/// <summary>
			/// Whether to skip post effects in overlay cameras.
			/// </summary>
			public static bool SkipInOverlayCameras = true;
		}

		/// <summary>
		/// Map rendering settings.
		/// </summary>
		public static class Mapping
		{
			/// <summary>
			/// Whether to skip mapping passes in overlay cameras.
			/// </summary>
			public static bool SkipInOverlayCameras = true;
		}
	}
}
