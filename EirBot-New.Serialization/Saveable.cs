using System.Text;
using System.Text.Json;

namespace EirBot_New.Serialization;

[Serializable]
public abstract class Saveable {
	public abstract void SetFilename(string fileName);
	public abstract string GetFilename();

	public bool Save() {
		string asJson = JsonSerializer.Serialize(this, this.GetType(), new JsonSerializerOptions() { IncludeFields = true });

		string path = "data/" + this.GetType().ToString();
		if (!Directory.Exists(path))
			try {
				Directory.CreateDirectory(path);
			// No write permissions
			} catch (Exception e) {
				Console.WriteLine("FATAL ERROR: Could not create data directory" + path + " (" + e.GetType() + "):\n" + e.Message);
				return false;
			}

		string fileName = GetFilename() + ".json";
		try {
			FileStream fileStream = File.Create(path + "/" + fileName);
			fileStream.Write(Encoding.UTF8.GetBytes(asJson));
			fileStream.Close();
		// No write permissions
		} catch (Exception e) {
			Console.WriteLine("FATAL ERROR: Could not write to file at data/" + fileName + ".json (" + e.GetType() + "):\n" + e.Message);
			return false;
		}
		return true;
	}

	public static object Load(string fileName, Type t) {
		string path = "data/" + t.ToString();
		if (!Directory.Exists(path))
			throw new DirectoryNotFoundException();

		FileStream fileStream = File.OpenRead(path + "/" + (fileName.EndsWith(".json") ? fileName : (fileName + ".json")));
		StreamReader sr = new StreamReader(fileStream);
		string asJson = sr.ReadToEnd();
		sr.Close();
		fileStream.Close();

		object? loadedData = JsonSerializer.Deserialize(asJson, t, new JsonSerializerOptions() { IncludeFields = true });
		if (loadedData != null)
			return loadedData;
		return null;
	}
}
