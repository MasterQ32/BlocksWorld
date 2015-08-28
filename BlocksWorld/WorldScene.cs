﻿using Jitter.Collision;
using Jitter.Collision.Shapes;
using Jitter.Dynamics;
using Jitter.LinearMath;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace BlocksWorld
{
    public class WorldScene : Scene
    {
        World world;
        WorldRenderer renderer;
        DebugRenderer debug;

        int objectShader;

        double totalTime = 0.0;
        private Player player;
        private TextureArray textures;

        private Network network;
        private BasicReceiver receiver;

        MeshModel playerModel;

        Dictionary<int, Vector4> proxies = new Dictionary<int, Vector4>();

        int networkUpdateCounter = 0;

        public WorldScene()
        {
            this.world = new World();

            this.debug = new DebugRenderer();
            this.renderer = new WorldRenderer(this.world);

            this.network = new Network(new TcpClient("localhost", 4523));
            this.receiver = new BasicReceiver(this.network, this.world);

            this.network[NetworkPhrase.LoadWorld] = this.LoadWorldFromNetwork;
            this.network[NetworkPhrase.SpawnPlayer] = this.SpawnPlayer;
            this.network[NetworkPhrase.UpdateProxy] = this.UpdateProxy;
            this.network[NetworkPhrase.DestroyProxy] = this.DestroyProxy;
        }

        private void UpdateProxy(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            float rot = reader.ReadSingle();

            // Takes creation into account as well
            proxies[id] = new Vector4(x, y, z, rot);
        }

        private void DestroyProxy(BinaryReader reader)
        {
            int id = reader.ReadInt32();
            proxies.Remove(id);
        }

        private void SpawnPlayer(BinaryReader reader)
        {
            float x = reader.ReadSingle();
            float y = reader.ReadSingle();
            float z = reader.ReadSingle();
            this.CreatePlayer(new JVector(x, y, z));
        }

        private void LoadWorldFromNetwork(BinaryReader reader)
        {
            this.world.Load(reader.BaseStream, true);
        }

        void CreatePlayer(JVector spawn)
        {
            this.player = new Player(this.world);
            this.player.Tool = new BlockPlaceTool(this.network, this.world);
            this.player.Position = spawn;

            this.world.AddBody(this.player);
            this.world.AddConstraint(new Jitter.Dynamics.Constraints.SingleBody.FixedAngle(this.player));
        }

        protected override void OnLoad()
        {
            this.objectShader = Shader.CompileFromResource(
                "BlocksWorld.Shaders.Object.vs",
                "BlocksWorld.Shaders.Object.fs");

            this.textures = TextureArray.LoadFromResource("BlocksWorld.Textures.Blocks.png");

            this.playerModel = MeshModel.LoadFromResource(
                "BlocksWorld.Models.Player.bwm");

            this.debug.Load();
            this.renderer.Load();
        }

        public override void UpdateFrame(IGameInputDriver input, double time)
        {
            this.totalTime += time;

            this.network.Dispatch();

            if (this.player != null)
                this.player.UpdateFrame(input, time);

            lock (typeof(World))
            {
                this.world.Step((float)time, true);
            }

            if (input.GetButtonDown(Key.F5))
                this.world.Save("world.dat");

            this.networkUpdateCounter++;
            if (this.networkUpdateCounter > 5)
            {
                this.network.Send(NetworkPhrase.SetPlayer, (s) =>
                {
                    var pos = this.player.FeetPosition;
                    s.Write(pos.X);
                    s.Write(pos.Y);
                    s.Write(pos.Z);
                    s.Write(this.player.BodyRotation);
                });
                this.networkUpdateCounter = 0;
            }
        }

        public override void RenderFrame(double time)
        {
            this.debug.Reset();


            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);
            GL.ClearColor(Color4.LightSkyBlue);
            GL.ClearDepth(1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

            var cam = this.player?.Camera ?? new StaticCamera()
            {
                Eye = new Vector3(0, 8, 0),
                Target = new Vector3(16, 2, 16)
            };

            Matrix4 worldViewProjection =
                Matrix4.Identity *
                cam.CreateViewMatrix() *
                cam.CreateProjectionMatrix(1280.0f / 720.0f); // HACK: Hardcoded aspect

            // Draw world
            {
                GL.UseProgram(this.objectShader);
                
                int loc = GL.GetUniformLocation(this.objectShader, "uTextures");
                if (loc >= 0)
                {
                    GL.Uniform1(loc, 0);
                }

                loc = GL.GetUniformLocation(this.objectShader, "uWorldViewProjection");
                if (loc >= 0)
                {
                    GL.UniformMatrix4(loc, false, ref worldViewProjection);
                }

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2DArray, this.textures.ID);

                this.renderer.Render(cam, time);

                foreach (var player in this.proxies.Concat(new[] { new KeyValuePair<int, Vector4>(-1, new Vector4(this.player.FeetPosition, this.player.BodyRotation))}))
                {
                    worldViewProjection =
                       Matrix4.CreateRotationY(player.Value.W) *
                       Matrix4.CreateTranslation(player.Value.Xyz) *
                       cam.CreateViewMatrix() *
                       cam.CreateProjectionMatrix(1280.0f / 720.0f); // HACK: Hardcoded aspect
                    if (loc >= 0)
                    {
                        GL.UniformMatrix4(loc, false, ref worldViewProjection);
                    }

                    this.playerModel.Render(cam, time);
                }

                GL.BindTexture(TextureTarget.Texture2DArray, 0);
                GL.UseProgram(0);
            }

            foreach (RigidBody body in this.world.RigidBodies)
            {
                body.DebugDraw(this.debug);
            }

            /*
            if (this.focus != null)
            {
                this.debug.DrawPoint(this.focus.Position);
                this.debug.DrawLine(this.focus.Position, this.focus.Position + 0.25f * this.focus.Normal);
            }
            */

            this.debug.Render(cam, time);
        }
    }
}