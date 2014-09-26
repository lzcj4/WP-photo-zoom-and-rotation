using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using ImageZoom.Resources;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace ImageZoom
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
            base.OnOrientationChanged(e);

            double degree = 0;
            PageOrientation orientation = e.Orientation;

            if (orientation == PageOrientation.LandscapeLeft)
            {
                degree = -90;
                oldOrientation = orientation;
            }
            else if (orientation == PageOrientation.LandscapeRight)
            {
                degree = 90;
                oldOrientation = orientation;
            }
            else if (orientation == PageOrientation.PortraitUp)
            {
                if (oldOrientation == PageOrientation.LandscapeLeft)
                {
                    degree = 90;
                }
                else if (oldOrientation == PageOrientation.LandscapeRight)
                {
                    degree = -90;
                }

                if (Math.Abs(degree) == 90)
                {
                    oldOrientation = orientation;
                }
            }

            if (Math.Abs(degree) == 90)
            {
                Storyboard story = new Storyboard();
                DoubleAnimation animation = new DoubleAnimation();
                animation.Duration = new Duration(TimeSpan.FromSeconds(0.2));
                animation.From = degree;
                animation.To = 0;
                Storyboard.SetTarget(animation, ucImage);
                Storyboard.SetTargetProperty(animation, new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.Rotation)"));
                story.Children.Add(animation);
                story.Begin();
            }
        }
    }
}