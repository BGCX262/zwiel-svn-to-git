using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Zwiel_Platformer
{
    /// <summary>
    /// A valuable item the player can collect.
    /// </summary>
    sealed class HealthPack: Item
    {
        public string Heal { get; set; }
        public string MaxHeal { get; set; }

        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Constructs a new gem.
        /// </summary>
        public HealthPack(Level level, Vector2 position, int pointVal, Color tint, string heal)
        {
            this.level = level;
            basePosition = position;
            PointValue = pointVal;
            Color = tint;
            Heal = heal;

            LoadContent();
        }

        /// <summary>
        /// Loads the gem texture and collected sound.
        /// </summary>
        public void LoadContent()
        {
            texture = Level.Content.LoadTexture2D("Misc/health");
            origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
        }

        public override void OnCollected(Player collectedBy)
        {
            level.Score += PointValue;
            int toHeal, toHealMax;
            if (!int.TryParse(Heal, out toHeal))
            {
                if (Heal.EndsWith("%"))
                    toHeal = level.Player.MaxHealth * int.Parse(Heal.Substring(0, Heal.Length - 1)) / 100;
                else
                    throw new Exception("Cannot heal player - templating error.");
            }
            if (!int.TryParse(MaxHeal, out toHealMax))
            {
                if (MaxHeal.EndsWith("%"))
                    toHealMax = level.Player.MaxHealth * int.Parse(MaxHeal.Substring(0, MaxHeal.Length - 1)) / 100;
                else
                    throw new Exception("Cannot heal player - templating error.");
            }
            if (toHeal < 0)
                level.Player.PoisonPlayer(toHeal, toHealMax);
            else
                level.Player.HealPlayer(toHeal, toHealMax);
            base.OnCollected(collectedBy);
        }
    }
}
