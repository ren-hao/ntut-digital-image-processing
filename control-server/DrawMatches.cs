using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.XFeatures2D;
using Emgu.CV.Flann;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.UI;

namespace control_server
{
    public static class DrawMatches
    {
        private static Point[] points;
        private static int moneyInScreen;

        public static Point[] GetPs()
        {
            return points;
        }

        public static int GetMoneyInScreen()
        {
            return moneyInScreen;
        }

        public static void FindMatch(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
        {
            int k = 2;
            double uniquenessThreshold = 0.80;

            Stopwatch watch;
            homography = null;

            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

            using (UMat uModelImage = modelImage.GetUMat(AccessType.Read))
            using (UMat uObservedImage = observedImage.GetUMat(AccessType.Read))
            {
                //KAZE featureDetector = new KAZE();
                SIFT surf = new SIFT();

                //extract features from the object image
                Mat modelDescriptors = new Mat();
                //featureDetector.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);
                surf.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);

                watch = Stopwatch.StartNew();

                // extract features from the observed image
                Mat observedDescriptors = new Mat();
                //featureDetector.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);
                surf.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);

                // Bruteforce, slower but more accurate
                // You can use KDTree for faster matching with slight loss in accuracy
                using (Emgu.CV.Flann.KdTreeIndexParams ip = new Emgu.CV.Flann.KdTreeIndexParams())
                using (Emgu.CV.Flann.SearchParams sp = new SearchParams())
                // 匹配器
                using (DescriptorMatcher matcher = new FlannBasedMatcher(ip, sp))
                {
                    matcher.Add(modelDescriptors);

                    matcher.KnnMatch(observedDescriptors, matches, k, null);
                    mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        //Console.WriteLine("1:"+nonZeroCount.ToString());
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                            matches, mask, 1.5, 20);
                        //Console.WriteLine("2:" + nonZeroCount.ToString());
                        if (nonZeroCount >= 15)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                                observedKeyPoints, matches, mask, 2);
                    }
                }
                watch.Stop();

            }
            matchTime = watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Draw the model image and observed image, the matched features and homography projection.
        /// </summary>
        /// <param name="modelImage">The model image</param>
        /// <param name="observedImage">The observed image</param>
        /// <param name="matchTime">The output total time for computing the homography matrix.</param>
        /// <returns>The model image and observed image, the matched features and homography projection.</returns>
        public static Mat Draw(Mat modelImage, Mat observedImage, out long matchTime)
        {
            points = null;
            Mat homography;
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                Mat mask;
                FindMatch(modelImage, observedImage, out matchTime, out modelKeyPoints, out observedKeyPoints, matches,
                   out mask, out homography);

                //Draw the matched keypoints
                Mat result = new Mat();
                Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                   matches, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask);



                #region draw the projected region on the image

                if (homography != null)
                {
                    //draw a rectangle along the projected model
                    Rectangle rect = new Rectangle(Point.Empty, modelImage.Size);
                    PointF[] pts = new PointF[]
                    {
                          new PointF(rect.Left, rect.Bottom),
                          new PointF(rect.Right, rect.Bottom),
                          new PointF(rect.Right, rect.Top),
                          new PointF(rect.Left, rect.Top)
                    };
                    pts = CvInvoke.PerspectiveTransform(pts, homography);

#if NETFX_CORE
               Point[] points = Extensions.ConvertAll<PointF, Point>(pts, Point.Round);
#else
                    points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
#endif
                    using (VectorOfPoint vp = new VectorOfPoint(points))
                    {
                        CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                    }
                }
                #endregion

                return result;

            }
        }

        public static Mat DetectBillInScreen(Mat frame, ref String[] b, int WIDTH, int HEIGHT)
        {
            long matchTime;
            moneyInScreen = 0;
            Point[][] pointArray = new Point[5][];
            Mat test = new Mat();
            for (int i = 0; i < b.Length; i++)
            {
                String bill = "resources/" + b[i] + ".jpg";
                //using (Mat observedImage = CvInvoke.Imread(frame, ImreadModes.Grayscale))
                using (Mat modelImage = CvInvoke.Imread(bill, ImreadModes.Grayscale))
                {
                    Mat observedImage = frame;
                    CvInvoke.Resize(observedImage, observedImage, new Size(WIDTH, HEIGHT));
                    CvInvoke.Resize(modelImage, modelImage, new Size(WIDTH, HEIGHT));
                    Mat result = DrawMatches.Draw(modelImage, observedImage, out matchTime);
                    //ImageViewer.Show(result, String.Format("Matched in {0} milliseconds", matchTime));
                    Point[] ps = DrawMatches.GetPs();
                    if (ps != null)
                    {
                        //Console.WriteLine(b[i] + ps[0].ToString() + ps[1].ToString() + ps[2].ToString() + ps[3].ToString());
                        moneyInScreen += Convert.ToInt32(b[i].Split('_')[0]);
                    }
                    if (i == 0) test = result;
                }
                pointArray[i] = DrawMatches.GetPs();
            }

            //return DrawRentengle(pointArray, frame);
            return test;
        }

        private static Mat DrawRentengle(Point[][] point, Mat frame)
        {
            for (int i = 0; i < 5; i++)
                if (point[i] != null)
                    using (VectorOfPoint vp = new VectorOfPoint(point[i]))
                    {
                        CvInvoke.Polylines(frame, vp, true, new MCvScalar(255, 0, 255, 255), 5);
                    }
            return frame;
        }
    }
}
