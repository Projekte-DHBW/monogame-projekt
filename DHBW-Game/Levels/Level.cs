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
        private Tileset _tileset;
        private Player _player;
        private List<Vector2> _exitPositions = new List<Vector2>();
        private ContentManager _content;
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
            _content = contentManager;
            LoadContent();
            LoadLevel(Path.Combine(contentManager.RootDirectory, "Levels", levelName));
        }

        /// <summary>
        /// Load textures and other content
        /// </summary>
        private void LoadContent()
        {
            // Load tileset texture
            Texture2D tilesetTexture = _content.Load<Texture2D>("Tiles/TilesetA");
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
            _exitPositions.Clear();
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

            // Store tile IDs for second pass rectangle merging
            int[,] tileIds = new int[height, width];


            // Parse level data and set tiles
            bool foundPlayer = false;
            for (int y = 0; y < height; y++)
            {
                if (lines[y].Length != width)
                    throw new Exception($"Inconsistent row width at line {y}.");

                for (int x = 0; x < width; x++)
                {
                    char tileChar = lines[y][x];

                    switch (tileChar)
                    {
                        case 'P':
                            StartPosition = new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2f,
                                                        y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2f);
                            foundPlayer = true;
                            break;
                        case 'X':
                            _exitPositions.Add(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            break;
                    }

                    int id = GetTileIdFromChar(tileChar);
                    tileIds[y, x] = id;
                    _tilemap.SetTile(x, y, id);
                }
            }

            if (!foundPlayer)
                throw new Exception("Level must have a player start position (P)!");
            if (_exitPositions.Count == 0)
                throw new Exception("Level must have at least one exit (X)!");

            // Build merged solid rectangles & create colliders
            BuildOptimizedRectangleCover(tileIds, width, height, Tiles.SOLID_TILE);

            // Create player
            _player = new Player(mass: 2f, isElastic: false);
            _player.Initialize(StartPosition);
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

                foreach (Vector2 exitPos in _exitPositions)
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

        private void BuildOptimizedRectangleCover(int[,] tileIds, int width, int height, int solidId)
        {
            // Maske: true = noch nicht abgedeckt
            bool[,] mask = new bool[height, width];
            int remaining = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (tileIds[y, x] == solidId)
                    {
                        mask[y, x] = true;
                        remaining++;
                    }
                }
            }

            if (remaining == 0)
                return;

            // Wiederhole bis alle SOLID Tiles abgedeckt
            while (remaining > 0)
            {
                // Finde größtes Rechteck aus True-Zellen
                if (!LargestRectangleInMask(mask, width, height,
                        out int bestTop, out int bestLeft,
                        out int bestH, out int bestW, out int bestArea))
                {
                    // Fallback (sollte nicht passieren): nimm erste verbliebene Zelle
                    FindFirstTrue(mask, width, height, out bestTop, out bestLeft);
                    bestH = 1;
                    bestW = 1;
                    bestArea = 1;
                }

                // Erstelle Collider
                CreateColliderForRectangle(bestLeft, bestTop, bestW, bestH);

                // Markiere Bereich als abgedeckt
                for (int dy = 0; dy < bestH; dy++)
                {
                    for (int dx = 0; dx < bestW; dx++)
                    {
                        if (mask[bestTop + dy, bestLeft + dx])
                        {
                            mask[bestTop + dy, bestLeft + dx] = false;
                            remaining--;
                        }
                    }
                }

                // (Optional) Early exit Heuristik; hier nicht nötig
            }
        }

        private void CreateColliderForRectangle(int leftTile, int topTile, int widthTiles, int heightTiles)
        {
            int tileSize = Tiles.TILE_SIZE;
            int wPx = widthTiles * tileSize;
            int hPx = heightTiles * tileSize;

            // Center
            float centerX = (leftTile + widthTiles / 2f) * tileSize;
            float centerY = (topTile + heightTiles / 2f) * tileSize;

            var seg = new TestSegment(wPx, hPx, 0f, isElastic: false, frictionCoefficient: 1f);
            seg.Initialize(new Vector2(centerX, centerY));
            Objects.Add(seg);
        }

        private bool LargestRectangleInMask(bool[,] mask, int width, int height,
            out int bestTop, out int bestLeft, out int bestHeight, out int bestWidth, out int bestArea)
        {
            // Histogram Höhen
            int[] heights = new int[width];
            bestArea = 0;
            bestTop = bestLeft = bestHeight = bestWidth = 0;

            for (int y = 0; y < height; y++)
            {
                // Update Histogramm
                for (int x = 0; x < width; x++)
                {
                    heights[x] = mask[y, x] ? heights[x] + 1 : 0;
                }

                // Größtes Rechteck in Histogramm dieser Zeile
                // Standard Stack-Algorithmus
                Stack<int> stack = new();
                int xIdx = 0;
                while (xIdx <= width)
                {
                    int currHeight = (xIdx == width) ? 0 : heights[xIdx];
                    if (stack.Count == 0 || currHeight >= heights[stack.Peek()])
                    {
                        stack.Push(xIdx++);
                    }
                    else
                    {
                        int top = stack.Pop();
                        int heightRect = heights[top];
                        int right = xIdx - 1;
                        int left = stack.Count == 0 ? 0 : stack.Peek() + 1;
                        int widthRect = right - left + 1;
                        int area = heightRect * widthRect;
                        if (area > bestArea)
                        {
                            bestArea = area;
                            bestHeight = heightRect;
                            bestWidth = widthRect;
                            int bottomRow = y;
                            bestTop = bottomRow - heightRect + 1;
                            bestLeft = left;
                        }
                    }
                }
            }

            return bestArea > 0;
        }

        private void FindFirstTrue(bool[,] mask, int width, int height, out int top, out int left)
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (mask[y, x])
                    {
                        top = y;
                        left = x;
                        return;
                    }
            top = left = 0;
        }
    }
}