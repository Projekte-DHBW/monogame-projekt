using System;
using Microsoft.Xna.Framework;

namespace GameLibrary.Graphics;

public class AnimatedSprite : Sprite 
{
    protected int _currentFrame;
    protected TimeSpan _elapsed;
    protected Animation _animation;
    
    /// <summary>
    /// Gets or Sets the animation for this animated sprite.
    /// </summary>
    public Animation Animation
    {
        get => _animation;
        set
        {
            _animation = value;
            Region = _animation.Frames[0];
        }
    }
    
    /// <summary>
    /// Creates a new animated sprite.
    /// </summary>
    public AnimatedSprite() { }
    
    /// <summary>
    /// Creates a new animated sprite with the specified frames and delay.
    /// </summary>
    /// <param name="animation">The animation for this animated sprite.</param>
    public AnimatedSprite(Animation animation)
    {
        Animation = animation;
    }
    
    /// <summary>
    /// Updates this animated sprite.
    /// </summary>
    /// <param name="gameTime">A snapshot of the game timing values provided by the framework.</param>
    public virtual void Update(GameTime gameTime)
    {
        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;
            _currentFrame++;

            if (_currentFrame >= _animation.Frames.Count)
            {
                _currentFrame = 0;
            }

            Region = _animation.Frames[_currentFrame];
        }
    }
}

public class AnimatedSpriteOnce : AnimatedSprite
{
    
    public bool IsFinished { get; private set; }

    public AnimatedSpriteOnce(Animation animation) : base(animation) {}

    public override void Update(GameTime gameTime)
    {
        _elapsed += gameTime.ElapsedGameTime;

        if (_elapsed >= _animation.Delay)
        {
            _elapsed -= _animation.Delay;
            if (!IsFinished)
                {
                _currentFrame++;

                if (_currentFrame >= _animation.Frames.Count)
                {
                    _currentFrame = _animation.Frames.Count-1;
                    IsFinished = true;
                }
            }
            Region = _animation.Frames[_currentFrame];
        }
    }

    public void ResetAnimation()
    {
        _currentFrame = 0;
        IsFinished = false;
    }
}