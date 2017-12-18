using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
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
    }

    private readonly List<Tuple<string, DataFileType>> dataFiles;

    [Serializable]
    public class DatabaseCollection<T> where T : DatabaseElement {
        public List<T> Collection = new List<T>();

        public T FindById(string id) {
            return Collection.Find(e => e.Id == id);
        }
    }

    public DatabaseCollection<Event> Events { get; }
    public DatabaseCollection<Genre> Genres { get; }
    public DatabaseCollection<Theme> Themes { get; }
    public DatabaseCollection<Platform> Platforms { get; }
    public DatabaseCollection<Room> Rooms { get; }
    public DatabaseCollection<Object> Objects { get; }
    public DatabaseCollection<News> News { get; }

    public Database() {
        dataFiles = new List<Tuple<string, DataFileType>>();
        Events = new DatabaseCollection<Event>();
        Genres = new DatabaseCollection<Genre>();
        Themes = new DatabaseCollection<Theme>();
        Platforms = new DatabaseCollection<Platform>();
        Rooms = new DatabaseCollection<Room>();
        Objects = new DatabaseCollection<Object>();
        News = new DatabaseCollection<News>();
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

    /// <summary>
    /// Try to load all the game data from the previously specified data source files.
    /// </summary>
    /// <returns>True if succesful, false otherwise</returns>
    public bool Load() {
        foreach (var sourceFile in dataFiles) {
            switch (sourceFile.Item2) {
                case DataFileType.Event:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Events))
                        return false;
                    break;
                case DataFileType.News:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, News))
                        return false;
                    break;
                case DataFileType.GameGenre:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Genres))
                        return false;
                    break;
                case DataFileType.GameTheme:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Themes))
                        return false;
                    break;
                case DataFileType.GamingPlatform:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Platforms))
                        return false;
                    break;
                case DataFileType.Room:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Rooms))
                        return false;
                    break;
                case DataFileType.RoomObject:
                    if (!LoadDataFile(sourceFile.Item1, sourceFile.Item2, Objects))
                        return false;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        return true;
    }

    private bool LoadDataFile<T>(string dataFile, DataFileType dataType,
        DatabaseCollection<T> existing) where T : DatabaseElement {
        string dataFileContent = File.ReadAllText(dataFile);
        DatabaseCollection<T> additions = JsonUtility.FromJson<DatabaseCollection<T>>(dataFileContent);
        if (additions == null) {
            Debug.LogWarning(
                $"Database - Invalid {dataType} JSON in \"{dataFile}\" data file.");
            return false;
        }

        int count = 0;
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
            ++count;
        }

        Debug.Log($"Database - Added {count} {dataType} elements.");
        return true;
    }
}

}
