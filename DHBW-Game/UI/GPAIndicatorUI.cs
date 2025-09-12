using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using GameLibrary.Graphics;
using Gum.Wireframe;
using MonoGameGum;
using System.Xml.Linq;
using DHBW_Game.Scenes;
using GameLibrary;
using GameLibrary.Scenes;

namespace DHBW_Game.UI;

/// <summary>
/// A custom GPA Indicator that inherits from Gum's FrameworkElement class.
/// </summary>
public class GPAIndicatorUI : FrameworkElement
{
    double value;
    public double Value
    {
        get => value;
        set
        {
#if DEBUG
            if (double.IsNaN(value))
            {
                throw new InvalidOperationException("Can't set the ScrollBar Value to NaN");
            }
#endif
            var oldValue = this.value;
            var newValue = value;

            if (oldValue != newValue)
            {
                this.value = newValue;

                //OnValueChanged(oldValue, this.value);

                ValueChanged?.Invoke(this, EventArgs.Empty);

                var shouldRaiseChangeCompleted = true;
            }
        }
    }

    public Stages Stage;

    /// <summary>
    /// Event raised whenever the Value property changes regardless
    /// of source. This event may be raised multiple times if the user
    /// pushes+drags on the track or thumb.
    /// </summary>
    public event EventHandler ValueChanged;

    public Bar GPABar;

    TextRuntime gradeText;

    GameScene scene;

    ContainerRuntime topLevelContainer;

    TextureAtlas Atlas;

    double decrement;


    /// <summary>
    /// Creates a new OptionsSlider instance using graphics from the specified texture atlas.
    /// </summary>
    /// <param name="atlas">The texture atlas containing slider graphics.</param>
    public GPAIndicatorUI(TextureAtlas atlas, Stages stage)
    {
        scene = ServiceLocator.Get<GameScene>();
        topLevelContainer = new ContainerRuntime();
        topLevelContainer.Width = 260;

        gradeText = new TextRuntime();
        gradeText.CustomFontFile = @"fonts/04b_30.fnt";
        gradeText.FontScale = 0.2f;
        gradeText.UseCustomFont = true;
        gradeText.Text = Value.ToString();
        gradeText.Anchor(Gum.Wireframe.Anchor.Left);
        gradeText.OutlineThickness = 1;

        topLevelContainer.AddChild(gradeText);
        topLevelContainer.AddToRoot();
       

        // Assign the configured container as this slider's visual
        ValueChanged += HandleValueChanged;
        scene.NewLevel += HandleNewLevel;

        Visual = topLevelContainer;

        Stage = stage;

        Atlas = atlas;
    }

    private void HandleValueChanged(object sender, EventArgs e)
    {
        if (GPABar != null)
        {
            GPABar.Value = Value;
        }
        gradeText.Text = "Grade: " + Stage.GetCurrentGrade(Value).ToString();
        gradeText.Color = Stage.GetCurrentColor(Value*100);
        if (Value <= 0 ){
            if (!ServiceLocator.Get<Game1>().IsGameOver)
            {
                ServiceLocator.Get<Game1>().GameOver();

                GameScene scene = (GameScene)ServiceLocator.Get<Scene>();
                scene.ShowGameOver();
            }
        }
    }

    private void HandleNewLevel(object sender, NewLevelEventArgs e)
    {
        Value = 1.0;
        decrement = 1.0 / e.Duration;
        GPABar = new Bar(Atlas, Stage);
        GPABar.Name = "bar";
        GPABar.Minimum = 0;
        GPABar.Maximum = 1;
        GPABar.Anchor(Gum.Wireframe.Anchor.Right);
        topLevelContainer.Height = GPABar.Height;

        topLevelContainer.AddChild(GPABar.Visual);
    }

    public void HandleAnswer(bool correctAnswer)
    {
        if (correctAnswer)
        {
            Value += decrement * 8;
        }
        else
        {
            Value -= decrement * 4;
        }
    }

    public void Update(GameTime gameTime)
    {
        if (ServiceLocator.Get<Game1>().IsPaused)
        {
            return;
        }
        if (Value > 0)
        {
            Value -= decrement * gameTime.ElapsedGameTime.TotalSeconds;
        }
    }
}
