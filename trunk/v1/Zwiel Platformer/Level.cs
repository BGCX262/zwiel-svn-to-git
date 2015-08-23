using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;
using System.Xml;
using System.IO;

namespace Zwiel_Platformer
{
    /// <summary>
    /// A uniform grid of tiles with collections of items and enemies.
    /// The level owns the player and controls the game's win and lose
    /// conditions as well as scoring.
    /// </summary>
    class Level : IDisposable
    {
        // Physical structure of the level.
        private Tile[,] tiles;
        private Layer[] layers;
        private float cameraPositionXAxis;
        public float cameraPositionYAxis; 
        // The layer which entities are drawn on top of.
        private const int EntityLayer = 2;
        private int ViewportWidth = -1;
        const float ViewMargin = 0.35f;
        const float TopMargin = 0.3f;
        const float BottomMargin = 0.1f;
        Rectangle levelSize;

        List<StaticTrap> m_trapStatic = new List<StaticTrap>();
        List<RisingTrap> m_trapRising = new List<RisingTrap>();
        List<ShootingTrap> m_trapShooting = new List<ShootingTrap>();
        List<FallingTrap> m_trapFalling = new List<FallingTrap>();

        // Entities in the level.
        public Player Player
        {
            get { return player; }
        }
        Player player;

        public string LevelName { get; private set; }

        List<Projectile> m_proj = new List<Projectile>();

        private List<Item> items;
        private List<Enemy> enemies = new List<Enemy>();

        // Key locations in the level.        
        private Dictionary<Vector2, int> start = new Dictionary<Vector2, int>();
        private Vector2 chosenStart;

        public string NextLevel { get; private set; }
        string defDest = null;

        // Level game state.

        public int Score
        {
            get { return score; }
            set { score = value; }
        }
        int score;

        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
            set { timeRemaining = value; }
        }
        TimeSpan timeRemaining;

        private const int PointsPerSecond = 5;

        // Level content.        
        public ContentLoader Content { get; private set; }
        public ContentManager ContentManager { get; private set; }

        private SoundEffect exitReachedSound;

        private Dictionary<Point, string> exits = new Dictionary<Point, string>();
        public List<Projectile> Projectiles
        {
            get { return m_proj; }
            set { m_proj = value; }
        }

        #region Loading

        /// <summary>
        /// Constructs a new level.
        /// </summary>
        /// <param name="serviceProvider">
        /// The service provider that will be used to construct a ContentManager.
        /// </param>
        /// <param name="path">
        /// The absolute path to the level file to be loaded.
        /// </param>
        public Level(IServiceProvider serviceProvider, string path, int score, int pHP, int pMaxHP)
        {
            // Create a new content manager to load content used just by this level.
            ContentManager = new ContentManager(serviceProvider, "Content");
            Content = new ContentLoader(this);

            timeRemaining = TimeSpan.FromMinutes(1.0);

            NextLevel = null;
            LoadTiles(path, pHP, pMaxHP);
            levelSize = new Rectangle(0, 0, Width * Tile.Width, Height * Tile.Height);

            // Load background layer textures. For now, all levels must
            // use the same backgrounds and only use the left-most part of them.
            layers = new Layer[3];
            layers[0] = new Layer(Content, "Backgrounds/Layer0", 0.2f);
            layers[1] = new Layer(Content, "Backgrounds/Layer1", 0.5f);
            layers[2] = new Layer(Content, "Backgrounds/Layer2", 0.8f);

            // Load sounds.
            this.score = score;
            exitReachedSound = Content.LoadSoundEffect("Sounds/ExitReached");
        }

        private void LoadXMLLevel(string path)
        {
            using (XmlReader reader = XmlReader.Create(path))
            {
                string name;
                bool started = false;
                int width, height;
                while (reader.Read())
                {
                    XmlNodeType type = reader.MoveToContent();
                    if (type == XmlNodeType.Element)
                    {
                        name = reader.Name.ToLower();

                        if (name == "level")
                        {
                            #region LoadGeneralData
                            if (started)
                                throw new Exception("'level' nodes cannot be nested");
                            started = true;
                            width = int.Parse(reader.GetAttribute("width"));
                            height = int.Parse(reader.GetAttribute("height"));
                            LevelName = reader.GetAttribute("name");
                            timeRemaining = TimeSpan.FromSeconds(double.Parse(reader.GetAttribute("time")));
                            if (width < 20)
                                throw new Exception("Level width must be at least 20");
                            tiles = new Tile[width, height];
                            items = new List<Item>();
                            #endregion
                        }
                        else if (!started)
                            throw new Exception("The 'level' node must be instantiated.");
                        else if (name == "tile")
                        {
                            #region LoadTileData
                            string tType = reader.GetAttribute("type");
                            if (!Directory.Exists("Content/Tiles/" + tType.ToLower()))
                                throw new Exception("Tile type '" + tType + "' doesn't exist.");
                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            tiles[x, y] = Content.LoadTile(tType);
                            #endregion
                        }
                        else if (name == "exit")
                        {
                            #region LoadExitData
                            string nextLevel = reader.GetAttribute("goTo");
                            if (nextLevel == null)
                                throw new Exception("Each exit must contain a attribute 'goTo'");
                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            if (tiles[x, y].Texture != null)
                                throw new Exception("A exit cannot exist where a tile already is.");
                            tiles[x, y] = new Tile(Content.LoadTexture2D("Tiles/Exit"), TileCollision.Passable);
                            exits.Add(GetBounds(x, y).Center, nextLevel);
                            #endregion
                        }
                        else if (name == "playerstart")
                        {
                            #region LoadPlayerStartData
                            int chance = int.Parse(reader.GetAttribute("chance"));
                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            start.Add(RectangleExtensions.GetBottomCenter(GetBounds(x, y)), chance);
                            #endregion
                        }
                        else if (name == "gem")
                        {
                            #region LoadGemData
                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            Point position = GetBounds(x, y).Center;
                            if (tiles[x, y].Collision == TileCollision.Impassable)
                                throw new Exception("A gem at location '" + location[0] + " - " + location[1] + "' cannot be reached by the player.");
                            items.Add(Content.LoadGem(reader.GetAttribute("type"), new Vector2(position.X, position.Y)));
                            string ptStr = reader.GetAttribute("points");
                            if (ptStr != null)
                                items[items.Count - 1].PointValue = int.Parse(ptStr);
                            string clrStr = reader.GetAttribute("tint");
                            if (clrStr != null)
                                items[items.Count - 1].Color = ContentLoader.ParseColor(clrStr);
                            #endregion
                        }
                        else if (name == "timebonus")
                        {
                            #region LoadTimeBonusData
                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            Point position = GetBounds(x, y).Center;
                            if (tiles[x, y].Collision == TileCollision.Impassable)
                                throw new Exception("A time bonus at location '" + location[0] + " - " + location[1] + "' cannot be reached by the player.");
                            items.Add(Content.LoadTimeBonus(reader.GetAttribute("type"), new Vector2(position.X, position.Y)));
                            string ptStr = reader.GetAttribute("points");
                            if (ptStr != null)
                                items[items.Count - 1].PointValue = int.Parse(ptStr);
                            string clrStr = reader.GetAttribute("tint");
                            if (clrStr != null)
                                items[items.Count - 1].Color = ContentLoader.ParseColor(clrStr);
                            string bonusStr = reader.GetAttribute("bonus");
                            if (bonusStr != null)
                                ((TimeBonus)items[items.Count - 1]).Bonus = int.Parse(bonusStr);
                            #endregion
                        }
                        else if (name == "healthpack")
                        {
                            #region LoadHealthPackData
                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            Point position = GetBounds(x, y).Center;
                            if (tiles[x, y].Collision == TileCollision.Impassable)
                                throw new Exception("A health pack at location '" + location[0] + " - " + location[1] + "' cannot be reached by the player.");
                            items.Add(Content.LoadHealthPack(reader.GetAttribute("type"), new Vector2(position.X, position.Y)));
                            string ptStr = reader.GetAttribute("points");
                            if (ptStr != null)
                                items[items.Count - 1].PointValue = int.Parse(ptStr);
                            string clrStr = reader.GetAttribute("tint");
                            if (clrStr != null)
                                items[items.Count - 1].Color = ContentLoader.ParseColor(clrStr);
                            string healStr = reader.GetAttribute("heal");
                            if (healStr != null)
                                ((HealthPack)items[items.Count - 1]).Heal = healStr;
                            string healMaxStr = reader.GetAttribute("maxHeal");
                            if (healMaxStr != null)
                                ((HealthPack)items[items.Count - 1]).MaxHeal = healMaxStr;
                            #endregion
                        }
                        else if (name == "enemy")
                        {
                            #region LoadEnemyData
                            string eName = reader.GetAttribute("name"), modHP = reader.GetAttribute("health"), modPt = reader.GetAttribute("points");
                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
                            if (tiles[x, y].Collision == TileCollision.Impassable)
                                throw new Exception("A gem at location '" + location[0] + " - " + location[1] + "' cannot be reached by the player.");

                            enemies.Add(Content.LoadEnemy(eName, position));
                            string dmgStr = reader.GetAttribute("damage");
                            if (dmgStr != null)
                                enemies[enemies.Count - 1].Damage = int.Parse(dmgStr);

                            if (modHP != null)
                            {
                                int hp;
                                if (!int.TryParse(modHP, out hp))
                                    throw new Exception("An enemy's health must be an integer");
                                enemies[enemies.Count - 1].Health = hp;
                            }
                            if (modPt != null)
                            {
                                int pt;
                                if (!int.TryParse(modPt, out pt))
                                    throw new Exception("An enemy's points must be an integer");
                                enemies[enemies.Count - 1].PointsOnDeath = pt;
                            }
                            #endregion
                        }
                        else if (name == "trap")
                        {
                            #region LoadTrapData
                            string tType = reader.GetAttribute("type").ToLower();

                            string[] location = reader.GetAttribute("location").Split('-');
                            if (location.Length != 2)
                                throw new Exception("The attribute 'location' must be formatted as '# - #'");
                            int x = int.Parse(location[0].Trim()), y = int.Parse(location[1].Trim());
                            Point position = GetBounds(x, y).Center;
                            if (tiles[x, y].Collision == TileCollision.Impassable)
                                throw new Exception("A trap at location '" + location[0] + " - " + location[1] + "' cannot be reached by the player.");

                            string dmgStr, clrStr;
                            if (tType == "static")
                            {
                                m_trapStatic.Add(Content.LoadStaticTrap(position));

                                dmgStr = reader.GetAttribute("damage");
                                clrStr = reader.GetAttribute("tint");
                                if (dmgStr != null)
                                    m_trapStatic[m_trapStatic.Count - 1].Damage = int.Parse(dmgStr);
                                if (clrStr != null)
                                    m_trapStatic[m_trapStatic.Count - 1].Tint = ContentLoader.ParseColor(clrStr);
                            }
                            else if (tType == "rising")
                            {
                                position = new Point(x * Tile.Width, y * Tile.Height);
                                m_trapRising.Add(Content.LoadRisingTrap(position));

                                dmgStr = reader.GetAttribute("damage");
                                clrStr = reader.GetAttribute("tint");
                                if (dmgStr != null)
                                    m_trapRising[m_trapRising.Count - 1].Damage = int.Parse(dmgStr);
                                if (clrStr != null)
                                    m_trapRising[m_trapRising.Count - 1].Tint = ContentLoader.ParseColor(clrStr);
                            }
                            else if (tType == "falling")
                            {
                                position = new Point(x * Tile.Width, y * Tile.Height);
                                m_trapFalling.Add(Content.LoadFallingTrap(position));

                                dmgStr = reader.GetAttribute("damage");
                                clrStr = reader.GetAttribute("tint");
                                if (dmgStr != null)
                                    m_trapFalling[m_trapFalling.Count - 1].Damage = int.Parse(dmgStr);
                                if (clrStr != null)
                                    m_trapFalling[m_trapFalling.Count - 1].Tint = ContentLoader.ParseColor(clrStr);
                            }
                            else if (tType == "shooting")
                            {
                                position = new Point(x * Tile.Width, y * Tile.Height);
                                var dir = (ShootingTrap.FaceDirection)Enum.Parse(typeof(ShootingTrap.FaceDirection), reader.GetAttribute("direction"));
                                m_trapShooting.Add(Content.LoadShootingTrap(position, dir));

                                dmgStr = reader.GetAttribute("damage");
                                clrStr = reader.GetAttribute("tint");
                                if (dmgStr != null)
                                    m_trapShooting[m_trapShooting.Count - 1].Damage = int.Parse(dmgStr);
                                if (clrStr != null)
                                    m_trapShooting[m_trapShooting.Count - 1].Tint = ContentLoader.ParseColor(clrStr);
                            }
                            else
                                throw new NotSupportedException("Trap type '" + tType + "' is not supported.");
                            #endregion
                        }
                    }
                    else if (type == XmlNodeType.EndElement)
                    {
                        #region EndElementRading
                        name = reader.Name.ToLower();

                        if (name == "level")
                        {
                            if (!started)
                                throw new Exception("The 'level' node must be instantiated.");
                            started = false;
                        }
                        else if (!started)
                            throw new Exception("The 'level' node must be instantiated.");
                        #endregion
                    }
                }
            }
        }

        /// <summary>
        /// Iterates over every tile in the structure file and loads its
        /// appearance and behavior. This method also validates that the
        /// file is well-formed with a player start point, exit, etc.
        /// </summary>
        /// <param name="path">
        /// The absolute path to the level file to be loaded.
        /// </param>
        private void LoadTiles(string path, int pHP, int pMaxHP)
        {
            // Load the level and ensure all of the lines are the same length.
            if (new FileInfo(path).Extension == ".xml")
                LoadXMLLevel("Worlds/" + path);
            else
            {
                #region PlaintextLoading
                LevelName = new FileInfo(path).Name.Replace(new FileInfo(path).Extension, "");
                int width;
                List<string> lines = new List<string>();
                using (StreamReader reader = new StreamReader("Worlds/" + path))
                {
                    defDest = reader.ReadLine().Trim();
                    string line = reader.ReadLine();
                    width = line.Length;
                    while (line != null)
                    {
                        lines.Add(line);
                        if (line.Length != width)
                            throw new Exception(String.Format("The length of line {0} is different from all preceeding lines.", lines.Count));
                        line = reader.ReadLine();
                    }
                }

                // Allocate the tile grid.
                tiles = new Tile[width, lines.Count];
                items = new List<Item>();

                // Loop over every tile position,
                for (int y = 0; y < Height; ++y)
                {
                    for (int x = 0; x < Width; ++x)
                    {
                        // to load each tile.
                        char tileType = lines[y][x];
                        tiles[x, y] = LoadTile(tileType, x, y);
                    }
                }
                #endregion
            }

            if (start.Count == 0)
                throw new NotSupportedException("A level must have a starting point.");

            int max = 0;
            foreach (var pair in start)
                max += pair.Value;
            int chance = Content.Random.Next(max), current = 0;
            foreach (var pair in start)
            {
                current += pair.Value;
                if (chance < current)
                {
                    chosenStart = pair.Key;
                    break;
                }
            }
            player = new Player(this, chosenStart, pHP, pMaxHP);
            // Verify that the level has a beginning and an end.
            if (Player == null)
                throw new NotSupportedException("A level must have a starting point.");
            if (exits.Count == 0)
                throw new NotSupportedException("A level must have an exit.");

        }

        /// <summary>
        /// Loads an individual tile's appearance and behavior.
        /// </summary>
        /// <param name="tileType">
        /// The character loaded from the structure file which
        /// indicates what should be loaded.
        /// </param>
        /// <param name="x">
        /// The X location of this tile in tile space.
        /// </param>
        /// <param name="y">
        /// The Y location of this tile in tile space.
        /// </param>
        /// <returns>The loaded tile.</returns>
        private Tile LoadTile(char tileType, int x, int y)
        {
            switch (tileType)
            {
                // Blank space
                case '.':
                    return new Tile(null, TileCollision.Passable);

                // Exit
                case 'X':
                    return LoadExitTile(x, y);

                // Gem
                case 'G':
                    return LoadGemTile(x, y);

                //Time bonus
                case 'T':
                    return LoadTimeBonusTile(x, y);

                //Regular health pack
                case '+':
                    return LoadHealthPackTile(x, y, null);

                //Poisonous health pack
                case '@':
                    return LoadHealthPackTile(x, y, "poison");

                //Weak health pack
                case '(':
                    return LoadHealthPackTile(x, y, "weak");

                //Strong health pack
                case ')':
                    return LoadHealthPackTile(x, y, "strong");

                //Ultimate health pack
                case '=':
                    return LoadHealthPackTile(x, y, "ultimate");

                // Floating platform
                case '-':
                    return LoadTile("Platform", TileCollision.Platform);

                // Platform block
                case '~':
                    return LoadVarietyTile("MossyGreen", TileCollision.Platform);

                // Passable block
                case ':':
                    return LoadVarietyTile("MossyGreen", TileCollision.Passable);

                // Player 1 start point
                case '1':
                    return LoadStartTile(x, y);

                // Impassable block
                case '#':
                    return LoadVarietyTile("Orange", TileCollision.Impassable);

                // Various enemies
                case 'A':
                    return LoadEnemyTile(x, y, "Barbarian");
                case 'B':
                    return LoadEnemyTile(x, y, "Pygmy");
                case 'C':
                    return LoadEnemyTile(x, y, "Zombie");
                case 'D':
                    return LoadEnemyTile(x, y, "Skeleton");

                //Various traps
                case '*':
                    return LoadStaticTrapTile(x, y);
                case '^':
                    return LoadRisingTrapTile(x, y);
                case 'v':
                    return LoadFallingTrapTile(x, y);
                case '>':
                    return LoadShootingTrapTile(x, y, ShootingTrap.FaceDirection.Right);
                case '<':
                    return LoadShootingTrapTile(x, y, ShootingTrap.FaceDirection.Left);

                // Unknown tile type character
                default:
                    throw new NotSupportedException(String.Format("Unsupported tile type character '{0}' at position {1}, {2}.", tileType, x, y));
            }
        }

        /// <summary>
        /// Creates a new tile. The other tile loading methods typically chain to this
        /// method after performing their special logic.
        /// </summary>
        /// <param name="name">
        /// Path to a tile texture relative to the Content/Tiles directory.
        /// </param>
        /// <param name="collision">
        /// The tile collision type for the new tile.
        /// </param>
        /// <returns>The new tile.</returns>
        private Tile LoadTile(string name, TileCollision collision)
        {
            return Content.LoadTile(name);
        }


        /// <summary>
        /// Loads a tile with a random appearance.
        /// </summary>
        /// <param name="baseName">
        /// The content name prefix for this group of tile variations. Tile groups are
        /// name LikeThis0.png and LikeThis1.png and LikeThis2.png.
        /// </param>
        /// <param name="variationCount">
        /// The number of variations in this group.
        /// </param>
        private Tile LoadVarietyTile(string baseName, TileCollision col)
        {
            Tile toRet = Content.LoadTile(baseName);
            toRet.Collision = col;
            return toRet;
        }


        /// <summary>
        /// Instantiates a player, puts him in the level, and remembers where to put him when he is resurrected.
        /// </summary>
        private Tile LoadStartTile(int x, int y)
        {
            start.Add(RectangleExtensions.GetBottomCenter(GetBounds(x, y)), 1);

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Remembers the location of the level's exit.
        /// </summary>
        private Tile LoadExitTile(int x, int y)
        {
            exits.Add(GetBounds(x, y).Center, null);

            return new Tile(Content.LoadTexture2D("Tiles/Exit"), TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates an enemy and puts him in the level.
        /// </summary>
        private Tile LoadEnemyTile(int x, int y, string spriteSet)
        {
            Vector2 position = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            enemies.Add(Content.LoadEnemy(spriteSet, position));
            enemies[enemies.Count - 1].PointsOnDeath = 50;

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a gem and puts it in the level.
        /// </summary>
        private Tile LoadGemTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            items.Add(Content.LoadGem(null, new Vector2(position.X, position.Y)));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a TimeBonus and puts it in the level.
        /// </summary>
        private Tile LoadTimeBonusTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            items.Add(Content.LoadTimeBonus(null, new Vector2(position.X, position.Y)));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Instantiates a HealthPack and puts it in the level.
        /// </summary>
        private Tile LoadHealthPackTile(int x, int y, string type)
        {
            Point position = GetBounds(x, y).Center;
            items.Add(Content.LoadHealthPack(type, new Vector2(position.X, position.Y)));

            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadStaticTrapTile(int x, int y)
        {
            Point position = GetBounds(x, y).Center;
            m_trapStatic.Add(Content.LoadStaticTrap(position));

            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadRisingTrapTile(int x, int y)
        {
            Point position = new Point(x * Tile.Width, y * Tile.Height);
            m_trapRising.Add(Content.LoadRisingTrap(position));

            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadFallingTrapTile(int x, int y)
        {
            Point position = new Point(x * Tile.Width, y * Tile.Height);
            m_trapFalling.Add(Content.LoadFallingTrap(position));

            return new Tile(null, TileCollision.Passable);
        }

        private Tile LoadShootingTrapTile(int x, int y, ShootingTrap.FaceDirection dir)
        {
            Point position = new Point(x * Tile.Width, y * Tile.Height);
            m_trapShooting.Add(Content.LoadShootingTrap(position, dir));

            return new Tile(null, TileCollision.Passable);
        }

        /// <summary>
        /// Unloads the level content.
        /// </summary>
        public void Dispose()
        {
            Content = null;
            ContentManager.Unload();
        }

        #endregion

        #region Bounds and collision

        /// <summary>
        /// Gets the collision mode of the tile at a particular location.
        /// This method handles tiles outside of the levels boundries by making it
        /// impossible to escape past the left or right edges, but allowing things
        /// to jump beyond the top of the level and fall off the bottom.
        /// </summary>
        public TileCollision GetCollision(int x, int y)
        {
            // Prevent escaping past the level ends.
            if (x < 0 || x >= Width)
                return TileCollision.Impassable;
            // Allow jumping past the level top and falling through the bottom.
            if (y < 0 || y >= Height)
                return TileCollision.Passable;

            return tiles[x, y].Collision;
        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion

        #region Update

        /// <summary>
        /// Updates all objects in the world, performs collision between them,
        /// and handles the time limit with scoring.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            // Pause while the player is dead or time is expired.
            if (!Player.IsAlive || TimeRemaining == TimeSpan.Zero)
            {
                // Still want to perform physics on the player.
                Player.ApplyPhysics(gameTime);
            }
            else if (ReachedExit)
            {
                // Animate the time being converted into points.
                int seconds = (int)Math.Round(gameTime.ElapsedGameTime.TotalSeconds * 100.0f);
                seconds = Math.Min(seconds, (int)Math.Ceiling(TimeRemaining.TotalSeconds));
                timeRemaining -= TimeSpan.FromSeconds(seconds);
                score += seconds * PointsPerSecond;
            }
            else
            {
                timeRemaining -= gameTime.ElapsedGameTime;

                Player.Update(gameTime);

                UpdateItems(gameTime);

                UpdateTraps(gameTime);

                // Falling off the bottom of the level kills the player.
                if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                    OnPlayerKilled(null);

                UpdateEnemies(gameTime);

                UpdateProjectiles(gameTime);

                // The player has reached the exit if they are standing on the ground and
                // his bounding rectangle contains the center of the exit tile. They can only
                // exit when they have collected all of the items.
                foreach (var exit in exits)
                {
                    if (Player.IsAlive && Player.IsOnGround && Player.BoundingRectangle.Contains(exit.Key))
                    {
                        if (defDest != null)
                            OnExitReached(defDest);
                        else
                            OnExitReached(exit.Value);
                    }
                }

            }

            // Clamp the time remaining at zero.
            if (timeRemaining < TimeSpan.Zero)
                timeRemaining = TimeSpan.Zero;
        }

        private void UpdateTraps(GameTime gameTime)
        {

            foreach (var trap in m_trapStatic)
            {
                trap.Update(gameTime);
                if (trap.BoundingCircle.Intersects(player.BoundingRectangle))
                    player.HurtPlayer(trap.Damage);
            }

            foreach (var trap in m_trapRising)
            {
                trap.Update(gameTime);
                if (trap.BoundingRectangle.Intersects(player.BoundingRectangle))
                    player.HurtPlayer(trap.Damage);
            }

            foreach (var trap in m_trapFalling)
            {
                trap.Update(gameTime);
                if (trap.BoundingRectangle.Intersects(player.BoundingRectangle))
                    player.HurtPlayer(trap.Damage);
            }

            foreach (var trap in m_trapShooting)
                trap.Update(gameTime);
        }

        private void UpdateProjectiles(GameTime gameTime)
        {
            List<Projectile> toRemove = new List<Projectile>();
            foreach (var proj in m_proj)
            {
                Rectangle bounding = proj.BoundingRectangle;
                int tileX = (bounding.X - bounding.X % Tile.Width) / Tile.Width,
                    tileY = (bounding.Y - bounding.Y % Tile.Height) / Tile.Height;
                if (tileX >= Width)
                    tileX = Width - 1;
                if (tileY >= Height)
                    tileY = Height - 1;
                Rectangle check1 = new Rectangle(tileX * Tile.Width, tileY * Tile.Height, Tile.Width, Tile.Height);
                TileCollision tC1 = tiles[tileX, tileY].Collision;
                tileX++;
                tileY++;
                if (tileX >= Width)
                    tileX = Width - 1;
                if (tileY >= Height)
                    tileY = Height - 1;
                Rectangle check2 = new Rectangle(tileX * Tile.Width, tileY * Tile.Height, Tile.Width, Tile.Height);
                TileCollision tC2 = tiles[tileX, tileY].Collision;
                tileX -= 2;
                if (tileX < 0)
                    tileX = 0;
                Rectangle check3 = new Rectangle(tileX * Tile.Width, tileY * Tile.Height, Tile.Width, Tile.Height);
                TileCollision tC3 = tiles[tileX, tileY].Collision;

                proj.Update(gameTime);
                if (!bounding.Intersects(levelSize)
                    || (tC1 != TileCollision.Passable && bounding.Intersects(check1))
                    || (tC2 != TileCollision.Passable && bounding.Intersects(check2))
                    || (tC3 != TileCollision.Passable && bounding.Intersects(check3)))
                {
                    toRemove.Add(proj);
                    continue;
                }
                if (proj.DamageEnemy)
                {
                    foreach (Enemy enemy in enemies)
                    {
                        if (!enemy.IsAlive)
                            continue;
                        if (bounding.Intersects(enemy.BoundingRectangle))
                        {
                            enemy.Health -= proj.Damage;
                            toRemove.Add(proj);
                            break;
                        }
                    }
                }
                if (proj.DamagePlayer && bounding.Intersects(player.BoundingRectangle))
                {
                    player.HurtPlayer(proj.Damage);
                    toRemove.Add(proj);
                }
            }
            foreach (var proj in toRemove)
                m_proj.Remove(proj);
        }

        /// <summary>
        /// Animates each item and checks to allows the player to collect them.
        /// </summary>
        private void UpdateItems(GameTime gameTime)
        {
            List<Item> toRemove = new List<Item>();
            foreach (var item in items)
            {
                item.Update(gameTime);

                if (item.BoundingCircle.Intersects(Player.BoundingRectangle))
                {
                    OnItemCollected(item, Player);
                    toRemove.Add(item);
                }
            }
            foreach (var item in toRemove)
                items.Remove(item);
        }

        /// <summary>
        /// Animates each enemy and allow them to kill the player.
        /// </summary>
        private void UpdateEnemies(GameTime gameTime)
        {
            foreach (Enemy enemy in enemies)
            {
                if (!enemy.IsAlive)
                    continue;
                enemy.Update(gameTime);

                // Touching an enemy instantly kills the player
                if (enemy.IsAlive && enemy.BoundingRectangle.Intersects(Player.BoundingRectangle))
                {
                    player.HurtPlayer(enemy.Damage);
                    if (player.Health < 0)
                    {
                        OnPlayerKilled(enemy);
                        break;
                    }
                }
            }
        }
        private void OnEnemyKilled(Enemy enemy, Player killedBy)
        {
            enemy.OnKilled(killedBy);
        }

        /// <summary>
        /// Called when a item is collected.
        /// </summary>
        /// <param name="item">The item that was collected.</param>
        /// <param name="collectedBy">The player who collected this gem.</param>
        private void OnItemCollected(Item item, Player collectedBy)
        {
            item.OnCollected(collectedBy);
        }

        /// <summary>
        /// Called when the player is killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This is null if the player was not killed by an
        /// enemy, such as when a player falls into a hole.
        /// </param>
        private void OnPlayerKilled(Enemy killedBy)
        {
            Player.OnKilled(killedBy);
        }

        /// <summary>
        /// Called when the player reaches the level's exit.
        /// </summary>
        private void OnExitReached(string dest)
        {
            Player.OnReachedExit();
            exitReachedSound.Play();
            NextLevel = dest;
            reachedExit = true;
        }

        /// <summary>
        /// Restores the player to the starting point to try the level again.
        /// </summary>
        public void StartNewLife()
        {
            timeRemaining -= new TimeSpan(0, 0, 15);
            Player.HealPlayer(player.MaxHealth / 10, 0);
            Player.Reset(chosenStart);
        }

        #endregion

        #region Draw

        /// <summary>
        /// Draw everything in the level from background to foreground.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            if (ViewportWidth == -1)
                ViewportWidth = spriteBatch.GraphicsDevice.Viewport.Width;

            spriteBatch.Begin();
            for (int i = 0; i <= EntityLayer; ++i)
                layers[i].Draw(spriteBatch, cameraPositionXAxis);
            spriteBatch.End();

            ScrollCamera(spriteBatch.GraphicsDevice.Viewport);
            Matrix cameraTransform = Matrix.CreateTranslation(-cameraPositionXAxis, -cameraPositionYAxis, 0.0f); ;
            spriteBatch.Begin(SpriteBlendMode.AlphaBlend, SpriteSortMode.Immediate, SaveStateMode.None, cameraTransform);

            DrawTraps(spriteBatch, gameTime);
            DrawTiles(spriteBatch);
            Drawitems(spriteBatch, gameTime);
            DrawProjectiles(spriteBatch);

            Player.Draw(gameTime, spriteBatch);

            foreach (Enemy enemy in enemies)
                enemy.Draw(gameTime, spriteBatch);

            spriteBatch.End();

            spriteBatch.Begin();
            for (int i = EntityLayer + 1; i < layers.Length; ++i)
                layers[i].Draw(spriteBatch, cameraPositionXAxis);
            spriteBatch.End();
        }

        private void DrawProjectiles(SpriteBatch spriteBatch)
        {
            foreach (var proj in m_proj)
                proj.Draw(spriteBatch);
        }

        /// <summary>
        /// Draws each tile in the level.
        /// </summary>
        private void DrawTiles(SpriteBatch spriteBatch)
        {
            int left = (int)Math.Floor(cameraPositionXAxis / Tile.Width);
            int right = left + spriteBatch.GraphicsDevice.Viewport.Width / Tile.Width;
            right = Math.Min(right, Width - 1);
            int bottom = (int)Math.Floor(cameraPositionYAxis / Tile.Height);
            int top = bottom + spriteBatch.GraphicsDevice.Viewport.Height / Tile.Height;
            top = Math.Min(top, Height - 1);
            // For each tile position
            for (int y = bottom; y <= top; ++y)
            {
                for (int x = left; x <= right; ++x)
                {
                    // If there is a visible tile in that position
                    Texture2D texture = tiles[x, y].Texture;
                    if (texture != null)
                    {
                        // Draw it in screen space.
                        Vector2 position = new Vector2(x, y) * Tile.Size;
                        spriteBatch.Draw(texture, position, Color.White);
                    }
                }
            }
        }

        private void DrawTraps(SpriteBatch spriteBatch, GameTime gameTime)
        {
            foreach (var trap in m_trapStatic)
                trap.Draw(spriteBatch);
            foreach (var trap in m_trapRising)
                trap.Draw(spriteBatch);
            foreach (var trap in m_trapFalling)
                trap.Draw(spriteBatch);
            foreach (var trap in m_trapShooting)
                trap.Draw(spriteBatch);
        }

        private void Drawitems(SpriteBatch spriteBatch, GameTime gameTime)
        {
            foreach (var item in items)
                item.Draw(gameTime, spriteBatch);
        }

        private void ScrollCamera(Viewport viewport)
        {
            // Calculate the edges of the screen.
            float marginWidth = viewport.Width * ViewMargin;
            float marginLeft = cameraPositionXAxis + marginWidth;
            float marginRight = cameraPositionXAxis + viewport.Width - marginWidth;
            float marginTop = cameraPositionYAxis + viewport.Height * TopMargin;
            float marginBottom = cameraPositionYAxis + viewport.Height - viewport.Height * BottomMargin;

            // Calculate how far to scroll when the player is near the edges of the screen.
            float cameraMovement = 0.0f;
            if (Player.Position.X < marginLeft)
                cameraMovement = Player.Position.X - marginLeft;
            else if (Player.Position.X > marginRight)
                cameraMovement = Player.Position.X - marginRight;
            float cameraMovementY = 0.0f;
            if (Player.Position.Y < marginTop) //above the top margin
                cameraMovementY = Player.Position.Y - marginTop; 
            else if (Player.Position.Y > marginBottom) //below the bottom margin
                cameraMovementY = Player.Position.Y - marginBottom; 

            // Update the camera position, but prevent scrolling off the ends of the level.
            float maxCameraPositionXOffset = Tile.Width * Width - viewport.Width;
            float maxCameraPositionYOffset = Tile.Height * Height - viewport.Height;
            cameraPositionXAxis = MathHelper.Clamp(cameraPositionXAxis + cameraMovement, 0.0f, maxCameraPositionXOffset);
            cameraPositionYAxis = MathHelper.Clamp(cameraPositionYAxis + cameraMovementY, 0.0f, maxCameraPositionYOffset);
        }

        #endregion
    }
}
