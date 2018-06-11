using Fleck;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

using Emgu.CV;
using Emgu.CV.Util;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;
using Emgu.CV.XFeatures2D;
using Emgu.CV.CvEnum;
using Emgu.CV.UI;
using Emgu.Util;
using System.IO;
using System.ServiceModel.Dispatcher;

namespace control_server
{
    public partial class Form1 : Form
    {
        private enum TrackStatus { None, Left, Right };
        private const bool USE_CAMERA = true;
        private const int CAM_ID = 0;
        private const int SEND_PER_SEC = 50;
        private Server _server = null;
        // delegate object obj_delegate();
        private const int PORT = 2229;
        public const int WIDTH = 853;
        public const int HEIGHT = 480;
        private const long DETECT_MONEY_INTERVAL = 1000;
        // property
        private volatile bool _isCapturing = false;
        private bool _isDetectorProcessing = false;
        private VideoCapture _capture = null;
        private Image<Bgr, Byte>[] _resultImgObj = new Image<Bgr, byte>[1];
        private Mat _captureFrame = new Mat();
        private Mat _captureObservedFrame = new Mat();
        private DrawMatches _momeyMatches = new DrawMatches(WIDTH, HEIGHT);
        private String[] b = new String[] { "100_0", "200_0", "500_0", "1000_0", "2000_0" };
        private double FPS = 24;
        private long FPS_TICKS;
        private long _lastFetchTime = 0;
        private long _lastDetectTime = 0;
        private CaptureExceptionHandler _capErrHandler = new CaptureExceptionHandler();

        private Point _clickPoint;
        private CamShiftTracking _trackingObjL = null;
        private CamShiftTracking _trackingObjR = null;
        Rectangle _drawRectL = Rectangle.Empty;
        Rectangle _drawRectR = Rectangle.Empty;
        private bool _isMouseDown = false;

        public Pen rectPenL = new Pen(Brushes.Red, 3);
        public Pen rectPenR = new Pen(Brushes.Blue, 3);
        public Pen moneyPen = new Pen(Brushes.Magenta, 5);
        private readonly Size SIZE = new Size(8, 8);
        private Point[][] moneyPoints = null;

        private TrackStatus _trackStatus = TrackStatus.None;

        private Queue<int> _moneyQueue = new Queue<int>();
        private const int MAX_QUEUE_SIZE = 10;
        private int _realMoney = 0;
        // property

        public Form1()
        {
            InitializeComponent();
            _timer.Interval = 1000 / SEND_PER_SEC;
            this.Closing += new System.ComponentModel.CancelEventHandler(Form1_Closing);
            _sourcePictureBox.Size = new Size(WIDTH, HEIGHT);

            // 用圖片當 observation
            //DrawMatches.DetectBillInScreen("resources/test1000.jpg", b, WIDTH, HEIGHT);
        }

        private void Form1_Closing(object sender, CancelEventArgs e)
        {
            if (IsCapturing())
            {
                MessageBox.Show("Please Close Camera first.", "WARNING", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StartServer();
            button1.Enabled = false;
            groupBox2.Enabled = true;
        }

        /// <summary>   Starts a server. </summary>
        private void StartServer()
        {
            if (_server != null) return;
            _server = new Server("127.0.0.1", PORT);
            _server.Start();
            _timer.Start();
        }

        /// <summary>   Send current position to client </summary>
        ///
        /// <param name="sender">   Source of the event. </param>
        /// <param name="e">        Event information. </param>
        private void _timer_Tick(object sender, EventArgs e)
        {
            if (_server == null)
            {
                _timer.Stop();
                return;
            }

            _server.SendToAll("{\"op\":\"move\",\"data\":[" + GetLeftValue() + ", " + GetRightValue() + "]}");
        }

        /// <summary>   Gets left value. </summary>
        ///
        /// <returns>   0~100, -1 mean not found </returns>
        private int GetLeftValue()
        {
            // TODO:  fetch from camera
            return _leftBar.Value;
        }

        /// <summary>   Gets right value. </summary>
        ///
        /// <returns>   0~100, -1 mean not found </returns>
        private int GetRightValue()
        {
            // TODO:  fetch from camera
            return _rightBar.Value;
        }

        private void SendDonateMoney(int money)
        {
            _server.SendToAll("{\"op\":\"donate\",\"data\":" + money + "}");
        }

        private void buttonDonate_Click(object sender, EventArgs e)
        {
            int money = int.Parse(((Button)sender).Text);
            SendDonateMoney(money);
        }

        private bool IsCapturing()
        {
            return _isCapturing && _capture != null && _capture.Ptr != IntPtr.Zero && _capture.IsOpened;
        }

        public static void ReleaseImage<T>(ref T data) where T : UnmanagedObject
        {
            if (data != null && data.Ptr != IntPtr.Zero)
            {
                data.Dispose();
            }
            data = null;
        }

        private void ShowDraggingRect(Rectangle rect, Pen pen, Bitmap target)
        {
            if (rect != Rectangle.Empty)
            {
                Console.WriteLine(rect.ToString());
                var ctx = Graphics.FromImage(target);
                ctx.DrawRectangle(pen, rect);
            }
        }

        private void UpdateTrackView(TrackBar bar, Rectangle rect, Pen pen, Bitmap target)
        {
            if (rect != Rectangle.Empty)
            {
                var ctx = Graphics.FromImage(target);
                ctx.DrawRectangle(pen, rect);
                int pointRectCnt = (rect.Left + rect.Width / 2);
                int pointBoxCnt = WIDTH >> 1;
                bar.Value = (int)Math.Round((double)(pointRectCnt - pointBoxCnt) / (WIDTH >> 1) * 100);
            }
            else
            {
                //bar.Value = 50;
            }
        }

        private void ProcessFrame(object sender, EventArgs e)
        {
            long diff = DateTime.Now.Ticks - _lastFetchTime;
            if (diff < FPS_TICKS)
            {
                Thread.Sleep((int)((FPS_TICKS - diff) / (1000 * 10)));
            }

            //Console.WriteLine("FPS: {0}", 1 / ((DateTime.Now.Ticks - _lastFetchTime) / (1000 * 1000 * 10f)));
            Interlocked.Exchange(ref _lastFetchTime, DateTime.Now.Ticks);
            if (IsCapturing())
            {
                //取得網路攝影機的影像
                _capture.Retrieve(_captureFrame);
                CvInvoke.Resize(_captureFrame, _captureFrame, new Size(WIDTH, HEIGHT), 0, 0, Emgu.CV.CvEnum.Inter.Cubic);

                if (DateTime.Now.Ticks - _lastDetectTime >= DETECT_MONEY_INTERVAL * 1000 * 10)
                {
                    _lastDetectTime = DateTime.Now.Ticks;
                    new Thread(() => ProgressCapture(sender, e)).Start();
                }

                this.Invoke((MethodInvoker)delegate
                {
                    if (!IsCapturing()) return;
                    //if (!_isDetectorProcessing) _captureObservedFrame = _captureFrame.Clone();
                    using (Mat _sourceFrame = _captureFrame.Clone())
                    using (Image<Bgr, byte> sourceImage = _sourceFrame.ToImage<Bgr, Byte>())
                    using (Image<Bgr, byte> blurImage = sourceImage.Clone())
                    {
                        CvInvoke.Blur(blurImage, blurImage, SIZE, Point.Empty);

                        if (_isMouseDown)
                        {
                            ShowDraggingRect(_drawRectL, rectPenL, sourceImage.Bitmap);
                            ShowDraggingRect(_drawRectR, rectPenR, sourceImage.Bitmap);
                        }
                        // runs on UI thread
                        _sourcePictureBox.Image = sourceImage.Bitmap;
                        _sourcePictureBox.Refresh();

                        if (_trackStatus != TrackStatus.None)
                            SetTrackObject(blurImage);
                        ///
                        ////////////////////////////////////////////////////////////////////////////////////
                        ///
                        lock (_resultImgObj)
                        {
                            ReleaseImage(ref _resultImgObj[0]);

                            if (_trackingObjL != null || _trackingObjR != null)
                            {
                                _resultImgObj[0] = sourceImage.Clone();
                            }

                            if (_trackingObjL != null)
                            {
                                UpdateTrackView(_leftBar, _trackingObjL.Tracking(blurImage), rectPenL, _resultImgObj[0].Bitmap);
                            }
                            if (_trackingObjR != null)
                            {
                                UpdateTrackView(_rightBar, _trackingObjR.Tracking(blurImage), rectPenR, _resultImgObj[0].Bitmap);
                            }

                            if (_resultImgObj[0] == null)
                            {
                                _resultImgObj[0] = sourceImage.Clone();
                            }

                            if (!_isDetectorProcessing && moneyPoints != null)
                            {
                                var ctx = Graphics.FromImage(_resultImgObj[0].Bitmap);
                                foreach (var ps in moneyPoints)
                                {
                                    if (ps != null)
                                    {
                                        var xQuery = from p in ps select p.X;
                                        var yQuery = from p in ps select p.Y;
                                        var minX = xQuery.Min();
                                        var minY = yQuery.Min();
                                        var maxX = xQuery.Max();
                                        var maxY = yQuery.Max();
                                        ctx.DrawRectangle(moneyPen, minX, minY, maxX - minX, maxY - minY);
                                    }
                                }
                            }

                            _resultPictureBox.Image = _resultImgObj[0].Bitmap;
                            _resultPictureBox.Refresh();
                        }
                    }
                });
                return;
            }
            else
            {
                _sourcePictureBox.Image = _resultPictureBox.Image = null;
            }

            //釋放繪圖資源->避免System.AccessViolationException
            //GC.Collect();
        }

        private void _openCameraButton_Click_1(object sender, EventArgs e)
        {
            if (IsCapturing())
            {
                _openCameraButton.Text = "開啟攝影機";
                Console.WriteLine("close");
                _isCapturing = false;
                _capture.ImageGrabbed -= ProcessFrame;
                lock (_capture)
                {
                    _capture.Stop();//摄像头关闭
                }
                _capture.Dispose();
                _capture = null;
                _sourcePictureBox.Image = _resultPictureBox.Image = null;
                _sourcePictureBox.Refresh();
                _resultPictureBox.Refresh();
            }
            else
            {
                _openCameraButton.Enabled = false;
                _openCameraButton.Text = "停止攝影機";
                if (USE_CAMERA)
                {
                    _capture = new VideoCapture(CAM_ID);
                    FPS = 24;
                }
                else
                {
                    string _videoPath = LoadVideoFile();
                    _capture = new VideoCapture(_videoPath);
                    FPS = _capture.GetCaptureProperty(Emgu.CV.CvEnum.CapProp.Fps);
                }

                FPS_TICKS = (long)(1000 * 1000 * 10 / FPS);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoExposure, 0);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, WIDTH);//_sourcePictureBox.Width
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, HEIGHT);// _sourcePictureBox.Height
                _capture.ImageGrabbed += ProcessFrame;
                //_capture.ImageGrabbed += ProgressCapture;

                _openCameraButton.Enabled = true;

                if (_capture != null)
                {
                    _capture.Start(_capErrHandler);
                    _isCapturing = true;
                }
            }
        }

        private void SetResultImage(Image<Bgr, byte> target)
        {
            _resultPictureBox.Image = null;
            ReleaseImage(ref _resultImgObj[0]);
            _resultPictureBox.Image = _resultImgObj[0].Bitmap;
            _resultPictureBox.Refresh();
        }

        private int GetMostItem()
        {
            int largestValue = 0;
            int largestKey = 0;
            Dictionary<int, int> count = new Dictionary<int, int>();
            foreach (int m in _moneyQueue)
            {
                if (count.ContainsKey(m))
                    count[m] += 1;
                else
                    count.Add(m, 1);
                if (count[m] > largestValue)
                {
                    largestValue = count[m];
                    largestKey = m;
                }
            }
            return largestKey;
        }

        private void ProgressCapture(object sender, EventArgs e)
        {
            if (_isDetectorProcessing)
                return;
            if (!IsCapturing())
                return;
            using (Mat _sourceFrame = _captureFrame.Clone())
            using (Mat _grayFrame = new Mat())
            {
                _isDetectorProcessing = true;

                CvInvoke.CvtColor(_sourceFrame, _grayFrame, ColorConversion.Bgr2Gray);
                moneyPoints = _momeyMatches.DetectBillInScreen(_grayFrame);
                _isDetectorProcessing = false;
            }

            if (_moneyQueue.Count == MAX_QUEUE_SIZE)
                _moneyQueue.Dequeue();
            _moneyQueue.Enqueue(_momeyMatches.GetMoneyInScreen());

            Console.WriteLine(GetMostItem());
        }

        private void _sourcePictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (!IsCapturing()) return;
            _clickPoint = _sourcePictureBox.PointToClient(Cursor.Position);
            _isMouseDown = true;

        }

        private void SetTrackAndRect(ref CamShiftTracking tracking, ref Rectangle rect, CamShiftTracking targetTracking, Rectangle targetRect)
        {
            if (tracking != null)
                tracking.Dispose();
            tracking = targetTracking;
            rect = targetRect;
        }

        private void SetTrackObject(Image<Bgr, byte> image)
        {
            if (_trackStatus == TrackStatus.None) return;
            CamShiftTracking trackObj = null;
            Rectangle rect = Rectangle.Empty;

            if (_trackStatus == TrackStatus.Left)
                rect = _drawRectL;
            else
                rect = _drawRectR;

            trackObj = new CamShiftTracking(image, rect);

            if (_trackStatus == TrackStatus.Left)
                SetTrackAndRect(ref _trackingObjL, ref _drawRectL, trackObj, Rectangle.Empty);
            else
                SetTrackAndRect(ref _trackingObjR, ref _drawRectR, trackObj, Rectangle.Empty);

            _trackStatus = TrackStatus.None;
        }

        private void _sourcePictureBox_MouseUp(object sender, MouseEventArgs e)
        {

            if (!_isMouseDown) return;
            if (e.Button == MouseButtons.Right)
                _trackStatus = TrackStatus.Right;
            if (e.Button == MouseButtons.Left)
                _trackStatus = TrackStatus.Left;
            _isMouseDown = false;
        }

        private void _sourcePictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isMouseDown) return;

            Point curPoint = _sourcePictureBox.PointToClient(Cursor.Position);
            if (e.Button == MouseButtons.Left) _drawRectL = GetRectFromPoint(curPoint, _clickPoint);
            else if (e.Button == MouseButtons.Right) _drawRectR = GetRectFromPoint(curPoint, _clickPoint);
            Console.WriteLine("Move");
        }

        private Rectangle GetRectFromPoint(Point p1, Point p2)
        {
            int left = Math.Min(p1.X, p2.X);
            int top = Math.Min(p1.Y, p2.Y);
            int width = Math.Abs(p1.X - p2.X);
            int height = Math.Abs(p1.Y - p2.Y);
            return new Rectangle(left, top, width, height);
        }
        private string LoadVideoFile()
        {
            string fileName = "";
            OpenFileDialog dialog = new OpenFileDialog();
            DirectoryInfo dir = new DirectoryInfo(System.Windows.Forms.Application.StartupPath);
            dialog.Title = "Open a Video File";
            dialog.RestoreDirectory = true;
            dialog.Filter = "Video File|*.mkv;*.mp4;*.avi;*.flv;*.mov";

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK && dialog.FileName != null)
            {
                fileName = dialog.FileName;
            }

            return fileName;
        }

        private void _leftBar_Scroll(object sender, EventArgs e)
        {

        }

    }
}
