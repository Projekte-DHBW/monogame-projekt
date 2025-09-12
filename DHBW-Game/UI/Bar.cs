using System;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Microsoft.Xna.Framework;
using Gum.Forms.Controls;
using MonoGameGum.GueDeriving;
using GameLibrary.Graphics;
using Gum.Wireframe;
using System.Collections.Generic;
using Microsoft.Win32.SafeHandles;

namespace DHBW_Game.UI;

public struct Stages {
    public Stage One;
    public Stage Two;
    public Stage Three;
    public Stage Four;
    public Stage Final;

    private List<Stage> _stages = new List<Stage>();

    public Stages() {
        One = new Stage();
        Two = new Stage();
        Three = new Stage();
        Four = new Stage();
        Final = new Stage();
        _stages.Add(One);
        _stages.Add(Two);
        _stages.Add(Three);
        _stages.Add(Four);
        _stages.Add(Final);
    }
    public Stages(Stage final)
    {
        One = new Stage();
        Two = new Stage();
        Three = new Stage();
        Four = new Stage();
        Final = final;
        _stages.Add(One);
        _stages.Add(Two);
        _stages.Add(Three);
        _stages.Add(Four);
        _stages.Add(Final);
    }

    public Stages(Stage one, Stage two, Stage three, Stage four, Stage final)
    {
        One = one;
        Two = two;
        Three = three;
        Four = four;
        Final = final;
        _stages.Add(One);
        _stages.Add(Two);
        _stages.Add(Three);
        _stages.Add(Four);
        _stages.Add(Final);
    }

    private Stage GetCurrentStage(double Value)
    {
        if (Value > One.Value)
        {
            return One;
        }
        else if (Value > Two.Value)
        {
            return Two;
        }
        else if (Value > Three.Value)
        {
            return Three;
        }
        else if (Value > Four.Value)
        {
            return Four;
        }
        else
        {
            return Final;
        }
    }
    public double GetCurrentValue(double Value)
    {
        return GetCurrentStage(Value).Value;
    }

    public Color GetCurrentColor(double Value)
    {
        return GetCurrentStage(Value).Color;
    }

    private Stage GetLowerStage(Stage CurrentStage)
    {
        return _stages[_stages.IndexOf(CurrentStage)-1];
    }
    public double GetCurrentGrade(double Value)
    {
        var LowerStageValue = 100.0;
        try
        {
            LowerStageValue = GetLowerStage(GetCurrentStage(Value * 100)).Value;
        }
        catch
        {
        }
        var CurrentValue = GetCurrentValue(Value*100);
        var Grade = 0.0;
        Grade = Value - CurrentValue * 0.01;
        Grade = Grade / ((LowerStageValue - CurrentValue) * 0.01);
        Grade = GetCurrentStage(Value * 100).Index == 1 ? 1 - 0.4 * Grade : GetCurrentStage(Value * 100).Index - Grade;
        Grade = Math.Round(Grade, 1);
        return Grade;
    }
}

public struct Stage
{
    public double Value { get; set; } = 100;
    public Color Color { get; set; } = Color.Green;

    public int Index { get; set; } = 1;

    public Stage()
    {
    }
    public Stage(int index)
    {
        Index = index;
    }

    public Stage(Color color, int index)
    {
        Color = color;
        Index = index;
    }

    public Stage(double value, Color color, int index)
    {
        Value = value;
        Color = color;
        Index = index;
    }
}

/// <summary>
/// A custom slider control that inherits from Gum's Slider class.
/// </summary>
public class Bar : FrameworkElement
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


            // Cap the values first so the comparison is done against
            // the capped value
            newValue = System.Math.Min(newValue, Maximum);
            newValue = System.Math.Max(newValue, Minimum);

            if (oldValue != newValue)
            {
                this.value = newValue;

                ValueChanged?.Invoke(this, EventArgs.Empty);

                var shouldRaiseChangeCompleted = true;

                PushValueToViewModel();
            }
        }
    }
    public double Minimum;
    public double Maximum;

    public Stages Stage;
    // Reference to the text label that displays the slider's title
    private TextRuntime _textInstance;

    // Reference to the rectangle that visually represents the current value
    private NineSliceRuntime _fillRectangle;
    public const string SliderCategoryName = "SliderCategory";

    /// <summary>
    /// Event raised whenever the Value property changes regardless
    /// of source. This event may be raised multiple times if the user
    /// pushes+drags on the track or thumb.
    /// </summary>
    public event EventHandler ValueChanged;

    /// <summary>
    /// Creates a new OptionsSlider instance using graphics from the specified texture atlas.
    /// </summary>
    /// <param name="atlas">The texture atlas containing slider graphics.</param>
    public Bar(TextureAtlas atlas, Stages stage)
    {
        Stage = stage;
        // Container Slider
        ContainerRuntime topLevelContainer = new ContainerRuntime();
        topLevelContainer.Height = 8f;
        topLevelContainer.Width = 200f;

        TextureRegion middleBackgroundRegion = atlas.GetRegion("bar");

        // Create the middle track portion of the slider
        NineSliceRuntime middleBackground = new NineSliceRuntime();
        middleBackground.Dock(Gum.Wireframe.Dock.Fill);
        middleBackground.Texture = middleBackgroundRegion.Texture;
        middleBackground.TextureAddress = TextureAddress.Custom;
        middleBackground.TextureHeight = middleBackgroundRegion.Height;
        middleBackground.TextureLeft = middleBackgroundRegion.SourceRectangle.Left;
        middleBackground.TextureTop = middleBackgroundRegion.SourceRectangle.Top;
        middleBackground.TextureWidth = middleBackgroundRegion.Width;
        topLevelContainer.AddChild(middleBackground);

        // Create the interactive track that responds to clicks
        // The special name "TrackInstance" is required for Slider functionality
        ContainerRuntime trackInstance = new ContainerRuntime();
        trackInstance.Name = "TrackInstance";
        trackInstance.Dock(Gum.Wireframe.Dock.Fill);
        middleBackground.AddChild(trackInstance);

        // Create the middle track portion of the slider
        _fillRectangle = new NineSliceRuntime();
        _fillRectangle.Dock(Gum.Wireframe.Dock.FillVertically);
        _fillRectangle.Texture = middleBackgroundRegion.Texture;
        _fillRectangle.TextureAddress = TextureAddress.Custom;
        _fillRectangle.TextureHeight = middleBackgroundRegion.Height;
        _fillRectangle.TextureLeft = middleBackgroundRegion.SourceRectangle.Left;
        _fillRectangle.TextureTop = middleBackgroundRegion.SourceRectangle.Top;
        _fillRectangle.TextureWidth = middleBackgroundRegion.Width;
        _fillRectangle.Width = 100f;
        _fillRectangle.WidthUnits = DimensionUnitType.PercentageOfParent;
        trackInstance.AddChild(_fillRectangle);

        middleBackgroundRegion = atlas.GetRegion("empty-bar");

        float[] widths = new float[] { (float)Stage.One.Value, (float)Stage.Two.Value, (float)Stage.Three.Value, (float)Stage.Four.Value, 100f}; // Prozentwerte

        for (int i = 0; i < widths.Length; i++)
        {
            var fill = new NineSliceRuntime();
            fill.Name = $"Fill_{i}";
            fill.Dock(Gum.Wireframe.Dock.FillVertically);
            fill.Texture = middleBackgroundRegion.Texture;
            fill.TextureAddress = TextureAddress.Custom;
            fill.TextureHeight = middleBackgroundRegion.Height;
            fill.TextureLeft = middleBackgroundRegion.SourceRectangle.Left;
            fill.TextureTop = middleBackgroundRegion.SourceRectangle.Top;
            fill.TextureWidth = middleBackgroundRegion.Width;

            fill.Width = widths[i];
            fill.WidthUnits = DimensionUnitType.PercentageOfParent;
            //fill.Dock(Gum.Wireframe.Dock.Left);
            //fill.X = -1f;

            trackInstance.AddChild(fill);
        }


        // Define colors for focused and unfocused states
        Color focusedColor = Color.Red;
        Color unfocusedColor = Color.White;

        // Create slider state category - Slider.SliderCategoryName is the required name
        StateSaveCategory sliderCategory = new StateSaveCategory();
        sliderCategory.Name = Slider.SliderCategoryName;
        topLevelContainer.AddCategory(sliderCategory);

        // Create the enabled (default/unfocused) state
        StateSave enabled = new StateSave();
        enabled.Name = FrameworkElement.EnabledStateName;
        enabled.Apply = () =>
        {
            // When enabled but not focused, use gray coloring for all elements
            middleBackground.Color = unfocusedColor;
            _fillRectangle.Color = focusedColor;
        };
        sliderCategory.States.Add(enabled);

        // Create the focused state
        StateSave focused = new StateSave();
        focused.Name = FrameworkElement.FocusedStateName;
        focused.Apply = () =>
        {
            // When focused, use white coloring for all elements
            middleBackground.Color = focusedColor;
            _fillRectangle.Color = focusedColor;
        };
        sliderCategory.States.Add(focused);

        // Create the highlighted+focused state by cloning the focused state
        StateSave highlightedFocused = focused.Clone();
        highlightedFocused.Name = FrameworkElement.HighlightedFocusedStateName;
        sliderCategory.States.Add(highlightedFocused);

        // Create the highlighted state by cloning the enabled state
        StateSave highlighted = enabled.Clone();
        highlighted.Name = FrameworkElement.HighlightedStateName;
        sliderCategory.States.Add(highlighted);

        // Assign the configured container as this slider's visual
        Visual = topLevelContainer;
        ValueChanged += HandleValueChanged;
    }
    protected override void ReactToVisualChanged()
    {
        UpdateState();
    }

    public override void UpdateState()
    {
        if (Visual == null) //don't try to update the UI when the UI is not set yet, mmmmkay?
            return;

        var state = GetDesiredState();
        Visual.SetProperty(SliderCategoryName + "State", state);
    }

    /// <summary>
    /// Updates the fill rectangle width to visually represent the current value
    /// </summary>
    private void HandleValueChanged(object sender, EventArgs e)
    {
        // Calculate the ratio of the current value within its range
        double ratio = (Value - Minimum) / (Maximum - Minimum);
        var CurrentColor = Stage.GetCurrentColor(_fillRectangle.Width);
        if (_fillRectangle.Color != CurrentColor)
            _fillRectangle.Color = CurrentColor;
        // Update the fill rectangle width as a percentage
        // _fillRectangle uses percentage width units, so we multiply by 100
        _fillRectangle.Width = 100 * (float)ratio;
    }
}
