// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Thrown when a method is called with illegal type parameters, or a class is constructed with 
	/// illegal type parameters. 
	/// </summary>
	/// <remarks>Normally, it is preferable to use type constraints, but in some cases this is not 
	/// possible. This exception can be thrown in such cases.</remarks>
	public class TypeArgumentException : Exception
	{
		/// <summary>The name of the type parameter that caused the exception.</summary>
		public readonly string parameterName;

		/// <summary>Initializes a new instance of <see cref="TypeArgumentException"/> with the given message.</summary>
		/// <param name="message">A message that describes the error.</param>
		public TypeArgumentException(string message)
			: base(message)
		{
			parameterName = "";
		}

		/// <summary>Initializes a new instance of <see cref="TypeArgumentException"/> for a specific type parameter.</summary>
		/// <param name="parameterName">The name of the type parameter that caused the exception.</param>
		/// <param name="message">A message that describes the error.</param>
		public TypeArgumentException(string parameterName, string message)
			: base($"{message}\nParameter Name: {parameterName}")
		{
			this.parameterName = parameterName;
		}
	}
}
