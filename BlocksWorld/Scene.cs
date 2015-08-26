﻿using System;

namespace BlocksWorld
{
    public abstract class Scene
    {
        public virtual void RenderFrame(double time)
        {

        }

        public virtual void UpdateFrame(IGameInputDriver input, double time)
        {

        }

        public virtual void Load()
        {
        }
    }
}