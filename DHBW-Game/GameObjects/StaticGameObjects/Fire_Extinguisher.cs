using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using Microsoft.Xna.Framework;

namespace GameObjects.Static_Sprites.Fire_Extinguisher;

public class Fire_Extinguisher : GameObject
{

    public Fire_Extinguisher()
    {

    }


    /// <summary>
    /// Initializes the <see cref="Fire_Extinguisher"/> object at the given starting position in the world.
    /// </summary>
    /// <param name="position">The position at which the <see cref="Fire_Extinguisher"/> object should spawn.</param>
    public override void Initialize(Vector2 position)
    {
        base.Initialize(position);
    }

    public override void LoadContent()
    {
        TextureAtlas fireExtinguisherAtlas = TextureAtlas.FromFile(Core.Content, "Static_Sprites/Fire_Extinguisher-definition.xml");

        // Create the fire extinguisher sprite
        var fireExtinguisherRegion = fireExtinguisherAtlas.GetRegion("fireExtinguisher");
        Sprite = new Sprite(fireExtinguisherRegion);
        Sprite.Scale = new Vector2(3.0f, 3.0f);

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