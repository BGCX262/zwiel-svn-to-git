using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Zwiel_Platformer
{
    sealed class StaticTrap
    {
        public Rectangle BoundingRectangle { get; private set; }
        public Circle BoundingCircle { get; private set; }
        private Texture2D m_tex;
        Vector2 m_center;
        public Color Tint = Color.White;
        public int Damage = 40;
        float m_spin;
        const float m_spinSpeed = .1f;

        public StaticTrap(Point pos, ContentLoader loader)
        {
            m_spin = loader.Random.Next(360);
            m_tex = loader.LoadTexture2D("Traps/Static");
            m_center = new Vector2(m_tex.Width / 2, m_tex.Height / 2);
            BoundingRectangle = new Rectangle(pos.X, pos.Y, m_tex.Width, m_tex.Height);
            BoundingCircle = new Circle(new Vector2(pos.X + m_tex.Width / 2, pos.Y + m_tex.Height / 2),
                m_tex.Width > m_tex.Height ? (float)m_tex.Width / 2 : (float)m_tex.Height / 2);
        }

        public void Update(GameTime gameTime)
        {
            m_spin += m_spinSpeed * gameTime.ElapsedGameTime.Milliseconds;
            if (m_spin > 360)
                m_spin %= 360;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(m_tex, BoundingRectangle, null, Tint, m_spin, m_center, SpriteEffects.None, 0);
        }
    }
}
