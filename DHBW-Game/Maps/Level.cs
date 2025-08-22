using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using GameObjects.Player;

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
        private Player _player;
        private List<Vector2> exitPositions = new List<Vector2>();

        // Content and graphics
        private ContentManager content;
        private Texture2D[] blockTextures;
        private Texture2D exitTexture;

        // Level dimensions
        public int Width => tiles?.GetLength(0) ?? 0;
        public int Height => tiles?.GetLength(1) ?? 0;

        // Tile size (adjust based on your tile dimensions)
        public const int TILE_SIZE = 32;

        public bool IsCompleted { get; private set; }

        // Neue Property für die Startposition
        public Vector2 StartPosition { get; private set; }

        /// <summary>
        /// Initialize the level from a text file
        /// </summary>
        public Level(ContentManager contentManager, string levelName)
        {
            content = contentManager;
            LoadContent();
            LoadLevelFromText(Path.Combine(contentManager.RootDirectory, "Levels", levelName));
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
                _player = new Player(mass: 2f, isElastic: false);
                _player.Initialize(StartPosition); // Hier die StartPosition verwenden
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
                    StartPosition = playerStart; // Hier setzen wir die öffentliche StartPosition
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
        /// Update level logic
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (IsCompleted)
                return;

            // Update player
            _player?.Update(gameTime);

            // Check if player reached any exit
            if (_player != null && !IsCompleted)
            {
                const float PLAYER_COLLISION_RADIUS = 32f;

                foreach (Vector2 exitPos in exitPositions)
                {
                    Rectangle exitBounds = new Rectangle(
                        (int)exitPos.X,
                        (int)exitPos.Y,
                        TILE_SIZE,
                        TILE_SIZE
                    );

                    Vector2 playerPos = _player.Position;
                    bool playerInExitX = playerPos.X + PLAYER_COLLISION_RADIUS > exitBounds.Left &&
                                        playerPos.X - PLAYER_COLLISION_RADIUS < exitBounds.Right;
                    bool playerInExitY = playerPos.Y + PLAYER_COLLISION_RADIUS > exitBounds.Top &&
                                        playerPos.Y - PLAYER_COLLISION_RADIUS < exitBounds.Bottom;

                    if (playerInExitX && playerInExitY)
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
            _player?.Draw();
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
}