using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Input;
using System.Xml;

namespace Zwiel_Platformer
{
    sealed class TileGenerator
    {
        public TileCollision Collision;
        Dictionary<Texture2D, int> m_textures = new Dictionary<Texture2D, int>();
        int m_maxChance = 0;
        ContentLoader m_loader;

        public TileGenerator(string tileName, ContentLoader loader)
        {
            m_loader = loader;

            #region LoadData
            using (XmlReader reader = XmlReader.Create("Content/Tiles/" + tileName + "/tile.xml"))
            {
                string name;
                bool started = false;
                while (reader.Read())
                {
                    XmlNodeType type = reader.MoveToContent();
                    if (type == XmlNodeType.Element)
                    {
                        name = reader.Name.ToLower();

                        if (name == "tile")
                        {
                            if (started)
                                throw new Exception("'tile' nodes cannot be nested");
                            started = true;
                            Collision = (TileCollision)Enum.Parse(typeof(TileCollision), reader.GetAttribute("collision"));
                        }
                        else if (!started)
                            throw new Exception("The 'tile' node must be instantiated.");
                        else if (name == "texture")
                        {
                            int chance = int.Parse(reader.GetAttribute("chance"));
                            m_maxChance += chance;
                            m_textures.Add(loader.LoadTexture2D("Tiles/" + reader.ReadString()), chance);
                        }
                        else
                            throw new NotSupportedException("Node '" + name + "' is not supported.");
                    }
                    else if (type == XmlNodeType.EndElement)
                    {
                        name = reader.Name.ToLower();

                        if (name == "tile")
                        {
                            if (!started)
                                throw new Exception("The 'tile' node must be instantiated.");
                            started = false;
                        }
                        else if (!started)
                            throw new Exception("The 'tile' node must be instantiated.");
                    }
                }
            }
            #endregion

            if (m_textures.Count == 0)
                throw new Exception("Tile '" + tileName + "' must have at least 1 texture.");
        }

        public Tile GenerateTile()
        {
            if (m_textures.Count == 1)
                return new Tile(m_textures.ElementAt(0).Key, Collision);
            int chance = m_loader.Random.Next(m_maxChance), currentChance = 0;
            foreach (var pair in m_textures)
            {
                currentChance += pair.Value;
                if (chance < currentChance)
                    return new Tile(pair.Key, Collision);
            }
            return new Tile(null, TileCollision.Passable);
        }
    }

    sealed class EnemyGenerator
    {
        Dictionary<string, string> m_enemySpriteSet = new Dictionary<string, string>();
        Dictionary<string, int> m_enemyHealth = new Dictionary<string, int>(),
            m_enemyPoints = new Dictionary<string,int>(),
            m_enemyDamage = new Dictionary<string,int>();
        Dictionary<string, Color> m_enemyTint = new Dictionary<string, Color>();
        Level m_lvl;

        public EnemyGenerator(Level lvl)
        {
            m_lvl = lvl;

            #region LoadData
            using (XmlReader reader = XmlReader.Create("Content/default-enemy.xml"))
            {
                string name;
                bool started = false;
                while (reader.Read())
                {
                    XmlNodeType type = reader.MoveToContent();
                    if (type == XmlNodeType.Element)
                    {
                        name = reader.Name.ToLower();

                        if (name == "enemies")
                        {
                            if (started)
                                throw new Exception("'enemies' nodes cannot be nested");
                            started = true;
                        }
                        else if (!started)
                            throw new Exception("The 'enemies' node must be instantiated.");
                        else if (name == "enemy")
                        {
                            string eName = reader.GetAttribute("name").ToLower();
                            int hp, pts, dmg;
                            if (!int.TryParse(reader.GetAttribute("health"), out hp))
                                throw new Exception("The attribute 'health' of node 'enemy' must be an integer.");
                            if (!int.TryParse(reader.GetAttribute("points"), out pts))
                                throw new Exception("The attribute 'points' of node 'enemy' must be an integer.");
                            if (!int.TryParse(reader.GetAttribute("damage"), out dmg))
                                throw new Exception("The attribute 'damage' of node 'enemy' must be an integer.");
                            m_enemySpriteSet.Add(eName, reader.GetAttribute("spriteSet").ToLower());
                            m_enemyHealth.Add(eName, hp);
                            m_enemyPoints.Add(eName, pts);
                            m_enemyDamage.Add(eName, dmg);
                            m_enemyTint.Add(eName, ContentLoader.ParseColor(reader.GetAttribute("tint")));
                        }
                        else
                            throw new NotSupportedException("Node '" + name + "' is not supported.");
                    }
                }
            }
            #endregion
        }

        public Enemy GenerateEnemy(string name, Vector2 position)
        {
            string nm = name.ToLower();
            if (!m_enemySpriteSet.ContainsKey(nm))
                throw new Exception("An enemy of type '" + name + "' does not exist.");
            Enemy toRet = new Enemy(m_lvl, position, m_enemySpriteSet[nm], m_enemyHealth[nm]);
            toRet.PointsOnDeath = m_enemyPoints[nm];
            toRet.Tint = m_enemyTint[nm];
            toRet.Damage = m_enemyDamage[nm];
            return toRet;
        }
    }

    sealed class TrapGenerator
    {
        Dictionary<string, int> m_trapDmg = new Dictionary<string, int>();
        Dictionary<string, Color> m_trapClr = new Dictionary<string, Color>();
        Level m_lvl;

        public TrapGenerator(Level lvl)
        {
            m_lvl = lvl;
            #region LoadData
            using (XmlReader reader = XmlReader.Create("Content/default-trap.xml"))
            {
                string name;
                bool started = false;
                while (reader.Read())
                {
                    XmlNodeType type = reader.MoveToContent();
                    if (type == XmlNodeType.Element)
                    {
                        name = reader.Name.ToLower();

                        if (name == "traps")
                        {
                            if (started)
                                throw new Exception("'traps' nodes cannot be nested");
                            started = true;
                        }
                        else if (!started)
                            throw new Exception("The 'traps' node must be instantiated.");
                        else if (name == "trap")
                        {
                            string tName = reader.GetAttribute("type").ToLower();
                            int dmg;
                            if (!int.TryParse(reader.GetAttribute("damage"), out dmg))
                                throw new Exception("The attribute 'damage' of node 'trap' must be an integer.");
                            m_trapDmg.Add(tName, dmg);
                            m_trapClr.Add(tName, ContentLoader.ParseColor(reader.GetAttribute("tint")));
                        }
                        else
                            throw new NotSupportedException("Node '" + name + "' is not supported.");
                    }
                }
            }
            #endregion
        }

        public StaticTrap GenerateStaticTrap(Point pos)
        {
            StaticTrap toRet = new StaticTrap(pos, m_lvl.Content);
            toRet.Damage = m_trapDmg["static"];
            toRet.Tint = m_trapClr["static"];
            return toRet;
        }

        public RisingTrap GenerateRisingTrap(Point pos)
        {
            RisingTrap toRet = new RisingTrap(pos, m_lvl.Content);
            toRet.Damage = m_trapDmg["rising"];
            toRet.Tint = m_trapClr["rising"];
            return toRet;
        }

        public FallingTrap GenerateFallingTrap(Point pos)
        {
            FallingTrap toRet = new FallingTrap(pos, m_lvl.Content);
            toRet.Damage = m_trapDmg["falling"];
            toRet.Tint = m_trapClr["falling"];
            return toRet;
        }

        public ShootingTrap GenerateShootingTrap(Point pos, ShootingTrap.FaceDirection dir)
        {
            ShootingTrap toRet = new ShootingTrap(m_lvl, pos, dir);
            toRet.Damage = m_trapDmg["rising"];
            toRet.Tint = m_trapClr["rising"];
            return toRet;
        }
    }

    sealed class ItemGenerator
    {
        Dictionary<string, int> m_itemPoints = new Dictionary<string, int>(),
            m_itemTimeBonus = new Dictionary<string, int>();
        Dictionary<string, string> m_itemHeal = new Dictionary<string, string>(),
            m_itemHealMax = new Dictionary<string, string>();   
        Dictionary<string, Color> m_itemClr = new Dictionary<string, Color>();
        Dictionary<string, List<string>> m_itemAttributes = new Dictionary<string, List<string>>();
        Level m_lvl;

        public ItemGenerator(Level lvl)
        {
            m_lvl = lvl;
            #region LoadData
            using (XmlReader reader = XmlReader.Create("Content/default-item.xml"))
            {
                string name;
                bool started = false;
                while (reader.Read())
                {
                    XmlNodeType type = reader.MoveToContent();
                    if (type == XmlNodeType.Element)
                    {
                        name = reader.Name.ToLower();

                        if (name == "items")
                        {
                            if (started)
                                throw new Exception("'items' nodes cannot be nested");
                            started = true;
                        }
                        else if (!started)
                            throw new Exception("The 'items' node must be instantiated.");
                        else if (name == "gem")
                        {
                            string gName = "gem-" + reader.GetAttribute("type").ToLower();
                            m_itemPoints.Add(gName, int.Parse(reader.GetAttribute("points")));
                            m_itemClr.Add(gName, ContentLoader.ParseColor(reader.GetAttribute("tint")));
                            m_itemAttributes.Add(gName, new List<string>());
                            if (!reader.IsEmptyElement)
                                LoadAttributes(gName, reader.ReadSubtree());
                        }
                        else if (name == "timebonus")
                        {
                            string gName = "timeBonus-" + reader.GetAttribute("type").ToLower();
                            m_itemPoints.Add(gName, int.Parse(reader.GetAttribute("points")));
                            m_itemClr.Add(gName, ContentLoader.ParseColor(reader.GetAttribute("tint")));
                            m_itemTimeBonus.Add(gName, int.Parse(reader.GetAttribute("bonus")));
                            m_itemAttributes.Add(gName, new List<string>());
                            if (!reader.IsEmptyElement)
                                LoadAttributes(gName, reader.ReadSubtree());
                        }
                        else if (name == "healthpack")
                        {
                            string gName = "healthPack-" + reader.GetAttribute("type").ToLower();
                            m_itemPoints.Add(gName, int.Parse(reader.GetAttribute("points")));
                            m_itemClr.Add(gName, ContentLoader.ParseColor(reader.GetAttribute("tint")));
                            m_itemHeal.Add(gName, reader.GetAttribute("heal"));
                            m_itemHealMax.Add(gName, reader.GetAttribute("maxHeal"));
                            m_itemAttributes.Add(gName, new List<string>());
                            if (!reader.IsEmptyElement)
                                LoadAttributes(gName, reader.ReadSubtree());
                        }
                        else
                            throw new NotSupportedException("Node '" + name + "' is not supported.");
                    }
                }
            }
            #endregion
        }
        private void LoadAttributes(string itemName, XmlReader reader)
        {
            string name;
            while (reader.Read())
            {
                XmlNodeType type = reader.MoveToContent();
                if (type == XmlNodeType.Element)
                {
                    name = reader.Name.ToLower();
                    if (name == "attribute")
                        m_itemAttributes[itemName].Add(reader.ReadString());
                    else
                        throw new Exception("Nodes of type '" + name + "' cannot be nested under items.");
                }
            }
        }

        public Gem GenerateGem(string type, Vector2 pos)
        {
            string name = "gem-" + type.ToLower();
            Gem toRet = new Gem(m_lvl, pos, m_itemPoints[name], m_itemClr[name]);
            toRet.Attributes = m_itemAttributes[name];
            return toRet;
        }

        public TimeBonus GenerateTimeBonus(string type, Vector2 pos)
        {
            string name = "timeBonus-" + type.ToLower();
            TimeBonus toRet = new TimeBonus(m_lvl, pos, m_itemPoints[name], m_itemClr[name], m_itemTimeBonus[name]);
            toRet.Attributes = m_itemAttributes[name];
            return toRet;
        }

        public HealthPack GenerateHealthPack(string type, Vector2 pos)
        {
            string name = "healthPack-" + type.ToLower();
            HealthPack toRet = new HealthPack(m_lvl, pos, m_itemPoints[name], m_itemClr[name], m_itemHeal[name]);
            toRet.MaxHeal = m_itemHealMax[name];
            toRet.Attributes = m_itemAttributes[name];
            return toRet;
        }
    }
}