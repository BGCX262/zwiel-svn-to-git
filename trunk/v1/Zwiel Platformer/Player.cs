using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Zwiel_Platformer
{
    /// <summary>
    /// Our fearless adventurer!
    /// </summary>
    class Player
    {
        private Texture2D healthBar;
        private Texture2D healthBarFill;

        // Animations
        private Animation idleAnimation;
        private Animation runAnimation;
        private Animation jumpAnimation;
        private Animation celebrateAnimation;
        private Animation dieAnimation;
        private Animation shieldAnimation;
        private Animation healAnimation;
        private SpriteEffects flip = SpriteEffects.None;
        private AnimationPlayer sprite;
        private Dictionary<string, AnimationPlayer> effects;

        // Sounds
        private SoundEffect killedSound;
        private SoundEffect jumpSound;
        private SoundEffect fallSound;
        private SoundEffect healSound;
        private List<SoundEffect> hurtSound = new List<SoundEffect>();

        public Level Level
        {
            get { return level; }
        }
        Level level;

        public bool IsAlive
        {
            get { return isAlive; }
        }
        bool isAlive;

        // Physics state
        public Vector2 Position
        {
            get { return position; }
            set { position = value; }
        }
        Vector2 position;

        private float previousBottom;

        public Vector2 Velocity
        {
            get { return velocity; }
            set { velocity = value; }
        }
        Vector2 velocity;

        public int Health { get; private set; }
        public int MaxHealth { get; private set; }

        // Constants for controling horizontal movement
        private const float MoveAcceleration = 14000.0f;
        private const float MaxMoveSpeed = 2000.0f;
        private const float GroundDragFactor = 0.58f;
        private const float AirDragFactor = 0.65f;

        // Constants for controlling vertical movement
        private const float MaxJumpTime = 0.35f;
        private const float JumpLaunchVelocity = -4000.0f;
        private const float GravityAcceleration = 3500.0f;
        private const float MaxFallSpeed = 600.0f;
        private const float JumpControlPower = 0.14f;

        // Input configuration
        private const float MoveStickScale = 1.0f;
        private const Buttons JumpButton = Buttons.A;

        private float tempInv = 0, healEffect = 0;
        private const float tempInvReset = 2000, healEffectReset = 2000;

        private float throwProjTimer = 0;
        private const float resetThrowProj = 1000;//1.5 sec between each throwable projectile
        private const int projPointCost = 100;//cost in score to throw projectile

        /// <summary>
        /// Gets whether or not the player's feet are on the ground.
        /// </summary>
        public bool IsOnGround
        {
            get { return isOnGround; }
        }
        bool isOnGround;

        /// <summary>
        /// Current user movement input.
        /// </summary>
        private float movement;

        // Jumping state
        private bool isJumping;
        private bool wasJumping;
        private float jumpTime;

        private Rectangle localBounds;
        /// <summary>
        /// Gets a rectangle which bounds this player in world space.
        /// </summary>
        public Rectangle BoundingRectangle
        {
            get
            {
                int left = (int)Math.Round(Position.X - sprite.Origin.X) + localBounds.X;
                int top = (int)Math.Round(Position.Y - sprite.Origin.Y) + localBounds.Y;

                return new Rectangle(left, top, localBounds.Width, localBounds.Height);
            }
        }

        /// <summary>
        /// Constructors a new player.
        /// </summary>
        public Player(Level level, Vector2 position, int health, int maxHealth)
        {
            Health = health;
            MaxHealth = maxHealth;
            effects = new Dictionary<string, AnimationPlayer>();

            this.level = level;

            LoadContent();

            Reset(position);
        }

        /// <summary>
        /// Loads the player sprite sheet and sounds.
        /// </summary>
        public void LoadContent()
        {
            //Load textures
            healthBar = Level.Content.LoadTexture2D("Misc/playerHealthBar");
            healthBarFill = Level.Content.LoadTexture2D("Misc/playerHealthBarFill");

            // Load animated textures.
            idleAnimation = new Animation(Level.Content.LoadTexture2D("Sprites/Player/Idle"), 0.1f, true);
            runAnimation = new Animation(Level.Content.LoadTexture2D("Sprites/Player/Run"), 0.1f, true);
            jumpAnimation = new Animation(Level.Content.LoadTexture2D("Sprites/Player/Jump"), 0.1f, false);
            celebrateAnimation = new Animation(Level.Content.LoadTexture2D("Sprites/Player/Celebrate"), 0.1f, false);
            dieAnimation = new Animation(Level.Content.LoadTexture2D("Sprites/Player/Die"), 0.1f, false);
            shieldAnimation = new Animation(Level.Content.LoadTexture2D("Sprites/Player/Shield"), 0.1f, true);
            healAnimation = new Animation(Level.Content.LoadTexture2D("Sprites/Player/Heal"), 0.1f, true);

            sprite = new AnimationPlayer();

            // Calculate bounds within texture size.            
            int width = (int)(idleAnimation.FrameWidth * 0.4);
            int left = (idleAnimation.FrameWidth - width) / 2;
            int height = (int)(idleAnimation.FrameWidth * 0.8);
            int top = idleAnimation.FrameHeight - height;
            localBounds = new Rectangle(left, top, width, height);

            // Load sounds.            
            killedSound = Level.Content.LoadSoundEffect("Sounds/PlayerKilled");
            jumpSound = Level.Content.LoadSoundEffect("Sounds/PlayerJump");
            fallSound = Level.Content.LoadSoundEffect("Sounds/PlayerFall");
            healSound = Level.Content.LoadSoundEffect("Sounds/Heal");
            
            int toLoad = 0;
            while (System.IO.File.Exists("Content/Sounds/Hurt/" + toLoad + ".xnb"))
            {
                hurtSound.Add(Level.Content.LoadSoundEffect("Sounds/Hurt/" + toLoad));
                toLoad++;
            }
        }

        /// <summary>
        /// Resets the player to life.
        /// </summary>
        /// <param name="position">The position to come to life at.</param>
        public void Reset(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            isAlive = true;
            sprite.PlayAnimation(idleAnimation);
        }

        /// <summary>
        /// Handles input, performs physics, and animates the player sprite.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            if (Health <= 0)
            {
                Health = 0;
                isAlive = false;
            }
            if (Health > MaxHealth)
                Health = MaxHealth;
            if (throwProjTimer > 0)
                throwProjTimer -= gameTime.ElapsedGameTime.Milliseconds;
            if (tempInv > 0)
            {
                tempInv -= gameTime.ElapsedGameTime.Milliseconds;
                if (tempInv <= 0)
                    effects.Remove("invincible");
            }
            if (healEffect > 0)
            {
                healEffect -= gameTime.ElapsedGameTime.Milliseconds;
                if (healEffect <= 0)
                    effects.Remove("heal");
            }
            GetInput();

            ApplyPhysics(gameTime);

            if (IsAlive && IsOnGround)
            {
                if (Math.Abs(Velocity.X) - 0.02f > 0)
                {
                    sprite.PlayAnimation(runAnimation);
                }
                else
                {
                    sprite.PlayAnimation(idleAnimation);
                }
            }

            // Clear input.
            movement = 0.0f;
            isJumping = false;
        }

        /// <summary>
        /// Gets player horizontal movement and jump commands from input.
        /// </summary>
        private void GetInput()
        {
            // Get input state.
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            KeyboardState keyboardState = Keyboard.GetState();

            // Get analog horizontal movement.
            movement = gamePadState.ThumbSticks.Left.X * MoveStickScale;

            // Ignore small movements to prevent running in place.
            if (Math.Abs(movement) < 0.5f)
                movement = 0.0f;

            // If any digital horizontal movement input is found, override the analog movement.
            if (gamePadState.IsButtonDown(Buttons.DPadLeft) ||
                keyboardState.IsKeyDown(Keys.Left) ||
                keyboardState.IsKeyDown(Keys.A))
            {
                movement = -1.0f;
            }
            else if (gamePadState.IsButtonDown(Buttons.DPadRight) ||
                     keyboardState.IsKeyDown(Keys.Right) ||
                     keyboardState.IsKeyDown(Keys.D))
            {
                movement = 1.0f;
            }

            if (keyboardState.IsKeyDown(Keys.Space) && throwProjTimer <= 0 && level.Score >= projPointCost)
            {
                throwProjTimer = resetThrowProj;
                level.Score -= projPointCost;
                level.Projectiles.Add(new Spear(new Vector2(BoundingRectangle.Left + (velocity.X >= 0 ? BoundingRectangle.Width : -BoundingRectangle.Width),
                        BoundingRectangle.Y), level.Content, Velocity.X >= 0 ? true : false));
            }

            // Check if the player wants to jump.
            isJumping =
                gamePadState.IsButtonDown(JumpButton) ||
                keyboardState.IsKeyDown(Keys.Up) ||
                keyboardState.IsKeyDown(Keys.W);
        }

        /// <summary>
        /// Updates the player's velocity and position based on input, gravity, etc.
        /// </summary>
        public void ApplyPhysics(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            Vector2 previousPosition = Position;

            // Base velocity is a combination of horizontal movement control and
            // acceleration downward due to gravity.
            velocity.X += movement * MoveAcceleration * elapsed;
            velocity.Y = MathHelper.Clamp(velocity.Y + GravityAcceleration * elapsed, -MaxFallSpeed, MaxFallSpeed);

            velocity.Y = DoJump(velocity.Y, gameTime);

            // Apply pseudo-drag horizontally.
            if (IsOnGround)
                velocity.X *= GroundDragFactor;
            else
                velocity.X *= AirDragFactor;

            // Prevent the player from running faster than his top speed.            
            velocity.X = MathHelper.Clamp(velocity.X, -MaxMoveSpeed, MaxMoveSpeed);

            // Apply velocity.
            Position += velocity * elapsed;
            Position = new Vector2((float)Math.Round(Position.X), (float)Math.Round(Position.Y));

            // If the player is now colliding with the level, separate them.
            HandleCollisions();

            // If the collision stopped us from moving, reset the velocity to zero.
            if (Position.X == previousPosition.X)
                velocity.X = 0;

            if (Position.Y == previousPosition.Y)
                velocity.Y = 0;
        }

        /// <summary>
        /// Calculates the Y velocity accounting for jumping and
        /// animates accordingly.
        /// </summary>
        /// <remarks>
        /// During the accent of a jump, the Y velocity is completely
        /// overridden by a power curve. During the decent, gravity takes
        /// over. The jump velocity is controlled by the jumpTime field
        /// which measures time into the accent of the current jump.
        /// </remarks>
        /// <param name="velocityY">
        /// The player's current velocity along the Y axis.
        /// </param>
        /// <returns>
        /// A new Y velocity if beginning or continuing a jump.
        /// Otherwise, the existing Y velocity.
        /// </returns>
        private float DoJump(float velocityY, GameTime gameTime)
        {
            // If the player wants to jump
            if (isJumping)
            {
                // Begin or continue a jump
                if ((!wasJumping && IsOnGround) || jumpTime > 0.0f)
                {
                    if (jumpTime == 0.0f)
                        jumpSound.Play();

                    jumpTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    sprite.PlayAnimation(jumpAnimation);
                }

                // If we are in the ascent of the jump
                if (0.0f < jumpTime && jumpTime <= MaxJumpTime)
                {
                    // Fully override the vertical velocity with a power curve that gives players more control over the top of the jump
                    velocityY = JumpLaunchVelocity * (1.0f - (float)Math.Pow(jumpTime / MaxJumpTime, JumpControlPower));
                }
                else
                {
                    // Reached the apex of the jump
                    jumpTime = 0.0f;
                }
            }
            else
            {
                // Continues not jumping or cancels a jump in progress
                jumpTime = 0.0f;
            }
            wasJumping = isJumping;

            return velocityY;
        }

        /// <summary>
        /// Detects and resolves all collisions between the player and his neighboring
        /// tiles. When a collision is detected, the player is pushed away along one
        /// axis to prevent overlapping. There is some special logic for the Y axis to
        /// handle platforms which behave differently depending on direction of movement.
        /// </summary>
        private void HandleCollisions()
        {
            // Get the player's bounding rectangle and find neighboring tiles.
            Rectangle bounds = BoundingRectangle;
            int leftTile = (int)Math.Floor((float)bounds.Left / Tile.Width);
            int rightTile = (int)Math.Ceiling(((float)bounds.Right / Tile.Width)) - 1;
            int topTile = (int)Math.Floor((float)bounds.Top / Tile.Height);
            int bottomTile = (int)Math.Ceiling(((float)bounds.Bottom / Tile.Height)) - 1;

            // Reset flag to search for ground collision.
            isOnGround = false;

            // For each potentially colliding tile,
            for (int y = topTile; y <= bottomTile; ++y)
            {
                for (int x = leftTile; x <= rightTile; ++x)
                {
                    // If this tile is collidable,
                    TileCollision collision = Level.GetCollision(x, y);
                    if (collision != TileCollision.Passable)
                    {
                        // Determine collision depth (with direction) and magnitude.
                        Rectangle tileBounds = Level.GetBounds(x, y);
                        Vector2 depth = RectangleExtensions.GetIntersectionDepth(bounds, tileBounds);
                        if (depth != Vector2.Zero)
                        {
                            float absDepthX = Math.Abs(depth.X);
                            float absDepthY = Math.Abs(depth.Y);

                            // Resolve the collision along the shallow axis.
                            if (absDepthY < absDepthX || collision == TileCollision.Platform)
                            {
                                // If we crossed the top of a tile, we are on the ground.
                                if (previousBottom <= tileBounds.Top)
                                    isOnGround = true;

                                // Ignore platforms, unless we are on the ground.
                                if (collision == TileCollision.Impassable || IsOnGround)
                                {
                                    // Resolve the collision along the Y axis.
                                    Position = new Vector2(Position.X, Position.Y + depth.Y);

                                    // Perform further collisions with the new bounds.
                                    bounds = BoundingRectangle;
                                }
                            }
                            else if (collision == TileCollision.Impassable) // Ignore platforms.
                            {
                                // Resolve the collision along the X axis.
                                Position = new Vector2(Position.X + depth.X, Position.Y);

                                // Perform further collisions with the new bounds.
                                bounds = BoundingRectangle;

                                isJumping = false;
                            }
                        }
                    }
                }
            }

            // Save the new bounds bottom.
            previousBottom = bounds.Bottom;
        }

        /// <summary>
        /// Called when the player has been killed.
        /// </summary>
        /// <param name="killedBy">
        /// The enemy who killed the player. This parameter is null if the player was
        /// not killed by an enemy (fell into a hole).
        /// </param>
        public void OnKilled(Enemy killedBy)
        {
            Health = 0;
            isAlive = false;

            if (killedBy != null)
                killedSound.Play();
            else
                fallSound.Play();

            sprite.PlayAnimation(dieAnimation);
        }

        /// <summary>
        /// Called when this player reaches the level's exit.
        /// </summary>
        public void OnReachedExit()
        {
            sprite.PlayAnimation(celebrateAnimation);
        }

        /// <summary>
        /// Draws the animated player.
        /// </summary>
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Flip the sprite to face the way we are moving.
            if (Velocity.X > 0)
                flip = SpriteEffects.FlipHorizontally;
            else if (Velocity.X < 0)
                flip = SpriteEffects.None;

            // Draw that sprite.
            sprite.Draw(gameTime, spriteBatch, Position, flip);
            foreach (var effectsSprite in effects)
                effectsSprite.Value.Draw(gameTime, spriteBatch, position + new Vector2(0, 5), flip);

            //Draw healthbar
            Rectangle toDraw = new Rectangle(0, 0, (int)((float)healthBarFill.Width * Health / MaxHealth), healthBarFill.Height),
                hpDisp = new Rectangle(BoundingRectangle.X - 30, BoundingRectangle.Y - healthBar.Height + 10, healthBar.Width / 3,
                    healthBar.Height / 3),
                hpFill = new Rectangle(hpDisp.X + 4, hpDisp.Y + 3, toDraw.Width / 3, healthBarFill.Height / 3);
            spriteBatch.Draw(healthBar, hpDisp, Color.White);
            spriteBatch.Draw(healthBarFill, hpFill, toDraw, Color.White);
        }

        public void HurtPlayer(int dmg)
        {
            if (tempInv > 0)
                return;
            hurtSound[level.Content.Random.Next(hurtSound.Count)].Play(1, 0, 0);
            Health -= dmg;
            tempInv = tempInvReset;
            effects.Add("invincible", new AnimationPlayer());
            effects["invincible"].PlayAnimation(shieldAnimation);
        }

        public void HealPlayer(int healedHP, int maxHP)
        {
            Health += healedHP;
            MaxHealth += maxHP;
            healEffect = healEffectReset;
            healSound.Play(1, 0, 0);
            if (healEffect <= 0)
            {
                effects.Add("heal", new AnimationPlayer());
                effects["heal"].PlayAnimation(healAnimation);
            }
        }

        public void PoisonPlayer(int poisonedHP, int poisonedMaxHP)
        {
            Health += poisonedHP;
            MaxHealth += poisonedMaxHP;
            hurtSound[level.Content.Random.Next(hurtSound.Count)].Play(1, 0, 0);
        }
    }
}
