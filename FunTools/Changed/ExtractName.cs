using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace DryTools
{
	public static class ExtractName
	{
		public static string From(Action source)
		{
			return From(source.Method);
		}

		public static string From<T>(Func<T> source)
		{
			return From(source.Method);
		}

		public static string From<TModel, TProperty>(Func<TModel, TProperty> source)
		{
			return From(source.Method);
		}

		public static string From(MethodInfo method)
		{
			return Setup.ExtractName(method);
		}

		public static class Setup
		{
			public static Func<MethodInfo, string> ExtractName = ExtractNameImplementation.ExtractLast;
		}
	}

	public static class ExtractNames
	{
		public static string[] From(Action source)
		{
			return From(source.Method);
		}

		public static string[] From<T>(Func<T> source)
		{
			return From(source.Method);
		}

		public static string[] From(MethodInfo method)
		{
			return Setup.ExtractNames(method);
		}

		public static class Setup
		{
			public static Func<MethodInfo, string[]> ExtractNames = ExtractNameImplementation.ExtractAll;
		}
	}

	#region Implementation

	internal class ExtractNameImplementation
	{
		internal static string[] ExtractAll(MethodInfo method)
		{
			var names = new List<string>();
			ExtractLastOrAll(method, names);
			return names.ToArray();
		}

		internal static string ExtractLast(MethodInfo method)
		{
			return ExtractLastOrAll(method);
		}

		private static string ExtractLastOrAll(MethodBase method, ICollection<string> names = null)
		{
			var methodBody = method.GetMethodBody();
			Debug.Assert(methodBody != null);

			var methodIL = methodBody.GetILAsByteArray();
			var module = method.Module;

			var declaringType = method.DeclaringType;
			Debug.Assert(declaringType != null);

			var declaringTypeGenericArgs = declaringType.IsGenericType ? declaringType.GetGenericArguments() : null;
			var methodGenericArgs = method.IsGenericMethod ? method.GetGenericArguments() : null;

			var ilToLook = methodIL.Length - TOKEN_LENGTH_BYTES;

			var tokenIndeces = new List<int>(2);

			for (var i = 0; i < ilToLook; ++i)
			{
				var code = methodIL[i];
				if (code == _field ||
					code == _staticField ||
					code == _call ||
					code == _calli ||
					code == _callvirt)
				{
					if (names == null)
						tokenIndeces.Add(i + 1);
					else
						names.Add(GetNameByTokenIndex(module, declaringTypeGenericArgs, methodGenericArgs, methodIL, i + 1));

					i += TOKEN_LENGTH_BYTES;
				}
			}

			if (names != null || // for names collection returning empty string - it will be ignored
				tokenIndeces.Count == 0) // if no tokens found, returning empty string
				return string.Empty;

			return GetNameByTokenIndex(module, declaringTypeGenericArgs, methodGenericArgs, methodIL, tokenIndeces[tokenIndeces.Count - 1]);
		}

		private static string GetNameByTokenIndex(
			Module module,
			Type[] declaringTypeGenericArgs,
			Type[] methodGenericArgs,
			byte[] methodIL,
			int index)
		{
			// Preventing sometimes possible InvalidOperationException "Token 0xX is not a valid MemberInfo token in the scope of module Y".
			try
			{
				return StripPrefixFast(module.ResolveMember(BitConverter.ToInt32(methodIL, index), declaringTypeGenericArgs, methodGenericArgs));
			}
			catch
			{
				return string.Empty;
			}
		}

		private static string StripPrefixFast(MemberInfo member)
		{
			var name = member.Name;
			if (!(member is MethodInfo))
				return name;

			if (name.Length > 4)
			{
				// get_, set_, add_
				if (name[3] == '_')
				{
					if (name[0] == 'g' &&
						name[1] == 'e' &&
						name[2] == 't' ||

						name[0] == 's' &&
						name[1] == 'e' &&
						name[2] == 't' ||

						name[0] == 'a' &&
						name[1] == 'd' &&
						name[2] == 'd')
					{
						return name.Substring(4);
					}
				}
				// remove_
				else if (
					name.Length > 7 && name[6] == '_' &&
					name[0] == 'r' &&
					name[1] == 'e' &&
					name[2] == 'm' &&
					name[3] == 'o' &&
					name[4] == 'v' &&
					name[5] == 'e')
				{
					return name.Substring(7);
				}
			}

			return name;
		}

		private const int TOKEN_LENGTH_BYTES = 4;

		private static readonly byte _field = (byte)OpCodes.Ldfld.Value;
		private static readonly byte _staticField = (byte)OpCodes.Ldsfld.Value;
		private static readonly byte _call = (byte)OpCodes.Call.Value;
		private static readonly byte _calli = (byte)OpCodes.Calli.Value;
		private static readonly byte _callvirt = (byte)OpCodes.Callvirt.Value;
	}

	#endregion
}