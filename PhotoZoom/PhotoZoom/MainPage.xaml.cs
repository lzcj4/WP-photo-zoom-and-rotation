using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PhotoZoom.Resources;
using System.Windows.Media.Animation;

namespace PhotoZoom
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }

        private PageOrientation oldOrientation = PageOrientation.Portrait;
        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            StartRotation(ucImage, e.Orientation);
            base.OnOrientationChanged(e);
        }

        private void StartRotation(UIElement control, PageOrientation orientation)
        {
            const double rotationDegree = 90;
            double degree = 0;

            if (orientation == PageOrientation.LandscapeLeft)
            {
                degree = -rotationDegree;
                oldOrientation = orientation;
            }
            else if (orientation == PageOrientation.LandscapeRight)
            {
                degree = rotationDegree;
                oldOrientation = orientation;
            }
            else if (orientation == PageOrientation.PortraitUp)
            {
                if (oldOrientation == PageOrientation.LandscapeLeft)
                {
                    degree = rotationDegree;
                }
                else if (oldOrientation == PageOrientation.LandscapeRight)
                {
                    degree = -rotationDegree;
                }

                if (Math.Abs(degree) == rotationDegree)
                {
                    oldOrientation = orientation;
                }
            }

            if (Math.Abs(degree) == rotationDegree)
            {
                Storyboard story = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation();
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                animation.From = degree;
                animation.To = 0;
                Storyboard.SetTarget(animation, control);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.Rotation)"));
                story.Children.Add(animation);
                story.Begin();
            }

        }
    }
}