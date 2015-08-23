using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Zwiel_Platformer
{
    sealed class ShootingTrap
    {
        public enum FaceDirection { Right, Left }

        SpriteEffects effects = SpriteEffects.None;
        private Texture2D tex;
        Vector2 pos;
        public Color Tint = Color.White;
        public int Damage = 10;
        Level level;
        float cooldown = 0f;
        const float defCooldown = 3750f;

        public ShootingTrap(Level level, Point pos, FaceDirection dir)
        {
            if (dir == FaceDirection.Left)
                effects = SpriteEffects.FlipHorizontally;
            tex = level.Content.LoadTexture2D("Traps/Shooting");
            this.pos = new Vector2(pos.X + (dir == FaceDirection.Left ? 35 : 0), pos.Y + 6);
            this.level = level;
        }

        public void Update(GameTime gameTime)
        {
            if (cooldown > 0)
                cooldown -= gameTime.ElapsedGameTime.Milliseconds;
            else// if (level.Player.BoundingRectangle.Y < pos.Y + Tile.Height * 2 && level.Player.BoundingRectangle.Bottom > pos.Y - Tile.Height * 2)
            {
                Vector2 dartPos = new Vector2(pos.X + (effects == SpriteEffects.None ? 10 : -20),
                    pos.Y + tex.Height / 2);
                Dart toAdd = new Dart(dartPos, level.Content, effects == SpriteEffects.None ? true : false);
                toAdd.Damage = Damage;
                level.Projectiles.Add(toAdd);
                cooldown = defCooldown;
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Draw(tex, pos, null, Tint, 0, Vector2.Zero, 1, effects, 0); 
        }
    }
}
