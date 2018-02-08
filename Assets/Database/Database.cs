using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Database {

/// <summary>
/// Loads from data files all the informations about the different game genres, themes,
/// game platforms, events...
/// </summary>
[Serializable]
public class Database {
    public enum DataFileType {
        Event,
        News,
        GameGenre,
        GameTheme,
        GamingPlatform,
        GameSeries,
        Room,
        RoomObject,
        EngineFeature,
        Skill,
        HiringMethod,
        CommonNames,
    }

    private readonly List<Tuple<string, DataFileType>> dataFiles = new List<Tuple<string, DataFileType>>();

    [Serializable]
    public class DatabaseCollection<T> where T : DatabaseElement {
        private readonly bool namesAreUnique;
        public bool NamesAreUnique => namesAreUnique;

        [SerializeField] private DataFileType type;
        public DataFileType Type => type;

        public List<T> Collection = new List<T>();

        public DatabaseCollection(bool namesAreUnique, DataFileType type) {
            this.namesAreUnique = namesAreUnique;
            this.type = type;
        }

        public T FindById(string id) {
            return Collection.Find(e => e.Id == id);
        }

        public T FindFirstByName(string name) {
            return Collection.Find(e => e.Name == name);
        }
    }

    private DatabaseCollection<Event> events = new DatabaseCollection<Event>(false, DataFileType.Event);
    public DatabaseCollection<Event> Events => events;

    private DatabaseCollection<Genre> genres = new DatabaseCollection<Genre>(true, DataFileType.GameGenre);
    public DatabaseCollection<Genre> Genres => genres;

    private DatabaseCollection<Theme> themes = new DatabaseCollection<Theme>(true, DataFileType.GameTheme);
    public DatabaseCollection<Theme> Themes => themes;

    private DatabaseCollection<Platform> platforms = new DatabaseCollection<Platform>(true, DataFileType.GamingPlatform);
    public DatabaseCollection<Platform> Platforms => platforms;

    private DatabaseCollection<GameSeries> gameSeries = new DatabaseCollection<GameSeries>(true, DataFileType.GameSeries);
    public DatabaseCollection<GameSeries> GameSeries => gameSeries;

    private DatabaseCollection<EngineFeature> engineFeatures = new DatabaseCollection<EngineFeature>(true, DataFileType.EngineFeature);
    public DatabaseCollection<EngineFeature> EngineFeatures => engineFeatures;

    private DatabaseCollection<Room> rooms = new DatabaseCollection<Room>(true, DataFileType.Room);
    public DatabaseCollection<Room> Rooms => rooms;

    private DatabaseCollection<Object> objects = new DatabaseCollection<Object>(true, DataFileType.RoomObject);
    public DatabaseCollection<Object> Objects => objects;

    private DatabaseCollection<News> news = new DatabaseCollection<News>(false, DataFileType.News);
    public DatabaseCollection<News> News => news;

    private DatabaseCollection<Skill> skills = new DatabaseCollection<Skill>(true, DataFileType.Skill);
    public DatabaseCollection<Skill> Skills => skills;

    private DatabaseCollection<HiringMethod> hiringMethods = new DatabaseCollection<HiringMethod>(true, DataFileType.HiringMethod);
    public DatabaseCollection<HiringMethod> HiringMethod => hiringMethods;

    private DatabaseCollection<Names> names = new DatabaseCollection<Names>(false, DataFileType.CommonNames);
    public DatabaseCollection<Names> Names => names;

    public Database AddDataFile(string dataFile, DataFileType dataType) {
        if (!File.Exists(dataFile)) {
            Debug.LogWarning($"Database - Cannot find data file \"{dataFile}\".");
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
                        return null;
                    break;
                case DataFileType.News:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, news))
                        return null;
                    break;
                case DataFileType.GameGenre:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, genres))
                        return null;
                    break;
                case DataFileType.GameTheme:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, themes))
                        return null;
                    break;
                case DataFileType.GamingPlatform:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, platforms))
                        return null;
                    break;
                case DataFileType.GameSeries:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, gameSeries))
                        return null;
                    break;
                case DataFileType.EngineFeature:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, engineFeatures))
                        return null;
                    break;
                case DataFileType.Room:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, rooms))
                        return null;
                    break;
                case DataFileType.RoomObject:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, objects))
                        return null;
                    break;
               case DataFileType.Skill:
                   if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, skills))
                       return null;
                   break;
               case DataFileType.HiringMethod:
                   if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, hiringMethods))
                       return null;
                   break;
               case DataFileType.CommonNames:
                   if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, names))
                       return null;
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
        PrintCollectionInfo(gameSeries);
        PrintCollectionInfo(engineFeatures);
        PrintCollectionInfo(rooms);
        PrintCollectionInfo(objects);
        PrintCollectionInfo(skills);
        PrintCollectionInfo(hiringMethods);
        PrintCollectionInfo(names);
    }

    private void PrintCollectionInfo<T>(DatabaseCollection<T> collection) where T : DatabaseElement {
        string plural = collection.Type == DataFileType.GameSeries ||
                        collection.Type == DataFileType.News ||
                        collection.Type == DataFileType.CommonNames
            ? "" : "s";
        Debug.Log($"Database - Loaded {collection.Collection.Count} {collection.Type}{plural}.");
    }

    private static bool LoadDataFile<T>(string dataFile, DataFileType dataType,
        DatabaseCollection<T> existing) where T : DatabaseElement {
        // JSON formatting
        string dataFileContent = JsonFormatter.Format(File.ReadAllText(dataFile));
        if (dataFileContent == null) {
            Debug.LogWarning($"Database - Invalid {dataType} JSON format in \"{dataFile}\" data file.");
            return false;
        }
        // JSON parsing
        DatabaseCollection<T> additions;
        try {
            additions = JsonUtility.FromJson<DatabaseCollection<T>>(dataFileContent);
        } catch (ArgumentException e) {
            Debug.LogError($"Database.LoadDataFile(\"{dataFile}\", {dataType}) : JSON error :\n{e.Message}");
            Debug.LogError($"Formatted JSON :\n{dataFileContent}");
            return false;
        }
        if (additions == null) {
            Debug.LogWarning(
                $"Database - Invalid {dataType} JSON in \"{dataFile}\" data file.");
            return false;
        }
        // Database processing
        foreach (var element in additions.Collection) {
            if (existing.Collection.Any(e => element.Id == e.Id)) {
                Debug.LogWarning(
                    $"Database - {dataType} element of ID \"{element.Id}\" already exists.");
                continue;
            }
            if (existing.NamesAreUnique && existing.Collection.Any(e => element.Name == e.Name)) {
                Debug.LogWarning(
                    $"Database - {dataType} element of name \"{element.Name}\" already exists (unique names activated).");
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
