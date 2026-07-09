// Copyright Gamelogic (c) http://www.gamelogic.co.za

using Gamelogic.Extensions.Internal;
using UnityEngine;

namespace Gamelogic.Extensions
{

	/// <summary>
	/// An alternative to PlayerPrefs that provides methods 
	/// for setting bool and array preferences.
	/// </summary>
	[Version(1, 0, 0)]
	public class GLPlayerPrefs
	{
		#region Constants

		private const string ScopeOperator = "::";
		private const string ArrayCountKey = "Count";
		private const string Array = "Array";

		#endregion

		#region Static Methods

		/// <summary>Sets an integer preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="val">The value to store.</param>
		public static void SetInt(string scope, string key, int val)
		{
			PlayerPrefs.SetInt(GetKey(scope, key), val);
		}

		/// <summary>Gets an integer preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="defaultValue">The value to return if the key does not exist.</param>
		/// <returns>The stored value, or <paramref name="defaultValue"/> if not found.</returns>
		public static int GetInt(string scope, string key, int defaultValue = 0)
		{
			return PlayerPrefs.GetInt(GetKey(scope, key), defaultValue);
		}

		/// <summary>Sets a boolean preference (stored as an integer 0 or 1).</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="val">The value to store.</param>
		public static void SetBool(string scope, string key, bool val)
		{
			PlayerPrefs.SetInt(GetKey(scope, key), val ? 1 : 0);
		}

		/// <summary>Gets a boolean preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="defaultValue">The value to return if the key does not exist.</param>
		/// <returns>The stored value, or <paramref name="defaultValue"/> if not found.</returns>
		public static bool GetBool(string scope, string key, bool defaultValue = false)
		{
			return PlayerPrefs.GetInt(GetKey(scope, key), (defaultValue ? 1 : 0)) == 1;
		}

		/// <summary>Sets a float preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="val">The value to store.</param>
		public static void SetFloat(string scope, string key, float val)
		{
			PlayerPrefs.SetFloat(GetKey(scope, key), val);
		}

		/// <summary>Gets a float preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="defaultValue">The value to return if the key does not exist.</param>
		/// <returns>The stored value, or <paramref name="defaultValue"/> if not found.</returns>
		public static float GetFloat(string scope, string key, float defaultValue = 0.0f)
		{
			return PlayerPrefs.GetFloat(GetKey(scope, key), defaultValue);
		}

		/// <summary>Sets a string preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="value">The value to store.</param>
		public static void SetString(string scope, string key, string value)
		{
			PlayerPrefs.SetString(GetKey(scope, key), value);
		}

		/// <summary>Gets a string preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="defaultValue">The value to return if the key does not exist.</param>
		/// <returns>The stored value, or <paramref name="defaultValue"/> if not found.</returns>
		public static string GetString(string scope, string key, string defaultValue = "")
		{
			return PlayerPrefs.GetString(GetKey(scope, key), defaultValue);
		}

		/// <summary>Returns whether the given key exists in the given scope.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <returns><see langword="true"/> if the key exists; otherwise, <see langword="false"/>.</returns>
		public static bool HasKey(string scope, string key)
		{
			return PlayerPrefs.HasKey(GetKey(scope, key));
		}

		private static string GetKey(string scope, string key)
		{
			return scope + ScopeOperator + key;
		}

		/// <summary>Sets an integer array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="values">The array of values to store.</param>
		public static void SetIntArray(string scope, string key, int[] values)
		{
			//Add a value so that HasKey also works for arrays
			PlayerPrefs.SetString(GetKey(scope, key), Array);

			PlayerPrefs.SetInt(GetArrayCountKey(scope, key), values.Length);

			for (var i = 0; i < values.Length; i++)
			{
				PlayerPrefs.SetInt(GetArrayIndexKey(scope, key, i), values[i]);
			}
		}

		/// <summary>Gets an integer array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <returns>The stored integer array, or an empty array if not found.</returns>
		public static int[] GetIntArray(string scope, string key)
		{
			var count = PlayerPrefs.GetInt(GetArrayCountKey(scope, key));
			var values = new int[count];

			for (var i = 0; i < count; i++)
			{
				values[i] = PlayerPrefs.GetInt(GetArrayIndexKey(scope, key, i));
			}

			return values;
		}

		/// <summary>Sets a float array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="values">The array of values to store.</param>
		public static void SetFloatArray(string scope, string key, float[] values)
		{
			//Add a value so that HasKey also works for arrays
			PlayerPrefs.SetString(GetKey(scope, key), Array);

			PlayerPrefs.SetInt(GetArrayCountKey(scope, key), values.Length);

			for (var i = 0; i < values.Length; i++)
			{
				PlayerPrefs.SetFloat(GetArrayIndexKey(scope, key, i), values[i]);
			}
		}

		/// <summary>Gets a float array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <returns>The stored float array, or an empty array if not found.</returns>
		public static float[] GetFloatArray(string scope, string key)
		{
			var count = PlayerPrefs.GetInt(GetArrayCountKey(scope, key));
			var values = new float[count];

			for (var i = 0; i < count; i++)
			{
				values[i] = PlayerPrefs.GetFloat(GetArrayIndexKey(scope, key, i));
			}

			return values;
		}

		/// <summary>Sets a boolean array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="values">The array of values to store.</param>
		public static void SetBoolArray(string scope, string key, bool[] values)
		{
			//Add a value so that HasKey also works for arrays
			PlayerPrefs.SetString(GetKey(scope, key), Array);

			PlayerPrefs.SetInt(GetArrayCountKey(scope, key), values.Length);

			for (var i = 0; i < values.Length; i++)
			{
				PlayerPrefs.SetInt(GetArrayIndexKey(scope, key, i), values[i] ? 1 : 0);
			}
		}

		/// <summary>Gets a boolean array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <returns>The stored boolean array, or an empty array if not found.</returns>
		public static bool[] GetBoolArray(string scope, string key)
		{
			var count = PlayerPrefs.GetInt(GetArrayCountKey(scope, key));

			var values = new bool[count];

			for (var i = 0; i < count; i++)
			{
				values[i] = PlayerPrefs.GetInt(GetArrayIndexKey(scope, key, i)) != 0;
			}

			return values;
		}

		/// <summary>Sets a string array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <param name="values">The array of values to store.</param>
		public static void SetStringArray(string scope, string key, string[] values)
		{
			//Add a value so that HasKey also works for arrays
			PlayerPrefs.SetString(GetKey(scope, key), Array);

			PlayerPrefs.SetInt(GetArrayCountKey(scope, key), values.Length);

			for (var i = 0; i < values.Length; i++)
			{
				PlayerPrefs.SetString(GetArrayIndexKey(scope, key, i), values[i]);
			}
		}

		/// <summary>Gets a string array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key within the scope.</param>
		/// <returns>The stored string array, or an empty array if not found.</returns>
		public static string[] GetStringArray(string scope, string key)
		{
			var count = PlayerPrefs.GetInt(GetArrayCountKey(scope, key));

			var values = new string[count];

			for (var i = 0; i < count; i++)
			{
				values[i] = PlayerPrefs.GetString(GetArrayIndexKey(scope, key, i));
			}

			return values;
		}

		private static string GetArrayIndexKey(string scope, string key, int index)
		{
			return scope + ScopeOperator + key + ScopeOperator + index;
		}

		private static string GetArrayCountKey(string scope, string key)
		{
			return scope + ScopeOperator + key + ScopeOperator + ArrayCountKey;
		}

		/// <summary>Deletes all entries belonging to an array preference.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key of the array to delete.</param>
		public static void DeleteArray(string scope, string key)
		{
			var count = PlayerPrefs.GetInt(GetArrayCountKey(scope, key));

			for (int i = 0; i < count; i++)
			{
				PlayerPrefs.DeleteKey(GetArrayIndexKey(scope, key, i));
			}
		}

		/// <summary>Deletes the preference with the given key in the given scope.</summary>
		/// <param name="scope">The scope namespace.</param>
		/// <param name="key">The key to delete.</param>
		public static void DeleteKey(string scope, string key)
		{
			PlayerPrefs.DeleteKey(GetKey(scope, key));
		}

		/// <summary>Deletes all preferences.</summary>
		public static void DeleteAll()
		{
			PlayerPrefs.DeleteAll();
		}

		/// <summary>Saves all preferences to disk.</summary>
		public static void Save()
		{
			PlayerPrefs.Save();
		}

		#endregion
	}

}
