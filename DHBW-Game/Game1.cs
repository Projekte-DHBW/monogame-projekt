using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameLibrary;

namespace DHBW_Game;

public class Game1 : Core
{
    // The MonoGame logo texture
    private Texture2D _logo;

    // 🔹 Hintergrundbild
    private Texture2D _background;

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
        // 🔹 Lade das Logo
        _logo = Content.Load<Texture2D>("images/logo");

        // 🔹 Lade den Pixel-Art-Hintergrund
        _background = Content.Load<Texture2D>("images/hintergrund");
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);

        SpriteBatch.Begin();

        // 🔹 Hintergrundbild zeichnen (exakte Fenstergröße, kein Zoom)
        SpriteBatch.Draw(
            _background,
            destinationRectangle: new Rectangle(0, 0, Window.ClientBounds.Width, Window.ClientBounds.Height),
            color: Color.White
        );

        // 🔹 Logo in der oberen linken Ecke zeichnen
        Vector2 position = new Vector2(10, 10);
        float scale = 0.2f;
        SpriteBatch.Draw(
            _logo,
            position,
            null,
            Color.White,
            0f,
            Vector2.Zero,
            scale,
            SpriteEffects.None,
            0f
        );

        SpriteBatch.End();

        base.Draw(gameTime);
    }

}
