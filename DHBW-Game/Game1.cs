using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;
 
namespace DHBW_Game;
 
public class Game1 : Core
{
    // The MonoGame logo texture
    private Texture2D _logo;
 
    public Game1() : base("DHBW", 1280, 720, false)
    {
 
    }
 
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
 
        base.Initialize();
    }
 
    protected override void LoadContent()
    {
        // TODO: use this.Content to load your game content here
        _logo = Content.Load<Texture2D>("images/logo");
       
    }
 
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
 
        // TODO: Add your update logic here
 
        base.Update(gameTime);
    }
 
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.White);
 
        // Begin the sprite batch to prepare for rendering.
        SpriteBatch.Begin();
 
         // Set position to top-left corner with a small margin
        Vector2 position = new Vector2(10, 10);
 
        // Scale the logo (z. B. auf 25 % der Originalgröße)
        float scale = 0.25f;
 
        // Draw the scaled logo in the top-left corner
        SpriteBatch.Draw(
            _logo,
            position,
            null,           // sourceRectangle
            Color.White,
            0f,             // rotation
            Vector2.Zero,   // origin
            scale,          // scale
            SpriteEffects.None,
            0f              // layerDepth
        );
 
        // Always end the sprite batch when finished.
        SpriteBatch.End();
 
        base.Draw(gameTime);
    }
}