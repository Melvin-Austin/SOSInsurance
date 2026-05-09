using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HyenaQuest;

public static class util_save
{
	private sealed class Vector3Converter : JsonConverter<Vector3>
	{
		public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(value.x);
			writer.WritePropertyName("y");
			writer.WriteValue(value.y);
			writer.WritePropertyName("z");
			writer.WriteValue(value.z);
			writer.WriteEndObject();
		}

		public override Vector3 ReadJson(JsonReader reader, Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			JObject jObject = JObject.Load(reader);
			return new Vector3(jObject["x"]?.Value<float>() ?? 0f, jObject["y"]?.Value<float>() ?? 0f, jObject["z"]?.Value<float>() ?? 0f);
		}
	}

	private sealed class QuaternionConverter : JsonConverter<Quaternion>
	{
		public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer)
		{
			writer.WriteStartObject();
			writer.WritePropertyName("x");
			writer.WriteValue(value.x);
			writer.WritePropertyName("y");
			writer.WriteValue(value.y);
			writer.WritePropertyName("z");
			writer.WriteValue(value.z);
			writer.WritePropertyName("w");
			writer.WriteValue(value.w);
			writer.WriteEndObject();
		}

		public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			JObject jObject = JObject.Load(reader);
			return new Quaternion(jObject["x"]?.Value<float>() ?? 0f, jObject["y"]?.Value<float>() ?? 0f, jObject["z"]?.Value<float>() ?? 0f, jObject["w"]?.Value<float>() ?? 1f);
		}
	}

	private static readonly JsonSerializerSettings JSON_SETTINGS = new JsonSerializerSettings
	{
		Formatting = Formatting.Indented,
		NullValueHandling = NullValueHandling.Include,
		ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
		Converters = new List<JsonConverter>
		{
			new Vector3Converter(),
			new QuaternionConverter()
		}
	};

	private static JObject LoadFile(SaveFileSettings settings)
	{
		if (settings == null || string.IsNullOrEmpty(settings.FilePath))
		{
			return new JObject();
		}
		try
		{
			return JObject.Parse(File.ReadAllText(settings.FilePath));
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Failed to read " + settings.FileName + ": " + ex.Message);
			return new JObject();
		}
	}

	private static void WriteFile(SaveFileSettings settings, JObject data)
	{
		try
		{
			string directoryName = Path.GetDirectoryName(settings.FilePath);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			string contents = data.ToString(Formatting.Indented);
			string text = settings.FilePath + ".tmp";
			File.WriteAllText(text, contents);
			if (File.Exists(settings.FilePath))
			{
				File.Delete(settings.FilePath);
			}
			File.Move(text, settings.FilePath);
		}
		catch (Exception ex)
		{
			Debug.LogError("Failed to write " + settings.FileName + ": " + ex.Message);
		}
	}

	public static void Save<T>(string key, T value, SaveFileSettings settings)
	{
		JObject jObject = LoadFile(settings);
		jObject[key] = JToken.FromObject(value, JsonSerializer.Create(JSON_SETTINGS));
		WriteFile(settings, jObject);
	}

	public static T Load<T>(string key, T defaultValue, SaveFileSettings settings)
	{
		if (!LoadFile(settings).TryGetValue(key, out JToken value))
		{
			return defaultValue;
		}
		try
		{
			return value.ToObject<T>(JsonSerializer.Create(JSON_SETTINGS));
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Failed to deserialize key '" + key + "' as " + typeof(T).Name + ": " + ex.Message);
			return defaultValue;
		}
	}

	public static bool KeyExists(string key, SaveFileSettings settings)
	{
		return LoadFile(settings).ContainsKey(key);
	}

	public static void DeleteKey(string key, SaveFileSettings settings)
	{
		JObject jObject = LoadFile(settings);
		if (jObject.Remove(key))
		{
			WriteFile(settings, jObject);
		}
	}

	public static void DeleteFile(SaveFileSettings settings)
	{
		try
		{
			if (File.Exists(settings.FilePath))
			{
				File.Delete(settings.FilePath);
			}
		}
		catch (Exception ex)
		{
			Debug.LogWarning("Failed to delete " + settings.FileName + ": " + ex.Message);
		}
	}
}
