using System;
using System.Windows;
using System.Windows.Media;

namespace SynchronizationCompletionCoordinator.Controls
{
    public class CircleProgressBar : FrameworkElement
    {
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircleProgressBar),
                new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeBrushProperty =
            DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(CircleProgressBar),
                new FrameworkPropertyMetadata(AppConfig.CircleProgressBrush, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeThicknessProperty =
            DependencyProperty.Register(nameof(StrokeThickness), typeof(double), typeof(CircleProgressBar),
                new FrameworkPropertyMetadata(2.5, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public Brush StrokeBrush
        {
            get => (Brush)GetValue(StrokeBrushProperty);
            set => SetValue(StrokeBrushProperty, value);
        }

        public double StrokeThickness
        {
            get => (double)GetValue(StrokeThicknessProperty);
            set => SetValue(StrokeThicknessProperty, value);
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double size = Math.Min(ActualWidth, ActualHeight);
            if (size <= StrokeThickness * 2) return;

            Point center = new(ActualWidth / 2.0, ActualHeight / 2.0);
            double radius = (size - StrokeThickness) / 2.0;

            // 绘制底色淡灰圆环
            dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromRgb(225, 225, 225)), 1.5), center, radius, radius);

            double progress = Math.Clamp(Value, 0.0, 1.0);
            if (progress <= 0.001) return;

            if (progress >= 0.999)
            {
                dc.DrawEllipse(null, new Pen(StrokeBrush, StrokeThickness), center, radius, radius);
                return;
            }

            // 从 12 点开始顺时针减少：
            // 空缺区域从 12 点（-90°）顺时针延伸，剩余弧线从起点顺时针到达 12 点（270°）
            double startAngle = -90.0 + (1.0 - progress) * 360.0;
            double endAngle = 270.0;
            double arcSpan = progress * 360.0;

            Point startPoint = GetPointOnCircle(center, radius, startAngle);
            Point endPoint = GetPointOnCircle(center, radius, endAngle);

            StreamGeometry geom = new();
            using (var ctx = geom.Open())
            {
                ctx.BeginFigure(startPoint, false, false);
                ctx.ArcTo(endPoint, new Size(radius, radius), 0, arcSpan > 180, SweepDirection.Clockwise, true, false);
            }

            dc.DrawGeometry(null, new Pen(StrokeBrush, StrokeThickness) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round }, geom);
        }

        private static Point GetPointOnCircle(Point center, double radius, double angleInDegrees)
        {
            double rad = angleInDegrees * Math.PI / 180.0;
            return new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));
        }
    }
}