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
    sealed class ContentLoader
    {
        Dictionary<string, TileGenerator> m_tiles = new Dictionary<string, TileGenerator>();
        public readonly Random Random = new Random();
        ContentManager m_mngr;
        EnemyGenerator m_eGen = null;
        TrapGenerator m_tGen = null;
        ItemGenerator m_iGen = null;
        Level m_lvl;

        public ContentLoader(Level lvl)
        {
            m_mngr = lvl.ContentManager;
            m_lvl = lvl;
        }

        public Texture2D LoadTexture2D(string path)
        {
            return m_mngr.Load<Texture2D>(path);
        }

        public SoundEffect LoadSoundEffect(string path)
        {
            return m_mngr.Load<SoundEffect>(path);
        }

        public Tile LoadTile(string name)
        {
            if (!m_tiles.ContainsKey(name.ToLower()))
                m_tiles.Add(name.ToLower(), new TileGenerator(name, this));
            return m_tiles[name.ToLower()].GenerateTile();
        }

        public Enemy LoadEnemy(string name, Vector2 position)
        {
            if (m_eGen == null)
                m_eGen = new EnemyGenerator(m_lvl);
            return m_eGen.GenerateEnemy(name, position);
        }

        public StaticTrap LoadStaticTrap(Point position)
        {
            if (m_tGen == null)
                m_tGen = new TrapGenerator(m_lvl);
            return m_tGen.GenerateStaticTrap(position);
        }

        public RisingTrap LoadRisingTrap(Point position)
        {
            if (m_tGen == null)
                m_tGen = new TrapGenerator(m_lvl);
            return m_tGen.GenerateRisingTrap(position);
        }

        public FallingTrap LoadFallingTrap(Point position)
        {
            if (m_tGen == null)
                m_tGen = new TrapGenerator(m_lvl);
            return m_tGen.GenerateFallingTrap(position);
        }

        public ShootingTrap LoadShootingTrap(Point position, ShootingTrap.FaceDirection dir)
        {
            if (m_tGen == null)
                m_tGen = new TrapGenerator(m_lvl);
            return m_tGen.GenerateShootingTrap(position, dir);
        }

        public Gem LoadGem(string type, Vector2 position)
        {
            if (m_iGen == null)
                m_iGen = new ItemGenerator(m_lvl);
            if (type == null)
                type = "default";
            return m_iGen.GenerateGem(type, position);
        }

        public TimeBonus LoadTimeBonus(string type, Vector2 position)
        {
            if (m_iGen == null)
                m_iGen = new ItemGenerator(m_lvl);
            if (type == null)
                type = "default";
            return m_iGen.GenerateTimeBonus(type, position);
        }

        public HealthPack LoadHealthPack(string type, Vector2 position)
        {
            if (m_iGen == null)
                m_iGen = new ItemGenerator(m_lvl);
            if (type == null)
                type = "default";
            return m_iGen.GenerateHealthPack(type, position);
        }

        public static Color ParseColor(string color)
        {
            if (color == null)
                return Color.White;
            string clr = color.ToLower();

            switch (color.ToLower())
            {
                case "black":
                    return Color.Black;
                case "gray":
                    return Color.Gray;
                case "brown":
                    return Color.Brown;
                case "red":
                    return Color.Red;
                case "yellow":
                    return Color.Yellow;
                case "blue":
                    return Color.Blue;
                case "orange":
                    return Color.Orange;
                case "purple":
                    return Color.Purple;
                case "green":
                    return Color.Green;
                case "orange-red":
                    return Color.OrangeRed;
                case "red-orange":
                    return Color.OrangeRed;
                case "purple-red":
                    return Color.Maroon;
                case "red-purple":
                    return Color.Maroon;
                case "green-blue":
                    return Color.Turquoise;
                case "blue-green":
                    return Color.Turquoise;
                case "orange-yellow":
                    return Color.LimeGreen;
                case "yellow-orange":
                    return Color.LimeGreen;
                case "purple-blue":
                    return Color.Lavender;
                case "blue-purple":
                    return Color.Lavender;
                case "green-yellow":
                    return Color.GreenYellow;
                case "yellow-green":
                    return Color.GreenYellow;
                default:
                    return Color.White;
            }
        }
    }
}
