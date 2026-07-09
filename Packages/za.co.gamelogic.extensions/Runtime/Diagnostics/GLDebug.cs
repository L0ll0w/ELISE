// Copyright Gamelogic (c) http://www.gamelogic.co.za

using System.Diagnostics;
using Gamelogic.Extensions.Internal;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Gamelogic.Extensions
{
	/// <summary>
	/// Class that contains methods useful for debugging.
	/// All methods are only compiled if the DEBUG symbol is defined.
	/// </summary>
	public static class GLDebug
	{
		#region Static Methods

		/// <summary>Checks whether the condition is <see langword="true"/>, and logs an error message if it is not (only in DEBUG builds).</summary>
		/// <param name="condition">The condition to check.</param>
		/// <param name="message">The error message to log if the condition is <see langword="false"/>.</param>
		/// <param name="context">Optional Unity Object context for the log message.</param>
		[Version(1, 2, 0)]
		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message, Object context=null)
		{
			if (!condition)
			{
				LogError("Assert failed", message, context);
			}
		}

		/// <summary>Logs a message to the Unity console (only in DEBUG builds).</summary>
		/// <param name="message">The message to log.</param>
		/// <param name="context">Optional Unity Object context for the log message.</param>
		[Conditional("DEBUG")]
		public static void Log(object message, Object context = null)
		{
			Debug.Log(message, context);
		}

		/// <summary>Logs a warning message to the Unity console (only in DEBUG builds).</summary>
		/// <param name="message">The message to log.</param>
		/// <param name="context">Optional Unity Object context for the log message.</param>
		[Conditional("DEBUG")]
		public static void LogWarning(object message, Object context = null)
		{
			Debug.LogWarning(message, context);
		}

		/// <summary>Logs an error message to the Unity console (only in DEBUG builds).</summary>
		/// <param name="message">The message to log.</param>
		/// <param name="context">Optional Unity Object context for the log message.</param>
		[Conditional("DEBUG")]
		public static void LogError(object message, Object context = null)
		{
			Debug.LogError(message, context);
		}

		/// <summary>Logs a prefixed message to the Unity console (only in DEBUG builds).</summary>
		/// <param name="type">A prefix string, typically a category or system name.</param>
		/// <param name="message">The message to log.</param>
		/// <param name="context">Optional Unity Object context for the log message.</param>
		[Conditional("DEBUG")]
		public static void Log(string type, object message, Object context = null)
		{
			Debug.Log(type + ": " + message, context);
		}

		/// <summary>Logs a prefixed warning message to the Unity console (only in DEBUG builds).</summary>
		/// <param name="type">A prefix string, typically a category or system name.</param>
		/// <param name="message">The message to log.</param>
		/// <param name="context">Optional Unity Object context for the log message.</param>
		[Conditional("DEBUG")]
		public static void LogWarning(string type, object message, Object context = null)
		{
			Debug.LogWarning(type + ": " + message, context);
		}

		/// <summary>Logs a prefixed error message to the Unity console (only in DEBUG builds).</summary>
		/// <param name="type">A prefix string, typically a category or system name.</param>
		/// <param name="message">The message to log.</param>
		/// <param name="context">Optional Unity Object context for the log message.</param>
		[Conditional("DEBUG")]
		public static void LogError(string type, object message, Object context = null)
		{
			Debug.LogError(type + ": " + message, context);
		}

		#endregion
	}
}
