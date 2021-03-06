﻿using System;
using Jitter;
using Jitter.LinearMath;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace BlocksWorld
{
    public sealed class DebugRenderer : IRenderer, IDebugDrawer, IDisposable
    {
        Shader debugShader;

        int vao;
        int buffer;

        List<Vector3> points = new List<Vector3>();
        List<Vector3> lines = new List<Vector3>();
        List<Vector3> triangles = new List<Vector3>();

        public DebugRenderer()
        {

        }

        ~DebugRenderer()
        {
            this.Dispose();
        }

        public void DrawLine(JVector start, JVector end)
        {
            lines.Add(start.TK()); lines.Add(end.TK());
        }

        public void DrawPoint(JVector pos)
        {
            points.Add(pos.TK());
        }

        public void DrawTriangle(JVector pos1, JVector pos2, JVector pos3)
        {
            triangles.AddRange(new[] { pos1.TK(), pos2.TK(), pos3.TK() });
        }

        public void Render(Camera camera, double time)
        {
            Matrix4 worldViewProjection =
                Matrix4.Identity *
                camera.CreateViewMatrix() *
                camera.CreateProjectionMatrix(1280.0f / 720.0f); // HACK: Hardcoded aspect

            this.debugShader.UseProgram();
            GL.UniformMatrix4(this.debugShader["uWorldViewProjection"], false, ref worldViewProjection);
            GL.Uniform3(this.debugShader["uColor"], Vector3.UnitX);

            GL.PointSize(5.0f);
            this.DrawBuffer(PrimitiveType.Points, this.points);
            this.DrawBuffer(PrimitiveType.Lines, this.lines);
            this.DrawBuffer(PrimitiveType.Triangles, this.triangles);

            GL.UseProgram(0);
        }

        public void Reset()
        {
            this.points.Clear();
            this.lines.Clear();
            this.triangles.Clear();
        }

        void DrawBuffer(PrimitiveType type, List<Vector3> vertices)
        {
            int vertexCount = vertices.Count;
            if (vertexCount <= 0)
            {
                return;
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, this.buffer);
            {
                var data = vertices.ToArray();
                GL.BufferData(
                    BufferTarget.ArrayBuffer,
                    new IntPtr(Vector3.SizeInBytes * data.Length),
                    data,
                    BufferUsageHint.StaticDraw);
            }
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);


            GL.BindVertexArray(this.vao);
            GL.DrawArrays(type, 0, vertexCount);
            GL.BindVertexArray(0);
        }

        internal void Load()
        {
            this.debugShader = Shader.CompileFromResource(
                "BlocksWorld.Shaders.Object.vs",
                "BlocksWorld.Shaders.Debug.fs");

            this.buffer = GL.GenBuffer();
            this.vao = GL.GenVertexArray();

            GL.BindVertexArray(this.vao);
            {
                GL.BindBuffer(BufferTarget.ArrayBuffer, this.buffer);

                GL.EnableVertexAttribArray(0);
                GL.VertexAttribPointer(
                    0,
                    3,
                    VertexAttribPointerType.Float,
                    false,
                    Vector3.SizeInBytes,
                    0);
            }
            GL.BindVertexArray(0);
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            if (this.vao != 0)
                GL.DeleteVertexArray(this.vao);
            if (this.buffer != 0)
                GL.DeleteBuffer(this.buffer);

            this.debugShader?.Dispose();

            this.vao = 0;
            this.buffer = 0;
            this.debugShader = null;
        }
    }
}