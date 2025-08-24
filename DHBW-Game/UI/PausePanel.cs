using GameLibrary.Graphics;
using Gum.Forms.Controls;
using Microsoft.Xna.Framework.Audio;

namespace DHBW_Game.UI;

/// <summary>
/// Represents a UI panel which is shown when the game is paused.
/// </summary>
public class PausePanel : Panel
{
    // TODO: implement PausePanel
    
    public PausePanel(TextureAtlas atlas, SoundEffect uiSoundEffect)
    {

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