using System.Text;
using System.Text.Json;

namespace EirBot_New.Serialization;

[Serializable]
public abstract class Saveable {
	public abstract void SetFilename(string fileName);
	public abstract string GetFilename();

	public bool Save() {
		var asJson = JsonSerializer.Serialize(this, this.GetType(), new JsonSerializerOptions() { IncludeFields = true });

		var path = "data/" + this.GetType().ToString();
		if (!Directory.Exists(path))
			try {
				Directory.CreateDirectory(path);
				// No write permissions
			} catch (Exception e) {
				Console.WriteLine("FATAL ERROR: Could not create data directory" + path + " (" + e.GetType() + "):\n" + e.Message);
				return false;
			}

		var fileName = this.GetFilename() + ".json";
		try {
			var fileStream = File.Create(path + "/" + fileName);
			fileStream.Write(Encoding.UTF8.GetBytes(asJson));
			fileStream.Close();
			// No write permissions
		} catch (Exception e) {
			Console.WriteLine("FATAL ERROR: Could not write to file at data/" + fileName + ".json (" + e.GetType() + "):\n" + e.Message);
			return false;
		}

		return true;
	}

	public static object? Load(string fileName, Type t) {
		var path = "data/" + t.ToString();
		if (!Directory.Exists(path))
			throw new DirectoryNotFoundException();

		var fileStream = File.OpenRead(path + "/" + (fileName.EndsWith(".json") ? fileName : fileName + ".json"));
		var sr = new StreamReader(fileStream);
		var asJson = sr.ReadToEnd();
		sr.Close();
		fileStream.Close();

		var loadedData = JsonSerializer.Deserialize(asJson, t, new JsonSerializerOptions() { IncludeFields = true });
		if (loadedData != null)
			return loadedData;

		return null;
	}
}