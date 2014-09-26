using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ImageZoom
{
    public partial class UCImage : UserControl
    {
        public UCImage()
        {
            InitializeComponent();
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
            typeof(UCImage), new PropertyMetadata(null, ImageSourceChangedCallback));

        public static void ImageSourceChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UCImage uc = d as UCImage;
            BitmapImage newImage = e.NewValue as BitmapImage;
            if (uc != null && newImage != null)
            {
                uc.OnImpageOpened();
            }
        }

        private void OnImpageOpened()
        {
            image.ImageOpened += (sender, e) => { this.InitialScale(); };
        }

        #region Pinch & Zoom

        const double maxScale = 10;
        double currentScale = 1.0;
        double minScale;
        double coercedScale;
        double originalScale;
        double firstLoadScale;

        bool pinching;
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

        Size viewportSize;
        /// <summary> 
        /// Either the user has manipulated the image or the size of the viewport has changed. We only 
        /// care about the size. 
        /// </summary> 
        void viewport_ViewportChanged(object sender, ViewportChangedEventArgs e)
        {
            Size newSize = new Size(viewport.Viewport.Width, viewport.Viewport.Height);
            if (newSize != viewportSize)
            {
                viewportSize = newSize;
                CoerceScale(true);
                ResizeImage(false);
            }
        }

        /// <summary> 
        /// When a new image is opened, set its initial scale. 
        /// </summary> 
        void InitialScale()
        {
            // Set scale to the minimum, and then save it. 
            currentScale = 0;
            CoerceScale(true);
            currentScale = coercedScale;
            firstLoadScale = coercedScale;
            ResizeImage(true);
        }

        void viewport_DoubleTap(object sender, GestureEventArgs e)
        {
            if (coercedScale >= 1.0)
            {
                coercedScale = firstLoadScale;
            }
            else
            {
                coercedScale = 1.0;
            }

            Point clickPoint = e.GetPosition(canvas);
            GetCanvasCenterPoint(clickPoint);
            ResizeImage(false);
        }

        /// <summary> 
        /// Handler for the ManipulationStarted event. Set initial state in case 
        /// it becomes a pinch later. 
        /// </summary> 
        void OnManipulationStarted(object sender, ManipulationStartedEventArgs e)
        {
            isReZoom = null;
            pinching = false;
            originalScale = currentScale;
        }

        private bool? isReZoom = null;
        public bool? IsReZoom
        {
            get { return isReZoom; }
            set
            {
                if (isReZoom != value)
                {
                    isReZoom = value;
                }
            }
        }
        /// <summary> 
        /// Handler for the ManipulationDelta event. It may or may not be a pinch. If it is not a  
        /// pinch, the ViewportControl will take care of it. 
        /// </summary> 
        /// <param name="sender"></param> 
        /// <param name="e"></param> 
        void OnManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (e.PinchManipulation != null)
            {
                e.Handled = true;

                if (!pinching)
                {
                    if (originalScale > minScale)
                    {
                        IsReZoom = true;
                    }
                    pinching = true;
                    Point center = e.PinchManipulation.Original.Center;
                    GetCanvasCenterPoint(center);
                }

                currentScale = originalScale * e.PinchManipulation.CumulativeScale;
                CoerceScale(false);
                ResizeImage(false);
            }
            else if (pinching)
            {
                pinching = false;
                originalScale = currentScale = coercedScale;
            }
        }

        /// <summary> 
        /// The manipulation has completed (no touch points anymore) so reset state. 
        /// </summary> 
        void OnManipulationCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            isReZoom = null;
            pinching = false;
            currentScale = coercedScale;
        }

        private Point GetAdjustPointForEdge(Point point)
        {
            const double offset = 60;
            if (point.X <= offset)
            {
                point.X = 0;
            }
            if (point.X >= (canvas.Width - offset))
            {
                point.X = viewport.Viewport.Width;
            }

            if (point.Y <= offset)
            {
                point.Y = 0;
            }
            if (point.Y >= (canvas.Height - offset))
            {
                point.Y = viewport.Viewport.Height;// canvas.Height;
            }
            return point;
        }

        private void GetCanvasCenterPoint(Point pinchCenter)
        {
            pinchCenter = GetAdjustPointForEdge(pinchCenter);
            var canvasTrans = viewport.TransformToVisual(canvas);
            absOriginCanvasCenterPoint = canvasTrans.Transform(pinchCenter);
            relOriginCanvasCenterPoint = new Point(absOriginCanvasCenterPoint.X / viewport.Bounds.Width,
                                                   absOriginCanvasCenterPoint.Y / viewport.Bounds.Height);

            Debug.WriteLine(@"/##### GetRelativeAndTransformPoint,pinchCenter:{0} ,absOriginCanvasCenterPoint:{1},
                                     viewport.Bounds：{2}, viewport.Viewport:{3}, ##### /",
                            pinchCenter, absOriginCanvasCenterPoint, viewport.Bounds, viewport.Viewport);
        }

        Point originRezoomPoint;
        /// <summary> 
        /// Adjust the size of the image according to the coerced scale factor. Optionally 
        /// center the image, otherwise, try to keep the original midpoint of the pinch 
        /// in the same spot on the screen regardless of the scale. 
        /// </summary> 
        /// <param name="center"></param> 
        void ResizeImage(bool center)
        {
            if (coercedScale == 0 || ImageSource == null)
            {
                return;
            }

            double newWidth = canvas.Width = Math.Round(ImageSource.PixelWidth * coercedScale);
            double newHeight = canvas.Height = Math.Round(ImageSource.PixelHeight * coercedScale);

            viewport.Bounds = new Rect(0, 0, newWidth, newHeight);
            compositeRenderTransform.ScaleX = coercedScale;
            compositeRenderTransform.ScaleY = coercedScale;

            if (center)
            {
                Point centerPoint = new Point(Math.Round((newWidth - viewport.ActualWidth) / 2),
                                              Math.Round((newHeight - viewport.ActualHeight) / 2));
                viewport.SetViewportOrigin(centerPoint);
            }
            else
            {
                Point newAbsCanvasPoint = new Point(newWidth * relOriginCanvasCenterPoint.X, newHeight * relOriginCanvasCenterPoint.Y);
                var viewportTrans = canvas.TransformToVisual(viewport);
                Point newViewportPoint = viewportTrans.Transform(newAbsCanvasPoint);

                Point newOffset = new Point(newAbsCanvasPoint.X - absOriginCanvasCenterPoint.X, newAbsCanvasPoint.Y - absOriginCanvasCenterPoint.Y);

                if (IsReZoom.HasValue)
                {
                    Point centerPoint = new Point(0, 0);
                    if (IsReZoom.Value)
                    {
                        originRezoomPoint = new Point(viewport.Viewport.X, viewport.Viewport.Y);
                        viewport.SetViewportOrigin(originRezoomPoint);
                    }
                    else
                    {
                        centerPoint = new Point(originRezoomPoint.X + newOffset.X,
                                                originRezoomPoint.Y + newOffset.Y);
                        viewport.SetViewportOrigin(centerPoint);
                    }
                    IsReZoom = false;

                    Debug.WriteLine(@"/++++  After ResizeImage absOriginCanvasCenterPoint:{0} ,newAbsCanvasPoint:{1},
                                             move offset:{2},canvas offset:{3}, newViewportPoint:{4}, 
                                             viewport.Viewport:{5},viewport.Bounds：{6}  ++++/",
                                   absOriginCanvasCenterPoint, newAbsCanvasPoint, centerPoint, newOffset,
                                   newViewportPoint, viewport.Viewport, viewport.Bounds);
                }
                else
                {
                    viewport.SetViewportOrigin(newOffset);

                    Debug.WriteLine(@"/++++  After ResizeImage absOriginCanvasCenterPoint:{0} ,newAbsCanvasPoint:{1},
                                             move offset:{2}, newViewportPoint:{3}, 
                                             viewport.Viewport:{4},viewport.Bounds：{5}  ++++/",
                                 absOriginCanvasCenterPoint, newAbsCanvasPoint, newOffset,
                                 newViewportPoint, viewport.Viewport, viewport.Bounds);
                }
            }
        }

        /// <summary> 
        /// Coerce the scale into being within the proper range. Optionally compute the constraints  
        /// on the scale so that it will always fill the entire screen and will never get too big  
        /// to be contained in a hardware surface. 
        /// </summary> 
        /// <param name="recompute">Will recompute the min max scale if true.</param> 
        void CoerceScale(bool recompute)
        {
            if (recompute && viewport != null && ImageSource != null)
            {
                // Calculate the minimum scale to fit the viewport 
                double minX = viewport.ActualWidth / ImageSource.PixelWidth;
                double minY = viewport.ActualHeight / ImageSource.PixelHeight;

                minScale = Math.Min(minX, minY);
            }
            coercedScale = Math.Min(maxScale, Math.Max(currentScale, minScale));
        }

        #endregion
    }
}
