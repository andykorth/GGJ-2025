using System.Collections.Generic;
using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class JSONUtilities {

	public static JsonSerializer serializer;
	private static JsonSerializerSettings settings;
	private static Formatting formatting;

	static JSONUtilities() {
		formatting = Formatting.Indented;

		settings = new JsonSerializerSettings ();
		settings.TypeNameHandling = TypeNameHandling.Auto;
		settings.Formatting = formatting;

		List<JsonConverter> converters = new List<JsonConverter> ();
		converters.Add (new StringEnumConverter ());

		settings.Converters = converters;
		serializer = JsonSerializer.Create(settings);
	}

	public static string? lastError;
	
	public static T Deserialize<T>(string filepath){
		// Debug.Log ("Attemping load from: " + filepath);
		// http://stackoverflow.com/questions/13740957/json-net-throwing-exception-in-a-property-setter-during-deserialization
		try{
			lastError = null;
			T t;
			using (StreamReader file = File.OpenText(filepath))
			{
				t = (T) serializer.Deserialize(file, typeof(T));
			}

			return t;
		}catch(FileNotFoundException){
			Log.Error ("Missing file in deserialization attempt: " + filepath);
		}catch(Exception e){
			if (e.InnerException != null) {
				lastError = e.InnerException.ToString();
				Log.Error ("SavedInfo Deserialization: <"+filepath+"> Inner exception: \n\n" + e.InnerException.ToString());
				Log.Error ("Since loading failed, we will return a default value (null probably) here.");
				//throw e.InnerException;
			} else {
				lastError = e.ToString();
				Log.Error ("Error deserializing: " +filepath + " - " + e.ToString());
			}
		}
		return default(T);
	}


	public static string SerializeToString<T>(T jsonContent){

		try{
			string jsonString = JsonConvert.SerializeObject(jsonContent, settings);
			return jsonString;
		}catch(Exception e){
			if (e.InnerException != null) {
				Log.Error ("String Serialization: Inner exception: \n\n" + e.InnerException.ToString());
				Log.Error ("Since loading failed, we will return null here.");
				lastError = e.InnerException.ToString();
			} else {
				lastError = "JSONUtilities.Serialize err: " + e.ToString();
				Log.Error (lastError);
			}
		}

		return null;
	}



}