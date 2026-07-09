using System;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// This attribute is used to associate a retrieval key with a field that can be used
	/// by a property drawer to retrieve a list of presets. 
	/// </summary>
	/// <remarks>
	/// The list must be registered with the same key using
	/// <see cref="Extensions.PropertyDrawerData.RegisterValuesRetriever"/> in order for the property drawer to get the
	/// values.
	/// </remarks>
	///
	// TODO It would be nice to be able to add this to any field, but doing so will be complex and a lot of work. 
	public sealed class PresetsAttribute : Attribute
	{
		/// <summary>
		/// The key used to retrieve the presets.
		/// </summary>
		public string PresetRetrievalKey { get; private set; }

		/// <summary>
		/// The title displayed in the presets popup window.
		/// </summary>
		public string Title { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PresetsAttribute"/> class.
		/// </summary>
		/// <param name="title">The title of the presets.</param>
		/// <param name="presetRetrievalKey">The key used to return the presets.</param>
		public PresetsAttribute(string title, string presetRetrievalKey)
		{
			PresetRetrievalKey = presetRetrievalKey;
			Title = title;
		}
	}
}
