using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Flight;

public class FlightRideableSeat : EntityRideableSeat
{
    private readonly Matrixf bankTransform = new Matrixf();

    public FlightRideableSeat(IMountable mountable, string seatId, SeatConfig config)
        : base(mountable, seatId, config) { }

    public override Matrixf RenderTransform
    {
        get
        {
            Matrixf baseTransform = base.RenderTransform;
            bool legacySeat = Config.MountOffset != null;
            if (Entity.World.Side != EnumAppSide.Client || Passenger == null || legacySeat)
            {
                return baseTransform;
            }

            IAnimator animator = Entity.AnimManager?.Animator;
            if (animator == null || (!IsBankRunning(animator, "flightbankleft") && !IsBankRunning(animator, "flightbankright")))
            {
                return baseTransform;
            }

            AttachmentPointAndPose attachment = animator.GetAttachmentPointPose(Config.APName);
            if (attachment == null) return baseTransform;

            float[] pose = attachment.AnimModelMatrix;
            float roll = (float)Math.Atan2(pose[6], pose[5]);
            float yaw = Entity.Pos.Yaw + GameMath.PIHALF;
            
            Vec3f riderOffset = Config.RiderOffset;
            float offsetX = riderOffset?.X ?? 0;
            float offsetY = riderOffset?.Y ?? 0;
            float offsetZ = riderOffset?.Z ?? 0;

            return bankTransform.Identity()
                .RotateY(yaw)
                .Translate(-offsetX, -offsetY, -offsetZ)
                .RotateX(roll)
                .Translate(offsetX, offsetY, offsetZ)
                .RotateY(-yaw)
                .Mul(baseTransform);
        }
    }

    private static bool IsBankRunning(IAnimator animator, string animation)
    {
        return animator.GetAnimationState(animation)?.Running == true;
    }
}
