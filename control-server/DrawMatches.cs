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
        private SIFT surf = new SIFT();
        private readonly int WIDTH;
        private readonly int HEIGHT;
        private const int MODEL_PIXEL = 480;

        private int _moneyInScreen = 0;
        private readonly Dictionary<string, Mat> _modelImages;
        private readonly Dictionary<string, FeatureModel> _modelFeatures;
        private static String[] b = new String[] { "100_0", "200_0", "500_0", "1000_0", "2000_0" };

        public DrawMatches(int width, int height)
        {
            WIDTH = width;
            HEIGHT = height;

            _modelImages = new Dictionary<string, Mat>();
            _modelFeatures = new Dictionary<string, FeatureModel>();
            foreach (var path in b)
            {
                var bill = "resources/" + MODEL_PIXEL.ToString() + "/" +path + ".jpg";
                Mat modelImage = CvInvoke.Imread(bill, ImreadModes.Grayscale);
                // CvInvoke.Resize(modelImage, modelImage, new Size(WIDTH, HEIGHT));
                _modelImages.Add(path, modelImage);
                _modelFeatures.Add(path, ExtractFeatures(modelImage));
            }
        }

        public int GetMoneyInScreen()
        {
            return _moneyInScreen;
        }

        private FeatureModel ExtractFeatures(Mat modelImage)
        {
            var modelKeyPoints = new VectorOfKeyPoint();
            Mat modelDescriptors = new Mat();
            using (UMat uModelImage = modelImage.GetUMat(AccessType.Read))
            {
                //featureDetector.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);
                surf.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);
            }

            return new FeatureModel(modelKeyPoints, modelDescriptors);
        }

        private void FindMatch(
            string modelImagePath, Mat observedImage,
            VectorOfVectorOfDMatch matches, 
            out Mat homography
        )
        {
            int k = 2;
            double uniquenessThreshold = 0.60;

            //Stopwatch watch;
            homography = null;
            var modelFeature = _modelFeatures[modelImagePath];
            using (var observedFeature = ExtractFeatures(observedImage))

            // You can use KDTree for faster matching with slight loss in accuracy
            using (Emgu.CV.Flann.KdTreeIndexParams ip = new Emgu.CV.Flann.KdTreeIndexParams())
            using (Emgu.CV.Flann.SearchParams sp = new SearchParams())
            // 匹配器
            using (DescriptorMatcher matcher = new FlannBasedMatcher(ip, sp))
            {
                matcher.Add(modelFeature.Descriptors);

                matcher.KnnMatch(observedFeature.Descriptors, matches, k, null);
                using (var mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1))
                {
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        //Console.WriteLine("1:"+nonZeroCount.ToString());
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelFeature.KeyPoints, observedFeature.KeyPoints,
                            matches, mask, 1.5, 20);
                        //Console.WriteLine("2:" + nonZeroCount.ToString());
                        if (nonZeroCount >= 15)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelFeature.KeyPoints,
                                observedFeature.KeyPoints, matches, mask, 2);
                    }
                }
            }
        }

        /// <summary>
        /// Draw the model image and observed image, the matched features and homography projection.
        /// </summary>
        /// <param name="modelImage">The model image</param>
        /// <param name="observedImage">The observed image</param>
        /// <param name="matchTime">The output total time for computing the homography matrix.</param>
        /// <returns>The model image and observed image, the matched features and homography projection.</returns>
        public Point[] Draw(string modelImagePath, Mat observedImage)
        {
            Point[] points = null;
            Mat homography;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                FindMatch(modelImagePath, observedImage, matches, out homography);

                //Draw the matched keypoints
                //Mat result = null;

                //result = new Mat();
                //Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                //    matches, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask);

                #region draw the projected region on the image

                if (homography != null)
                {
                    //draw a rectangle along the projected model
                    Rectangle rect = new Rectangle(Point.Empty, _modelImages[modelImagePath].Size);
                    PointF[] pts = new PointF[]
                    {
                        new PointF(rect.Left, rect.Bottom),
                        new PointF(rect.Right, rect.Bottom),
                        new PointF(rect.Right, rect.Top),
                        new PointF(rect.Left, rect.Top)
                    };
                    pts = CvInvoke.PerspectiveTransform(pts, homography);

                    points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                    //using (VectorOfPoint vp = new VectorOfPoint(points))
                    //{
                    //    CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                    //}

                    homography.Dispose();
                }
                #endregion
                return points;
            }
        }

        public Point[][] DetectBillInScreen(Mat frame)
        {
            _moneyInScreen = 0;
            Point[][] pointArray = new Point[b.Length][];

            int[] detectedMoney = new int[b.Length];
            using(Mat observedImage = frame.Clone())
            {
                CvInvoke.Resize(observedImage, observedImage, new Size(WIDTH, HEIGHT));
                //for(int i = 0; i < b.Length; i++)
                Parallel.For(0, b.Length, i =>
                {
                    try
                    {
                        detectedMoney[i] = 0;
                        var modelImage = b[i];
                        var ps = Draw(modelImage, observedImage);

                        pointArray[i] = ps;
                        if (ps != null)
                        {
                            detectedMoney[i] = Convert.ToInt32(b[i].Split('_')[0]);
                        }
                    }
                    catch (Exception) { }
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
            foreach (var v in _modelFeatures.Values)
                v.Dispose();
            foreach (var v in _modelImages.Values)
                v.Dispose();
            _modelFeatures.Clear();
            _modelImages.Clear();
        }

        private class FeatureModel : IDisposable
        {
            public readonly VectorOfKeyPoint KeyPoints;
            public readonly Mat Descriptors;

            public FeatureModel(VectorOfKeyPoint modelKeyPoints, Mat modelDescriptors)
            {
                this.KeyPoints = modelKeyPoints;
                this.Descriptors = modelDescriptors;
            }

            public void Dispose()
            {
                KeyPoints.Dispose();
                Descriptors.Dispose();
            }
        }
    }
}
