using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Zwiel_Platformer
{
    sealed class FallingTrap
    {
        public Rectangle BoundingRectangle { get; private set; }
        private Vector2 m_bounds;
        private Texture2D m_tex;
        public Color Tint = Color.White;
        public int Damage = 20;
        private float m_rise;
        private const float m_riseSpeed = .025f;

        public FallingTrap(Point pos, ContentLoader loader)
        {
            m_rise = -loader.Random.Next(Tile.Height);
            m_tex = loader.LoadTexture2D("Traps/Rising");
            m_bounds = new Vector2(pos.X, pos.Y);
            GenBounds();
        }

        public void Update(GameTime gameTime)
        {
            m_rise -= m_riseSpeed * gameTime.ElapsedGameTime.Milliseconds;
            if (m_rise <= -Tile.Height)
                m_rise = 2 * Tile.Height;
            GenBounds();
        }
        private void GenBounds()
        {
            float yDiff = m_rise;
            if (m_rise > 0)
                yDiff = MathHelper.Clamp(-m_rise, -Tile.Height, Tile.Height);
            BoundingRectangle = new Rectangle((int)m_bounds.X, (int)(m_bounds.Y + yDiff), m_tex.Width, m_tex.Height);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(m_tex, BoundingRectangle, null, Tint, 0, Vector2.Zero, SpriteEffects.FlipVertically, 0);
        }
    }
}
