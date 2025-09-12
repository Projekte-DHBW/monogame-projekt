using GameLibrary.Graphics;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Audio;
using System;
using DHBW_Game.Scenes;
using Gum.DataTypes;
using Gum.Managers;
using MonoGameGum.GueDeriving;
using GameLibrary;
using DHBW_Game.Save_System;
using Microsoft.Xna.Framework;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel which is shown when the game finished successfully.
/// </summary>
public class FinalWinPanel : Panel
{
    /// <summary>
    /// The restart button UI element.
    /// </summary>
    private AnimatedButton _NextfloorButton;

    /// <summary>
    /// The UI sound effect to play when a UI event is triggered.
    /// </summary>
    private readonly SoundEffect _uiSoundEffect;

    /// <summary>
    /// Reference to the texture atlas that we can pass to UI elements when they
    /// are created.
    /// </summary>
    private readonly TextureAtlas _atlas;

    TextRuntime grade;
    TextRuntime average;

    public double Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PausePanel"/> class.
    /// </summary>
    /// <param name="atlas">The texture atlas used for UI elements.</param>
    /// <param name="uiSoundEffect">The sound effect to play on UI interactions.</param>
    /// <param name="onOptions">The action to invoke when the options button is clicked.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="atlas"/> or <paramref name="uiSoundEffect"/> is null.
    /// </exception>
    public FinalWinPanel(TextureAtlas atlas, SoundEffect uiSoundEffect)
    {
        // Validate input parameters
        if (atlas == null) throw new ArgumentNullException(nameof(atlas));
        if (uiSoundEffect == null) throw new ArgumentNullException(nameof(uiSoundEffect));

        _uiSoundEffect = uiSoundEffect;
        _atlas = atlas;
        Anchor(Gum.Wireframe.Anchor.Center);
        Visual.WidthUnits = DimensionUnitType.Absolute;
        Visual.HeightUnits = DimensionUnitType.Absolute;
        Visual.Height = 70;
        Visual.Width = 264;

        IsVisible = false; // Initially hide the panel
        // Initialize child UI elements
        InitializeChildren(atlas);
    }

    /// <summary>
    /// Initializes the child UI elements of the pause panel.
    /// </summary>
    /// <param name="atlas">The texture atlas used for UI elements.</param>
    private void InitializeChildren(TextureAtlas atlas)
    {
        Value = 0.0;
        TextureRegion backgroundRegion = _atlas.GetRegion("panel-background");

        NineSliceRuntime background = new NineSliceRuntime();
        background.Dock(Gum.Wireframe.Dock.Fill);
        background.Texture = backgroundRegion.Texture;
        background.TextureAddress = TextureAddress.Custom;
        background.TextureHeight = backgroundRegion.Height;
        background.TextureLeft = backgroundRegion.SourceRectangle.Left;
        background.TextureTop = backgroundRegion.SourceRectangle.Top;
        background.TextureWidth = backgroundRegion.Width;
        AddChild(background);

        TextRuntime textInstance = new TextRuntime();
        textInstance.Text = "YOU PASSED!";
        textInstance.CustomFontFile = @"fonts/04b_30.fnt";
        textInstance.UseCustomFont = true;
        textInstance.FontScale = 0.5f;
        textInstance.X = 10f;
        textInstance.Y = 10f;
        AddChild(textInstance);

        AnimatedButton quitButton = new AnimatedButton(_atlas);
        quitButton.Text = "QUIT";
        quitButton.Anchor(Gum.Wireframe.Anchor.BottomRight);
        quitButton.Visual.X = -9f;
        quitButton.Visual.Y = -9f;
        quitButton.Click += HandleQuitButtonClicked;
        AddChild(quitButton);

        grade = new TextRuntime();
        grade.Text = "FINAL GRADE: " + Value.ToString();
        grade.CustomFontFile = @"fonts/04b_30.fnt";
        grade.UseCustomFont = true;
        grade.FontScale = 0.3f;
        grade.X = 10f;
        grade.Y = 30f;
        AddChild(grade);

        average = new TextRuntime();
        average.Text = "TOTAL AVERAGE GRADE: " + Value.ToString();
        average.CustomFontFile = @"fonts/04b_30.fnt";
        average.UseCustomFont = true;
        average.FontScale = 0.3f;
        average.X = 10f;
        average.Y = 45f;
        AddChild(average);
    }


    /// <summary>
    /// Handles the click event for the restart button.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The event data.</param>
    private void HandleQuitButtonClicked(object sender, EventArgs e)
    {
        ServiceLocator.Get<GameScene>().NextLevel();
        // A UI interaction occurred, play the sound effect
        Core.Audio.PlaySoundEffect(_uiSoundEffect);

        // Make the pause panel invisible to resume the game.
        IsVisible = false;

        // Go back to the title scene.
        Core.ChangeScene(new TitleScene());
        SaveManager.ResetProgress();

        // Resume the game
        ServiceLocator.Get<Game1>().UnGameOver();
        ServiceLocator.Get<Game1>().Resume();
    }

    public void UpdateGrade(double value, Color color, double averageValue)
    {
        Value = value;
        grade.Text = "GRADE: " + value.ToString();
        grade.Color = color;
        average.Text = "AVERAGE GRADE: " + averageValue.ToString();
    }

    /// <summary>
    /// Shows the pause panel.
    /// </summary>
    public void Show()
    {
        IsVisible = true; // Make the panel visible
    }

    /// <summary>
    /// Hides the pause panel.
    /// </summary>
    public void Hide()
    {
        IsVisible = false; // Hide the panel
    }
}