using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml.Schema;
using GameLibrary.Entities;
using GameLibrary.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using DHBW_Game.GameObjects;

namespace DHBW_Game.Maps;

public class Map
{
    /// <summary>
    /// Gets the background tilemap for this map.
    /// </summary>
    public Tilemap Background { get; private set; }
    
    /// <summary>
    /// Gets the starting position for the character in this map.
    /// </summary>
    public Vector2 StartPosition { get; private set; }
    
    /// <summary>
    /// Gets the list of game objects placed in this map.
    /// </summary>
    public List<GameObject> Objects { get; private set; } = new List<GameObject>();
    
    /// <summary>
    /// Creates a new map based on a map XML configuration file.
    /// </summary>
    /// <param name="content">The content manager used to load textures and other assets.</param>
    /// <param name="filename">The path to the XML file, relative to the content root directory.</param>
    /// <returns>The map created by this method.</returns>
    public static Map FromFile(ContentManager content, string filename)
    {
        string filePath = Path.Combine(content.RootDirectory, filename);
    
        using (Stream stream = TitleContainer.OpenStream(filePath))
        {
            XDocument doc = XDocument.Load(stream);
            XElement root = doc.Root;

            if (root == null || root.Name != "Map")
            {
                throw new XmlSchemaException("Missing or invalid root element! Expected <Map>.");
            }

            Map map = new Map();
            Vector2 startPos = Vector2.Zero;
            Tilemap background = null;
            List<GameObject> objects = new List<GameObject>();

            // Parse StartPosition
            XElement startElem = root.Element("StartPosition");
            if (startElem != null)
            {
                float x = float.TryParse(startElem.Attribute("x")?.Value, out float parsedX) ? parsedX : 0f;
                float y = float.TryParse(startElem.Attribute("y")?.Value, out float parsedY) ? parsedY : 0f;
                startPos = new Vector2(x, y);
            }

            // Parse Tilemap using Tilemap.FromXmlElement
            XElement tilemapElem = root.Element("Tilemap");
            if (tilemapElem != null)
            {
                background = Tilemap.FromXmlElement(content, tilemapElem);
            }

            // Parse Objects
            XElement objectsElem = root.Element("Objects");
            if (objectsElem != null)
            {
                foreach (XElement objElem in objectsElem.Elements("Object"))
                {
                    string type = objElem.Attribute("type")?.Value;
                    if (string.IsNullOrEmpty(type)) continue;

                    float ox = float.TryParse(objElem.Attribute("x")?.Value, out float parsedOx) ? parsedOx : 0f;
                    float oy = float.TryParse(objElem.Attribute("y")?.Value, out float parsedOy) ? parsedOy : 0f;
                    Vector2 pos = new Vector2(ox, oy);

                    GameObject obj = null;
                    switch (type)
                    {
                        case "TestCharacter":
                            float tcMass = float.TryParse(objElem.Attribute("mass")?.Value, out float parsedTcMass) ? parsedTcMass : 1f;
                            bool tcElastic = bool.TryParse(objElem.Attribute("elastic")?.Value, out bool parsedTcElastic) && parsedTcElastic;
                            obj = new TestCharacter(tcMass, tcElastic);
                            break;
                        case "CircleColliderTest":
                            float ccMass = float.TryParse(objElem.Attribute("mass")?.Value, out float parsedCcMass) ? parsedCcMass : 1f;
                            bool ccElastic = bool.TryParse(objElem.Attribute("elastic")?.Value, out bool parsedCcElastic) && parsedCcElastic;
                            obj = new CircleColliderTest(ccMass, ccElastic);
                            break;
                        case "RectangleColliderTest":
                            float rcMass = float.TryParse(objElem.Attribute("mass")?.Value, out float parsedRcMass) ? parsedRcMass : 1f;
                            bool rcElastic = bool.TryParse(objElem.Attribute("elastic")?.Value, out bool parsedRcElastic) && parsedRcElastic;
                            obj = new RectangleColliderTest(rcMass, rcElastic);
                            break;
                        case "TestSegment":
                            int tsWidth = int.TryParse(objElem.Attribute("width")?.Value, out int parsedTsWidth) ? parsedTsWidth : 0;
                            int tsHeight = int.TryParse(objElem.Attribute("height")?.Value, out int parsedTsHeight) ? parsedTsHeight : 0;
                            float tsRotation = float.TryParse(objElem.Attribute("rotation")?.Value, out float parsedTsRotation) ? parsedTsRotation : 0f;
                            bool tsElastic = bool.TryParse(objElem.Attribute("elastic")?.Value, out bool parsedTsElastic) && parsedTsElastic;
                            float tsFrictionCoefficient = float.TryParse(objElem.Attribute("frictionCoefficient")?.Value, out float parsedTsFrictionCoefficient) ? parsedTsFrictionCoefficient : 1f;
                            obj = new TestSegment(tsWidth, tsHeight, tsRotation, tsElastic, tsFrictionCoefficient);
                            break;
                        default:
                            // Unknown type;
                            break;
                    }

                    if (obj != null)
                    {
                        obj.Initialize(pos);
                        objects.Add(obj);
                    }
                }
            }

            map.Background = background;
            map.StartPosition = startPos;
            map.Objects = objects;

            return map;
        }
    }
    
    /// <summary>
    /// Updates all game objects in this map.
    /// </summary>
    /// <param name="gameTime">A snapshot of the timing values for the current update cycle.</param>
    public void Update(GameTime gameTime)
    {
        foreach (var obj in Objects)
        {
            obj.Update(gameTime);
        }
    }
    
    /// <summary>
    /// Draws the background tilemap and all game objects in this map.
    /// </summary>
    /// <param name="spriteBatch">The sprite batch used to draw this map.</param>
    public void Draw(SpriteBatch spriteBatch)
    {
        Background?.Draw(spriteBatch);
        
        foreach (var obj in Objects)
        {
            obj.Draw();
        }
    }
}