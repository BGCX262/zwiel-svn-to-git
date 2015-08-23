using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Media;
using System.Xml;

namespace Zwiel_Platformer
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PlatformerGame : Microsoft.Xna.Framework.Game
    {
        // Resources for drawing.
        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        // Global content.
        private SpriteFont hudFont, hudLargeFont;

        private Texture2D winOverlay;
        private Texture2D loseOverlay;
        private Texture2D diedOverlay;

        // Meta-level game state.
        private int levelIndex = -1;
        private int score = 0;
        private Level level;
        private bool wasContinuePressed;

        // When the time remaining is less than the warning time, it blinks on the hud
        private static readonly TimeSpan WarningTime = TimeSpan.FromSeconds(30);

        private const int TargetFrameRate = 60;
        private const int BackBufferWidth = 1280;
        private const int BackBufferHeight = 720;
        private const Buttons ContinueButton = Buttons.A;

        private string userName;
        private string currentLevel = null;
        private int playerHealth = 100, playerMaxHealth = 100;

        public PlatformerGame()
        {
            userName = System.Windows.Forms.SystemInformation.UserName;
            if (!File.Exists(userName + ".sav"))
                File.Copy("Worlds/worlds.xml", userName + ".sav");
            LoadSavedGame();

            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferWidth = BackBufferWidth;
            graphics.PreferredBackBufferHeight = BackBufferHeight;

            Content.RootDirectory = "Content";

            // Framerate differs between platforms.
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / TargetFrameRate);
        }
        private void LoadSavedGame()
        {
            using (XmlReader reader = XmlReader.Create(userName + ".sav"))
            {
                while (reader.Read())
                {
                    if (reader.MoveToContent() == XmlNodeType.Element)
                    {
                        string name = reader.Name.ToLower();
                        if (name == "save")
                        {
                            currentLevel = reader.GetAttribute("currentLevel");
                            if (reader.GetAttribute("score") != null)
                                score = int.Parse(reader.GetAttribute("score"));
                            if (reader.GetAttribute("playerHP") != null)
                                playerHealth = int.Parse(reader.GetAttribute("playerHP"));
                            if (reader.GetAttribute("playerMaxHP") != null)
                                playerMaxHealth = int.Parse(reader.GetAttribute("playerMaxHP"));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // Load fonts
            hudFont = Content.Load<SpriteFont>("Fonts/Hud");
            hudLargeFont = Content.Load<SpriteFont>("Fonts/HudLarge");

            // Load overlay textures
            winOverlay = Content.Load<Texture2D>("Overlays/you_win");
            loseOverlay = Content.Load<Texture2D>("Overlays/you_lose");
            diedOverlay = Content.Load<Texture2D>("Overlays/you_died");

            MediaPlayer.IsRepeating = true;
            MediaPlayer.Play(Content.Load<Song>("Sounds/Music"));

            LoadNextLevel();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            HandleInput();

            level.Update(gameTime);

            base.Update(gameTime);
        }

        private void HandleInput()
        {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamepadState = GamePad.GetState(PlayerIndex.One);

            bool continuePressed = keyboardState.IsKeyDown(Keys.Up) ||
                gamepadState.IsButtonDown(ContinueButton);

            // Perform the appropriate action to advance the game and
            // to get the player back to playing.
            if (!wasContinuePressed && continuePressed)
            {
                if (!level.Player.IsAlive)
                {
                    level.StartNewLife();
                }
                else if (level.TimeRemaining == TimeSpan.Zero)
                {
                    if (level.ReachedExit)
                    {
                        this.score = level.Score;
                        this.playerHealth = level.Player.Health;
                        this.playerMaxHealth = level.Player.MaxHealth;
                        LoadNextLevel();
                    }
                    else
                    {
                        playerHealth = playerMaxHealth;
                        ReloadCurrentLevel();
                    }
                }
            }

            wasContinuePressed = continuePressed;
        }

        private void LoadNextLevel()
        {
            // Find the path of the next level.
            string levelPath;

            // Loop here so we can try again when we can't find a level.
            if (level != null && level.NextLevel != null)
            {
                levelPath = level.NextLevel;
                currentLevel = levelPath;
            }
            else if (currentLevel != null)
                levelPath = currentLevel;
            else
            {
                while (true)
                {
                    // Try to find the next level. They are sequentially numbered txt files.
                    levelPath = String.Format("Levels/{0}.txt", ++levelIndex);
                    levelPath = Path.Combine(StorageContainer.TitleLocation, "Content/" + levelPath);
                    if (File.Exists(levelPath))
                        break;

                    // If there isn't even a level 0, something has gone wrong.
                    if (levelIndex == 0)
                        throw new Exception("No levels found.");

                    // Whenever we can't find a level, start over again at 0.
                    levelIndex = -1;
                }
            }

            // Unloads the content for the current level before loading the next one.
            if (level != null)
                level.Dispose();

            // Load the level.
            SaveGame();
            level = new Level(Services, levelPath, this.score, playerHealth, playerMaxHealth);
        }
        private void SaveGame()
        {
            using (XmlWriter writer = XmlWriter.Create(userName + ".sav"))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("save");
                writer.WriteAttributeString("currentLevel", currentLevel);
                writer.WriteAttributeString("score", score.ToString());
                writer.WriteAttributeString("playerHP", playerHealth.ToString());
                writer.WriteAttributeString("playerMaxHP", playerMaxHealth.ToString());
                writer.WriteEndElement();
                writer.WriteEndDocument();
            }
        }

        private void ReloadCurrentLevel()
        {
            score = 0;
            --levelIndex;
            LoadNextLevel();
        }

        /// <summary>
        /// Draws the game from background to foreground.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);


            level.Draw(gameTime, spriteBatch);
            if (level.Player.Health != playerHealth)
                playerHealth = level.Player.Health;
            if (level.Player.MaxHealth != playerMaxHealth)
                playerMaxHealth = level.Player.MaxHealth;

            DrawHud();

            base.Draw(gameTime);
        }

        private void DrawHud()
        {
            Rectangle titleSafeArea = GraphicsDevice.Viewport.TitleSafeArea;
            Vector2 hudLocation = new Vector2(titleSafeArea.X, titleSafeArea.Y);
            Vector2 center = new Vector2(titleSafeArea.X + titleSafeArea.Width / 2.0f,
                                         titleSafeArea.Y + titleSafeArea.Height / 2.0f);

            // Draw time remaining. Uses modulo division to cause blinking when the
            // player is running out of time.
            spriteBatch.Begin();

            DrawShadowedString(hudFont, "LEVEL", hudLocation + new Vector2(titleSafeArea.Width / 2.5f, 5), Color.Yellow);
            DrawShadowedString(hudLargeFont, level.LevelName, hudLocation + new Vector2(titleSafeArea.Width / 2.5f, 30), Color.Red);
            string timeString = "TIME: " + level.TimeRemaining.Minutes.ToString("00") + ":" + level.TimeRemaining.Seconds.ToString("00");
            Color timeColor;
            if (level.TimeRemaining > WarningTime ||
                level.ReachedExit ||
                (int)level.TimeRemaining.TotalSeconds % 2 == 0)
            {
                timeColor = Color.Yellow;
            }
            else
            {
                timeColor = Color.Red;
            }
            DrawShadowedString(hudFont, timeString, hudLocation, timeColor);

            DrawShadowedString(hudFont, "Health: " + playerHealth.ToString() + "/" + playerMaxHealth.ToString(), new Vector2(0, 65), Color.Yellow);

            // Draw score
            float timeHeight = hudFont.MeasureString(timeString).Y;
            DrawShadowedString(hudFont, "SCORE: " + level.Score.ToString(), hudLocation + new Vector2(0.0f, timeHeight * 1.2f), Color.Yellow);

            // Determine the status overlay message to show.
            Texture2D status = null;
            if (level.TimeRemaining == TimeSpan.Zero)
            {
                if (level.ReachedExit)
                {
                    status = winOverlay;
                }
                else
                {
                    status = loseOverlay;
                }
            }
            else if (!level.Player.IsAlive)
            {
                status = diedOverlay;
            }

            if (status != null)
            {
                // Draw status message.
                Vector2 statusSize = new Vector2(status.Width, status.Height);
                spriteBatch.Draw(status, center - statusSize / 2, Color.White);
            }

            spriteBatch.End();
        }

        private void DrawShadowedString(SpriteFont font, string value, Vector2 position, Color color)
        {
            spriteBatch.DrawString(font, value, position + new Vector2(1.0f, 1.0f), Color.Black);
            spriteBatch.DrawString(font, value, position, color);
        }
    }
}
