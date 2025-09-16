using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using Microsoft.Xna.Framework;

namespace GameObjects.Static_Sprites.Door_Closed;

public class Door_Closed : GameObject
{

    public Door_Closed()
    {

    }


    /// <summary>
    /// Initializes the <see cref="Door_Closed"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="position">The position at which the <see cref="Door_Closed"/> object should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
    }

    public override void LoadContent()
    {
        TextureAtlas closedDoorAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Door/Door_Closed-definition.xml");

        // Create the closed door sprite
        var closedDoorRegion = closedDoorAtlas.GetRegion("closedDoor");
        Sprite = new Sprite(closedDoorRegion);
        Sprite.Scale = new Vector2(4.0f, 4.0f);

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