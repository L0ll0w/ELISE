// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Thrown when trying to load a resource (using <see cref="UnityEngine.Resources.Load(string)"/> and variants) but the resource is not found.
	/// </summary>
	/// <seealso cref="System.Exception" />
	public class ResourceNotFoundException : Exception
	{
		/// <summary>The name of the resource that was not found.</summary>
		public string resourceName;

		/// <summary>The path at which the resource was expected to be found.</summary>
		public string resourcePath;

		/// <summary>Initializes a new instance of <see cref="ResourceNotFoundException"/> with a generic message.</summary>
		public ResourceNotFoundException() : base("Resource not found")
		{
		}

		/// <summary>Initializes a new instance of <see cref="ResourceNotFoundException"/> for a named resource.</summary>
		/// <param name="resourceName">The name of the resource that was not found.</param>
		public ResourceNotFoundException(string resourceName) : base(string.Format("Resource '{0}' not found", resourceName))
		{
			this.resourceName = resourceName;
		}

		/// <summary>Initializes a new instance of <see cref="ResourceNotFoundException"/> for a named resource at a specific path.</summary>
		/// <param name="resourceName">The name of the resource that was not found.</param>
		/// <param name="resourcePath">The path at which the resource was expected.</param>
		public ResourceNotFoundException(string resourceName, string resourcePath) : base(string.Format("Resource '{0}' not found at '{1}'", resourceName, resourcePath))
		{
			this.resourceName = resourceName;
			this.resourcePath = resourcePath;
		}

		/// <summary>Initializes a new instance of <see cref="ResourceNotFoundException"/> with a custom message.</summary>
		/// <param name="resourceName">The name of the resource that was not found.</param>
		/// <param name="resourcePath">The path at which the resource was expected.</param>
		/// <param name="message">A custom exception message.</param>
		public ResourceNotFoundException(string resourceName, string resourcePath, string message) : base(message)
		{
			this.resourceName = resourceName;
			this.resourcePath = resourcePath;
		}
	}
}
