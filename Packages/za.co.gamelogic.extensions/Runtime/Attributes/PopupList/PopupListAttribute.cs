using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// The base class for all popup list attributes. 
	/// </summary>
	/// <remarks>
	/// You can extend from this class if you want to create a custom popup list attribute.
	/// </remarks>
	[Version(4, 3, 0)]
	public abstract class PopupListAttribute : PropertyAttribute
	{
		/// <summary>Gets or sets the data object that describes where and how to retrieve the popup list values.</summary>
		public PopupListData PopupListData { get; set; }

		/// <summary>Gets the retrieval method specified in <see cref="PopupListData"/>.</summary>
		public ValuesRetrievalMethod RetrievalMethod => PopupListData.RetrievalMethod;

		/// <summary>
		/// Initializes a new instance of <see cref="PopupListAttribute"/> with the given popup list data.
		/// </summary>
		/// <param name="popupListData">The data describing how to retrieve the popup values.</param>
		protected PopupListAttribute(PopupListData popupListData) => PopupListData = popupListData;
	}
}
