﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuPhysics.CollisionDetection.SweepTasks;
using BepuUtilities;
using BepuUtilities.Memory;
using DemoContentLoader;
using DemoRenderer;
using DemoRenderer.Constraints;
using DemoRenderer.UI;
using DemoUtilities;
using Quaternion = BepuUtilities.Quaternion;

namespace Demos.SpecializedTests
{
    public class SimplexWalkerTestDemo : Demo
    {
        Buffer<LineInstance> shapeLines;
        List<SimplexWalkerStep> steps;
        Vector3 basePosition;

        public override void Initialize(ContentArchive content, Camera camera)
        {
            camera.Position = new Vector3(-13f, 6, -13f);
            camera.Yaw = MathF.PI * 3f / 4;
            camera.Pitch = MathF.PI * 0.05f;
            Simulation = Simulation.Create(BufferPool, new DemoNarrowPhaseCallbacks(), new DemoPoseIntegratorCallbacks(new Vector3(0, -10, 0)));
            {
                var shapeA = new Cylinder(0.5f, 1f);
                var poseA = new RigidPose(new Vector3(0, 0, 0));
                var shapeB = new Cylinder(1f, 2f);
                //var positionB = new Vector3(-0.2570486f, 1.780561f, -1.033215f);
                //var localOrientationBMatrix = new Matrix3x3
                //{
                //    X = new Vector3(0.9756086f, 0.1946615f, 0.101463f),
                //    Y = new Vector3(-0.1539477f, 0.9362175f, -0.3159063f),
                //    Z = new Vector3(-0.1564862f, 0.2925809f, 0.9433496f)
                //};
                var positionB = new Vector3(-2.437585f, 1.386236f, -2.124907f);
                var localOrientationBMatrix = new Matrix3x3
                {
                    X = new Vector3(-0.7615921f, 0.001486331f, -0.648055f),
                    Y = new Vector3(0.6341797f, 0.2075436f, -0.7448099f),
                    Z = new Vector3(-0.1333926f, -0.9782246f, -0.1590062f)
                };
                //var poseB = new RigidPose(new Vector3(-0.2570486f, 1.780561f, -1.033215f), Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1, 1, 1)), MathF.PI * 0.35f));
                var poseB = new RigidPose(positionB, Quaternion.CreateFromRotationMatrix(localOrientationBMatrix));

                basePosition = default;
                shapeLines = MinkowskiShapeVisualizer.CreateLines<Cylinder, CylinderWide, CylinderSupportFinder, Cylinder, CylinderWide, CylinderSupportFinder>(
                    shapeA, shapeB, poseA, poseB, 65536,
                    0.01f, new Vector3(0.4f, 0.4f, 0),
                    0.1f, new Vector3(0, 1, 0), default, basePosition, BufferPool);

                var aWide = default(CylinderWide);
                var bWide = default(CylinderWide);
                aWide.Broadcast(shapeA);
                bWide.Broadcast(shapeB);
                var worldOffsetB = poseB.Position - poseA.Position;
                var localOrientationB = Matrix3x3.CreateFromQuaternion(Quaternion.Concatenate(poseB.Orientation, Quaternion.Conjugate(poseA.Orientation)));
                var localOffsetB = Quaternion.Transform(worldOffsetB, Quaternion.Conjugate(poseA.Orientation));
                Vector3Wide.Broadcast(localOffsetB, out var localOffsetBWide);
                Matrix3x3Wide.Broadcast(localOrientationB, out var localOrientationBWide);
                var cylinderSupportFinder = default(CylinderSupportFinder);

                var initialNormal = Vector3.Normalize(localOffsetB);
                Vector3Wide.Broadcast(initialNormal, out var initialNormalWide);
                steps = new List<SimplexWalkerStep>();
                SimplexWalker<Cylinder, CylinderWide, CylinderSupportFinder, Cylinder, CylinderWide, CylinderSupportFinder>.FindMinimumDepth(
                    aWide, bWide, localOffsetBWide, localOrientationBWide, ref cylinderSupportFinder, ref cylinderSupportFinder, initialNormalWide, new Vector<int>(), new Vector<float>(1e-7f), new Vector<float>(-500),
                    out var depthWide2, out var localNormalWide2, steps, 1000);

                //const int iterationCount = 1000;
                //var start = Stopwatch.GetTimestamp();
                //for (int i = 0; i < iterationCount; ++i)
                //{
                //    PlaneWalker<Cylinder, CylinderWide, CylinderSupportFinder, Cylinder, CylinderWide, CylinderSupportFinder>.FindMinimumDepth(
                //        aWide, bWide, localOffsetBWide, localOrientationBWide, ref cylinderSupportFinder, ref cylinderSupportFinder, initialNormalWide, new Vector<int>(), new Vector<float>(1e-7f), new Vector<float>(-500), out var depthWide3, out var localNormalWide3, null, 1000);
                //}
                //var stop = Stopwatch.GetTimestamp();
                //var span = (stop - start) * 1e9f / (iterationCount * (double)Stopwatch.Frequency);
                //Console.WriteLine($"Time (ns): {span}");
            }
            //{
            //    var shapeA = new Box(0.5f, 0.5f, 0.5f);
            //    var poseA = new RigidPose(new Vector3(0, 0, 0));
            //    var shapeB = new Box(0.5f, 0.5f, 0.5f);
            //    var poseB = new RigidPose(new Vector3(0.4f, -0.55f, 0.45f), Quaternion.CreateFromAxisAngle(Vector3.Normalize(new Vector3(1, 1, 1)), MathF.PI * 0.35f));

            //    basePosition = default;
            //    shapeLines = MinkowskiShapeVisualizer.CreateLines<Box, BoxWide, BoxSupportFinder, Box, BoxWide, BoxSupportFinder>(
            //        shapeA, shapeB, poseA, poseB, 65536,
            //        0.01f, new Vector3(0.4f, 0.4f, 0),
            //        0.1f, new Vector3(0, 1, 0), default, basePosition, BufferPool);

            //    var aWide = default(BoxWide);
            //    var bWide = default(BoxWide);
            //    aWide.Broadcast(shapeA);
            //    bWide.Broadcast(shapeB);
            //    var worldOffsetB = poseB.Position - poseA.Position;
            //    var localOrientationB = Matrix3x3.CreateFromQuaternion(Quaternion.Concatenate(poseB.Orientation, Quaternion.Conjugate(poseA.Orientation)));
            //    var localOffsetB = Quaternion.Transform(worldOffsetB, Quaternion.Conjugate(poseA.Orientation));
            //    Vector3Wide.Broadcast(localOffsetB, out var localOffsetBWide);
            //    Matrix3x3Wide.Broadcast(localOrientationB, out var localOrientationBWide);
            //    var cylinderSupportFinder = default(BoxSupportFinder);

            //    var initialNormal = Vector3.Normalize(localOffsetB);
            //    Vector3Wide.Broadcast(initialNormal, out var initialNormalWide);
            //    steps = new List<PlaneWalkerStep>();
            //    PlaneWalker<Box, BoxWide, BoxSupportFinder, Box, BoxWide, BoxSupportFinder>.FindMinimumDepth(
            //        aWide, bWide, localOffsetBWide, localOrientationBWide, ref cylinderSupportFinder, ref cylinderSupportFinder, initialNormalWide, new Vector<int>(), new Vector<float>(1e-7f), new Vector<float>(-0.1f), out var depthWide2, out var localNormalWide2, steps, 1000);
            //    const int iterationCount = 100000;
            //    var start = Stopwatch.GetTimestamp();
            //    for (int i = 0; i < iterationCount; ++i)
            //    {
            //        PlaneWalker<Box, BoxWide, BoxSupportFinder, Box, BoxWide, BoxSupportFinder>.FindMinimumDepth(
            //          aWide, bWide, localOffsetBWide, localOrientationBWide, ref cylinderSupportFinder, ref cylinderSupportFinder, initialNormalWide, new Vector<int>(), new Vector<float>(1e-7f), new Vector<float>(-0.1f), out var depthWide3, out var localNormalWide3, null, 1000);
            //    }
            //    var stop = Stopwatch.GetTimestamp();
            //    var span = (stop - start) * 1e9f / (iterationCount * (double)Stopwatch.Frequency);
            //    Console.WriteLine($"Time (ns): {span}");

            //}
        }

        int stepIndex;
        public override void Update(Window window, Camera camera, Input input, float dt)
        {
            if (input.TypedCharacters.Contains('x'))
            {
                stepIndex = Math.Max(0, stepIndex - 1);
            }
            else if (input.TypedCharacters.Contains('c'))
            {
                stepIndex = Math.Min(stepIndex + 1, steps.Count - 1);
            }
            base.Update(window, camera, input, dt);
        }

        public override void Render(Renderer renderer, Camera camera, Input input, TextBuilder text, Font font)
        {
            MinkowskiShapeVisualizer.Draw(shapeLines, renderer);
            renderer.TextBatcher.Write(
                text.Clear().Append($"Enumerate step with X and C. Current step: ").Append(stepIndex + 1).Append(" out of ").Append(steps.Count),
                new Vector2(32, renderer.Surface.Resolution.Y - 140), 20, new Vector3(1), font);
            var step = steps[stepIndex];
            renderer.TextBatcher.Write(
                text.Clear().Append($"Simplex normal source: ").Append(step.NormalSource.ToString()),
                new Vector2(32, renderer.Surface.Resolution.Y - 120), 20, new Vector3(1), font);
            renderer.TextBatcher.Write(
               text.Clear().Append($"Best depth: ").Append(MathF.Min(step.A.Depth, MathF.Min(step.B.Depth, step.C.Depth)), 9),
               new Vector2(32, renderer.Surface.Resolution.Y - 100), 20, new Vector3(1), font);
            SimplexWalkerVertex best, medium, worst;
            if (step.A.Depth < step.B.Depth)
            {
                if (step.A.Depth < step.C.Depth)
                {
                    best = step.A;
                    if (step.B.Depth < step.C.Depth)
                    {
                        medium = step.B;
                        worst = step.C;
                    }
                    else
                    {
                        medium = step.C;
                        worst = step.B;
                    }
                }
                else
                {
                    best = step.C;
                    medium = step.A;
                    worst = step.B;
                }
            }
            else
            {
                if (step.B.Depth < step.C.Depth)
                {
                    best = step.B;
                    if (step.C.Depth < step.A.Depth)
                    {
                        medium = step.C;
                        worst = step.A;
                    }
                    else
                    {
                        worst = step.A;
                        medium = step.C;
                    }
                }
                else
                {
                    best = step.C;
                    medium = step.B;
                    worst = step.A;
                }
            }
            renderer.Lines.Allocate() = new LineInstance(step.A.Support + basePosition, step.B.Support + basePosition, new Vector3(0, 0.6f, 0.1f), default);
            renderer.Lines.Allocate() = new LineInstance(step.B.Support + basePosition, step.C.Support + basePosition, new Vector3(0, 0.6f, 0.1f), default);
            renderer.Lines.Allocate() = new LineInstance(step.C.Support + basePosition, step.A.Support + basePosition, new Vector3(0, 0.6f, 0.1f), default);

            renderer.Lines.Allocate() = new LineInstance(best.Support + basePosition, best.Support + basePosition + best.Normal, new Vector3(0, 1f, 0), default);
            renderer.Lines.Allocate() = new LineInstance(medium.Support + basePosition, medium.Support + basePosition + medium.Normal, new Vector3(0, 0.6f, 0.1f), default);
            renderer.Lines.Allocate() = new LineInstance(worst.Support + basePosition, worst.Support + basePosition + worst.Normal, new Vector3(0, 0.3f, 0.2f), default);

            switch (step.NormalSource)
            {
                case SimplexNormalSource.Edge:
                    renderer.Lines.Allocate() = new LineInstance(step.InterpolatedSupport + basePosition, step.InterpolatedSupport + basePosition + step.InterpolatedNormal, new Vector3(0.5f, 0.75f, 0.1f), default);
                    renderer.Lines.Allocate() = new LineInstance(step.InterpolatedSupport + basePosition, step.InterpolatedSupport + step.NextNormal + basePosition, new Vector3(1f, 0, 0.4f), default);
                    renderer.Lines.Allocate() = new LineInstance(step.InterpolatedSupport + basePosition, step.PointOnOriginLine + basePosition, new Vector3(1f, 0, 0.4f), default);
                    break;
                case SimplexNormalSource.TriangleNormal:
                    var center = (step.A.Support + step.B.Support + step.C.Support) / 3;
                    renderer.Lines.Allocate() = new LineInstance(center + basePosition, center + step.NextNormal + basePosition, new Vector3(1f, 0, 0.4f), default);
                    break;
                case SimplexNormalSource.Barycentric:
                    renderer.Lines.Allocate() = new LineInstance(step.InterpolatedSupport + basePosition, step.InterpolatedSupport + basePosition + step.NextNormal, new Vector3(1f, 0, 0.4f), default);
                    break;
            }
            base.Render(renderer, camera, input, text, font);
        }
    }
}
