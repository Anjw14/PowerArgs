﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PowerArgs.Cli.Physics
{
    public static class SpacialAwareness
    {
        public const string PassThruTag = "passthru";

        public static bool HasLineOfSight(this IRectangularF from, IRectangularF to, List<IRectangularF> obstacles, float increment = .1f)
        {
            IRectangularF current = from;
            var currentDistance = current.CalculateDistanceTo(to);
            var firstDistance = currentDistance;
            while (currentDistance > increment)
            {
                current = RectangularF.Create(MoveTowards(current.Center(), to.Center(), 1), current);
                current = RectangularF.Create(current.Left - current.Width / 2, current.Top - current.Height / 2, current.Width, current.Height);

                foreach (var obstacle in obstacles)
                {
                    if (obstacle == to || obstacle == from)
                    {
                        continue;
                    }
                    else if (obstacle.OverlapPercentage(current) > 0)
                    {
                        return false;
                    }
                }

                currentDistance = current.CalculateDistanceTo(to);
            }

            return true;
        }

        public static float LineOfSightVisibility(this IRectangularF from, float angle, List<IRectangularF> obstacles, float range, float increment = .1f)
        {
            for (var d = increment; d < range; d += increment)
            {
                var testLocation = from.MoveTowards(angle, d);
                var testRect = RectangularF.Create(testLocation.Left, testLocation.Top, from.Width, from.Height);
                if (obstacles.Where(o => o.Touches(testRect)).Any() || SpaceTime.CurrentSpaceTime.Bounds.Contains(testRect) == false)
                {
                    return d;
                }
            }

            return range;
        }



        public static ILocationF MoveTowards(this ILocationF a, ILocationF b, float distance)
        {
            float slope = (a.Top - b.Top) / (a.Left - b.Left);
            bool forward = a.Left <= b.Left;
            bool up = a.Top <= b.Top;

            float abDistance = Geometry.CalculateNormalizedDistanceTo(a,b);
            double angle = Math.Asin(Math.Abs(b.Top - a.Top) / abDistance);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top + dy : a.Top - dy;

            var ret = LocationF.Create(x2, y2);
            return ret;
        }

        public static ILocationF MoveTowards(this ILocationF a, float angle, float distance)
        {
            distance = Geometry.NormalizeQuantity(distance, angle);
            var forward = angle > 270 || angle < 90;
            var up = angle > 180;

            // convert to radians
            angle = (float)(angle * Math.PI / 180);
            float dy = (float)Math.Abs(distance * Math.Sin(angle));
            float dx = (float)Math.Sqrt((distance * distance) - (dy * dy));

            float x2 = forward ? a.Left + dx : a.Left - dx;
            float y2 = up ? a.Top - dy : a.Top + dy;

            var ret = LocationF.Create(x2, y2);
            return ret;
        }

        public static List<IRectangularF> GetObstacles(this SpacialElement element, IEnumerable<SpacialElement> exclusions = null)
        {
            var ret = new List<IRectangularF>();
            foreach (var e in SpaceTime.CurrentSpaceTime.Elements)
            {
                if(e == element)
                {
                    continue;
                }
                else if(exclusions != null && exclusions.Contains(e))
                {
                    continue;
                }
                else if(e.ZIndex != element.ZIndex)
                {
                    continue;
                }
                else if(e.HasSimpleTag(PassThruTag))
                {
                    continue;
                }
                else
                {
                    ret.Add(e);
                }
            }
            return ret;
        }



        public static async Task AnimateAsync(this IRectangularF rectangular, RectangularAnimationOptions options)
        {
            var startX = rectangular.Left;
            var startY = rectangular.Top;
            var startW = rectangular.Width;
            var startH = rectangular.Height;

            var xDelta = options.Destination.Left - startX;
            var yDelta = options.Destination.Top - startY;
            var wDelta = options.Destination.Width - startW;
            var hDelta = options.Destination.Height - startH;

            await Animator.AnimateAsync(new FloatAnimatorOptions()
            {
                Duration = options.Duration,
                AutoReverse = options.AutoReverse,
                AutoReverseDelay = options.AutoReverseDelay,
                DelayProvider = options.DelayProvider,
                Loop = options.Loop,
                EasingFunction = options.EasingFunction,
                From = 0,
                To = 1,
                Setter = v =>
                {
                    var frameX = startX + (v * xDelta);
                    var frameY = startY + (v * yDelta);
                    var frameW = startW + (v * wDelta);
                    var frameH = startH + (v * hDelta);
                    var frameBounds = RectangularF.Create(frameX, frameY, frameW, frameH);
                    options.Setter(rectangular, frameBounds);
                }
            });
        }
    }

    public class SpacialElementAnimationOptions : RectangularAnimationOptions
    {
        public override void Setter(IRectangularF target, IRectangularF bounds)
        {
            (target as SpacialElement).MoveTo(bounds.Left, bounds.Top);
            (target as SpacialElement).ResizeTo(bounds.Width, bounds.Height);
        }
    }

    public class ConsoleControlAnimationOptions : RectangularAnimationOptions
    {
        public override void Setter(IRectangularF target, IRectangularF bounds)
        {
            (target as ConsoleControl).X = (int)Math.Round(bounds.Left);
            (target as ConsoleControl).Y = (int)Math.Round(bounds.Top);
            (target as ConsoleControl).Width = (int)Math.Round(bounds.Width);
            (target as ConsoleControl).Height = (int)Math.Round(bounds.Height);
        }
    }

    public abstract class RectangularAnimationOptions
    {
        public IRectangularF Destination { get; set; }

        public abstract void Setter(IRectangularF target, IRectangularF bounds);

        public Func<float, float> EasingFunction { get; set; } = new FloatAnimatorOptions().EasingFunction;
        public double Duration { get; set; } = new FloatAnimatorOptions().Duration;
 
        /// <summary>
        /// If true then the animation will automatically reverse itself when done
        /// </summary>
        public bool AutoReverse { get; set; }

        /// <summary>
        /// When specified, the animation will loop until this lifetime completes
        /// </summary>
        public ILifetimeManager Loop { get; set; }

        /// <summary>
        /// The provider to use for delaying between animation frames
        /// </summary>
        public IDelayProvider DelayProvider { get; set; }

        /// <summary>
        /// If auto reverse is enabled, this is the pause, in milliseconds, after the forward animation
        /// finishes, to wait before reversing
        /// </summary>
        public float AutoReverseDelay { get; set; } = 0;
    }
}
