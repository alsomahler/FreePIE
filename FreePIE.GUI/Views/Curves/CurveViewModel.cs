﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Caliburn.Micro;
using FreePIE.Core.Common;
using FreePIE.Core.Model;
using FreePIE.Core.Model.Events;
using FreePIE.GUI.Common.Visiblox;
using FreePIE.GUI.Events;
using IEventAggregator = FreePIE.Core.Common.Events.IEventAggregator;

namespace FreePIE.GUI.Views.Curves
{
    public class CurveViewModel : PropertyChangedBase
    {
        private readonly IEventAggregator eventAggregator;
        public Curve Curve { get; private set; }
        public int? selectedPointIndex;

        public CurveViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;
        }

        public CurveViewModel Configure(Curve curve)
        {
            this.Curve = curve;
            InitCurve();

            return this;
        }

        private void InitCurve()
        {
            SetSelectablePoints();
            Points = CalculateNewPoints();
        }

        private void SetSelectablePoints()
        {
            SelectablePoints = Curve.Points.Skip(1);
        }

        public string Name
        {
            get { return Curve.Name; }
            set 
            { 
                Curve.Name = value;
                eventAggregator.Publish(new CurveChangedNameEvent(Curve));
                NotifyOfPropertyChange(() => Name);
            }
        }

        public void Delete()
        {
            eventAggregator.Publish(new DeleteCurveEvent(this));
        }

        public bool HasSelectedPoint
        {
            get { return selectedPointIndex.HasValue; }
        }

        private bool setDefault;
        public bool SetDefault
        {
            get { return setDefault; }
            set
            {
                setDefault = value;
                NotifyOfPropertyChange(() => SetDefault);
                NotifyOfPropertyChange(() => CanSetSelectedPointX);
            }
        }

        private bool canSetDefault;
        public bool CanSetDefault
        {
            get { return canSetDefault; }
            set
            {
                canSetDefault = value;
                NotifyOfPropertyChange(() => canSetDefault);
            }
        }

        public bool CanSetSelectedPointX { get { return !SetDefault; } }

        public void OnPointSelected(MovePointBehaviour.PointSelectedEventArgs e)
        {
            var index = Curve.IndexOf(e.Point);
            if(index != selectedPointIndex)
                SetDefault = false;

            selectedPointIndex = index;

            CanSetDefault = selectedPointIndex == Curve.Points.Count - 1;
            NotifyOfPropertyChange(() => HasSelectedPoint);
            NotifyOfPropertyChange(() => SelectedPointX);
            NotifyOfPropertyChange(() => SelectedPointY);
        }

        public double SelectedPointX
        {
            get { return GetSelectedPoint().X; }

            set
            {
                SetSelectedPoint(new Point(value, SelectedPointY));
            }
        }
        
        public double SelectedPointY
        {
            get { return GetSelectedPoint().Y; }

            set
            {
                SetSelectedPoint(new Point(SelectedPointX, value));
            }
        }

        private void SetSelectedPoint(Point newPoint)
        {
            if(SetDefault)
            {
                Curve.Reset(newPoint.Y);
                InitCurve();
                return;
            }

            var args = new MovePointBehaviour.PointMoveEventArgs
            {
                OldPoint = GetSelectedPoint(),
                NewPoint = newPoint
            };
            OnPointDragged(args);
            SetSelectablePoints();
            NotifyOfPropertyChange(() => SelectedPointX);
            NotifyOfPropertyChange(() => SelectedPointY);    
        }

        private Point GetSelectedPoint()
        {
            if (selectedPointIndex.HasValue)
                return Curve.Points[selectedPointIndex.Value];

            return new Point();
        }

        public void OnPointDragged(MovePointBehaviour.PointMoveEventArgs e)
        {
            var oldPoint = e.OldPoint;
            var newPoint = e.NewPoint;

            var index = Curve.IndexOf(e.OldPoint);
            var prevPoint = Curve.Points[index - 1];
            var biggestValueForY = double.MinValue;

            var newCurve = Curve.Points.GetRange(0, Curve.Points.Count);
            newCurve[index] = newPoint;
            var lastPoint = newCurve[newCurve.Count - 1];

            for (double x = 0; x < lastPoint.X; x++)
            {
                var y = CurveMath.SolveCubicSpline(newCurve, x);
                if (biggestValueForY > y)
                {
                    newPoint = oldPoint;
                    break;
                }

                if (y > biggestValueForY)
                    biggestValueForY = y;
            }

            e.NewPoint = newPoint;
            Curve.Points[index] = e.NewPoint;

            Points = CalculateNewPoints();
        }

        private IEnumerable<Point> CalculateNewPoints()
        {
            var points = CurveMath.GetInterpolatedCubicSplinedCurve(Curve.Points);

            return points;
        }

        private IEnumerable<Point> points;
        public IEnumerable<Point> Points
        {
            get { return points; }
            set
            {
                points = value;
                NotifyOfPropertyChange(() => Points);
            }
        }

        private IEnumerable<Point> selectablePoints;
        public IEnumerable<Point> SelectablePoints
        {
            get { return selectablePoints; }
            set
            {
                selectablePoints = value;
                NotifyOfPropertyChange(() => SelectablePoints);
            }
        }

    }
}
