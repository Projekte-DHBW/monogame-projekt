using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DHBW_Game.GameObjects;

namespace DHBW_Game.Levels
{
    /// <summary>
    /// Simple text-based level class that creates tiles and basic collision from .txt files
    /// </summary>
    public class Level
    {
        // Tile system
        private LevelTile[,] tiles;
        private readonly Random random = new Random();
        
        // Game objects
        private SimplePlayer player;
        private List<Vector2> exitPositions = new List<Vector2>();
        
        // Content and graphics
        private ContentManager content;
        private Texture2D[] blockTextures;
        private Texture2D exitTexture;
        
        // Level dimensions
        public int Width => tiles?.GetLength(0) ?? 0;
        public int Height => tiles?.GetLength(1) ?? 0;
        
        // Tile size (adjust based on your tile dimensions)
        public const int TILE_SIZE = 64;
        
        public SimplePlayer Player => player;
        public bool IsCompleted { get; private set; }
        
        /// <summary>
        /// Initialize the level from a text file
        /// </summary>
        public Level(ContentManager contentManager, string levelName)
        {
            content = contentManager;
            LoadContent();
            LoadLevelFromText(Path.Combine(contentManager.RootDirectory,"Levels", levelName));
        }
        
        /// <summary>
        /// Load textures and other content
        /// </summary>
        private void LoadContent()
        {
            // Load the three block textures
            blockTextures = new Texture2D[3];
            blockTextures[0] = content.Load<Texture2D>("Tiles/BlockA1");
            blockTextures[1] = content.Load<Texture2D>("Tiles/BlockA2");
            blockTextures[2] = content.Load<Texture2D>("Tiles/BlockA3");
            
            // Load exit texture
            exitTexture = content.Load<Texture2D>("Tiles/Exit");
        }
        
        /// <summary>
        /// Load level from text file
        /// </summary>
        private void LoadLevelFromText(string levelPath)
        {
            List<string> lines = new List<string>();
            
            // Read the level file
            using (var stream = TitleContainer.OpenStream(levelPath))
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lines.Add(line);
                }
            }
            
            if (lines.Count == 0)
                throw new Exception("Level file is empty!");
            
            int width = lines[0].Length;
            int height = lines.Count;
            
            // Validate all lines are same length
            foreach (var line in lines)
            {
                if (line.Length != width)
                    throw new Exception("All level lines must be the same length!");
            }
            
            // Initialize tile array
            tiles = new LevelTile[width, height];
            
            // Parse each character in the level
            Vector2 playerStartPos = Vector2.Zero;
            bool foundPlayer = false;
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char tileChar = lines[y][x];
                    tiles[x, y] = CreateTileFromChar(tileChar, x, y, ref playerStartPos, ref foundPlayer);
                }
            }
            
            // Create player if start position was found
            if (foundPlayer)
            {
                player = new SimplePlayer(playerStartPos, content);
            }
            else
            {
                throw new Exception("Level must have a player start position (P)!");
            }
            
            if (exitPositions.Count == 0)
            {
                throw new Exception("Level must have at least one exit (X)!");
            }
        }
        
        /// <summary>
        /// Create a tile based on the character from the level file
        /// </summary>
        private LevelTile CreateTileFromChar(char tileChar, int x, int y, ref Vector2 playerStart, ref bool foundPlayer)
        {
            switch (tileChar)
            {
                case '.': // Empty space
                    return new LevelTile(null, TileCollisionType.Empty);
                
                case '#': // Wall/Floor (solid block)
                    // Randomly select one of the three block textures
                    int randomIndex = random.Next(3);
                    return new LevelTile(blockTextures[randomIndex], TileCollisionType.Solid);
                
                case 'P': // Player start
                    playerStart = new Vector2(x * TILE_SIZE + TILE_SIZE / 2, y * TILE_SIZE + TILE_SIZE / 2);
                    foundPlayer = true;
                    return new LevelTile(null, TileCollisionType.Empty);
                
                case 'X': // Exit
                    exitPositions.Add(new Vector2(x * TILE_SIZE, y * TILE_SIZE));
                    return new LevelTile(exitTexture, TileCollisionType.Exit);
                
                default:
                    // Unknown character, treat as empty
                    return new LevelTile(null, TileCollisionType.Empty);
            }
        }
        
        /// <summary>
        /// Get collision type at world position
        /// </summary>
        public TileCollisionType GetCollisionAt(Vector2 worldPosition)
        {
            int tileX = (int)(worldPosition.X / TILE_SIZE);
            int tileY = (int)(worldPosition.Y / TILE_SIZE);
            
            return GetCollisionAt(tileX, tileY);
        }
        
        /// <summary>
        /// Get collision type at tile coordinates
        /// </summary>
        public TileCollisionType GetCollisionAt(int tileX, int tileY)
        {
            // Outside level bounds
            if (tileX < 0 || tileX >= Width || tileY < 0 || tileY >= Height)
                return TileCollisionType.Solid; // Treat out of bounds as solid
            
            return tiles[tileX, tileY].CollisionType;
        }
        
        /// <summary>
        /// Get tile bounds in world space
        /// </summary>
        public Rectangle GetTileBounds(int tileX, int tileY)
        {
            return new Rectangle(tileX * TILE_SIZE, tileY * TILE_SIZE, TILE_SIZE, TILE_SIZE);
        }
        
        /// <summary>
        /// Update level logic
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (IsCompleted)
                return;
            
            // Update player
            player?.Update(gameTime, this);
            
            // Check if player reached any exit
            if (player != null && !IsCompleted)
            {
                Rectangle playerBounds = player.GetBounds();
                
                foreach (Vector2 exitPos in exitPositions)
                {
                    Rectangle exitBounds = new Rectangle((int)exitPos.X, (int)exitPos.Y, TILE_SIZE, TILE_SIZE);
                    
                    if (playerBounds.Intersects(exitBounds))
                    {
                        IsCompleted = true;
                        break;
                    }
                }
            }
        }
        
        /// <summary>
        /// Draw the level
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            // Draw tiles
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    LevelTile tile = tiles[x, y];
                    if (tile.Texture != null)
                    {
                        Vector2 position = new Vector2(x * TILE_SIZE, y * TILE_SIZE);
                        spriteBatch.Draw(tile.Texture, position, Color.White);
                    }
                }
            }
            
            // Draw player
            player?.Draw(spriteBatch);
        }
        
        /// <summary>
        /// Reset player to start position
        /// </summary>
        public void ResetPlayer()
        {
            player?.ResetToStart();
            IsCompleted = false;
        }
    }
    
    /// <summary>
    /// Represents a single tile in the level
    /// </summary>
    public class LevelTile
    {
        public Texture2D Texture { get; }
        public TileCollisionType CollisionType { get; }
        
        public LevelTile(Texture2D texture, TileCollisionType collisionType)
        {
            Texture = texture;
            CollisionType = collisionType;
        }
    }
    
    /// <summary>
    /// Types of tiles for collision detection
    /// </summary>
    public enum TileCollisionType
    {
        Empty,    // Player can move through
        Solid,    // Blocks player movement
        Exit      // Level completion trigger
    }
    
    /// <summary>
    /// Simple player class for text-based levels
    /// </summary>
    public class SimplePlayer
    {
        // Player properties
        private Vector2 position;
        private Vector2 startPosition;
        private Vector2 velocity;
        private bool isOnGround;
        private Texture2D texture;
        
        // Player constants
        private const float MOVE_SPEED = 200f;
        private const float JUMP_STRENGTH = 400f;
        private const float GRAVITY = 800f;
        private const float MAX_FALL_SPEED = 500f;
        
        // Player size
        private const int PLAYER_WIDTH = 32;
        private const int PLAYER_HEIGHT = 32;
        
        public Vector2 Position => position;
        public bool IsAlive { get; private set; } = true;
        
        /// <summary>
        /// Initialize player at given position
        /// </summary>
        public SimplePlayer(Vector2 startPos, ContentManager content)
        {
            startPosition = startPos;
            position = startPos;
            velocity = Vector2.Zero;
            isOnGround = false;
            
            // Load player texture (you'll need to create this)
            try
            {
                texture = content.Load<Texture2D>("Player");
            }
            catch
            {
                // If no player texture exists, we'll draw a colored rectangle
                texture = null;
            }
        }
        
        /// <summary>
        /// Update player logic
        /// </summary>
        public void Update(GameTime gameTime, Level level)
        {
            if (!IsAlive)
                return;
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            HandleInput(deltaTime);
            ApplyPhysics(deltaTime);
            HandleCollisions(level);
        }
        
        /// <summary>
        /// Handle player input
        /// </summary>
        private void HandleInput(float deltaTime)
        {
            var keyboardState = Microsoft.Xna.Framework.Input.Keyboard.GetState();
            
            // Horizontal movement
            if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Left) || 
                keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.A))
            {
                velocity.X = -MOVE_SPEED;
            }
            else if (keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Right) || 
                     keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.D))
            {
                velocity.X = MOVE_SPEED;
            }
            else
            {
                velocity.X = 0;
            }
            
            // Jumping
            if ((keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Space) || 
                 keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.Up) || 
                 keyboardState.IsKeyDown(Microsoft.Xna.Framework.Input.Keys.W)) && isOnGround)
            {
                velocity.Y = -JUMP_STRENGTH;
                isOnGround = false;
            }
        }
        
        /// <summary>
        /// Apply physics (gravity, movement)
        /// </summary>
        private void ApplyPhysics(float deltaTime)
        {
            // Apply gravity
            velocity.Y += GRAVITY * deltaTime;
            
            // Limit fall speed
            if (velocity.Y > MAX_FALL_SPEED)
                velocity.Y = MAX_FALL_SPEED;
            
            // Update position
            position += velocity * deltaTime;
        }
        
        /// <summary>
        /// Handle collisions with the level
        /// </summary>
        private void HandleCollisions(Level level)
        {
            Rectangle bounds = GetBounds();
            
            // Get the range of tiles the player overlaps
            int leftTile = (int)(bounds.Left / Level.TILE_SIZE);
            int rightTile = (int)(bounds.Right / Level.TILE_SIZE);
            int topTile = (int)(bounds.Top / Level.TILE_SIZE);
            int bottomTile = (int)(bounds.Bottom / Level.TILE_SIZE);
            
            // Reset ground state
            isOnGround = false;
            
            // Check collisions with solid tiles
            for (int y = topTile; y <= bottomTile; y++)
            {
                for (int x = leftTile; x <= rightTile; x++)
                {
                    TileCollisionType tileType = level.GetCollisionAt(x, y);
                    if (tileType == TileCollisionType.Solid)
                    {
                        Rectangle tileBounds = level.GetTileBounds(x, y);
                        HandleTileCollision(tileBounds);
                    }
                }
            }
        }
        
        /// <summary>
        /// Handle collision with a specific tile
        /// </summary>
        private void HandleTileCollision(Rectangle tileBounds)
        {
            Rectangle playerBounds = GetBounds();
            
            // Calculate overlap
            int overlapLeft = playerBounds.Right - tileBounds.Left;
            int overlapRight = tileBounds.Right - playerBounds.Left;
            int overlapTop = playerBounds.Bottom - tileBounds.Top;
            int overlapBottom = tileBounds.Bottom - playerBounds.Top;
            
            // Find the minimum overlap (collision direction)
            int minOverlap = Math.Min(Math.Min(overlapLeft, overlapRight), Math.Min(overlapTop, overlapBottom));
            
            if (minOverlap == overlapTop && velocity.Y > 0) // Landing on top
            {
                position.Y = tileBounds.Top - PLAYER_HEIGHT / 2;
                velocity.Y = 0;
                isOnGround = true;
            }
            else if (minOverlap == overlapBottom && velocity.Y < 0) // Hitting ceiling
            {
                position.Y = tileBounds.Bottom + PLAYER_HEIGHT / 2;
                velocity.Y = 0;
            }
            else if (minOverlap == overlapLeft && velocity.X > 0) // Hitting left side
            {
                position.X = tileBounds.Left - PLAYER_WIDTH / 2;
                velocity.X = 0;
            }
            else if (minOverlap == overlapRight && velocity.X < 0) // Hitting right side
            {
                position.X = tileBounds.Right + PLAYER_WIDTH / 2;
                velocity.X = 0;
            }
        }
        
        /// <summary>
        /// Get player bounding rectangle
        /// </summary>
        public Rectangle GetBounds()
        {
            return new Rectangle(
                (int)(position.X - PLAYER_WIDTH / 2),
                (int)(position.Y - PLAYER_HEIGHT / 2),
                PLAYER_WIDTH,
                PLAYER_HEIGHT);
        }
        
        /// <summary>
        /// Draw the player
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (!IsAlive)
                return;
            
            Rectangle destRect = GetBounds();
            
            if (texture != null)
            {
                spriteBatch.Draw(texture, destRect, Color.White);
            }
            else
            {
                // If no texture, draw a colored rectangle as placeholder
                // You'll need a 1x1 white pixel texture for this to work
                // spriteBatch.Draw(whitePixel, destRect, Color.Blue);
            }
        }
        
        /// <summary>
        /// Reset player to start position
        /// </summary>
        public void ResetToStart()
        {
            position = startPosition;
            velocity = Vector2.Zero;
            isOnGround = false;
            IsAlive = true;
        }
        
        /// <summary>
        /// Kill the player
        /// </summary>
        public void Kill()
        {
            IsAlive = false;
            velocity = Vector2.Zero;
        }
    }
}