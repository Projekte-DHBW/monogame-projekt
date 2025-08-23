using DHBW_Game.GameObjects;
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameLibrary.Physics.Colliders;
using GameObjects.Player;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace DHBW_Game.Levels
{
    /// <summary>
    /// Simple text-based level class that creates tiles and basic collision from .txt files
    /// </summary>
    public class Level
    {
        private Tilemap _tilemap;
        private Tileset _tileset; // Add this field
        private Player _player;
        private List<Vector2> exitPositions = new List<Vector2>();
        private ContentManager content;
        public List<GameObject> Objects { get; private set; } = new List<GameObject>();

        public int Width => _tilemap?.Columns ?? 0;
        public int Height => _tilemap?.Rows ?? 0;
        public bool IsCompleted { get; private set; }
        public Vector2 StartPosition { get; private set; }

        /// <summary>
        /// Initialize the level from a text file
        /// </summary>
        public Level(ContentManager contentManager, string levelName)
        {
            content = contentManager;
            LoadContent();
            LoadLevel(Path.Combine(contentManager.RootDirectory, "Levels", levelName));
        }

        /// <summary>
        /// Load textures and other content
        /// </summary>
        private void LoadContent()
        {
            // Load tileset texture
            Texture2D tilesetTexture = content.Load<Texture2D>("Tiles/TilesetA");
            // Create texture region for the entire tileset
            TextureRegion tilesetRegion = new TextureRegion(tilesetTexture, 0, 0, tilesetTexture.Width, tilesetTexture.Height);
            // Create tileset with 32x32 tiles
            _tileset = new Tileset(tilesetRegion, Tiles.TILE_SIZE, Tiles.TILE_SIZE);
            
            // Initialize empty tilemap (will be populated in LoadLevel)
            _tilemap = new Tilemap(_tileset, 0, 0);
        }

        /// <summary>
        /// Load level from text file
        /// </summary>
        private void LoadLevel(string levelPath)
        {
            // Get physics engine once
            var physicsEngine = ServiceLocator.Get<PhysicsEngine>();

            // Clear all physics components and colliders
            physicsEngine.ClearComponents();  // You'll need to add this method to PhysicsEngine
            physicsEngine.CollisionEngine.ClearColliders();
            
            // Clear lists and references
            Objects.Clear();
            exitPositions.Clear();
            _player = null;

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

                // Create new tilemap with correct dimensions using the stored tileset
            _tilemap = new Tilemap(_tileset, width, height);

            // Parse level data and set tiles
            bool foundPlayer = false;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    char tileChar = lines[y][x];
                    int tileId = GetTileIdFromChar(tileChar);
                    _tilemap.SetTile(x, y, tileId);
                    if(tileId == 1)
                    {
                        TestSegment obj = new TestSegment(Tiles.TILE_SIZE, Tiles.TILE_SIZE, 0, isElastic: false, frictionCoefficient: 1f);
                        obj.Initialize(new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2, y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2));
                        Objects.Add(obj);
                    }
                    

                    // Handle special tiles
                    switch (tileChar)
                    {
                        case 'P': // Player start
                            StartPosition = new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2, y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2);
                            foundPlayer = true;
                            break;
                        case 'X': // Exit
                            exitPositions.Add(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            break;
                    }                   
                }
            }


            // Create player if start position was found
            if (foundPlayer)
            {
                _player = new Player(mass: 2f, isElastic: false);
                _player.Initialize(StartPosition);
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
        /// Get the tile ID corresponding to a character in the level file
        /// </summary>
        private int GetTileIdFromChar(char tileChar)
        {
            return tileChar switch
            {
                '.' => Tiles.EMPTY_TILE,  // Empty space
                '#' => Tiles.SOLID_TILE,  // Wall/Floor
                'P' => Tiles.PLAYER_START,  // Player start (empty tile)
                'X' => Tiles.EXIT_TILE,  // Exit
                _ => Tiles.EMPTY_TILE     // Default to empty
            };
        }

        /// <summary>
        /// Update level logic
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (IsCompleted)
                return;

            foreach (var obj in Objects)
            {
                obj.Update(gameTime);
            }

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
                        Tiles.TILE_SIZE,
                        Tiles.TILE_SIZE
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
            _tilemap.Draw(spriteBatch);
            _player?.Draw();
        }
    }
}