﻿using Client.Data;
using Client.Data.BMD;
using Client.Main.Content;
using Client.Main.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Client.Main.Objects
{
    public abstract class WorldObject
    {
        private GraphicsDevice _graphicsDevice;

        private VertexBuffer[] _boneVertexBuffers;
        private IndexBuffer[] _boneIndexBuffers;
        private Texture2D[] _boneTextures;

        private Matrix[] _boneMatrix;
        private int _currentAction = 0;
        private int _priorAction = 0;
        private int _boneHead;
        private float _bodyHeight;
        private float _bodyScale = 1;
        private BoundingSphere _originalBoundingSphere;
        private BoundingSphere _transformedBoundingSphere;
        private bool OutOfView = true;
        private BasicEffect _effect;
        private Vector3 _position, _angle;
        private float _scale = 1f;
        private bool _invalidatedBuffers = true;
        public string ObjectName => GetType().Name;

        public float Alpha { get; set; } = 1f;
        public Vector3 Position { get => _position; set { _position = value; UpdateWorldPosition(); } }
        public Vector3 Angle { get => _angle; set { _angle = value; UpdateWorldPosition(); } }
        public float Scale { get => _scale; set { _scale = value; UpdateWorldPosition(); } }
        public Vector3 Light { get; set; } = new Vector3(0.3f, 0.3f, 0.3f);
        public bool LightEnabled { get; set; } = true;
        public BMD Model { get; set; }
        public bool Ready => Model != null;
        public bool Visible => Ready && !OutOfView;
        public WorldControl World { get; set; }
        public Matrix WorldPosition { get; set; }
        public ushort Type { get; set; }

        public float BlendMeshLight { get; set; } = 1f;
        public float BodyHeight { get; private set; }

        public virtual Task Load(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;

            if (Model == null)
            {
                Debug.WriteLine($"Model is not assigned for {ObjectName}");
                return Task.CompletedTask;
            }

            _effect = new BasicEffect(graphicsDevice)
            {
                TextureEnabled = true,
                VertexColorEnabled = true,
                World = Matrix.Identity,
            };

            _boneMatrix = new Matrix[Model.Bones.Length];

            UpdateWorldPosition();

            return Task.CompletedTask;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (!Ready) return;

            OutOfView = Camera.Instance.Frustum.Contains(_transformedBoundingSphere) == ContainmentType.Disjoint;

            if (OutOfView)
                return;

            _effect.View = Camera.Instance.View;
            _effect.Projection = Camera.Instance.Projection;

            Animation(gameTime);
            SetDynamicBuffers();
        }

        private void UpdateWorldPosition()
        {
            _invalidatedBuffers = true;

            WorldPosition = Matrix.CreateScale(Scale)
                                   * Matrix.CreateFromQuaternion(MathUtils.AngleQuaternion(Angle))
                                   * Matrix.CreateTranslation(Position);

            UpdateBoundings();
        }

        private void UpdateBoundings()
        {
            _transformedBoundingSphere = TransformBoundingSphere(_originalBoundingSphere, WorldPosition);
        }

        private static BoundingSphere TransformBoundingSphere(BoundingSphere sphere, Matrix transform)
        {
            transform.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);
            float maxScale = Math.Max(scale.X, Math.Max(scale.Y, scale.Z));
            Vector3 transformedCenter = Vector3.Transform(sphere.Center, transform);
            return new BoundingSphere(transformedCenter, sphere.Radius * maxScale);
        }

        private void Animation(GameTime gameTime)
        {
            float animationSpeed = 3f;
            float currentFrame = (float)(gameTime.TotalGameTime.TotalSeconds * animationSpeed);

            currentFrame %= Model.Actions[_currentAction].NumAnimationKeys;
            var priorFrame = Math.Max(0, currentFrame - 1);

            Animation(currentFrame, priorFrame, _priorAction);
        }

        public virtual void Draw(GameTime gameTime)
        {
            if (!Visible) return;

            if (_boneVertexBuffers == null)
                return;

            _graphicsDevice.BlendState = BlendState.AlphaBlend;
            _effect.Alpha = Alpha;

            for (var i = 0; i < _boneVertexBuffers.Length; i++)
            {
                _effect.Texture = _boneTextures[i];

                var vertexBuffer = _boneVertexBuffers[i];
                var indexBuffer = _boneIndexBuffers[i];
                var primitiveCount = indexBuffer.IndexCount / 3;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    _graphicsDevice.SetVertexBuffer(vertexBuffer);
                    _graphicsDevice.Indices = indexBuffer;
                    _graphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, primitiveCount);
                }
            }
        }

        private void SetDynamicBuffers()
        {
            if (!_invalidatedBuffers)
                return;

            _boneVertexBuffers ??= new VertexBuffer[Model.Meshes.Length];
            _boneIndexBuffers ??= new IndexBuffer[Model.Meshes.Length];
            _boneTextures ??= new Texture2D[Model.Meshes.Length];

            for (int meshIndex = 0; meshIndex < Model.Meshes.Length; meshIndex++)
            {
                var mesh = Model.Meshes[meshIndex];

                var terrainLight = World.Terrain.RequestTerrainLight(Position.X, Position.Y);

                if (!LightEnabled)
                    terrainLight *= 0.1f;

                terrainLight += Light;

                var bodyLight = new Color(terrainLight.X, terrainLight.Y, terrainLight.Z);

                _boneVertexBuffers[meshIndex]?.Dispose();
                _boneIndexBuffers[meshIndex]?.Dispose();

                BMDLoader.Instance.GetModelBuffers(Model, meshIndex, bodyLight, _boneMatrix, out var vertexBuffer, out var indexBuffer);

                _boneVertexBuffers[meshIndex] = vertexBuffer;
                _boneIndexBuffers[meshIndex] = indexBuffer;

                if (_boneTextures[meshIndex] == null)
                    _boneTextures[meshIndex] = TextureLoader.Instance.GetTexture2D(BMDLoader.Instance.GetTexturePath(Model, mesh.TexturePath));

                _invalidatedBuffers = false;
            }
        }

        private void Animation(float currentFrame, float priorFrame, int priorAction)
        {
            if (Model.Actions.Length <= 0)
                return;

            if (priorAction >= Model.Actions.Length) priorAction = 0;
            if (_currentAction >= Model.Actions.Length) _currentAction = 0;

            int currentAnimationFrame = (int)currentFrame;
            float interpolationFactor = currentFrame - currentAnimationFrame;

            int priorAnimationFrame = (int)priorFrame;
            if (priorAnimationFrame < 0) priorAnimationFrame = 0;
            if (currentAnimationFrame < 0) currentAnimationFrame = 0;

            var priorActionData = Model.Actions[priorAction];
            var currentActionData = Model.Actions[_currentAction];

            if (priorAnimationFrame >= priorActionData.NumAnimationKeys) priorAnimationFrame = 0;
            if (currentAnimationFrame >= currentActionData.NumAnimationKeys) currentAnimationFrame = 0;

            GenerateBoneMatrix(priorAction, _currentAction, priorAnimationFrame, currentAnimationFrame, interpolationFactor);
        }

        private void GenerateBoneMatrix(int priorAction, int currentAction, int priorAnimationFrame, int currentAnimationFrame, float interpolationFactor)
        {
            var priorActionData = Model.Actions[priorAction];
            var currentActionData = Model.Actions[currentAction];

            for (int i = 0; i < Model.Bones.Length; i++)
            {
                var bone = Model.Bones[i];

                if (bone == BMDTextureBone.Dummy)
                    continue;

                var bm1 = bone.Matrixes[priorAction];
                var bm2 = bone.Matrixes[_currentAction];

                var q1 = bm1.Quaternion[priorAnimationFrame];
                var q2 = bm2.Quaternion[currentAnimationFrame];

                var boneQuaternion = q1 != q2
                    ? Quaternion.Slerp(q1, q2, interpolationFactor)
                    : q1;

                Matrix matrix = Matrix.CreateFromQuaternion(boneQuaternion);

                Vector3 position1 = bm1.Position[priorAnimationFrame];
                Vector3 position2 = bm2.Position[currentAnimationFrame];
                Vector3 interpolatedPosition = Vector3.Lerp(position1, position2, interpolationFactor);

                if (i == 0 && (priorActionData.LockPositions || currentActionData.LockPositions))
                {
                    matrix.M41 = bm2.Position[0].X;
                    matrix.M42 = bm2.Position[0].Y;
                    matrix.M43 = position1.Z * (1 - interpolationFactor) + position2.Z * interpolationFactor + BodyHeight;
                }
                else
                {
                    matrix.Translation = interpolatedPosition;
                }

                Matrix newMatrix;

                if (bone.Parent != -1)
                    newMatrix = matrix * _boneMatrix[bone.Parent];
                else
                    newMatrix = matrix * WorldPosition;

                if (_boneMatrix[i] != newMatrix)
                {
                    _boneMatrix[i] = newMatrix;
                    _invalidatedBuffers = true;
                }
            }
        }
    }
}
