namespace EirBot_New.Serialization;

public struct BotDataCollection {
	public Dictionary<Type, Dictionary<string, object>> allData { get; private set; } = new Dictionary<Type, Dictionary<string, object>>();

	// Loads all saved data
	public BotDataCollection() {
		if (!Directory.Exists("data"))
			return;

		string[] subfolders = Directory.GetDirectories("data");
		foreach (string sub in subfolders) {
			string subfolder = Path.GetRelativePath("data", sub);
			Type? type = Type.GetType(subfolder);
			if (type == null) {
				Console.WriteLine("FATAL ERROR: Could not parse subfolder " + subfolder + " to type.");
				continue;
			}

			allData[type] = new Dictionary<string, object>();
			foreach (string f in Directory.GetFiles("data/" + subfolder)) {
				string filename = Path.GetRelativePath("data/" + subfolder, f);
				allData[type][Path.GetFileNameWithoutExtension(filename)] = Convert.ChangeType(Saveable.Load(filename, type), type);
			}
		}
	}

	public T Get<T>(string fileName) {
		if (!allData.ContainsKey(typeof(T)))
			allData[typeof(T)] = new Dictionary<string, object>();
		if (!allData[typeof(T)].ContainsKey(fileName)) {
			allData[typeof(T)][fileName] = Activator.CreateInstance(typeof(T));
			((Saveable)allData[typeof(T)][fileName]).SetFilename(fileName);
			if (((Saveable)allData[typeof(T)][fileName]).Save())
				Console.WriteLine("Created new \"" + typeof(T).Name + "\" at \"" + fileName + "\".");
		}
		return (T)allData[typeof(T)][fileName];
	}
}
