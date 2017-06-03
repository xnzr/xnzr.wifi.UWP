using System;
using Android.Views;
using Android.Content;
using Android.Graphics;

namespace WiFiCircles
{
    public class AimView : View
    {
        public AimView(Context context) : base(context)
        {
            _circleFill = new Paint();
            _circleFill.Color = Color.Green;
            //_circleFill.setStrokeWidth(5);
            _circleFill.SetStyle(Paint.Style.Fill);
            _circleFill.AntiAlias = true;
            _circleFill.Alpha = 95;

            _circleStroke = new Paint();
            _circleStroke.Color = Color.Green;
            _circleStroke.StrokeWidth = 5;
            _circleStroke.SetStyle(Paint.Style.Stroke);
            _circleStroke.AntiAlias = true;

            _circlePath = new Path();
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            _width = MeasuredWidth / 2;
            _height = MeasuredHeight / 2;
            MaxRadius = System.Math.Min(MeasuredWidth, MeasuredHeight) / 2;
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (Radius > 0)
            {
                _circlePath.Reset();
                _circlePath.AddCircle(_width, _height, Radius, Path.Direction.Cw);
                canvas.DrawPath(_circlePath, _circleFill);

                canvas.DrawCircle(_width, _height, Radius, _circleStroke);
            }
        }

        private int _width;
        private int _height;
        private Paint _circleStroke;
        private Paint _circleFill;
        private Path _circlePath;

        public int MaxRadius { get; private set; }
        public float Radius { get; set; }
    }
}

