#region Using
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

#endregion
namespace Asteroids
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        
        GamePadState lastState = GamePad.GetState(PlayerIndex.One);

        //Camera/View information
        Vector3 cameraPosition = new Vector3(0.0f, 0.0f, GameConstants.CameraHeight);
        float aspectRatio;
        Matrix projectionMatrix;
        Matrix viewMatrix;

        //Audio Components
        AudioEngine audioEngine;
        WaveBank waveBank;
        SoundBank soundBank;
        //Cue so we can hang on to the sound of the engine.
        Cue engineSound = null;

        //Visual components
        Ship ship = new Ship();
        Model asteroidModel;
        Matrix[] asteroidTransforms;
        Asteroids[] asteroidList = new Asteroids[GameConstants.NumAsteroids];
        Random random = new Random();
        Model bulletModel;
        Matrix[] bulletTransforms;
        Bullet[] bulletList = new Bullet[GameConstants.NumBullets];
        Texture2D stars;
        SpriteFont lucidaConsole;
        int score;
        Vector2 scorePosition = new Vector2(100, 50);

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            aspectRatio = (float)GraphicsDeviceManager.DefaultBackBufferWidth / GraphicsDeviceManager.DefaultBackBufferHeight;

        }


        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>

#region Initialize
        protected override void Initialize()
        {
            //audioEngine = new AudioEngine("Content\\Audio\\MyGameAudio.xgs");
            //waveBank = new WaveBank(audioEngine, "Content\\Audio\\Wave Bank.xwb");
            //soundBank = new SoundBank(audioEngine, "Content\\Audio\\Sound Bank.xsb");

            projectionMatrix = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f), GraphicsDevice.DisplayMode.AspectRatio, GameConstants.CameraHeight - 1000.0f, GameConstants.CameraHeight  + 1000.0f);
            
            viewMatrix = Matrix.CreateLookAt(cameraPosition, Vector3.Zero, Vector3.Up);
            ResetAsteroids();

            base.Initialize();
        }
           

        private Matrix[] SetupEffectDefault(Model myModel)
        {
            Matrix[] absoluteTransforms = new Matrix[myModel.Bones.Count];
            myModel.CopyAbsoluteBoneTransformsTo(absoluteTransforms);

            foreach (ModelMesh mesh in myModel.Meshes)
            {
                foreach ( BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.Projection = projectionMatrix;
                    effect.View = viewMatrix;
                }
            }
            return absoluteTransforms;
        }

        private void ResetAsteroids()
        {
            float xStart;
            float yStart;
            for (int i = 0; i < GameConstants.NumAsteroids; i++)
            {
                if (random.Next(2) == 0)
                {
                    xStart = (float)-GameConstants.PlayfieldSizeX;
                }
                else
                {
                    xStart = (float)GameConstants.PlayfieldSizeX;
                }

                yStart = (float)random.NextDouble() * GameConstants.PlayfieldSizeY;
                asteroidList[i].position = new Vector3(xStart, yStart, 0.0f);
                double angle = random.NextDouble() * 2 * Math.PI;
                asteroidList[i].direction.X = -(float)Math.Sin(angle);
                asteroidList[i].direction.Y = (float)Math.Cos(angle);
                asteroidList[i].speed = GameConstants.AsteroidMaxSpeed + (float)random.NextDouble() * GameConstants.AsteroidMaxSpeed;
                asteroidList[i].isActive = true;
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        #endregion

#region LoadContent

        float aspectRation;

        protected override void LoadContent()
        {
            //Create a new sprite, which can be used to draw textures
            spriteBatch = new SpriteBatch(GraphicsDevice);

           ship.Model = Content.Load<Model>("models\\p1_wedge");
           ship.Transforms = SetupEffectDefault(ship.Model);
           asteroidModel = Content.Load<Model>("models\\asteroid1");
           asteroidTransforms = SetupEffectDefault(asteroidModel);
           bulletModel = Content.Load<Model>("models\\pea_proj");
           bulletTransforms = SetupEffectDefault(bulletModel);
           stars = Content.Load<Texture2D>("textures\\B1_stars");
           lucidaConsole = Content.Load<SpriteFont>("fonts/Lucida Console");
            
        }


        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        #endregion

#region UnloadContent
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        #endregion

#region Update
        protected override void Update(GameTime gameTime)
        {
            float timeDelta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed)
                this.Exit();

            //Get some input
            UpdateInput();

            //Update audioEngine
            //audioEngine.Update();

            //Add velocity to the current position.
            ship.Position += ship.Velocity;

            //Bleed off to velocity over time
            ship.Velocity *= 0.95f;

            for (int i = 0; i < GameConstants.NumAsteroids; i++)
            {
                 asteroidList[i].Update(timeDelta);
            }
            for (int i = 0; i < GameConstants.NumBullets; i++)
            {
                if (bulletList[i].isActive)
                {
                    bulletList[i].Update(timeDelta);
                }
            }
           
            //Bullet-asteroid collision check
            for (int i = 0; i < asteroidList.Length; i++)
            {
                if (asteroidList[i].isActive)
                {
                    BoundingSphere asteroidSphere = new BoundingSphere(asteroidList[i].position, asteroidModel.Meshes[0].BoundingSphere.Radius * GameConstants.AsteroidBoundingSphereScale);
                    for (int j = 0; j < bulletList.Length; j++)
                    {
                        if (bulletList[j].isActive)
                        {
                            BoundingSphere bulletSphere = new BoundingSphere(bulletList[j].position, bulletModel.Meshes[0].BoundingSphere.Radius);
                            if (asteroidSphere.Intersects(bulletSphere))
                            {
                                soundBank.PlayCue("explosion2");
                                asteroidList[i].isActive = false;
                                bulletList[j].isActive = false;
                                score += GameConstants.KillBonus;
                                break; //no need to check other bullets
                            }
                        }
                    }
                }
            }

            //Ship-asteroid collision check
            if (ship.isActive)
            {
                BoundingSphere shipSphere = new BoundingSphere(ship.Position, ship.Model.Meshes[0].BoundingSphere.Radius * GameConstants.ShipBoundingSphereScale);
                for (int i = 0; i < asteroidList.Length; i++)
                {
                    if (asteroidList[i].isActive)
                    {
                        BoundingSphere b = new BoundingSphere(asteroidList[i].position, asteroidModel.Meshes[0].BoundingSphere.Radius * GameConstants.AsteroidBoundingSphereScale);
                        if (b.Intersects(shipSphere))
                        {
                            //Blow up ship
                            soundBank.PlayCue("explosion3");
                            ship.isActive = false;
                            asteroidList[i].isActive = false;
                            score -= GameConstants.DeathPenalty;
                            break; //exit the loop
                        }
                    }
                }
            }


            base.Update(gameTime);
        }

        protected void UpdateInput()
        {
            
            

            //Get the game pad state
            GamePadState currentState = GamePad.GetState(PlayerIndex.One);
            if (currentState.IsConnected)
            {
                ship.Update(currentState);

                //Set audio based on whether we're pressing a trigger.
                if (currentState.Triggers.Right > 0)
                {

                    if (engineSound == null)
                    {
                        engineSound = soundBank.GetCue("engine_2");
                        engineSound.Play();
                    }
                    else if (engineSound.IsPaused)
                    {
                        engineSound.Resume();
                    }
                }
                else
                    if (engineSound != null && engineSound.IsPlaying)
                    {
                        engineSound.Pause();
                    }
            }
            

            //In case you get lost, press B to warp back to the center
            if (currentState.Buttons.B == ButtonState.Pressed &&
                    lastState.Buttons.B == ButtonState.Released)
            {
                ship.Position = Vector3.Zero;
                ship.Velocity = Vector3.Zero;
                ship.Rotation = 0.0f;
                ship.isActive = true;
                score -= GameConstants.WarpPenalty;
                //Make a sounf when warp.
                soundBank.PlayCue("hyperspace_activate");
            }

            //Shooting...?
            if (ship.isActive && currentState.Buttons.A == ButtonState.Pressed &&
                    lastState.Buttons.A == ButtonState.Released || Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                //add another bullet.  Find an inactive bullet slot and use it
                //if all bullets slots are used, ignore the user input
                for (int i = 0; i < GameConstants.NumBullets; i++)
                {
                    if (!bulletList[i].isActive)
                    {
                        bulletList[i].direction = ship.RotationMatrix.Forward;
                        bulletList[i].speed = GameConstants.BulletSpeedAdjustment;
                        bulletList[i].position = ship.Position +
                  (200 * bulletList[i].direction);
                        bulletList[i].isActive = true;
                        score -= GameConstants.ShotPenalty;
                        //soundBank.PlayCue("tx0_fire1");

                        break; //exit the loop     
                    }
                }
                
            }
            lastState = currentState;
            
        }

#endregion

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
       
#region Draw
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.CornflowerBlue);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.Draw(stars, new Rectangle (0, 0, 800, 600), Color.White);
            spriteBatch.End();
            Matrix shipTransformMatrix = ship.RotationMatrix * Matrix.CreateTranslation(ship.Position);
            if (ship.isActive)
            {
                DrawModel(ship.Model, shipTransformMatrix, ship.Transforms);
            }
            for (int i = 0; i < GameConstants.NumAsteroids; i++)
            {
                Matrix asteroidTransform = Matrix.CreateTranslation(asteroidList[i].position);
                if (asteroidList[i].isActive)
                {
                    DrawModel(asteroidModel, asteroidTransform, asteroidTransforms);
                }
            }
                for (int i = 0; i < GameConstants.NumBullets; i++)
                {
                    if (bulletList[i].isActive)
                    {
                        Matrix bulletTransform =
                        Matrix.CreateTranslation(bulletList[i].position);
                        DrawModel(bulletModel, bulletTransform, bulletTransforms);
                    }

                    spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
            spriteBatch.DrawString(lucidaConsole, "Score: " + score, scorePosition, Color.LightGreen);
            spriteBatch.End();
            base.Draw(gameTime);
        }
    }

        public static void DrawModel(Model model, Matrix modelTransform, Matrix[] absoluteBoneTranforms)
        {
            //Draw the model, a model can have multiple meshes, so loop
            foreach (ModelMesh mesh in model.Meshes)
            {
                //This is where the mesh orientation is set
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.World = absoluteBoneTranforms[mesh.ParentBone.Index] * modelTransform;
                }
                //Draw the mesh, will use the effects set above
                mesh.Draw();

                }     
            }
        }
    }

        #endregion