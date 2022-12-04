using Godot;
using System.Collections.Generic;
using System.Linq;

public class AstarPath : Node2D
{
    [Export]
    public bool VisualizeAstarPath;                                 // If the path should be visualized
    [Export]
    public bool UseDiagonalPathFinding = true;                      // If Diagonal path finding should be used
    private Godot.Collections.Array NonWalkableTiles;               // Array of non-walkable tiles
    public List<Vector2> PathNodeList = new List<Vector2>();        // The path node list
    private TileMap TileMapWorld;                                   // The tilemap to find the path on
    private AStar2D Astar2DPath = new AStar2D();                    // The Astar 2D Pathfind class
    private Vector2 PathStartPos = Vector2.Zero;                    // The start position for the path
    private Vector2 PathEndPos = Vector2.Zero;                      // The end position for the path
    public Vector2 HalfTileSize;                                    // Variable to hold the half-size of a tile
    private Vector2 MapSize = Vector2.Zero;                         // The size of the TileMap

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        // Set the Astar node as top level node, so it won't inherit parent transforms
        // This is so that _Draw() will function properly
        this.SetAsToplevel(true);
    }
    private void GetTileMapBounds()
    {
        var mapSize = TileMapWorld.GetUsedRect();
        MapSize = new Vector2(mapSize.Size.x,mapSize.Size.y);
    }
    private bool IsTileOutsideTheMap(Vector2 tile)
    {
        // Check if the tile position is outside of the map
        if(tile.x < 0 || tile.y < 0 || tile.x >= MapSize.x || tile.y >= MapSize.y)
        {
	        return true;    // If so, return true
        }
        return false;       // If not, return false
    }
    private int CalculateUniqueTileIdentifier(Vector2 tile)
    {
        // Caclculate a unique identifier value for the specific tile, and return it.
        return (int)(tile.y * MapSize.x + tile.x);
    }

    private void SetPathStartPosition(Vector2 value)
    {
        // If the non-walkable tiles contains the value
        if(NonWalkableTiles.Contains(value))
        {
            // return out of the method, we cannot place this tile as start position
            return;
        }
        // If the tile is outside of the map
        if(IsTileOutsideTheMap(value))
        {
            // return out of the method, we cannot set a tile outside the map as start position
            return;
        }
        // Set the start position to the provided Vector2 value
        PathStartPos = value;

        // If the path end position is valid, and the path end position is not the same as the start position
        if(PathEndPos != null && PathEndPos != PathStartPos)
        {
            // Calculate the A* path
            CalculateAstarPath();
        }
    }

    private void SetPathEndPosition(Vector2 value)
    {
        // If the non-walkable tiles contains the value
        if(NonWalkableTiles.Contains(value))
        {
            // return out of the method, we cannot place this tile as end position
            return;
        }
        // If the tile is outside of the map
        if(IsTileOutsideTheMap(value))
        {
            // return out of the method, we cannot set a tile outside the map as start position
            return;
        }
        // Set the end position to the provided Vector2 value
        PathEndPos = value;
    }
    private void CalculateAstarPath()
    {
        // Calculate the start tile position unique identifier
        var tileStartId = CalculateUniqueTileIdentifier(PathStartPos);
        // Calculate the end tile position unique identifier
        var tileEndId = CalculateUniqueTileIdentifier(PathEndPos);
        // Get the path between the start and the end tile, as a List
        PathNodeList = Astar2DPath.GetPointPath(tileStartId,tileEndId).ToList();
    }

    private Godot.Collections.Array AddWalkableTiles(Godot.Collections.Array nonWalkableTiles)
    {
        // Create a new array for the walkable tiles
        Godot.Collections.Array walkableTiles = new Godot.Collections.Array();

        // Loop through all tiles on the map
        for(int y = 0; y < MapSize.y;  ++y)
        {
            for(int x =0; x < MapSize.x; ++x)
            {
                // Get the tile at the x, y position
                var tile = new Vector2(x, y);
                // If the tile is a non-walkable tile
                if(nonWalkableTiles.Contains(tile))
                {
                    // Go to the next tile in the loop
                    continue;
                }

                // Add the tile to the walkable tiles list
                walkableTiles.Add(tile);

                // Calculate the unique tile id
                var tileId = CalculateUniqueTileIdentifier(tile);

                // Add the tile to the list of points
                Astar2DPath.AddPoint(tileId, new Vector2(tile.x, tile.y));
            }
        }
        // Return the list of walkable tiles
        return walkableTiles;
    }

    private void ConnectWalkableTiles(Godot.Collections.Array walkableTiles)
    {
        // Loop through all the walkable tiles
        foreach(Vector2 tile in walkableTiles)
        {
            // Get the unique tile identifier for the tile
            var tileId = CalculateUniqueTileIdentifier(tile);

            // Create a Vector2 list for the neighbouring tiles
            // around the current tile (marked with X)
            //    [ ]
            // [ ][X][ ]
            //    [ ]
            Vector2[] neighbouringTiles = {
                new Vector2(tile.x + 1, tile.y),    // Tile to the right
                new Vector2(tile.x - 1, tile.y),    // Tile to the left
                new Vector2(tile.x, tile.y - 1),    // Tile above
                new Vector2(tile.x, tile.y + 1)     // Tile below

            };
            // loop through all the neighbouring tiles
            foreach(var neighbourTile in neighbouringTiles)
            {
                // Get the tile Id for the current neighboring tile in the loop
                var neighbourTileId = CalculateUniqueTileIdentifier(neighbourTile);
                // If the tile is outside the map
                if(IsTileOutsideTheMap(neighbourTile))
                {
                    // Go to the next tile in the list
                    continue;
                }
                // If the tile is not part of the Astar2D path
                if(!Astar2DPath.HasPoint(neighbourTileId))
                {
                    // Go to the next tile in the list
                    continue;
                }
                // Connect the neighbouring tile to the the current walkable tile
                Astar2DPath.ConnectPoints(tileId, neighbourTileId, false);
            }
        }
    }
    private void ConnectDiagonalWalkableTiles(Godot.Collections.Array walkableTiles)
    {
        // Loop through all walkable tiles
        foreach(Vector2 tile in walkableTiles)
        {
            // Get thte uniqe idenfier for the tile
            var tileId = CalculateUniqueTileIdentifier(tile);

            // loop through all the neighbouring tiles around the current tile (marked with X)
            // [ ][ ][ ]
            // [ ][X][ ]
            // [ ][ ][ ]
            for(int y = 0; y < 3; y++)
            {
                for(int x = 0; x< 3; x++)
                {
                    // Get the neighbouring tile
                    var neighbourTile = new Vector2(tile.x + x - 1, tile.y + y - 1);
                    // Get the unique identifier for the tile
                    var neighbourTileId = CalculateUniqueTileIdentifier(neighbourTile);
                    // If the neigbour tile found is the actual tile, or if the tile is outisde of the map
                    if(neighbourTile == tile || IsTileOutsideTheMap(neighbourTile))
                    {
                        continue;   // Go to the next tile in the loop
                    }
                    // If the tile is not part of the Astar2D path
                    if(!Astar2DPath.HasPoint(neighbourTileId))
                    {
                        continue;   // Go to the next tile in the loop
                    }
                    // Connect the neighbouring tile to the the current tile
                    Astar2DPath.ConnectPoints(tileId, neighbourTileId, true);
                }
            }
        }
    }
    public void SetTileMap(TileMap tileMap)
    {
        TileMapWorld = tileMap;                                     // Set the tilemap to perform A* pathfinding

        // Once the tilemap is set, we can initialize the Astar path finding
        InitAstarPathFind();
    }
    private void InitAstarPathFind()
    {
        GetTileMapBounds();                                         // Get the tilemap boundries
        HalfTileSize = TileMapWorld.CellSize / 2;                   // Calculate the half tile size
        NonWalkableTiles = TileMapWorld.GetUsedCellsById(0);        // Get the non walkable tiles (tile with value 0, the blue wall tile)
        var walkableTilesList = AddWalkableTiles(NonWalkableTiles); // Get the walkable tiles

        if(UseDiagonalPathFinding)
        {
            // Use one of these two for path-finding
            ConnectDiagonalWalkableTiles(walkableTilesList);        // Connect diagonal walkable tiles (the player can walk diagonally)
        }
        else
        {
            ConnectWalkableTiles(walkableTilesList);                // Connect walkable tiles (the player cannot walk diagonally)
        }

        SetPathStartPosition(Vector2.Zero);                         // Set the start position to 0,0
        SetPathEndPosition(Vector2.Zero);                           // Set the end position to 0,0
    }

    public bool SetAstarPath(Vector2 startPositon, Vector2 endPosition)
    {
        // If a TileMap has not been provided to the A* search
        if(TileMapWorld == null)
        {
            // Write an error to the console about it.
            GD.PrintErr("AstarPathFind - Tilemap is null, Make sure to call SetTileMap(TileMapNode) on the node that is parent to the Astar Script");
            return false;
        }

        // Update the path start position to the startPositon position
        this.PathStartPos = TileMapWorld.WorldToMap(startPositon);
        // Update the Path end position to the endPosition
        this.PathEndPos = TileMapWorld.WorldToMap(endPosition);

        // If both positions are valid tiles to build a path between
        if(TileMapWorld.GetCell((int)PathStartPos.x,(int)PathStartPos.y) != 0
        && TileMapWorld.GetCell((int)PathEndPos.x,(int)PathEndPos.y) != 0)
        {
            CalculateAstarPath();   // Calculate the A* path
            return true;
        }
        // The path could not be built, so return false
        return false;
    }
    public override void _Draw()
    {
        // If the path should not be visualized
        if(!VisualizeAstarPath)
        {
            return; // Return out of the method
        }
        var path = PathNodeList;       // Get the current PathList from the world tile map

        // If the path is null or the path doesn't contain any nodes
        if(path == null || path.Count <= 0)
        {
            return;                                 // Return out of the method
        }
        var pathStart = path[0];                    // Get the path start node
        var pathEnd = path[path.Count - 1];         // Get the end path node

        // Set the last path position to the start position
        // We add half the tile-size to center the line
        var lastPathPos = TileMapWorld.MapToWorld(new Vector2(pathStart.x, pathStart.y)) + HalfTileSize;

        // Loop through all nodes of the path
        for(int i = 0; i < path.Count; i++)
        {
            // Get the current path position
            var currentPathPos = TileMapWorld.MapToWorld(new Vector2(path[i].x, path[i].y)) + HalfTileSize;

            // Draw a white line from the last path position, to the current path postion
            DrawLine(lastPathPos, currentPathPos, new Color(1,1,1), 3f, true);

            // If it's the start node, and the line between the first node has been drawn
            // This is to that the red circle will be drawn on-top of the line.
            if(i == 1)
            {
                DrawCircle(lastPathPos, 8.0f, new Color(1,0,0));    // Draw a red circle at the start position
                DrawCircle(currentPathPos, 5.0f, new Color(1,1,1)); // Draw a white circle at the 2nd node position
            }
            // If it's the end node
            else if(i == path.Count-1)
            {
                DrawCircle(currentPathPos, 8.0f, new Color(0,1,0)); // Draw a green circle
            }
            // If it'a a node in-between the start and end nodes
            else
            {
                DrawCircle(currentPathPos, 5.0f, new Color(1,1,1)); // Draw a white circle, a little bit smaller
            }
            lastPathPos = currentPathPos;                           // Set the last path position to the current position
        }
    }

    public override void _Process(float delta)
    {
        // If the path should be visualized
        if(VisualizeAstarPath)
        {
            Update();   // Call update, so the graphics will be drawn
        }
    }
}
