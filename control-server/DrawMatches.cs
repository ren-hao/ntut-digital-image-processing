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
        // private readonly Dictionary<string, FeatureModel> _modelFeatures;
        private readonly Dictionary<string, Matcher> _modelMatcher;
        private static String[] b = new String[] { "100_0", "200_0", "500_0", "1000_0", "2000_0" };

        public DrawMatches(int width, int height)
        {
            WIDTH = width;
            HEIGHT = height;

            _modelImages = new Dictionary<string, Mat>();
            // _modelFeatures = new Dictionary<string, FeatureModel>();
            _modelMatcher = new Dictionary<string, Matcher>();
            foreach (var path in b)
            {
                var bill = "resources/" + MODEL_PIXEL.ToString() + "/" +path + ".jpg";
                Mat modelImage = CvInvoke.Imread(bill, ImreadModes.Grayscale);
                // CvInvoke.Resize(modelImage, modelImage, new Size(WIDTH, HEIGHT));
                _modelImages.Add(path, modelImage);
                var matcher = new Matcher();
                matcher.Add(ExtractFeatures(modelImage));
                //
                _modelMatcher.Add(path, matcher.Train());
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
            int k = 20;
            double uniquenessThreshold = 0.80;

            //Stopwatch watch;
            homography = null;
            var matcher = _modelMatcher[modelImagePath];
            using (var observedFeature = ExtractFeatures(observedImage))
            {
                matcher.KnnMatch(observedFeature.Descriptors, matches, k, null);
                using (var mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1))
                {
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= k / 2)
                    {
                        foreach (var modelFeature in matcher.Features)
                        {
                            nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelFeature.KeyPoints, observedFeature.KeyPoints,
                            matches, mask, 1.5, 20);
                            //Console.WriteLine("2:" + nonZeroCount.ToString());
                            if (nonZeroCount >= k)
                            {
                                homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelFeature.KeyPoints,
                                    observedFeature.KeyPoints, matches, mask, 2);
                                break;
                            }
                        }
                        //Console.WriteLine("1:"+nonZeroCount.ToString());

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
                for(int i = 0; i < b.Length; i++)
                //Parallel.For(0, b.Length, i =>
                {
                    detectedMoney[i] = 0;
                    var modelImage = b[i];
                    var ps = Draw(modelImage, observedImage);

                    pointArray[i] = ps;
                    if (ps != null)
                    {
                        detectedMoney[i] = Convert.ToInt32(b[i].Split('_')[0]);
                    }
                }//);
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
            //foreach (var v in _modelFeatures.Values)
            //    v.Dispose();
            foreach (var v in _modelImages.Values)
                v.Dispose();
            //_modelFeatures.Clear();
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

        private class Matcher : IDisposable
        {
            private List<IDisposable> _disposables = new List<IDisposable>();
            private List<FeatureModel> _models = new List<FeatureModel>();
            private KdTreeIndexParams ip;
            private SearchParams sp;
            public readonly DescriptorMatcher matcher;

            public IEnumerable<FeatureModel> Features => _models;

            public Matcher()
            {
                ip = new Emgu.CV.Flann.KdTreeIndexParams();
                sp = new SearchParams();
                matcher = new FlannBasedMatcher(ip, sp);
                _disposables.Add(ip);
                _disposables.Add(sp);
                _disposables.Add(matcher);
            }

            public void Add(FeatureModel feature)
            {
                _models.Add(feature);
                _disposables.Add(feature);
            }

            public Matcher Train()
            {
                foreach(var m in _models)
                    matcher.Add(m.Descriptors);

                return this;
            }

            public void KnnMatch(Mat queryDescriptor, VectorOfVectorOfDMatch matches, int k, IInputArray mask)
            {
                matcher.KnnMatch(queryDescriptor, matches, k, mask);
            }

            public void Dispose()
            {
                foreach (var m in _disposables)
                    m.Dispose();
            }
        }
    }
}
