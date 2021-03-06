﻿/*
 * Radius: Complete Unity Reference Project
 * 
 * Source: https://github.com/MadLittleMods/Radius
 * Author: Eric Eastwood, ericeastwood.com
 * 
 * File: ExtensionMethods.cs, May 2014
 */

using UnityEngine;
using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public static class ExtensionMethods 
{
	
	public static string ToVerboseString(this Vector3 vec, int decimalCount = 3, bool forceDecimalValue = false)
	{
		string x = vec.x.ToString("f"+decimalCount);
		string y = vec.y.ToString("f"+decimalCount);
		string z = vec.z.ToString("f"+decimalCount);
		
		// If the string is 0, we should print out a more decimal for this component
		if(forceDecimalValue)
		{
			x = Mathf.Approximately(float.Parse(x), 0f) ? vec.x.ToString() : x;
			y = Mathf.Approximately(float.Parse(y), 0f) ? vec.y.ToString() : y;
			z = Mathf.Approximately(float.Parse(z), 0f) ? vec.z.ToString() : z;
		}
		
		return "(" + x + ", " + y + ", " + z + ")";
	}

	// Convert Dictionary to string
	// via: http://stackoverflow.com/a/5899291/796832
	public static string ToDebugString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
	{
		return "{" + string.Join(", ", dictionary.Select(kv => GetToDebugString(kv.Key) + "=" + GetToDebugString(kv.Value)).ToArray()) + "}";
	}
	public static string ToDebugString<TKey, TValue>(this ReadOnlyDictionary<TKey, TValue> dictionary)
	{
		return ToDebugString((IDictionary<TKey, TValue>)dictionary);
	}

	// Convert List to string
	public static string ToDebugString<T>(this IList<T> list)
	{
		return list.Count.ToString() + "`[" + string.Join("; ", list.Select(i => GetToDebugString(i)).ToArray()) + "]";
	}

	// Convert Vector3 to string
	public static string ToDebugString(this Vector3 vector)
	{
		return vector.ToVerboseString();
	}


	static string GetToDebugString<T>(T objectToGetStringFrom)
	{
		// This will try to call the `ToDebugString()` method from the class first
		// Then try to call `ToDebugString()` if it has an extension method in ExtensionMethods class
		// Otherwise just use the plain old `ToString()`

		// Get the MethodInfo
		// This will check in the class itself for the method
		var mi = objectToGetStringFrom.GetMethodOrNull("ToDebugString"); 

		string keyString = "";

		if(mi != null)
			// Get string from method in class
			keyString = (string)mi.Invoke(objectToGetStringFrom, null);
		else
		{
			// Try and find an extension method
			mi = objectToGetStringFrom.GetExtensionMethodOrNull("ToDebugString");
			
			if(mi != null)
				// Get the string from the extension method
				keyString = (string)mi.Invoke(null, new object[] {objectToGetStringFrom});
			else
				// Otherwise just get the normal ToString
				keyString = objectToGetStringFrom.ToString();
		}

		return keyString;
	}





	// Dictionary GetValueOrDefault
	public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
	{
		TValue value;
		return dictionary.TryGetValue(key, out value) ? value : defaultValue;
	}
	public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> defaultValueProvider)
	{
		TValue value;
		return dictionary.TryGetValue(key, out value) ? value : defaultValueProvider();
	}


	// Random method to compare the "r", "g", and "b" keys of two dictionarys
	public static bool CompareRGB(this IDictionary<string, float> dictionary, IDictionary<string, float> compareToDictionary)
	{
		// early exits
		if(dictionary == null || compareToDictionary == null)
			return false;

		if(dictionary["r"] == compareToDictionary["r"] || dictionary["g"] == compareToDictionary["g"] || dictionary["b"] == compareToDictionary["b"])
			return true;
		
		return false;
	}



	public static NetworkManager.NetworkConnectionErrorFancy ToFancyError(this NetworkConnectionError netConnectError)
	{
		return (NetworkManager.NetworkConnectionErrorFancy)netConnectError;
	}




	// ------------------------------------------------------------
	// ------------------------------------------------------------


	// via: http://stackoverflow.com/a/5114514/796832
	public static bool HasMethod(this object objectToCheck, string methodName)
	{
		// Checks for methods only in the class itself

		var type = objectToCheck.GetType();
		return type.GetMethod(methodName) != null;
	}

	public static bool HasMethodOrExtensionMethod(this object objectToCheck, string methodName)
	{
		// Checks for method in class or extension method in ExtensionMethods class

		if(objectToCheck.HasMethod("methodName"))
			return true;
		else
		{
			Assembly thisAssembly = typeof(ExtensionMethods).Assembly;
			foreach (MethodInfo method in GetExtensionMethods(thisAssembly, objectToCheck.GetType()))
				if(methodName == method.Name)
					return true;
		}
		
		return false;
	}

	public static MethodInfo GetMethodOrNull(this object objectToCheck, string methodName)
	{
		// Get MethodInfo if it is available in the class
		// Usage:
		// 		string myString = "testing";
		// 		var mi = myString.GetMethodOrNull("ToDebugString"); 
		// 		string keyString = mi != null ? (string)mi.Invoke(myString, null) : myString.ToString();

		var type = objectToCheck.GetType();
		MethodInfo method = type.GetMethod(methodName);
		if(method != null)
			return method;
		
		return null;
	}

	public static MethodInfo GetExtensionMethodOrNull(this object objectToCheck, string methodName)
	{
		// Get MethodInfo if it available as an extension method in the ExtensionMethods class
		// Usage:
		// 		string myString = "testing";
		// 		var mi = myString.GetMethodOrNull("ToDebugString"); 
		// 		string keyString = mi != null ? (string)mi.Invoke(null, new object[] {myString}); : myString.ToString();

		Assembly thisAssembly = typeof(ExtensionMethods).Assembly;
		foreach (MethodInfo methodEntry in GetExtensionMethods(thisAssembly, objectToCheck.GetType()))
			if(methodName == methodEntry.Name)
				return methodEntry;
		
		return null;
	}

	// via: http://stackoverflow.com/a/299526/796832
	static IEnumerable<MethodInfo> GetExtensionMethods(Assembly assembly, Type extendedType)
	{
		var query = from type in assembly.GetTypes()
			where type.IsSealed && !type.IsGenericType && !type.IsNested
				from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				where method.IsDefined(typeof(ExtensionAttribute), false)
				where method.GetParameters()[0].ParameterType == extendedType
				select method;
		return query;
	}


}
