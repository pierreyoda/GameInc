using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Loads from data files all the informations about the different game genres, themes
/// and game platforms.
/// </summary>
public class Database {
    private enum DataFileType {
        GamingPlatform,
    }
    private readonly List<Tuple<string, DataFileType>> dataFiles;

    [Serializable]
    public class DatabaseCollection<T> where T : DatabaseElement {
        public List<T> Collection = new List<T>();
    }
    public DatabaseCollection<Platform> Platforms { get; }

    public Database() {
        dataFiles = new List<Tuple<string, DataFileType>>();
        Platforms = new DatabaseCollection<Platform>();
    }

    public Database AddPlatformsDataFile(string dataFile) {
        AddDataFile(dataFile, DataFileType.GamingPlatform);
        return this;
    }

    private void AddDataFile(string dataFile, DataFileType dataType) {
        if (!File.Exists(dataFile)) {
            Debug.LogWarningFormat("Database - Cannot find data file \"{0}\".", dataFile);
            return;
        }

        dataFiles.Add(new Tuple<string, DataFileType>(dataFile, dataType));
        Debug.Log($"Database - Added {dataType} data file \"{dataFile}\".");
    }

    /// <summary>
    /// Try to load all the game data from the previously specified data source files.
    /// </summary>
    /// <returns>True if succesful, false otherwise</returns>
    public bool Load() {
        foreach (var sourceFile in dataFiles) {
            if (sourceFile.Item2 == DataFileType.GamingPlatform) {
                if (!LoadDataFile<Platform>(sourceFile.Item1, sourceFile.Item2, Platforms)) {
                    return false;
                }
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

            existing.Collection.Add(element);
            ++count;
        }

        Debug.Log($"Database - Added {count} {dataType} elements.");
        return true;
    }
}