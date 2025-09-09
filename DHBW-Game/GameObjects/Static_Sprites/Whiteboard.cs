using System;
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameLibrary.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGameTutorial;

namespace GameObjects.Static_Sprites.Whiteboard;

public class Whiteboard : GameObject
{
    private string _spriteRegionName;

    public Whiteboard() : this("whiteboard1")
    {

    }

    public Whiteboard(string spriteRegionName)
    {
        _spriteRegionName = spriteRegionName;
    }

    /// <summary>
    /// Initializes the test character at the given starting position in the world.
    /// </summary>
    /// <param name="startingPosition">The position at which the test character should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
    }

    public override void LoadContent()
    {
        TextureAtlas whiteboardAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Whiteboard-definition.xml");

        // Create the closed door sprite
        var whiteboardRegion = whiteboardAtlas.GetRegion(_spriteRegionName);
        Sprite = new Sprite(whiteboardRegion);
        Sprite.Scale = new Vector2(8.0f, 8.0f);

        base.LoadContent();
    }


    /// <summary>
    /// Updates the sprite which is set and also handles the input.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public override void Update(GameTime gameTime)
    {
        base.Update(gameTime);


    }

    /// <summary>
    /// Draws the sprite which is set.
    /// </summary>
    public override void Draw()
    {
        base.Draw();
    }
}