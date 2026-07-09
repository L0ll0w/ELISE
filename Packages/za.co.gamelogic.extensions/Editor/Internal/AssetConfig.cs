using System;
using System.Collections.Generic;
using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>
	/// Configuration data for the Gamelogic asset tools and windows.
	/// </summary>
	/*	Design note: Public so any asset can use it.
	*/
	[Serializable] // So windows remain good even when scripts recompile
	public class AssetConfig
	{
		/// <summary>
		/// Keeps a list of features supported in the context of this asset.
		/// </summary>
		/// <remarks>
		/// Gamelogic plugins add entries to this dictionary so that it is easier to see what code branches should be
		/// active.
		/// </remarks>
		/* This could be made internal, and access granted through InternalsVisibleTo.
			I opted not to do it:
				1. This is a low-risk feature, used for debugging.  
				2. It 
				3. It simplifies adding new packages (otherwise extensions had to change for additions).
		*/
		[EditorInternal]
		public static readonly Dictionary<string, bool> ContextFeatures = new Dictionary<string, bool>();

		// <summary>The display name of the asset shown in windows and labels.</summary>
		public string assetDisplayName;
		/// <summary>The Unity package ID of this asset.</summary>
		public string packageId;
		/// <summary>The current version of the package.</summary>
		public string packageVersion;
		/// <summary>The URL to the documentation for this asset.</summary>
		public string documentationUrl;
		/// <summary>The URL to the YouTube channel for this asset.</summary>
		public string youTubeChannelUrl;

		/// <summary>The PlayerPrefs key used to track whether the welcome window has been shown.</summary>
		public string shownKey;

		/// <summary>
		/// All packages belonging to this asset. The first entry is treated as the main package
		/// (used for sample imports).
		/// </summary>
		public Package[] uninstallList;
	}
}
