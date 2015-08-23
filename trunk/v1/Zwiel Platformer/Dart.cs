using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Zwiel_Platformer
{
    sealed class Dart : Projectile
    {
        const float speed = .3f;
        public Dart(Vector2 location, ContentLoader loader, bool throwRight) :
            base(loader.LoadTexture2D("Misc/dart"), location, throwRight ? speed : -speed, 10)
        {
            m_dmgEnemy = false;
            m_dmgPlayer = true;
        }
    }
}
