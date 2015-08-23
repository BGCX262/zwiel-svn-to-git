using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace Zwiel_Platformer
{
    sealed class Spear : Projectile
    {
        const float speed = .5f;
        public Spear(Vector2 location, ContentLoader loader, bool throwRight) :
            base(loader.LoadTexture2D("Misc/projectile"), location, throwRight ? speed : -speed, 10) { }
    }
}
