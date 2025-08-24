using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Graphics.Animation;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using GameLibrary.Graphics;
using Gum.Converters;
using MonoGameGum;
using RenderingLibrary.Graphics;

namespace DHBW_Game.UI;

/// <summary>
/// A custom button designed for displaying multiple-choice option textures, featuring an animated nine-slice background for visual feedback on focus states.
/// </summary>
public class OptionButton : Button
{
    // Sprite for displaying the option texture within the button
    private SpriteRuntime _optionSprite;

    /// <summary>
    /// Initializes a new instance of the <see cref="OptionButton"/> class using graphics from the specified texture atlas.
    /// </summary>
    /// <param name="atlas">The texture atlas containing the button graphics and animations for focused and unfocused states.</param>
    public OptionButton(TextureAtlas atlas)
    {
        // Set up the button's visual properties with fixed initial dimensions
        ButtonVisual buttonVisual = (ButtonVisual)Visual;
        buttonVisual.Height = 14f;
        buttonVisual.HeightUnits = DimensionUnitType.Absolute;
        buttonVisual.Width = 21f;
        buttonVisual.WidthUnits = DimensionUnitType.Absolute;

        // Hide the default text instance since the button uses a sprite for content
        TextRuntime textInstance = buttonVisual.TextInstance;
        textInstance.Visible = false;

        // Add a sprite for displaying the option texture, centered within the button
        _optionSprite = new SpriteRuntime();
        _optionSprite.XOrigin = HorizontalAlignment.Center;
        _optionSprite.XUnits = GeneralUnitType.Percentage;
        _optionSprite.YOrigin = VerticalAlignment.Center;
        _optionSprite.YUnits = GeneralUnitType.Percentage;
        _optionSprite.X = 50;
        _optionSprite.Y = 50;
        _optionSprite.Anchor(Gum.Wireframe.Anchor.Center);
        _optionSprite.WidthUnits = DimensionUnitType.Absolute;
        _optionSprite.HeightUnits = DimensionUnitType.Absolute;
        buttonVisual.AddChild(_optionSprite);

        // Configure the nine-slice background using the atlas texture
        NineSliceRuntime background = buttonVisual.Background;
        background.Texture = atlas.Texture;
        background.TextureAddress = TextureAddress.Custom;
        background.Color = Color.White * 0.2f;
        background.Alpha = 200;

        // Set up the unfocused state animation chain with a single frame
        TextureRegion unfocusedTextureRegion = atlas.GetRegion("unfocused-button");
        AnimationChain unfocusedAnimation = new AnimationChain { Name = "unfocused" };
        unfocusedAnimation.Add(new AnimationFrame
        {
            TopCoordinate = unfocusedTextureRegion.TopTextureCoordinate,
            BottomCoordinate = unfocusedTextureRegion.BottomTextureCoordinate,
            LeftCoordinate = unfocusedTextureRegion.LeftTextureCoordinate,
            RightCoordinate = unfocusedTextureRegion.RightTextureCoordinate,
            FrameLength = 0.3f,
            Texture = unfocusedTextureRegion.Texture
        });

        // Set up the focused state animation chain using frames from the atlas animation
        Animation focusedAtlasAnimation = atlas.GetAnimation("focused-button-animation");
        AnimationChain focusedAnimation = new AnimationChain { Name = "focused" };
        foreach (TextureRegion region in focusedAtlasAnimation.Frames)
        {
            focusedAnimation.Add(new AnimationFrame
            {
                TopCoordinate = region.TopTextureCoordinate,
                BottomCoordinate = region.BottomTextureCoordinate,
                LeftCoordinate = region.LeftTextureCoordinate,
                RightCoordinate = region.RightTextureCoordinate,
                FrameLength = (float)focusedAtlasAnimation.Delay.TotalSeconds,
                Texture = region.Texture
            });
        }

        // Assign the animation chains to the background
        background.AnimationChains = new AnimationChainList { unfocusedAnimation, focusedAnimation };

        // Reset and configure button states for visual transitions
        buttonVisual.ButtonCategory.ResetAllStates();

        // Enabled unfocused state: Use unfocused animation
        StateSave enabledState = buttonVisual.States.Enabled;
        enabledState.Apply = () =>
        {
            background.CurrentChainName = "unfocused";
        };

        // Focused state: Use focused animation and enable animation playback
        StateSave focusedState = buttonVisual.States.Focused;
        focusedState.Apply = () =>
        {
            background.CurrentChainName = "focused";
            background.Animate = true;
        };

        // HighlightedFocused state: Same as focused
        StateSave highlightedFocused = buttonVisual.States.HighlightedFocused;
        highlightedFocused.Apply = focusedState.Apply;

        // Highlighted state: Same as enabled
        StateSave highlighted = buttonVisual.States.Highlighted;
        highlighted.Apply = enabledState.Apply;

        // Subscribe to event handlers for keyboard input and roll-on focus
        KeyDown += HandleKeyDown;
        buttonVisual.RollOn += HandleRollOn;
    }

    /// <summary>
    /// Sets the texture for the option sprite and dynamically adjusts the button's size to fit the texture with added padding.
    /// </summary>
    /// <param name="texture">The texture to display as the option content.</param>
    public void SetOptionTexture(Texture2D texture)
    {
        // Retrieve the current zoom level of the Gum camera to scale the texture correctly
        float zoom = GumService.Default.Renderer.Camera.Zoom;

        _optionSprite.Texture = texture;
        _optionSprite.Width = texture.Width / zoom;
        _optionSprite.Height = texture.Height / zoom;

        // Adjust the button's visual dimensions to accommodate the sprite with horizontal and vertical padding
        Visual.Width = _optionSprite.Width + 20; // Add horizontal padding
        Visual.Height = _optionSprite.Height + 10; // Add vertical padding
    }

    /// <summary>
    /// Handles key down events for navigation, simulating tab behavior with left and right arrow keys.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The key event arguments.</param>
    private void HandleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Keys.Left)
        {
            HandleTab(TabDirection.Up, loop: true);
        }
        if (e.Key == Keys.Right)
        {
            HandleTab(TabDirection.Down, loop: true);
        }
    }

    /// <summary>
    /// Handles the roll-on event by setting the button to focused state.
    /// </summary>
    /// <param name="sender">The sender of the event.</param>
    /// <param name="e">The event arguments.</param>
    private void HandleRollOn(object sender, EventArgs e)
    {
        IsFocused = true;
    }
}