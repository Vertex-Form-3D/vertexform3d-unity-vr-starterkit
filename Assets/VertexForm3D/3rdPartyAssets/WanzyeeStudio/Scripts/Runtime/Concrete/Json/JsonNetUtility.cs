
/*WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW*\     (   (     ) )
|/                                                      \|       )  )   _((_
||  (c) Wanzyee Studio  < wanzyeestudio.blogspot.com >  ||      ( (    |_ _ |=n
|\                                                      /|   _____))   | !  ] U
\.ZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZZ./  (_(__(S)   |___*/

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace WanzyeeStudio.Json{
	
	/// <summary>
	/// Integrate custom <c>Newtonsoft.Json.JsonConverter</c> to use 
	/// <a href="http://www.newtonsoft.com/json" target="_blank">Json.NET</a> in Unity.
	/// </summary>
	/// 
	/// <remarks>
	/// To use Json.NET, please set Unity "PlayerSettings/Api Compatibility Lavel" to .NET 2.0.
	/// Then download from its website and import the .NET 2.0 dll.
	/// Json.NET doesn't support serializing some types originally, e.g., <c>UnityEngine.Vector3</c>.
	/// This has the <c>defaultSettings</c> includes necessary custom converters by default for Unity using it.
	/// And assign to <c>Newtonsoft.Json.JsonConvert.DefaultSettings</c> when initializing if the original <c>null</c>.
	/// </remarks>
	/// 
	/// <example>
	/// Now we can use Json.NET just like before:
	/// </example>
	/// 
	/// <code>
	/// Debug.Log(JsonConvert.SerializeObject(Vector3.up));
	/// var vec = JsonConvert.DeserializeObject<Vector2>("{'x':1.0,'y':0.0}");
	/// </code>
	/// 
	/// <example>
	/// User can directly modify <c>defaultSettings</c> for customization, and override it:
	/// </example>
	/// 
	/// <code>
	/// JsonConvert.DefaultSettings = () => new JsonSerializerSettings(){
	/// 	Converters = JsonNetUtility.defaultSettings.Converters,
	/// 	DefaultValueHandling = DefaultValueHandling.Populate
	/// };
	/// </code>
	/// 
	public static class JsonNetUtility{

		#region Fields

		/// <summary>
		/// The default <c>Newtonsoft.Json.JsonSerializerSettings</c>.
		/// </summary>
		/// 
		/// <remarks>
		/// All its properties stay default, but the <c>Converters</c> includes below:
		/// 	1. Any custom <c>Newtonsoft.Json.JsonConverter</c> has constructor without parameters.
		/// 	2. Any <c>Newtonsoft.Json.JsonConverter</c> from <c>WanzyeeStudio.Json</c>.
		/// 	3. <c>Newtonsoft.Json.Converters.StringEnumConverter</c>.
		/// 	4. <c>Newtonsoft.Json.Converters.VersionConverter</c>.
		/// </remarks>
		/// 
		public static JsonSerializerSettings defaultSettings = new JsonSerializerSettings(){
			Converters = CreateConverters()
		};

		#endregion


		#region Methods

		/// <summary>
		/// Initialize when start up, set <c>Newtonsoft.Json.JsonConvert.DefaultSettings</c> if not yet.
		/// </summary>
		[RuntimeInitializeOnLoadMethod]
		private static void Initialize(){
			if(null == JsonConvert.DefaultSettings) JsonConvert.DefaultSettings = () => defaultSettings;
		}

		/// <summary>
		/// Create the converter instances.
		/// </summary>
		/// <returns>The converters.</returns>
		private static List<JsonConverter> CreateConverters(){

			var _customs = FindConverterTypes().Select((type) => CreateConverter(type));

			var _builtins = new JsonConverter[]{new StringEnumConverter(), new VersionConverter()};

			return _customs.Concat(_builtins).Where((converter) => null != converter).ToList();

		}

		/// <summary>
		/// Try to create the converter of specified type.
		/// </summary>
		/// <returns>The converter.</returns>
		/// <param name="type">Type.</param>
		private static JsonConverter CreateConverter(Type type){
			
			try{ return Activator.CreateInstance(type) as JsonConverter; }

			catch(Exception exception){ Debug.LogErrorFormat("Can't create JsonConverter {0}:\n{1}", type, exception); }

			return null;

		}

		/// <summary>
		/// Find all the valid converter types.
		/// </summary>
		/// <returns>The types.</returns>
		private static Type[] FindConverterTypes(){
			
			return AppDomain.CurrentDomain.GetAssemblies(

				).SelectMany((dll) => dll.GetTypes()
				).Where((type) => typeof(JsonConverter).IsAssignableFrom(type)

				).Where((type) => (!type.IsAbstract && !type.IsGenericTypeDefinition)
				).Where((type) => null != type.GetConstructor(new Type[0])

				).Where((type) => !(null != type.Namespace && type.Namespace.StartsWith("Newtonsoft.Json"))
				).OrderBy((type) => null != type.Namespace && type.Namespace.StartsWith("WanzyeeStudio")
				
			).ToArray();

		}

		#endregion


		#if UNITY_EDITOR

		/// <summary>
		/// Initialize in edit mode.
		/// </summary>
		/*
		 * http://stackoverflow.com/q/44655667
		 * There's a bug of RuntimeInitializeOnLoadMethodAttribute issued above.
		 * Here directly split into two initialize methods to avoid that, and make the code cleaner.
		 */
		[UnityEditor.InitializeOnLoadMethod]
		private static void InitializeEditor(){
			Initialize();
		}

		#endif

	}

}
