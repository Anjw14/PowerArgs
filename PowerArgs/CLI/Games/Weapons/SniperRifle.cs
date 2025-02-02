﻿using PowerArgs.Cli;
using PowerArgs.Cli.Physics;
using System;
using System.Collections.Generic;

namespace PowerArgs.Games
{

    public class SniperRifleHitEventArgs
    {
        public SniperRifle Rifle { get; set; }
        public SpacialElement ElementHit { get; set; }
    }

    public class SniperRifle : Weapon
    {
        public static Event<SniperRifle> OnMiss { get; private set; } = new Event<SniperRifle>();
        public static Event<SniperRifleHitEventArgs> OnHit { get; private set; } = new Event<SniperRifleHitEventArgs>();

        public override WeaponStyle Style => WeaponStyle.Primary;
        public override void FireInternal()
        {
            if (Holder.Target != null)
            {
                DamageBroker.Instance.ReportImpact(new Impact()
                {
                    HitType = HitType.Obstacle,
                    ObstacleHit = Holder.Target,
                    MovingObject= Holder,
                    Angle = Holder.CalculateAngleTo(Holder.Target)
                });
                OnHit.Fire(new SniperRifleHitEventArgs() { Rifle = this, ElementHit = Holder.Target });
            }
            else
            {
                OnMiss.Fire(this);
            }
        }
    }

    public class AimLineOptions
    {
        public IRectangularF From { get; set; }
        public IRectangularF To { get; set; }
        public ILifetimeManager LifetimeManager { get; set; }
        public int Z { get; set; } = -34543;
    }



    public class AimLineSegment : SpacialElement
    {
        public AimLineSegment(int z, ConsoleCharacter pen)
        {
            this.Pen = pen;
            this.ResizeTo(1, 1);
            this.MoveTo(0, 0, z);
        }
    }

    [SpacialElementBinding(typeof(AimLineSegment))]
    public class AimLineSegmentRenderer : SpacialElementRenderer
    {
        protected override void OnPaint(ConsoleBitmap context) => context.DrawPoint((Element as AimLineSegment).Pen.Value, 0, 0);
    }

    public class AimLineHandle
    {
        private List<AimLineSegment> elements;
        public AimLineHandle(List<AimLineSegment> elements)
        {
            this.elements = elements;
        }
    }

    public static class AimLine
    {
        public const char DefaultWireChar = '.';

        public static AimLineHandle Connect(AimLineOptions options)
        {
            var aimLineSegments = new List<AimLineSegment>();

            var d = options.From.CalculateDistanceTo(options.To)-1;
            var angle = options.From.CalculateAngleTo(options.To);
            for (var i = 1; i < d; i++)
            {
                var location = options.From.MoveTowards(angle, i);
                var segment = SpaceTime.CurrentSpaceTime.Add(new AimLineSegment(options.Z, new ConsoleCharacter(DefaultWireChar, ConsoleColor.Cyan)));
                segment.MoveTo(location.Left, location.Top);
                aimLineSegments.Add(segment);
            }

            options.LifetimeManager.OnDisposed(() =>
            {
                foreach (var segment in aimLineSegments)
                {
                    segment.Lifetime.Dispose();
                }
            });

            return new AimLineHandle(aimLineSegments);
        }
    }
}
