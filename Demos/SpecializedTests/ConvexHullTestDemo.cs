﻿using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using BepuPhysics.Collidables;
using BepuUtilities.Collections;
using DemoContentLoader;
using DemoRenderer;
using BepuPhysics;
using DemoRenderer.UI;
using DemoUtilities;
using DemoRenderer.Constraints;
using static BepuPhysics.Collidables.ConvexHullHelper;
using System.Diagnostics;

namespace Demos.SpecializedTests
{
    public class ConvexHullTestDemo : Demo
    {
        QuickList<Vector3> points;
        //List<DebugStep> debugSteps;
        public override void Initialize(ContentArchive content, Camera camera)
        {
            camera.Position = new Vector3(0, 5, 10);
            camera.Yaw = 0;
            camera.Pitch = 0;

            Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(), new DemoPoseIntegratorCallbacks(new Vector3(0, 0, 0)));

            const int pointCount = 128;
            points = new QuickList<Vector3>(pointCount * 2, BufferPool);
            //points.Allocate(BufferPool) = new Vector3(0, 0, 0);
            //points.Allocate(BufferPool) = new Vector3(0, 0, 1);
            //points.Allocate(BufferPool) = new Vector3(0, 1, 0);
            //points.Allocate(BufferPool) = new Vector3(0, 1, 1);
            //points.Allocate(BufferPool) = new Vector3(1, 0, 0);
            //points.Allocate(BufferPool) = new Vector3(1, 0, 1);
            //points.Allocate(BufferPool) = new Vector3(1, 1, 0);
            //points.Allocate(BufferPool) = new Vector3(1, 1, 1);
            var random = new Random(5);
            for (int i = 0; i < pointCount; ++i)
            {
                points.AllocateUnsafely() = new Vector3((float)random.NextDouble(), 1 * (float)random.NextDouble(), (float)random.NextDouble());
                //points.AllocateUnsafely() = new Vector3(0, 1, 0) + Vector3.Normalize(new Vector3((float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1, (float)random.NextDouble() * 2 - 1)) * (float)random.NextDouble();
            }

            var pointsBuffer = points.Span.Slice(0, points.Count);
            ConvexHullHelper.ComputeHull(pointsBuffer, BufferPool, out var hullData);
            hullData.Dispose(BufferPool);
            const int iterationCount = 100;
            var start = Stopwatch.GetTimestamp();
            for (int i = 0; i < iterationCount; ++i)
            {
                ConvexHullHelper.ComputeHull(pointsBuffer, BufferPool, out hullData);
                hullData.Dispose(BufferPool);
            }
            var end = Stopwatch.GetTimestamp();
            Console.WriteLine($"Hull computation time (us): {(end - start) * 1e6 / (iterationCount * Stopwatch.Frequency)}");

            ConvexHullHelper.CreateShape(pointsBuffer, BufferPool, out var hullShape);

            hullShape.ComputeInertia(1, out var inertia, out var center);

            Simulation.Bodies.Add(BodyDescription.CreateDynamic(new Vector3(0, 0, 0), inertia, new CollidableDescription(Simulation.Shapes.Add(hullShape), 0.1f), new BodyActivityDescription(0.01f)));
        }

        int stepIndex = 0;

        public override void Update(Window window, Camera camera, Input input, float dt)
        {
            //if (input.TypedCharacters.Contains('x'))
            //{
            //    stepIndex = Math.Max(stepIndex - 1, 0);
            //}
            //if (input.TypedCharacters.Contains('c'))
            //{
            //    stepIndex = Math.Min(stepIndex + 1, debugSteps.Count - 1);
            //}
            base.Update(window, camera, input, dt);
        }

        public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
            //var step = debugSteps[stepIndex];
            //var scale = 10f;
            //for (int i = 0; i < points.Count; ++i)
            //{
            //    var pose = new RigidPose(points[i] * scale);
            //    renderer.Shapes.AddShape(new Box(0.1f, 0.1f, 0.1f), Simulation.Shapes, ref pose, new Vector3(0.5f, 0.5f, 0.5f));
            //    if (!step.AllowVertex[i])
            //        renderer.Shapes.AddShape(new Box(0.6f, 0.25f, 0.25f), Simulation.Shapes, ref pose, new Vector3(1, 0, 0));
            //}
            //for (int i = 0; i < step.Raw.Count; ++i)
            //{
            //    var pose = new RigidPose(points[step.Raw[i]] * scale);
            //    renderer.Shapes.AddShape(new Box(0.25f, 0.6f, 0.25f), Simulation.Shapes, ref pose, new Vector3(0, 0, 1));
            //}
            //for (int i = 0; i < step.Reduced.Count; ++i)
            //{
            //    var pose = new RigidPose(points[step.Reduced[i]] * scale);
            //    renderer.Shapes.AddShape(new Box(0.25f, 0.25f, 0.6f), Simulation.Shapes, ref pose, new Vector3(0, 1, 0));
            //}
            //for (int i = 0; i <= stepIndex; ++i)
            //{
            //    var pose = RigidPose.Identity;
            //    var oldStep = debugSteps[i];
            //    for (int j = 2; j < oldStep.Reduced.Count; ++j)
            //    {
            //        renderer.Shapes.AddShape(new Triangle
            //        {
            //            A = points[oldStep.Reduced[0]] * scale,
            //            B = points[oldStep.Reduced[j]] * scale,
            //            C = points[oldStep.Reduced[j - 1]] * scale
            //        }, Simulation.Shapes, ref pose, new Vector3(1, 0, 1));

            //    }
            //}
            //var edgeMidpoint = (points[step.SourceEdge.A] + points[step.SourceEdge.B]) * scale * 0.5f;
            //renderer.Lines.Allocate() = new LineInstance(edgeMidpoint, edgeMidpoint + step.BasisX * scale * 0.5f, new Vector3(1, 1, 0), new Vector3());
            //renderer.Lines.Allocate() = new LineInstance(edgeMidpoint, edgeMidpoint + step.BasisY * scale * 0.5f, new Vector3(0, 1, 0), new Vector3());
            //renderer.TextBatcher.Write(
            //    text.Clear().Append($"Enumerate step with X and C. Current step: ").Append(stepIndex + 1).Append(" out of ").Append(debugSteps.Count),
            //    new Vector2(32, renderer.Surface.Resolution.Y - 140), 20, new Vector3(1), font);

            base.Render(renderer, camera, input, text, font);
        }
    }
}
