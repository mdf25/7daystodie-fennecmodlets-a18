using System;
using System.Collections.Generic;


/**
 * Class to contain helper methods for navigating through coordinates and chunks in the world.
 */

public static class CoordinateHelper
{
    /**
     * Returns a list of all coordinates around a position.
     * Range in X, Y and Z can be specified.
     * Example: Adjacent blocks NSEW - set rangeX = rangeZ = 1, rangeY = 0, cardinal to true.
     */

    public static List<Vector3i> GetCoOrdinatesAround(Vector3i _pos, bool onlyCardinal = false, int rangeX = 1, int rangeY = 1, int rangeZ = 1)
    {
        if (rangeX < 0 | rangeY < 0 | rangeZ < 0)
        {
            throw new ArgumentException("Ranges must be non-negative.");
        }

        List<Vector3i> positions = new List<Vector3i>();
        for (int x = -rangeX; x <= rangeX; x += 1)
        {
            for (int y = -rangeY; y <= rangeY; y+= 1)
            {
                for (int z = -rangeZ; z <= rangeZ; z += 1)
                {
                    Vector3i step = new Vector3i(x, y, z);
                    Vector3i coordinate = _pos + step;

                    if (coordinate.y < 0 | coordinate.y > 255)
                    {
                        continue;
                    }

                    if (onlyCardinal && !IsCardinal(step))
                    {
                        continue;
                    }
                    
                    positions.Add(coordinate);
                }
            }
        }
        return positions;
    }


    /**
     * Shorter version to pass in vector3i of ranges instead.
     */

    public static List<Vector3i> GetCoOrdinatesAround(Vector3i _pos, Vector3i range, bool onlyCardinal = false)
    {
        return GetCoOrdinatesAround(_pos, onlyCardinal, range.x, range.y, range.z);
    }


    /**
     * Returns the block directly below this one.
     */

    public static Vector3i GetCoordinateBelow(Vector3i coordinate)
    {
        if (coordinate.y > 0)
        {
            coordinate.y -= 1;
            return coordinate;
        }
        return coordinate;
    }
    

    /**
     * Whether a vector is cardinal from the origin.
     */

    public static bool IsCardinal(Vector3i vector)
    {
        if (vector.x != 0 & vector.y == 0 & vector.z == 0)
        {
            return true;
        }
        else if (vector.x == 0 & vector.y != 0 & vector.z == 0)
        {
            return true;
        }
        else if (vector.x == 0 & vector.y == 0 & vector.z != 0)
        {
            return true;
        }
        return false;
    }


    /**
     * Gets a set of chunks from world positions.
     */

    public static List<Chunk> GetChunksFromCoordinates(World _world, List<Vector3i> coordinates)
    {
        List<Chunk> chunks = new List<Chunk>();
        foreach (Vector3i coordinate in coordinates)
        {
            Chunk chunk = _world.GetChunkFromWorldPos(coordinate) as Chunk;
            if (!chunks.Contains(chunk))
            {
                chunks.Add(chunk);
            }
        }
        return chunks;
    }


    /**
     * Returns a list of all tile entities within a set of world coordinates. 
     * You could use this in order to find out whether nearby tile entities are of a certain type, for example.
     */

    public static Dictionary<Vector3i, TileEntity> GetTileEntitiesInCoordinates(World _world, List<Vector3i> coordinates, TileEntityType type = TileEntityType.None)
    {
        List<Chunk> chunks = GetChunksFromCoordinates(_world, coordinates);
        Dictionary<Vector3i, TileEntity> tileEntities = new Dictionary<Vector3i, TileEntity>();
        foreach (Chunk chunk in chunks)
        {
            Dictionary<Vector3i, TileEntity> tileEntitiesInChunk = chunk.GetTileEntities().dict;

            if (tileEntitiesInChunk.Count == 0)
            {
                return tileEntitiesInChunk;
            }

            foreach (KeyValuePair<Vector3i, TileEntity> entry in tileEntitiesInChunk)
            {
                if (!coordinates.Contains(entry.Value.ToWorldPos()))
                {
                    continue;
                }

                if (type == TileEntityType.None)
                {
                    tileEntities.Add(entry.Value.ToWorldPos(), entry.Value);
                    continue;
                }
                
                if (entry.Value.GetTileEntityType() == type)
                {
                    tileEntities.Add(entry.Value.ToWorldPos(), entry.Value);
                }
            }
        }
        return tileEntities;
    }


    /**
     * Gets the name of a block at a certain coordinate.
     */

    public static string GetBlockNameAtCoordinate(World _world, Vector3i _pos)
    {
        BlockValue block = _world.GetBlock(_pos);
        return block.Block.GetBlockName();
    }


    /**
     * Returns whether the block at a coordinate is the block needed.
     */

    public static bool BlockAtCoordinateIs(World _world, Vector3i _pos, string name)
    {
        return GetBlockNameAtCoordinate(_world, _pos).ToLower().Trim().Equals(name.ToLower().Trim());
    }

}
