﻿using Client.Main.Content;
using Client.Main.Controls;
using Client.Main.Scenes;
using Client.Main.Worlds;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Main
{
    public class MuGame : Game
    {
        public static MuGame Instance { get; private set; }

        private GraphicsDeviceManager _graphics;
        public static Random Random { get; } = new Random();

        public BaseScene ActiveScene;
        public SpriteBatch SpriteBatch { get; private set; }
        public SpriteFont Font { get; private set; }
        public RenderTarget2D EffectRenderTarget { get; private set; }
        public BlendState InverseDestinationBlend { get; private set; }
        public Effect AlphaRGBEffect { get; set; }

        public int Width => _graphics.PreferredBackBufferWidth;
        public int Height => _graphics.PreferredBackBufferHeight;

        public Texture2D Pixel { get; private set; }
        public MouseState Mouse { get; private set; }

        public MuGame()
        {
            Instance = this;

            _graphics = new GraphicsDeviceManager(this);
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;

            InverseDestinationBlend = new BlendState
            {
                ColorSourceBlend = Blend.InverseDestinationColor,
                ColorDestinationBlend = Blend.One,
                AlphaSourceBlend = Blend.One,
                AlphaDestinationBlend = Blend.One,
                BlendFactor = Color.White
            };

            if (Constants.UNLIMITED_FPS)
            {
                _graphics.SynchronizeWithVerticalRetrace = false;
                IsFixedTimeStep = false;
            }

            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        public void ChangeScene<T>() where T : BaseScene, new()
        {
            ActiveScene?.Dispose();
            ActiveScene = new T();
        }

        protected override void Initialize()
        {
            IsMouseVisible = false;
            base.Initialize();
        }
        protected override void LoadContent()
        {
            BMDLoader.Instance.SetGraphicsDevice(GraphicsDevice);
            TextureLoader.Instance.SetGraphicsDevice(GraphicsDevice);

            Pixel = new Texture2D(GraphicsDevice, 1, 1);
            Pixel.SetData(new[] { Color.White });
            EffectRenderTarget = new RenderTarget2D(GraphicsDevice, 800, 600);
            AlphaRGBEffect = Content.Load<Effect>("AlphaRGB");
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            Font = Content.Load<SpriteFont>("Arial");
            ChangeScene<GameScene>();
        }

        protected override void Update(GameTime gameTime)
        {
            Mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();
            ActiveScene?.Update(gameTime);
            base.Update(gameTime);
        }

        public DepthStencilState DisableDepthMask = new()
        {
            DepthBufferEnable = true,
            DepthBufferWriteEnable = false
        };

        protected override void Draw(GameTime gameTime)
        {
            FPSCounter.Instance.CalcFPS(gameTime);

            GraphicsDevice.SetRenderTarget(EffectRenderTarget);
            GraphicsDevice.Clear(Color.Black);
            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.Black);

            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.BlendState = BlendState.Opaque;

            if (ActiveScene?.Status == GameControlStatus.Ready)
                ActiveScene?.Draw(gameTime);

            SpriteBatch.Begin();
            SpriteBatch.DrawString(Font, $"FPS: {(int)FPSCounter.Instance.FPS_AVG}", new Vector2(10, 10), Color.White);
            if (ActiveScene.World != null && ActiveScene.World is WalkableWorldControl walkableWorld)
                SpriteBatch.DrawString(Font, $"PX: {walkableWorld.PositionX}, PY: {walkableWorld.PositionY}", new Vector2(10, 30), Color.White);
            SpriteBatch.End();

            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            base.Draw(gameTime);
        }
    }
}
