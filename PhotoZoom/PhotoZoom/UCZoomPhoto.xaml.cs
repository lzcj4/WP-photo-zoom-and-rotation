using Microsoft.Phone.Shell;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PhotoZoom
{
    public partial class UCZoomPhoto : UserControl
    {
        public UCZoomPhoto()
        {
            InitializeComponent();
            ///Sometime image has thumenail and preview ,change image source before user control loaded
            ///the viewport width and height is 0 , so add loaded event to initial the coercedScale
            this.image.ImageOpened += image_ImageOpened;
        }

        void image_ImageOpened(object sender, RoutedEventArgs e)
        {
            InitialScale();
        }


        public BitmapImage ImageSource
        {
            get { return this.GetValue(ImageSourceProperty) as BitmapImage; }
            set
            {
                this.SetValue(ImageSourceProperty, value);
            }
        }

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register("ImageSource", typeof(BitmapImage),
            typeof(UCZoomPhoto), new PropertyMetadata(null, ImageSourceChangedCallback));

        public static void ImageSourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UCZoomPhoto uc = d as UCZoomPhoto;
            BitmapImage newImage = e.NewValue as BitmapImage;
            if (uc != null && newImage != null && e.OldValue != e.NewValue)
            {
                uc.InitialScale();
            }
        }


        #region Pinch & Zoom

        const double maxScale = 10;
        double cumulativeScale = 1.0;
        double minScale;
        double lastCumulativeScale;

        private double coercedScale;
        private double CoercedScale
        {
            get { return coercedScale; }
            set
            {
                if (value != coercedScale)
                {
                    coercedScale = value;
                }
            }
        }

        /// <summary>
        /// Absolute pinch bounds point
        /// </summary>
        Point absOriginCanvasCenterPoint;
        /// <summary>
        /// Relative pinch bounds point
        /// </summary>
        Point relOriginCanvasCenterPoint;

        //double MainHeight = Application.Current.RootVisual.RenderSize.Height;
        //double MainWidth = Application.Current.RootVisual.RenderSize.Width;

        Rect viewportRect;
        /// <summary> 
        /// Either the user has manipulated the image or the size of the viewport has changed. We only 
        /// care about the size. 
        /// </summary> 
        void viewport_ViewportChanged(object sender, ViewportChangedEventArgs e)
        {
            Size newSize = new Size(viewport.Viewport.Width, viewport.Viewport.Height);
            if (viewportRect != viewport.Viewport)
            {
                if (viewportRect.Height != viewport.Viewport.Height ||
                    viewportRect.Width != viewport.Viewport.Width)
                {
                    InitialScale();
                }
                viewportRect = viewport.Viewport;
            }
        }

        private double FirstLoadScale { get; set; }
        /// <summary> 
        /// When a new image is opened, set its initial scale. 
        /// </summary> 
        void InitialScale()
        {
            // Set scale to the minimum, and then save it. 
            cumulativeScale = 0;
            CoerceScale(true);
            cumulativeScale = CoercedScale;
            FirstLoadScale = CoercedScale;
            ResizeImage(true);
        }

        void viewport_DoubleTap(object sender, GestureEventArgs e)
        {

            if (CoercedScale >= 1.0)
            {
                CoercedScale = FirstLoadScale;
            }
            else
            {
                CoercedScale = 1.0;
            }

            Point clickPoint = e.GetPosition(image);
            GetCanvasCenterPoint(clickPoint);
            ResizeImage(false);
        }

        /// <summary> 
        /// Handler for the ManipulationStarted event. Set initial state in case 
        /// it becomes a pinch later. 
        /// </summary> 
        void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            lastCumulativeScale = cumulativeScale;
            Debug.WriteLine("/+++OnManipulationStarted viewPort.Bounds: {0}, Viewport: {1} +++/", viewport.Bounds, viewport.Viewport);
        }

        /// <summary> 
        /// Handler for the ManipulationDelta event. It may or may not be a pinch. If it is not a  
        /// pinch, the ViewportControl will take care of it. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            Debug.WriteLine("/^^^^ OnManipulationDelta  viewPort.Bounds: {0}, Viewport: {1} ^^^/", viewport.Bounds, viewport.Viewport);

            if (e.PinchManipulation != null)
            {
                GetCanvasCenterPoint(e.PinchManipulation.Original.Center);
                cumulativeScale = lastCumulativeScale * e.PinchManipulation.CumulativeScale;
                CoerceScale(false);
                ResizeImage(false);
            }
            else
            {
                lastCumulativeScale = cumulativeScale = CoercedScale;
            }
        }

        /// <summary> 
        /// The manipulation has completed (no touch points anymore) so reset state. 
        /// </summary> 
        void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            Debug.WriteLine("/--- OnManipulationCompleted viewPort.Bounds: {0}, Viewport: {1} ---/", viewport.Bounds, viewport.Viewport);
            cumulativeScale = CoercedScale;
        }

        private void GetCanvasCenterPoint(Point pinchCenter)
        {
            var canvasTrans = image.TransformToVisual(canvas);
            var canvasCenter = canvasTrans.Transform(pinchCenter);
            absOriginCanvasCenterPoint = canvasCenter;
            relOriginCanvasCenterPoint = new Point(absOriginCanvasCenterPoint.X / canvas.Width,
                                                   absOriginCanvasCenterPoint.Y / canvas.Height);
        }

        double lastScaleValue = 0;
        /// <summary> 
        /// Adjust the size of the image according to the coerced scale factor. Optionally 
        /// center the image, otherwise, try to keep the original midpoint of the pinch 
        /// in the same spot on the screen regardless of the scale. 
        /// </summary> 
        /// <param name="center"></param> 
        void ResizeImage(bool center)
        {
            if (CoercedScale == 0 || ImageSource == null || lastScaleValue == CoercedScale)
            {
                return;
            }

            lastScaleValue = CoercedScale;
            double newWidth = Math.Round(ImageSource.PixelWidth * CoercedScale);
            double newHeight = Math.Round(ImageSource.PixelHeight * CoercedScale);

            viewport.Bounds = new Rect(0, 0, newWidth, newHeight);
            canvas.Width = newWidth;
            canvas.Height = newHeight;

            compositeRenderTransform.ScaleX = CoercedScale;
            compositeRenderTransform.ScaleY = CoercedScale;

            Point centerPoint = new Point();

            if (center)
            {
                ///This center value doesn't work 
                double wCenter = Math.Round(newWidth - viewport.ActualWidth) / 2;
                double hCenter = Math.Round(newWidth - viewport.ActualHeight) / 2;
                centerPoint = new Point(wCenter, hCenter);
            }
            else
            {
                Point newOffset = new Point(newWidth * relOriginCanvasCenterPoint.X - absOriginCanvasCenterPoint.X,
                                            newHeight * relOriginCanvasCenterPoint.Y - absOriginCanvasCenterPoint.Y);

                centerPoint = new Point(viewport.Viewport.X + newOffset.X, viewport.Viewport.Y + newOffset.Y);
            }

            viewport.SetViewportOrigin(centerPoint);
        }

        /// <summary> 
        /// Coerce the scale into being within the proper range. Optionally compute the constraints  
        /// on the scale so that it will always fill the entire screen and will never get too big  
        /// to be contained in a hardware surface. 
        /// </summary> 
        /// <param name="recompute">Will recompute the min max scale if true.</param> 
        void CoerceScale(bool recompute)
        {
            if (recompute && ImageSource != null)
            {
                // Calculate the minimum scale to fit the viewport 
                double actualWidth = viewport.ActualWidth;
                double actualHeight = viewport.ActualHeight;

                double minX = actualWidth / ImageSource.PixelWidth;
                double minY = actualHeight / ImageSource.PixelHeight;

                minScale = Math.Min(minX, minY);
            }

            CoercedScale = Math.Min(maxScale, Math.Max(cumulativeScale, minScale));
        }

        #endregion
    }
}
