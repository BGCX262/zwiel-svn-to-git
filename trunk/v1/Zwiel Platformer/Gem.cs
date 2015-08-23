using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Audio;

namespace Zwiel_Platformer
{
    /// <summary>
    /// A valuable item the player can collect.
    /// </summary>
    sealed class Gem : Item
    {
        public Level Level
        {
            get { return level; }
        }
        Level level;

        /// <summary>
        /// Constructs a new gem.
        /// </summary>
        public Gem(Level level, Vector2 position, int pointVal, Color tint)
        {
            this.level = level;
            basePosition = position;
            PointValue = pointVal;
            Color = tint;

            LoadContent();
        }

        /// <summary>
        /// Loads the gem texture and collected sound.
        /// </summary>
        public void LoadContent()
        {
            texture = Level.Content.LoadTexture2D("Misc/Gem");
            origin = new Vector2(texture.Width / 2.0f, texture.Height / 2.0f);
            collectedSound = Level.Content.LoadSoundEffect("Sounds/GemCollected");
        }

        public override void OnCollected(Player collectedBy)
        {
            level.Score += PointValue;
            collectedSound.Play();
        }
    }
}
