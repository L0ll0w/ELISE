using Gamelogic.Extensions.Internal;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Marks a string field that should not be empty or whitespace.
	/// </summary>
	[Version(4, 3, 0)]
	public class ValidateNotWhiteSpaceOrEmpty : ValidateMatchRegularExpressionAttribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ValidateNotWhiteSpaceOrEmpty"/> class.
		/// </summary>
		public ValidateNotWhiteSpaceOrEmpty() : base(@"\S")
		{
			Message = "Value cannot be empty or whitespace.";
		}
	}
}
