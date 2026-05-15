using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Fellhaven.Core;
using Godot;

namespace Fellhaven.Systems;

/// <summary>
/// Loads dungeon graphs from JSON files.
/// </summary>
public static class DungeonLoader
{
    /// <summary>
    /// Loads a dungeon from a JSON file.
    /// </summary>
    /// <param name="filePath">Path to the dungeon JSON file.</param>
    /// <returns>Dictionary of locations keyed by ID, or null on error.</returns>
    public static Dictionary<string, Location>? LoadDungeon(string filePath)
    {
        if (!FileAccess.FileExists(filePath))
        {
            GD.PrintErr($"[DungeonLoader] File not found: {filePath}");
            return null;
        }

        try
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"[DungeonLoader] Failed to open: {filePath}");
                return null;
            }

            string jsonText = file.GetAsText();
            var dungeonData = JsonSerializer.Deserialize<DungeonData>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (dungeonData == null)
            {
                GD.PrintErr($"[DungeonLoader] Failed to parse JSON: {filePath}");
                return null;
            }

            return BuildDungeonGraph(dungeonData);
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[DungeonLoader] Error loading {filePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Builds the dungeon graph from deserialized data.
    /// </summary>
    private static Dictionary<string, Location> BuildDungeonGraph(DungeonData data)
    {
        var locations = new Dictionary<string, Location>();

        // Create all locations
        foreach (var locData in data.Locations)
        {
            var location = new Location(locData.Id)
            {
                NameKey = locData.NameKey,
                DescriptionKey = locData.DescriptionKey,
                BackgroundTexture = locData.BackgroundTexture
            };

            // Add objects if any
            if (locData.Objects != null)
            {
                foreach (var objData in locData.Objects)
                {
                    location.Objects.Add(new LocationObject
                    {
                        Id = objData.Id,
                        Type = objData.Type,
                        NameKey = objData.NameKey
                    });
                }
            }

            locations[location.Id] = location;
        }

        // Create all edges and link them
        foreach (var edgeData in data.Edges)
        {
            if (!locations.TryGetValue(edgeData.Source, out var source))
            {
                GD.PrintErr($"[DungeonLoader] Edge '{edgeData.Id}': source location '{edgeData.Source}' not found");
                continue;
            }

            if (!locations.TryGetValue(edgeData.Destination, out var destination))
            {
                GD.PrintErr($"[DungeonLoader] Edge '{edgeData.Id}': destination location '{edgeData.Destination}' not found");
                continue;
            }

            var edge = new Edge
            {
                Id = edgeData.Id,
                Source = source,
                Destination = destination,
                Direction = ParseDirection(edgeData.Direction),
                Priority = edgeData.Priority,
                WallTexture = edgeData.WallTexture,
                DoorTexture = edgeData.DoorTexture,
                ConditionScript = edgeData.ConditionScript ?? string.Empty
            };

            // Add actions
            if (edgeData.Actions != null)
            {
                foreach (var actionData in edgeData.Actions)
                {
                    edge.Actions.Add(new EdgeAction
                    {
                        Id = actionData.Id,
                        ConditionScript = actionData.ConditionScript ?? string.Empty,
                        ActionScript = actionData.ActionScript ?? string.Empty,
                        Description = actionData.Description ?? string.Empty
                    });
                }
            }

            source.OutgoingEdges.Add(edge);
        }

        GD.Print($"[DungeonLoader] Loaded dungeon '{data.DungeonId}' with {locations.Count} locations and {data.Edges.Count} edges");

        return locations;
    }

    /// <summary>
    /// Parses a direction string to Direction enum.
    /// </summary>
    private static Direction ParseDirection(string directionString)
    {
        return directionString.ToLower() switch
        {
            "north" or "n" => Direction.North,
            "northeast" or "ne" => Direction.NorthEast,
            "east" or "e" => Direction.East,
            "southeast" or "se" => Direction.SouthEast,
            "south" or "s" => Direction.South,
            "southwest" or "sw" => Direction.SouthWest,
            "west" or "w" => Direction.West,
            "northwest" or "nw" => Direction.NorthWest,
            "up" or "u" => Direction.Up,
            "down" or "d" => Direction.Down,
            _ => Direction.North
        };
    }
}

// === JSON Data Classes ===

internal class DungeonData
{
    [JsonPropertyName("dungeon_id")]
    public string DungeonId { get; set; } = string.Empty;

    [JsonPropertyName("name_key")]
    public string NameKey { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("locations")]
    public List<LocationData> Locations { get; set; } = new();

    [JsonPropertyName("edges")]
    public List<EdgeData> Edges { get; set; } = new();
}

internal class LocationData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_key")]
    public string NameKey { get; set; } = string.Empty;

    [JsonPropertyName("description_key")]
    public string DescriptionKey { get; set; } = string.Empty;

    [JsonPropertyName("background_texture")]
    public string BackgroundTexture { get; set; } = string.Empty;

    [JsonPropertyName("objects")]
    public List<LocationObjectData>? Objects { get; set; }
}

internal class LocationObjectData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("name_key")]
    public string NameKey { get; set; } = string.Empty;
}

internal class EdgeData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; set; } = string.Empty;

    [JsonPropertyName("destination")]
    public string Destination { get; set; } = string.Empty;

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 100;

    [JsonPropertyName("wall_texture")]
    public string WallTexture { get; set; } = string.Empty;

    [JsonPropertyName("door_texture")]
    public string DoorTexture { get; set; } = string.Empty;

    [JsonPropertyName("condition_script")]
    public string? ConditionScript { get; set; }

    [JsonPropertyName("actions")]
    public List<ActionData>? Actions { get; set; }
}

internal class ActionData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("condition_script")]
    public string? ConditionScript { get; set; }

    [JsonPropertyName("action_script")]
    public string? ActionScript { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
