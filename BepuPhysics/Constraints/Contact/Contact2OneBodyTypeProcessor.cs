﻿using BepuUtilities.Memory;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace BepuPhysics.Constraints.Contact
{

    public struct Contact2OneBodyPrestepData
    {
        //NOTE: Prestep data memory layout is relied upon by the constraint description for marginally more efficient setting and getting.
        //If you modify this layout, be sure to update the associated ContactManifold4Constraint.
        //Note that this layout is defined by the execution order in the prestep. The function accesses it sequentially to ensure the prefetcher can do its job.
        public Vector3Wide OffsetA0;
        public Vector3Wide OffsetA1;
        public Vector<float> FrictionCoefficient;
        //In a convex manifold, all contacts share the same normal and tangents.
        public Vector3Wide Normal;
        //All contacts also share the spring settings.
        public SpringSettingsWide SpringSettings;
        public Vector<float> MaximumRecoveryVelocity;
        public Vector<float> PenetrationDepth0;
        public Vector<float> PenetrationDepth1;
    }
    
    //The key observation here is that we have 7DOFs worth of constraints that all share the exact same bodies.
    //Despite the potential premultiplication optimizations, we focus on a few big wins:
    //1) Sharing the inverse mass for the impulse->velocity projection across all constraints.
    //2) Sharing the normal as much as possible.
    //3) Resorting to iteration-side redundant calculation if it reduces memory bandwidth.
    //This is expected to slow down the single threaded performance when running on a 128 bit SIMD machine.
    //However, when using multiple threads, memory bandwidth very rapidly becomes a concern.
    //In fact, a hypothetical CLR and machine that supported AVX512 would hit memory bandwidth limits on the older implementation that used 2032 bytes per bundle for projection data...
    //on a single thread.

    public struct Contact2OneBodyProjection
    {
        public BodyInertias InertiaA;
        public Vector<float> PremultipliedFrictionCoefficient;
        public Vector3Wide Normal;
        public TangentFrictionOneBody.Projection Tangent;
        public PenetrationLimit2OneBody.Projection Penetration;
        //Lever arms aren't included in the twist projection because the number of arms required varies independently of the twist projection itself.
        public Vector<float> LeverArm0;
        public Vector<float> LeverArm1;
        public TwistFrictionProjection Twist;
    }

    //TODO: at the time of writing (May 19 2017 2.0.0-preview2-25309-07), using the 'loop body structdelegate' style introduces additional inits and overhead 
    //relative to a manually inlined version. That isn't fundamental. With any luck, future compilers will change things. 
    //Since the difference is less than 5%, we'll use the loopbodystructdelegate approach for other constraints until the incremental performance improvement 
    //of manual inlining is worth it.
    public struct Contact2OneBodyFunctions :
        IOneBodyConstraintFunctions<Contact2OneBodyPrestepData, Contact2OneBodyProjection, Contact2AccumulatedImpulses>
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Prestep(Bodies bodies, ref Vector<int> bodyReferences, int count,
            float dt, float inverseDt, ref Contact2OneBodyPrestepData prestep, out Contact2OneBodyProjection projection)
        {
            //Some speculative compression options not (yet) pursued:
            //1) Store the surface basis in a compressed fashion. It could be stored within 32 bits by using standard compression schemes, but we lack the necessary
            //instructions to properly SIMDify the decode operation (e.g. shift). Even with the potential savings of 3 floats (relative to uncompressed storage), it would be questionable.
            //We could drop one of the four components of the quaternion and reconstruct it relatively easily- that would just require that the encoder ensures the W component is positive.
            //It would require a square root, but it might still be a net win. On an IVB, sqrt has a 7 cycle throughput. 4 bytes saved * 4 lanes = 16 bytes, which takes 
            //about 16 / 5.5GBps = 2.9ns, where 5.5 is roughly the per-core bandwidth on a 3770K. 7 cycles is only 2ns at 3.5ghz. 
            //There are a couple of other instructions necessary to decode, but sqrt is by far the heaviest; it's likely a net win.
            //Be careful about the execution order here. It should be aligned with the prestep data layout to ensure prefetching works well.

            bodies.GatherInertia(ref bodyReferences, count, out projection.InertiaA);
            Vector3Wide.Add(ref prestep.OffsetA0, ref prestep.OffsetA1, out var offsetToManifoldCenterA);
            var scale = new Vector<float>(0.5f);
            Vector3Wide.Scale(ref offsetToManifoldCenterA, ref scale, out offsetToManifoldCenterA);
            projection.PremultipliedFrictionCoefficient = scale * prestep.FrictionCoefficient;
            projection.Normal = prestep.Normal;
            Helpers.BuildOrthnormalBasis(ref prestep.Normal, out var x, out var z);
            TangentFrictionOneBody.Prestep(ref x, ref z, ref offsetToManifoldCenterA, ref projection.InertiaA, out projection.Tangent);
            PenetrationLimit2OneBody.Prestep(ref projection.InertiaA, ref prestep.Normal, ref prestep, dt, inverseDt, out projection.Penetration);
            //Just assume the lever arms for B are the same. It's a good guess. (The only reason we computed the offset B is because we didn't want to go into world space.)
            Vector3Wide.Distance(ref prestep.OffsetA0, ref offsetToManifoldCenterA, out projection.LeverArm0);
            Vector3Wide.Distance(ref prestep.OffsetA1, ref offsetToManifoldCenterA, out projection.LeverArm1);
            TwistFrictionOneBody.Prestep(ref projection.InertiaA, ref prestep.Normal, out projection.Twist);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void WarmStart(ref BodyVelocities wsvA, ref Contact2OneBodyProjection projection, ref Contact2AccumulatedImpulses accumulatedImpulses)
        {
            Helpers.BuildOrthnormalBasis(ref projection.Normal, out var x, out var z);
            TangentFrictionOneBody.WarmStart(ref x, ref z, ref projection.Tangent, ref projection.InertiaA, ref accumulatedImpulses.Tangent, ref wsvA);
            PenetrationLimit2OneBody.WarmStart(ref projection.Penetration, ref projection.InertiaA,
                ref projection.Normal,
                ref accumulatedImpulses.Penetration0,
                ref accumulatedImpulses.Penetration1, ref wsvA);
            TwistFrictionOneBody.WarmStart(ref projection.Normal, ref projection.InertiaA, ref accumulatedImpulses.Twist, ref wsvA);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Solve(ref BodyVelocities wsvA, ref Contact2OneBodyProjection projection, ref Contact2AccumulatedImpulses accumulatedImpulses)
        {
            Helpers.BuildOrthnormalBasis(ref projection.Normal, out var x, out var z);
            var maximumTangentImpulse = projection.PremultipliedFrictionCoefficient *
                (accumulatedImpulses.Penetration0 + accumulatedImpulses.Penetration1);
            TangentFrictionOneBody.Solve(ref x, ref z, ref projection.Tangent, ref projection.InertiaA, ref maximumTangentImpulse, ref accumulatedImpulses.Tangent, ref wsvA);
            //Note that we solve the penetration constraints after the friction constraints. 
            //This makes the penetration constraints more authoritative at the cost of the first iteration of the first frame of an impact lacking friction influence.
            //It's a pretty minor effect either way.
            PenetrationLimit2OneBody.Solve(ref projection.Penetration, ref projection.InertiaA, ref projection.Normal,
                ref accumulatedImpulses.Penetration0,
                ref accumulatedImpulses.Penetration1, ref wsvA);
            var maximumTwistImpulse = projection.PremultipliedFrictionCoefficient * (
                accumulatedImpulses.Penetration0 * projection.LeverArm0 +
                accumulatedImpulses.Penetration1 * projection.LeverArm1);
            TwistFrictionOneBody.Solve(ref projection.Normal, ref projection.InertiaA, ref projection.Twist, ref maximumTwistImpulse, ref accumulatedImpulses.Twist, ref wsvA);
        }

    }

    /// <summary>
    /// Handles the solve iterations of a bunch of 2-contact convex manifold constraints.
    /// </summary>
    public class Contact2OneBodyTypeProcessor :
        OneBodyTypeProcessor<Contact2OneBodyPrestepData, Contact2OneBodyProjection, Contact2AccumulatedImpulses, Contact2OneBodyFunctions>
    {
        public const int BatchTypeId = 1;
    }
}