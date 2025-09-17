using DHBW_Game.GameObjects;
using GameLibrary;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using GameLibrary.Physics;
using GameObjects.Enemy;
using GameObjects.Player;
using GameObjects.Static_Sprites.Door_Open;
using GameObjects.Static_Sprites.Door_Closed;
using GameObjects.Static_Sprites.Fire_Extinguisher;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using GameObjects.Static_Sprites.Whiteboard;
using GameObjects.Static_Sprites.Elevator;
using GameObjects.Static_Sprites.Desk;
using GameLibrary.Rendering;
using GameObjects;

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
        private ContentManager _content;
        private Texture2D _backgroundTexture; // Background image
        private Rectangle _backgroundDestination; // Size and position of the background
        private Camera _camera = ServiceLocator.Get<Camera>();

        public List<GameObject> Segments { get; private set; } = new List<GameObject>();
        public List<Enemy> Enemys { get; private set; } = new List<Enemy>();
        public List<GameObject> BackgroundSprites { get; private set; } = new List<GameObject>();
        public List<GameObject> MoveableObjects { get; private set; } = new List<GameObject>();

        public int Width => _tilemap?.Columns ?? 0;
        public int Height => _tilemap?.Rows ?? 0;
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

            // Load background image
            try
            {
                _backgroundTexture = _content.Load<Texture2D>("Backgrounds/background");
            }
            catch (ContentLoadException)
            {
                // If the background image is not found, set it to null
                _backgroundTexture = null;
            }
        }

        /// <summary>
        /// Load level from text file
        /// </summary>
        private void LoadLevel(string levelPath)
        {
            // Get physics engine once
            var physicsEngine = ServiceLocator.Get<PhysicsEngine>();

            // Clear all physics components and colliders
            physicsEngine.ClearComponents();
            physicsEngine.CollisionEngine.ClearColliders();

            // Clear lists and references
            Segments.Clear();
            Enemys.Clear();
            BackgroundSprites.Clear();
            MoveableObjects.Clear();
            _player = null;

            // Define bracket pairs
            var bracketPairs = new Dictionary<char, char>
            {
                { '[', ']' },
                { '(', ')' },
                { '{', '}' }
            };

            // Collect starts and ends for each opening bracket
            var starts = new Dictionary<char, List<(int x, int y)>>();
            var ends = new Dictionary<char, List<(int x, int y)>>();
            foreach (var open in bracketPairs.Keys)
            {
                starts[open] = new List<(int x, int y)>();
                ends[open] = new List<(int x, int y)>();
            }

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

            // Create new tilemap with correct dimensions (all empty by default—no need to set tiles)
            _tilemap = new Tilemap(_tileset, width, height);

            if (_backgroundTexture != null)
            {
                float scaleFactor = 3f;
                int scaledWidth = (int)(_backgroundTexture.Width * scaleFactor);
                int scaledHeight = (int)(_backgroundTexture.Height * scaleFactor);

                int offsetX = -(scaledWidth - _backgroundTexture.Width) / 3;
                int offsetY = -(scaledHeight - _backgroundTexture.Height) / 3;

                _backgroundDestination = new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);
            }

            // Store tile IDs for collision merging (only solids matter)
            int[,] tileIds = new int[height, width];

            // Parse level data
            bool foundPlayer = false;
            for (int y = 0; y < height; y++)
            {
                if (lines[y].Length != width)
                    throw new Exception($"Inconsistent row width at line {y}.");

                for (int x = 0; x < width; x++)
                {
                    char tileChar = lines[y][x];

                    // Set collision ID: only '#' is solid, everything else empty
                    tileIds[y, x] = (tileChar == '#') ? Tiles.SOLID_TILE : Tiles.EMPTY_TILE;

                    // Handle slope markers
                    if (bracketPairs.ContainsKey(tileChar))
                    {
                        starts[tileChar].Add((x, y));
                    }
                    else if (bracketPairs.ContainsValue(tileChar))
                    {
                        // Find the opening bracket for this closing
                        char opening = '\0';
                        foreach (var pair in bracketPairs)
                        {
                            if (pair.Value == tileChar)
                            {
                                opening = pair.Key;
                                break;
                            }
                        }
                        if (opening != '\0')
                        {
                            ends[opening].Add((x, y));
                        }
                    }

                    switch (tileChar)
                    {
                        case 'P':
                            StartPosition = new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2f,
                                                        y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2f);
                            foundPlayer = true;
                            break;
                        case 'D': // Lecturers
                            Enemy enemy = new Professor("berninger", mass: 1.5f, isElastic: false);
                            enemy.Initialize(new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2, y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2));
                            Enemys.Add(enemy);
                            break;
                        case 'W': // Lecturers
                            enemy = new Professor("schwenker", mass: 1.5f, isElastic: false);
                            enemy.Initialize(new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2, y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2));
                            Enemys.Add(enemy);
                            break;
                        case 'S': // Student
                            enemy = new Student(mass: 1f, isElastic: false);
                            enemy.Initialize(new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2, y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2));
                            Enemys.Add(enemy);
                            break;
                        case 'E': // Exit Elevator Sprite
                            Elevator exitElevator = new Elevator();
                            exitElevator.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            BackgroundSprites.Add(exitElevator);
                            break;
                        case 'M': // Startpoint Elevator Sprite, initially open
                            Elevator startElevator = new Elevator(false);
                            startElevator.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE +28));
                            BackgroundSprites.Add(startElevator);
                            break;
                        case 'O': // Door Open Sprite
                            Door_Open openDoor = new Door_Open();
                            openDoor.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            BackgroundSprites.Add(openDoor);
                            break;
                        case 'C': // Door closed Sprite
                            Door_Closed closedDoor = new Door_Closed();
                            closedDoor.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            BackgroundSprites.Add(closedDoor);
                            break;
                        case 'F': // Fire Extinguisher Sprite
                            Fire_Extinguisher fireExtinguisher = new Fire_Extinguisher();
                            fireExtinguisher.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE - 15));
                            BackgroundSprites.Add(fireExtinguisher);
                            break;
                        case 'G': // End, Goal
                            Goal goal = new Goal();
                            goal.Initialize(new Vector2(x * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2, y * Tiles.TILE_SIZE + Tiles.TILE_SIZE / 2));
                            BackgroundSprites.Add(goal);
                            break;
                        case '1': // Whiteboard 1 Sprite
                            Whiteboard whiteboard = new Whiteboard();
                            whiteboard.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            BackgroundSprites.Add(whiteboard);
                            break;
                        case '2': // Whiteboard 2 Sprite
                            Whiteboard whiteboard2 = new Whiteboard("whiteboard2");
                            whiteboard2.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            BackgroundSprites.Add(whiteboard2);
                            break;
                        case '3': // Whiteboard 3 Sprite
                            Whiteboard whiteboard3 = new Whiteboard("whiteboard3");
                            whiteboard3.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            BackgroundSprites.Add(whiteboard3);
                            break;
                        case 'T': // Desk Sprite
                            Desk desk = new Desk(mass: 2f, isElastic: false);
                            desk.Initialize(new Vector2(x * Tiles.TILE_SIZE, y * Tiles.TILE_SIZE));
                            MoveableObjects.Add(desk);
                            break;
                    }
                }
            }

            // Process slopes for each bracket type
            foreach (var opening in starts.Keys)
            {
                var sList = starts[opening];
                var eList = ends[opening];

                if (sList.Count != eList.Count)
                    throw new Exception($"Mismatched slope markers for {opening}-{bracketPairs[opening]}! Number of opens: {sList.Count}, closes: {eList.Count}");

                // Sort starts and ends by x position (assuming left-to-right slopes; y as tiebreaker)
                sList.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
                eList.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

                for (int i = 0; i < sList.Count; i++)
                {
                    var s = sList[i];
                    var e = eList[i];

                    // If start x > end x, swap to ensure consistent direction
                    if (s.x > e.x)
                    {
                        var temp = s;
                        s = e;
                        e = temp;
                    }

                    int tileSize = Tiles.TILE_SIZE;
                    float dx = (e.x - s.x) * tileSize;
                    float dy = (e.y - s.y) * tileSize;
                    float length = (float)Math.Sqrt(dx * dx + dy * dy);
                    float rotation = (float)(Math.Atan2(dy, dx) * 180 / Math.PI);

                    Vector2 startPos = new Vector2(s.x * tileSize + tileSize / 2f, s.y * tileSize + tileSize / 2f);
                    Vector2 endPos = new Vector2(e.x * tileSize + tileSize / 2f, e.y * tileSize + tileSize / 2f);
                    Vector2 center = (startPos + endPos) / 2f;

                    float thickness = Tiles.TILE_SIZE;

                    // Generate tiled texture
                    int targetWidth = (int)Math.Ceiling(length); // Round up to avoid gaps
                    int targetHeight = (int)thickness;
                    Texture2D tiledTexture = CreateTiledTexture(targetWidth, targetHeight);
                    var seg = new Segment((int)length, (int)thickness, rotation, isElastic: false, frictionCoefficient: 0.1f, tiledTexture); // Use lower friction for slopes
                    seg.Initialize(center);
                    Segments.Add(seg);
                }
            }

            if (!foundPlayer)
                throw new Exception("No player start position found in level file!");

            // Build optimized colliders for solid tiles
            BuildOptimizedRectangleCover(tileIds, width, height, Tiles.SOLID_TILE);

            // Create player
            _player = new Player(mass: 2f, isElastic: false);
            _player.Initialize(StartPosition);
        }

        /// <summary>
        /// Creates a tiled texture for a segment of given dimensions.
        /// Uses the solid tile from the tileset to fill the area in a grid pattern.
        /// </summary>
        private Texture2D CreateTiledTexture(int widthPx, int heightPx)
        {
            RenderTarget2D rt = new RenderTarget2D(Core.GraphicsDevice, widthPx, heightPx);
            Core.GraphicsDevice.SetRenderTarget(rt);
            Core.GraphicsDevice.Clear(Color.Transparent);

            using (SpriteBatch tempBatch = new SpriteBatch(Core.GraphicsDevice))
            {
                tempBatch.Begin();

                // Get the solid tile region from tileset
                TextureRegion tileRegion = _tileset.GetTile(Tiles.SOLID_TILE);
                int tileSizeInt = Tiles.TILE_SIZE;

                // Tile in a 2D grid across the width and height
                int numTilesX = (int)Math.Ceiling((double)widthPx / tileSizeInt);
                int numTilesY = (int)Math.Ceiling((double)heightPx / tileSizeInt);
                for (int tileY = 0; tileY < numTilesY; tileY++)
                {
                    for (int tileX = 0; tileX < numTilesX; tileX++)
                    {
                        Vector2 tilePos = new Vector2(tileX * tileSizeInt, tileY * tileSizeInt);
                        tempBatch.Draw(tileRegion.Texture, tilePos, tileRegion.SourceRectangle, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
                    }
                }

                tempBatch.End();
            }

            Core.GraphicsDevice.SetRenderTarget(null);
            return rt;
        }

        /// <summary>
        /// Update level logic
        /// </summary>
        public void Update(GameTime gameTime)
        {
            foreach (var seg in Segments)
            {
                seg.Update(gameTime);
            }

            foreach (var enemy in Enemys)
            {
                enemy.Update(gameTime);
            }

            foreach (var sprite in BackgroundSprites)
            {
                sprite.Update(gameTime);
            }

            foreach (var sprite in MoveableObjects)
            {
                sprite.Update(gameTime);
            }

            // Update player
            _player?.Update(gameTime);
        }

        /// <summary>
        /// Draw the level
        /// </summary>
        public void Draw(SpriteBatch spriteBatch)
        {
            if (_backgroundTexture != null)
            {

                _camera.Draw(
                    spriteBatch,
                    _backgroundTexture,
                    Vector2.Zero,
                    null,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    new Vector2((float)_backgroundDestination.Width / _backgroundTexture.Width,
                               (float)_backgroundDestination.Height / _backgroundTexture.Height),
                    SpriteEffects.None,
                    1.0f
                );
            }

            _tilemap.Draw(spriteBatch);

            foreach (var seg in Segments)
            {
                seg.Draw();
            }

            foreach (var sprite in BackgroundSprites)
            {
                sprite.Draw();
            }

            foreach (var sprite in MoveableObjects)
            {
                sprite.Draw();
            }

            foreach (var enemy in Enemys)
            {
                enemy.Draw();
            }

            _player?.Draw();
        }

        private void BuildOptimizedRectangleCover(int[,] tileIds, int width, int height, int solidId)
        {
            // Mask: true = not yet covered
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

            // Repeat until all SOLID tiles are covered
            while (remaining > 0)
            {
                // Find the largest rectangle of true cells
                if (!LargestRectangleInMask(mask, width, height,
                        out int bestTop, out int bestLeft,
                        out int bestH, out int bestW, out int bestArea))
                {
                    // Fallback (should not happen): take the first remaining cell
                    FindFirstTrue(mask, width, height, out bestTop, out bestLeft);
                    bestH = 1;
                    bestW = 1;
                    bestArea = 1;
                }

                // Create collider
                CreateColliderForRectangle(bestLeft, bestTop, bestW, bestH);

                // Mark area as covered
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

            Texture2D tiledTexture = CreateTiledTexture(wPx, hPx);
            var seg = new Segment(wPx, hPx, 0f, isElastic: false, frictionCoefficient: 1f, tiledTexture);
            seg.Initialize(new Vector2(centerX, centerY));
            Segments.Add(seg);
        }

        private bool LargestRectangleInMask(bool[,] mask, int width, int height,
            out int bestTop, out int bestLeft, out int bestHeight, out int bestWidth, out int bestArea)
        {
            // Histogram heights
            int[] heights = new int[width];
            bestArea = 0;
            bestTop = bestLeft = bestHeight = bestWidth = 0;

            for (int y = 0; y < height; y++)
            {
                // Update histogram
                for (int x = 0; x < width; x++)
                {
                    heights[x] = mask[y, x] ? heights[x] + 1 : 0;
                }

                // Largest rectangle in the histogram of this row
                // Standard stack algorithm
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