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
using System.Threading.Tasks;
using System.Linq;

namespace control_server
{
    public class DrawMatches : IDisposable
    {
        private readonly int WIDTH;
        private readonly int HEIGHT;

        private int _moneyInScreen = 0;
        private readonly Dictionary<string, Mat> _modelImages;
        private static String[] b = new String[] { "100_0", "200_0", "500_0", "1000_0", "2000_0" };

        public DrawMatches(int width, int height)
        {
            WIDTH = width;
            HEIGHT = height;

            _modelImages = new Dictionary<string, Mat>();
            foreach(var path in b)
            {
                var bill = "resources/" + path + ".jpg";
                Mat modelImage = CvInvoke.Imread(bill, ImreadModes.Grayscale);
                CvInvoke.Resize(modelImage, modelImage, new Size(WIDTH, HEIGHT));
                _modelImages.Add(path, modelImage);
            }
        }

        public int GetMoneyInScreen()
        {
            return _moneyInScreen;
        }

        private static void FindMatch(Mat modelImage, Mat observedImage, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
        {
            int k = 2;
            double uniquenessThreshold = 0.80;

            //Stopwatch watch;
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

                //watch = Stopwatch.StartNew();

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
                //watch.Stop();

            }
            //matchTime = watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// Draw the model image and observed image, the matched features and homography projection.
        /// </summary>
        /// <param name="modelImage">The model image</param>
        /// <param name="observedImage">The observed image</param>
        /// <param name="matchTime">The output total time for computing the homography matrix.</param>
        /// <returns>The model image and observed image, the matched features and homography projection.</returns>
        public static Tuple<Mat, Point[]> Draw(Mat modelImage, Mat observedImage)
        {
            Point[] points = null;
            Mat homography;
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                Mat mask;
                FindMatch(modelImage, observedImage, out modelKeyPoints, out observedKeyPoints, matches,
                   out mask, out homography);

                //Draw the matched keypoints
                Mat result = null;

                result = new Mat();
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

                    points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                    using (VectorOfPoint vp = new VectorOfPoint(points))
                    {
                        CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                    }
                }
                #endregion
                
                return new Tuple<Mat, Point[]>(result, points);
            }
        }

        public Point[][] DetectBillInScreen(Mat frame)
        {
            long matchTime;
            _moneyInScreen = 0;
            Point[][] pointArray = new Point[b.Length][];
            Mat test = null;

            int[] detectedMoney = new int[b.Length];
            using(Mat observedImage = frame.Clone())
            {
                CvInvoke.Resize(observedImage, observedImage, new Size(WIDTH, HEIGHT));
                //for(int i = 0; i < b.Length; i++)
                Parallel.For(0, b.Length, i =>
                {
                    detectedMoney[i] = 0;
                    var modelImage = _modelImages[b[i]];
                    var result = Draw(modelImage, observedImage);

                    var ps = result.Item2;
                    pointArray[i] = ps;
                    if (ps != null)
                    {
                        detectedMoney[i] = Convert.ToInt32(b[i].Split('_')[0]);
                    }
                    result.Item1.Dispose();
                });
            }

            _moneyInScreen = detectedMoney.Sum();
            
            return pointArray;
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

        public void Dispose()
        {
            foreach(var v in _modelImages.Values)
            {
                v.Dispose();
            }
        }
    }
}
