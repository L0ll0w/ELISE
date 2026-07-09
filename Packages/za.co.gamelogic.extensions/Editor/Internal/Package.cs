using System;

namespace Gamelogic.Extensions.Editor.Internal
{
	/// <summary>Represents a Unity package with a display name and package ID.</summary>
	[Serializable] // So windows remain good even when scripts recompile
	public class Package
	{
		/// <summary>The human-readable name of the package.</summary>
		public string displayName;
		/// <summary>The Unity package ID (e.g. <c>com.example.package</c>).</summary>
		public string id;
	}
}