using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Database {

/// <summary>
/// Loads from data files all the informations about the different game genres, themes
/// and game platforms.
/// </summary>
public class Database {
    public enum DataFileType {
        Event,
        News,
        GameGenre,
        GameTheme,
        GamingPlatform,
        Room,
        RoomObject,
        EngineFeature,
    }

    private readonly List<Tuple<string, DataFileType>> dataFiles;

    [Serializable]
    public class DatabaseCollection<T> where T : DatabaseElement {
        [SerializeField] private DataFileType type;
        public DataFileType Type => type;

        public List<T> Collection = new List<T>();

        public DatabaseCollection(DataFileType type) {
            this.type = type;
        }

        public T FindById(string id) {
            return Collection.Find(e => e.Id == id);
        }
    }

    public DatabaseCollection<Event> Events { get; }
    public DatabaseCollection<Genre> Genres { get; }
    public DatabaseCollection<Theme> Themes { get; }
    public DatabaseCollection<Platform> Platforms { get; }
    public DatabaseCollection<EngineFeature> EngineFeatures { get; }
    public DatabaseCollection<Room> Rooms { get; }
    public DatabaseCollection<Object> Objects { get; }
    public DatabaseCollection<News> News { get; }

    public Database() {
        dataFiles = new List<Tuple<string, DataFileType>>();
        Events = new DatabaseCollection<Event>(DataFileType.Event);
        News = new DatabaseCollection<News>(DataFileType.News);
        Genres = new DatabaseCollection<Genre>(DataFileType.GameGenre);
        Themes = new DatabaseCollection<Theme>(DataFileType.GameTheme);
        Platforms = new DatabaseCollection<Platform>(DataFileType.GamingPlatform);
        EngineFeatures = new DatabaseCollection<EngineFeature>(DataFileType.EngineFeature);
        Rooms = new DatabaseCollection<Room>(DataFileType.Room);
        Objects = new DatabaseCollection<Object>(DataFileType.RoomObject);
    }

    public Database AddDataFile(string dataFile, DataFileType dataType) {
        if (!File.Exists(dataFile)) {
            Debug.LogWarningFormat("Database - Cannot find data file \"{0}\".", dataFile);
            return this;
        }

        dataFiles.Add(new Tuple<string, DataFileType>(dataFile, dataType));
        Debug.Log($"Database - Added {dataType} data file \"{dataFile}\".");
        return this;
    }

    public Database AddDataFolder(string dataFolder, DataFileType dataType) {
        string dataPath = new DirectoryInfo(Application.dataPath).Parent.ToString();
        DirectoryInfo directoryInfo = new DirectoryInfo(dataPath + "/" + dataFolder);
        foreach (FileInfo fileInfo in directoryInfo.EnumerateFiles("*.json")) {
            AddDataFile($"{dataFolder}/{fileInfo.Name}", dataType);
        }
        return this;
    }

    /// <summary>
    /// Try to load all the game data from the previously specified data source files.
    /// </summary>
    /// <returns>True if succesful, false otherwise</returns>
    public Database Load() {
        foreach (var sourceFile in dataFiles) {
            switch (sourceFile.Item2) {
                case DataFileType.Event:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Events))
                        return this;
                    break;
                case DataFileType.News:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, News))
                        return this;
                    break;
                case DataFileType.GameGenre:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Genres))
                        return this;
                    break;
                case DataFileType.GameTheme:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Themes))
                        return this;
                    break;
                case DataFileType.GamingPlatform:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Platforms))
                        return this;
                    break;
                case DataFileType.EngineFeature:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, EngineFeatures))
                        return this;
                    break;
                case DataFileType.Room:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Rooms))
                        return this;
                    break;
                case DataFileType.RoomObject:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Objects))
                        return this;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return this;
    }

    public void PrintDatabaseInfo() {
        PrintCollectionInfo(Events);
        PrintCollectionInfo(News);
        PrintCollectionInfo(Genres);
        PrintCollectionInfo(Themes);
        PrintCollectionInfo(Platforms);
        PrintCollectionInfo(EngineFeatures);
        PrintCollectionInfo(Rooms);
        PrintCollectionInfo(Objects);
    }

    private void PrintCollectionInfo<T>(DatabaseCollection<T> collection) where T : DatabaseElement {
        string plural = collection.Type == DataFileType.News ? "" : "s";
        Debug.Log($"Database - Loaded {collection.Collection.Count} {collection.Type}{plural}.");
    }

    private static bool LoadDataFile<T>(string dataFile, DataFileType dataType,
        DatabaseCollection<T> existing) where T : DatabaseElement {
        string dataFileContent = FormatJson(File.ReadAllText(dataFile));
        if (dataFileContent == null) {
            Debug.LogWarning($"Database - Invalid {dataType} JSON format in \"{dataFile}\" data file.");
            return false;
        }
        DatabaseCollection<T> additions = JsonUtility.FromJson<DatabaseCollection<T>>(dataFileContent);
        if (additions == null) {
            Debug.LogWarning(
                $"Database - Invalid {dataType} JSON in \"{dataFile}\" data file.");
            return false;
        }

        foreach (var element in additions.Collection) {
            if (existing.Collection.Any(existingElement => element.Id == existingElement.Id)) {
                Debug.LogWarning(
                    $"Database - {dataType} element of ID \"{element.Id}\" already exists.");
                continue;
            }
            if (!element.IsValid()) {
                Debug.LogWarning($"Database - {dataType} element of ID \"{element.Id}\" is invalid.");
                continue;
            }

            existing.Collection.Add(element);
        }
        return true;
    }

    /// <summary>
    /// Format the given JSON data file to strip it of any unsupported feature :
    /// - Single-line comments (example : "// comment").
    /// - Multi-lines strings. Example :
    /// {
    ///     "text" : """
    /// my
    /// multi
    /// lines
    /// string
    /// """,
    ///     "value" : 0,
    /// }
    ///
    ///
    /// </summary>
    /// <param name="json">The clean JSON file.</param>
    /// <returns></returns>
    private static string FormatJson(string json) {
        string cleaned = "";

        int multistringStart = -1;
        List<string> multistrings = new List<string>();
        foreach (string line in json.Split('\n')) {
            string lineTrimmed = line.Trim();
            // Empty line : count only inside multi-lines string
            if (lineTrimmed.Length == 0 && multistringStart == -1) continue;

            // Process multi-lines string literals
            int multistringDelimiterIndex = lineTrimmed.IndexOf("\"\"\"", StringComparison.Ordinal); // NB : position of last double quote
            if (multistringStart == -1 && multistringDelimiterIndex != -1) { // start
                multistringStart = multistringDelimiterIndex;
                multistrings.Add(lineTrimmed.Substring(0, multistringStart - 1));
                continue;
            }
            if (multistringStart != -1 && multistringDelimiterIndex != -1) { // end
                if (multistringDelimiterIndex >= 2)
                    multistrings.Add(lineTrimmed.Substring(0, multistringDelimiterIndex - 2));
                string comma = lineTrimmed.EndsWith(",") ? "," : "";

                if (multistrings.Count == 0) {
                    Debug.LogError("Database.FormatJson : invalid multi-lines string.");
                    return null;
                }
                string start = multistrings.First();
                string literal = string.Join("\\n", multistrings.Skip(1));
                cleaned += $"{start} \"{literal}\"{comma}\n";

                multistrings.Clear();
                multistringStart = -1;
                continue;
            }
            if (multistringStart >= 0) { // middle
                multistrings.Add(lineTrimmed);
                continue;
            }

            // Ignore single-line comments (except in multi-line strings)
            int commentStart = lineTrimmed.IndexOf("//", StringComparison.Ordinal);
            if (multistringStart == -1 && commentStart != -1) {
                cleaned += lineTrimmed.Substring(0, commentStart).TrimEnd() + "\n";
                continue;
            }

            cleaned += $"{lineTrimmed}\n";
        }

        return cleaned;
    }
}

}
