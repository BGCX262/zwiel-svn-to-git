using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Zwiel_Platformer
{
    abstract class Projectile
    {
        //should always face right
        protected Texture2D m_tex;
        protected Vector2 m_location;
        protected float m_velocity;
        protected SpriteEffects m_effects = SpriteEffects.None;
        protected Color m_tint = Color.White;
        protected bool m_dmgEnemy = true, m_dmgPlayer = false;

        public Rectangle BoundingRectangle { get { return new Rectangle((int)m_location.X, (int)m_location.Y, m_tex.Width, m_tex.Height); } }

        protected int m_dmg = 0;
        public int Damage { get { return m_dmg; } set { m_dmg = value; } }

        public bool DamageEnemy { get { return m_dmgEnemy; } }
        public bool DamagePlayer { get { return m_dmgPlayer; } }

        public Projectile(Texture2D tex, Vector2 location, float velocity, int dmg)
        {
            m_tex = tex;
            m_location = location;
            m_velocity = velocity;
            m_dmg = dmg;
            if (velocity < 0)
                m_effects = SpriteEffects.FlipHorizontally;
        }

        public void Update(GameTime gameTime)
        {
            m_location.X += m_velocity * gameTime.ElapsedGameTime.Milliseconds;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(m_tex, m_location, null, m_tint, 0, Vector2.Zero, 1,  m_effects, 0);
        }

        public bool IntersectsWith(Rectangle rect)
        {
            return rect.Intersects(BoundingRectangle);
        }
    }
}
