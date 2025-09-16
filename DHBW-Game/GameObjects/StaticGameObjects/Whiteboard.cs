using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using Microsoft.Xna.Framework;

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
    /// Initializes the <see cref="Whiteboard"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="position">The position at which the <see cref="Whiteboard"/> object should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
    }

    public override void LoadContent()
    {
        TextureAtlas whiteboardAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Whiteboard-definition.xml");

        // Create the whiteboard sprite
        var whiteboardRegion = whiteboardAtlas.GetRegion(_spriteRegionName);
        Sprite = new Sprite(whiteboardRegion);
        Sprite.Scale = new Vector2(8.0f, 8.0f);

        base.LoadContent();
    }


    /// <summary>
    /// Updates the sprite which is set.
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