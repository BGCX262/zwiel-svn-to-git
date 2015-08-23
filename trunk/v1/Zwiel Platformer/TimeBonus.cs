using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Zwiel_Platformer
{
    /// <summary>
    /// A valuable item the player can collect.
    /// </summary>
    sealed class TimeBonus : Item
    {
        public int Bonus { get; set; }

        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Constructs a new gem.
        /// </summary>
        public TimeBonus(Level level, Vector2 position, int pointVal, Color tint, int bonus)
        {
            this.level = level;
            basePosition = position;
            PointValue = pointVal;
            Color = tint;
            Bonus = bonus;

            LoadContent();
        }

        /// <summary>
        /// Loads the gem texture and collected sound.
        /// </summary>
        public void LoadContent()
        {
            texture = Level.Content.LoadTexture2D("Misc/timeBonus");
            origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
            collectedSound = Level.Content.LoadSoundEffect("Sounds/TimeBonusCollected");
        }

        public override void OnCollected(Player collectedBy)
        {
            level.Score += PointValue;
            level.TimeRemaining += TimeSpan.FromMilliseconds(Bonus);
            collectedSound.Play();
        }
    }
}
