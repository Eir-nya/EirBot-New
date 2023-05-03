using System.Text;
using System.Text.Json;

namespace EirBot_New.Serialization;

public abstract class Saveable {
	public abstract string GetFilename();

	public bool Save() {
		string asJson = JsonSerializer.Serialize(this);

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
			FileStream fileStream = File.OpenWrite(path + "/" + fileName);
			fileStream.Write(Encoding.UTF8.GetBytes(asJson));
			fileStream.Close();
		// No write permissions
		} catch (Exception e) {
			Console.WriteLine("FATAL ERROR: Could not write to file at data/" + fileName + ".json (" + e.GetType() + "):\n" + e.Message);
			return false;
		}
		return true;
	}

	public static T Load<T>(string fileName) {
		string path = "data/" + typeof(T).ToString();
		if (!Directory.Exists(path))
			throw new DirectoryNotFoundException();

		FileStream fileStream = File.OpenWrite(path + "/" + (fileName.EndsWith(".json") ? fileName : (fileName + ".json")));
		StreamReader sr = new StreamReader(fileStream);
		string asJson = sr.ReadToEnd();
		sr.Close();
		fileStream.Close();

		object? loadedData = JsonSerializer.Deserialize(asJson, typeof(T));
		if (loadedData != null)
			return (T)loadedData;
		return default(T);
	}
}
