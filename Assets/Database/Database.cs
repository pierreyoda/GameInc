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
[Serializable]
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

    private readonly List<Tuple<string, DataFileType>> dataFiles = new List<Tuple<string, DataFileType>>();

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

    private DatabaseCollection<Event> events = new DatabaseCollection<Event>(DataFileType.Event);
    public DatabaseCollection<Event> Events => events;

    private DatabaseCollection<Genre> genres = new DatabaseCollection<Genre>(DataFileType.GameGenre);
    public DatabaseCollection<Genre> Genres => genres;

    private DatabaseCollection<Theme> themes = new DatabaseCollection<Theme>(DataFileType.GameTheme);
    public DatabaseCollection<Theme> Themes => themes;

    private DatabaseCollection<Platform> platforms = new DatabaseCollection<Platform>(DataFileType.GamingPlatform);
    public DatabaseCollection<Platform> Platforms => platforms;

    private DatabaseCollection<EngineFeature> engineFeatures = new DatabaseCollection<EngineFeature>(DataFileType.EngineFeature);
    public DatabaseCollection<EngineFeature> EngineFeatures => engineFeatures;

    private DatabaseCollection<Room> rooms = new DatabaseCollection<Room>(DataFileType.Room);
    public DatabaseCollection<Room> Rooms => rooms;

    private DatabaseCollection<Object> objects = new DatabaseCollection<Object>(DataFileType.RoomObject);
    public DatabaseCollection<Object> Objects => objects;

    private DatabaseCollection<News> news = new DatabaseCollection<News>(DataFileType.News);
    public DatabaseCollection<News> News => news;

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
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, events))
                        return this;
                    break;
                case DataFileType.News:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, news))
                        return this;
                    break;
                case DataFileType.GameGenre:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, genres))
                        return this;
                    break;
                case DataFileType.GameTheme:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, themes))
                        return this;
                    break;
                case DataFileType.GamingPlatform:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, platforms))
                        return this;
                    break;
                case DataFileType.EngineFeature:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, engineFeatures))
                        return this;
                    break;
                case DataFileType.Room:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, rooms))
                        return this;
                    break;
                case DataFileType.RoomObject:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, objects))
                        return this;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        return this;
    }

    public void PrintDatabaseInfo() {
        PrintCollectionInfo(events);
        PrintCollectionInfo(news);
        PrintCollectionInfo(genres);
        PrintCollectionInfo(themes);
        PrintCollectionInfo(platforms);
        PrintCollectionInfo(engineFeatures);
        PrintCollectionInfo(rooms);
        PrintCollectionInfo(objects);
    }

    private void PrintCollectionInfo<T>(DatabaseCollection<T> collection) where T : DatabaseElement {
        string plural = collection.Type == DataFileType.News ? "" : "s";
        Debug.Log($"Database - Loaded {collection.Collection.Count} {collection.Type}{plural}.");
    }

    private static bool LoadDataFile<T>(string dataFile, DataFileType dataType,
        DatabaseCollection<T> existing) where T : DatabaseElement {
        string dataFileContent = JsonFormatter.Format(File.ReadAllText(dataFile));
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
}

}
