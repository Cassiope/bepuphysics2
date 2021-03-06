﻿using BepuUtilities.Memory;
using DemoRenderer;
using DemoUtilities;
using BepuPhysics;
using System;
using System.Collections.Generic;
using System.Text;
using DemoRenderer.UI;
using DemoContentLoader;

namespace Demos
{
    public abstract class Demo : IDisposable
    {
        /// <summary>
        /// Gets the simulation created by the demo's Initialize call.
        /// </summary>
        public Simulation Simulation { get; protected set; }

        //Note that the buffer pool used by the simulation is not considered to be *owned* by the simulation. The simulation merely uses the pool.
        //Disposing the simulation will not dispose or clear the buffer pool.
        /// <summary>
        /// Gets the buffer pool used by the demo's simulation.
        /// </summary>
        public BufferPool BufferPool { get; private set; }

        /// <summary>
        /// Gets the thread dispatcher available for use by the simulation.
        /// </summary>
        public SimpleThreadDispatcher ThreadDispatcher { get; private set; }

        protected Demo()
        {
            BufferPool = new BufferPool();
            ThreadDispatcher = new SimpleThreadDispatcher(Environment.ProcessorCount);
        }

        public abstract void Initialize(ContentArchive content, Camera camera);

        public virtual void Update(Window window, Camera camera, Input input, float dt)
        {
            //In the demos, we use one time step per frame. We don't bother modifying the physics time step duration for different monitors so different refresh rates
            //change the rate of simulation. This doesn't actually change the result of the simulation, though, and the simplicity is a good fit for the demos.
            //In the context of a 'real' application, you could instead use a time accumulator to take time steps of fixed length as needed, or
            //fully decouple simulation and rendering rates across different threads.
            //(In either case, you'd also want to interpolate or extrapolate simulation results during rendering for smoothness.)
            //Note that taking steps of variable length can reduce stability. Gradual or one-off changes can work reasonably well.
            Simulation.Timestep(1 / 60f, ThreadDispatcher);
        }

        public virtual void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
        }

        protected virtual void OnDispose()
        {

        }

        bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;
                OnDispose();
                Simulation.Dispose();
                BufferPool.Clear();
                ThreadDispatcher.Dispose();
            }
        }

#if DEBUG
        ~Demo()
        {
            Helpers.CheckForUndisposed(disposed, this);
        }
#endif
    }
}
