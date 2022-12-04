using Godot;
using System;

public class Player : AnimatedSprite
{
    private int PlayerPathTarget = 1;   // Set the Player path target to the 2nd node in the list
    private TileMap WorldTileMap;       // The world tile map
    private AstarPath AstarPathFind;    // The AstarPath
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        WorldTileMap = GetParent().GetNode<TileMap>("TileMap");         // Get the TileMap node
        AstarPathFind = GetNode<AstarPath>("AstarPath");                // Get the AstarPathFind node
        AstarPathFind.SetTileMap(WorldTileMap);                         // Set the TileMap the astar should perform pathfinding on
    }
    public override void _Input(InputEvent @event)
    {
        // If a mouse button is pressed
        if(@event is InputEventMouseButton mbEvent && mbEvent.IsPressed())
        {
            // And it's the left one
            if(mbEvent.ButtonIndex == (int)ButtonList.Left)
            {
                // If a path was found
                if(AstarPathFind.SetAstarPath(this.GlobalPosition, GetGlobalMousePosition()))
                {
                    // Set path target as the first node in the found path
                    PlayerPathTarget = 1;
                }
            }
        }
    }
    private void WalkPath(float delta)
    {
        // If the Player has not reached the end node in the path list
        if(PlayerPathTarget < AstarPathFind.PathNodeList.Count)
        {
            // Get the target node to walk towards
            var targetNode = WorldTileMap.MapToWorld(AstarPathFind.PathNodeList[PlayerPathTarget]) + AstarPathFind.HalfTileSize;
            // Rotate the Player towards the target node
            this.Rotation = (targetNode - this.GlobalPosition).Angle() - Mathf.Pi/2;
            // Move the Player towards the target node
            this.Position = this.Position.MoveToward(targetNode, 150 * delta);
            // If the Player has reached the target node
            if(this.Position == targetNode)
            {
                PlayerPathTarget++;   // Set the next node in the PathNodeList as the target
            }
            // Play the walk animation for the Player
            this.Play("Walk");
        }
        // If the Player has reached the final node in the PathNodeList
        else
        {
            // Play the idle frame
            this.Play("Idle");
        }
    }
    public override void _Process(float delta)
    {
        WalkPath(delta);    // Walk the pathW
    }
}
