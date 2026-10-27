using System;
using System.Windows;

namespace PSKCharms
{
    public struct VerletPoint
    {
        public double X;
        public double Y;
        public double OldX;
        public double OldY;

        public VerletPoint(double x, double y)
        {
            X = x;
            Y = y;
            OldX = x;
            OldY = y;
        }
    }

    public class PhysicsSimulator
    {
        public const int LinkCount = 12;
        public VerletPoint[] Points { get; private set; }

        public Point Anchor { get; private set; }
        public double TotalLength { get; private set; }
        public double SegmentLength { get; private set; }

        public bool IsDragging { get; private set; }
        public double DragOffsetX { get; set; }
        public double DragOffsetY { get; set; }

        private double _prevDragX;
        private double _prevDragY;
        private double _dragVelocityX;
        private double _dragVelocityY;
        private double _breezePhase;

        public PhysicsSimulator(double anchorX, double anchorY, double totalLength)
        {
            Points = new VerletPoint[LinkCount];
            SetAnchorAndLength(anchorX, anchorY, totalLength);
        }

        public void SetAnchorAndLength(double anchorX, double anchorY, double totalLength)
        {
            Anchor = new Point(anchorX, anchorY);
            TotalLength = Math.Max(60.0, totalLength);
            SegmentLength = TotalLength / (LinkCount - 1);

            if (Points[0].X == 0 && Points[0].Y == 0)
            {
                // Initial placement
                for (int i = 0; i < LinkCount; i++)
                {
                    double py = anchorY + (i * SegmentLength);
                    Points[i] = new VerletPoint(anchorX, py);
                }
            }
            else
            {
                // Keep current configuration but update anchor
                Points[0].X = anchorX;
                Points[0].Y = anchorY;
                Points[0].OldX = anchorX;
                Points[0].OldY = anchorY;
            }
        }

        public void SetAnchor(double anchorX, double anchorY)
        {
            double dx = anchorX - Anchor.X;
            double dy = anchorY - Anchor.Y;
            Anchor = new Point(anchorX, anchorY);

            // Shift points smoothly
            for (int i = 0; i < LinkCount; i++)
            {
                Points[i].X += dx;
                Points[i].Y += dy;
                Points[i].OldX += dx;
                Points[i].OldY += dy;
            }
            Points[0].X = anchorX;
            Points[0].Y = anchorY;
            Points[0].OldX = anchorX;
            Points[0].OldY = anchorY;
        }

        public void SetLength(double totalLength)
        {
            TotalLength = Math.Max(60.0, totalLength);
            SegmentLength = TotalLength / (LinkCount - 1);
        }

        public void BeginDrag(double mouseX, double mouseY)
        {
            IsDragging = true;
            int last = LinkCount - 1;
            DragOffsetX = mouseX - Points[last].X;
            DragOffsetY = mouseY - Points[last].Y;
            _prevDragX = mouseX;
            _prevDragY = mouseY;
            _dragVelocityX = 0;
            _dragVelocityY = 0;
        }

        public void Drag(double mouseX, double mouseY)
        {
            if (!IsDragging) return;

            int last = LinkCount - 1;
            double targetX = mouseX - DragOffsetX;
            double targetY = mouseY - DragOffsetY;

            _dragVelocityX = (targetX - Points[last].X) * 0.4 + _dragVelocityX * 0.6;
            _dragVelocityY = (targetY - Points[last].Y) * 0.4 + _dragVelocityY * 0.6;

            Points[last].X = targetX;
            Points[last].Y = targetY;
            Points[last].OldX = targetX - _dragVelocityX;
            Points[last].OldY = targetY - _dragVelocityY;

            _prevDragX = mouseX;
            _prevDragY = mouseY;
        }

        public void EndDrag()
        {
            if (!IsDragging) return;
            IsDragging = false;

            int last = LinkCount - 1;
            // Impart momentum
            double maxSpeed = 35.0;
            double vx = Math.Max(-maxSpeed, Math.Min(maxSpeed, _dragVelocityX));
            double vy = Math.Max(-maxSpeed, Math.Min(maxSpeed, _dragVelocityY));

            Points[last].OldX = Points[last].X - vx;
            Points[last].OldY = Points[last].Y - vy;
        }

        public void ApplyImpulse(double fx, double fy)
        {
            int last = LinkCount - 1;
            Points[last].OldX -= fx;
            Points[last].OldY -= fy;
        }

        public void Step(double dt, bool ambientBreeze, double breezeIntensity, double gravityVal, double dampingVal)
        {
            _breezePhase += dt * 1.8;

            double windAcc = 0;
            if (ambientBreeze && breezeIntensity > 0.05)
            {
                windAcc = (Math.Sin(_breezePhase) * 1.8 + Math.Sin(_breezePhase * 2.3 + 1.2) * 0.8) * breezeIntensity * 15.0;
            }

            double gravityAcc = gravityVal * 45.0; // Scaled for screen visual scale
            double damping = Math.Max(0.92, Math.Min(0.998, dampingVal));

            int lastIndex = LinkCount - 1;

            // Verlet position updates for non-pinned, non-dragged points
            for (int i = 1; i < LinkCount; i++)
            {
                if (i == lastIndex && IsDragging)
                {
                    // Dragged point handled in Drag()
                    continue;
                }

                double vx = (Points[i].X - Points[i].OldX) * damping;
                double vy = (Points[i].Y - Points[i].OldY) * damping;

                Points[i].OldX = Points[i].X;
                Points[i].OldY = Points[i].Y;

                // Scale breeze force along rope (more force at bottom)
                double linkWeight = (double)i / LinkCount;
                Points[i].X += vx + (windAcc * linkWeight * dt * dt);
                Points[i].Y += vy + (gravityAcc * dt * dt);
            }

            // Lock anchor
            Points[0].X = Anchor.X;
            Points[0].Y = Anchor.Y;

            // Relaxation constraint iterations (12 passes for tight, stable cord length without vibration)
            int constraintPasses = 12;
            for (int pass = 0; pass < constraintPasses; pass++)
            {
                // Pin anchor
                Points[0].X = Anchor.X;
                Points[0].Y = Anchor.Y;

                for (int i = 0; i < LinkCount - 1; i++)
                {
                    double dx = Points[i + 1].X - Points[i].X;
                    double dy = Points[i + 1].Y - Points[i].Y;
                    double dist = Math.Sqrt(dx * dx + dy * dy);

                    if (dist > 0.0001)
                    {
                        double diff = (dist - SegmentLength) / dist;

                        if (i == 0)
                        {
                            // Pinned anchor: only move i+1
                            if (!(i + 1 == lastIndex && IsDragging))
                            {
                                Points[i + 1].X -= dx * diff;
                                Points[i + 1].Y -= dy * diff;
                            }
                        }
                        else if (i + 1 == lastIndex && IsDragging)
                        {
                            // Dragged end: only move i
                            Points[i].X += dx * diff;
                            Points[i].Y += dy * diff;
                        }
                        else
                        {
                            // Both free
                            Points[i].X += dx * diff * 0.5;
                            Points[i].Y += dy * diff * 0.5;
                            Points[i + 1].X -= dx * diff * 0.5;
                            Points[i + 1].Y -= dy * diff * 0.5;
                        }
                    }
                }
            }
        }

        public Point GetCharmPosition()
        {
            int last = LinkCount - 1;
            return new Point(Points[last].X, Points[last].Y);
        }

        public double GetRotationAngle()
        {
            int last = LinkCount - 1;
            double dx = Points[last].X - Points[last - 1].X;
            double dy = Points[last].Y - Points[last - 1].Y;
            double rad = Math.Atan2(dx, dy);
            return -rad * (180.0 / Math.PI);
        }
    }
}
