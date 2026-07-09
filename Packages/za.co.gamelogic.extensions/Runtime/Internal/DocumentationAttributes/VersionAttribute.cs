// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System;

namespace Gamelogic.Extensions.Internal
{
	/// <summary>
	/// An attribute to mark API in what API version a class or member was introduced. 
	/// </summary>
	// The idea is to mnake it easier from a single document set to tell whether a feature is available.
	// I (HT) is always in two minds whether to remove it. 
	// This does not indicate when a breaking change (such as signature change) was introduced. 
	[Version(1, 4, 0)]
	[AttributeUsage(AttributeTargets.All, Inherited = false)]
	public class VersionAttribute : Attribute
	{
		#region Properties

		/// <summary>
		/// The main version number of this element.
		/// </summary>
		public int MainVersion { get; private set; }

		/// <summary>
		/// The sub version number of this element.
		/// </summary>
		public int SubVersion { get; private set; }

		/// <summary>
		/// The sub-sub version of this element.
		/// </summary>
		public int SubSubVersion { get; private set; }

		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of <see cref="VersionAttribute"/>.</summary>
		/// <param name="mainVersion">The major version number.</param>
		/// <param name="subVersion">The minor version number.</param>
		/// <param name="subSubVersion">The patch version number.</param>
		public VersionAttribute(int mainVersion, int subVersion, int subSubVersion)
		{
			MainVersion = mainVersion;
			SubVersion = subVersion;
			SubSubVersion = subSubVersion;
		}

		#endregion
	}
}
