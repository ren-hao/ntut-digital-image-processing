using System;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace control_server
{
    //Ref: http://www.emgu.com/forum/viewtopic.php?t=4989 , https://github.com/imZack/Digital-Image-Processing-2/blob/master/ObjectTracking.cs
    class CamShiftTracking : IDisposable
    {
        public Image<Hsv, Byte> hsv;
        public Image<Gray, Byte> hue;
        public Image<Gray, Byte> mask;
        public Image<Gray, Byte> backproject;
        public DenseHistogram hist;
        private Rectangle trackingWindow;

        private readonly Size SIZE=new Size(0, 0);
        private const int SIGMAX = 3;

        const int iBinSize = 60;

        MCvTermCriteria TermCriteria = new MCvTermCriteria() { Epsilon = 100 * Double.Epsilon, MaxIter = 50 };

        public CamShiftTracking(Image<Bgr, Byte> image, Rectangle ROI)
        {
            using (Image<Bgr, Byte> blur = image.Clone())
            {
                CvInvoke.GaussianBlur(blur, blur, SIZE, SIGMAX);
                hue = new Image<Gray, byte>(blur.Width, blur.Height);
                hue._EqualizeHist();
                mask = new Image<Gray, byte>(blur.Width, blur.Height);
                hist = new DenseHistogram(iBinSize, new RangeF(0, 360));
                backproject = new Image<Gray, byte>(blur.Width, blur.Height);

                // Assign Object's ROI from source image.
                trackingWindow = ROI;

                CalObjectHist(blur);
            }

           
        }

        public void Dispose()
        {
            if (hsv != null) hsv.Dispose();
            if (hue != null) hue.Dispose();
            if (mask != null) mask.Dispose();
            if (backproject != null) backproject.Dispose();
            if (hist != null) hist.Dispose();
        }

        public Rectangle Tracking(Image<Bgr, Byte> image)
        {
            using (Image<Bgr, Byte> blur = image.Clone())
            {
                CvInvoke.GaussianBlur(blur, blur, SIZE, SIGMAX);
                UpdateHue(blur);
            }
            if (backproject != null) backproject.Dispose();
            backproject = hist.BackProject(new Image<Gray, Byte>[] { hue });

            // Apply mask
            backproject._And(mask);

            // Tracking windows empty means camshift lost bounding-box last time
            // here we give camshift a new start window from 0,0 (you could change it)
            if (trackingWindow.IsEmpty || trackingWindow.Width == 0 || trackingWindow.Height == 0)
            {
                trackingWindow = new Rectangle(0, 0, 100, 100);
            }

            var r = CvInvoke.CamShift(backproject, ref trackingWindow, TermCriteria);
            
            return trackingWindow;
        }

        public Image<Bgr, byte> TrackAndDraw(Image<Bgr, Byte> sourceImage, Pen rectPen)
        {
            var result = sourceImage.Clone();
            var trackObjRect = Tracking(sourceImage);
            var ctx = Graphics.FromImage(result.Bitmap);
            ctx.DrawRectangle(rectPen, trackObjRect);
            return result;
        }

        /// <summary>  Producing Object's hist </summary>
        ///
        /// <param name="image">    The image. </param>
        private void CalObjectHist(Image<Bgr, Byte> image)
        {
            UpdateHue(image);
            hue.ROI = trackingWindow;
            mask.ROI = trackingWindow;
            //// Set tracking object's ROI
            //Image<Gray, byte> imgTrackingImageROI;
            //image.ROI = trackingWindow;
            //imgTrackingImageROI = hue.Copy();
            //image.ROI = Rectangle.Empty;
            hist.Calculate(new Image<Gray, Byte>[] { hue }, false, mask);
            // Scale Historgram
            CvInvoke.Normalize(hist, hist, 0, 255, NormType.MinMax);

            //using (Mat matBackProjectionMask = new Mat())
            //using (VectorOfMat vmTrackingImage = new VectorOfMat(image.Mat))
            //using (VectorOfMat vmTrackingImageROI = new VectorOfMat(imgTrackingImageROI.Mat))
            //{
            //    CvInvoke.CalcHist(vmTrackingImageROI, channelsVec, matBackProjectionMask, hist, histSizeVec, rangesVec, bAccumulate);
            //    CvInvoke.Normalize(hist, hist, 0, 255, NormType.MinMax);
            //    CvInvoke.CalcBackProject(vmTrackingImage, channelsVec, hist, backproject, rangesVec, 1);
            //}
            //imgTrackingImageROI.Save(@"Y:\tmp.png");

            // Clear ROI
            hue.ROI = System.Drawing.Rectangle.Empty;
            mask.ROI = System.Drawing.Rectangle.Empty;
            // Now we have Object's Histogram, called hist.
        }

        private void UpdateHue(Image<Bgr, Byte> image)
        {
            // release previous image memory
            if (hsv != null) hsv.Dispose();
            hsv = image.Convert<Hsv, Byte>();
            // Drop low saturation pixels
            mask = hsv.Split()[1].ThresholdBinary(new Gray(60), new Gray(255));

            hue = hsv.Split()[0];
        }
    }
}
