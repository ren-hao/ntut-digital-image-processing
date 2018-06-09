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

namespace control_server
{
    public partial class Form1 : Form
    {
        private enum TrackStatus { None, Left, Right };
        private const int SEND_PER_SEC = 50;
        private Server _server = null;
        // delegate object obj_delegate();
        private const int PORT = 2229;
        public const int WIDTH = 480;
        public const int HEIGHT = 270;
        // property
        private bool _isCapturing = false;
        private bool _isDetectorProcessing = false;
        private VideoCapture _capture = null;
        private Image<Bgr, Byte> _resultImage;
        private Mat _captureFrame;
        private Mat _captureObservedFrame = new Mat();
        private int moneyInScreen = 0;
        private const int CAM_ID = 0;
        private String[] b = new String[] { "100_0", "200_0", "500_0", "1000_0", "2000_0" };
        private double FPS = 24;
        private long FPS_TICKS;
        private long _lastFetchTime = 0;

        private Point _clickPoint;
        private CamShiftTracking _trackingObjL = null;
        private CamShiftTracking _trackingObjR = null;
        Rectangle _drawRectL = Rectangle.Empty;
        Rectangle _drawRectR = Rectangle.Empty;
        private bool _isMouseDown = false;

        public Pen rectPenL = new Pen(Brushes.Red, 3);
        public Pen rectPenR = new Pen(Brushes.Blue, 3);
        private const bool USE_CAMERA = true;
        private readonly Size SIZE = new Size(0, 0);
        private const int SIGMAX = 3;

        private TrackStatus _trackStatus = TrackStatus.None;

        // property

        public Form1()
        {
            InitializeComponent();
            _timer.Interval = 1000 / SEND_PER_SEC;
            this.Closing += new System.ComponentModel.CancelEventHandler(Form1_Closing);
            _sourcePictureBox.Size = _resultPictureBox.Size = new Size(WIDTH, HEIGHT);

            // 用圖片當 observation
            //DrawMatches.DetectBillInScreen("resources/test1000.jpg", b, WIDTH, HEIGHT);
            moneyInScreen = DrawMatches.GetMoneyInScreen();
            Console.WriteLine(moneyInScreen.ToString());
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
            return !(!_isCapturing || _capture == null || _capture.Ptr == IntPtr.Zero || !_capture.IsOpened);
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
                this.Invoke((MethodInvoker)delegate
                {
                    if (!IsCapturing()) return;
                    //if (!_isDetectorProcessing) _captureObservedFrame = _captureFrame.Clone();
                    using (Mat _sourceFrame = _captureFrame.Clone())
                    using (Image<Bgr, byte> sourceImage = _sourceFrame.ToImage<Bgr, Byte>())
                    using (Image<Bgr, byte> blurImage = sourceImage.Clone())
                    {
                        CvInvoke.GaussianBlur(blurImage, blurImage, SIZE, SIGMAX);

                        if (_isMouseDown)
                        {
                            ShowDraggingRect(_drawRectL, rectPenL, _sourceFrame.Bitmap);
                            ShowDraggingRect(_drawRectR, rectPenR, _sourceFrame.Bitmap);
                        }
                        // runs on UI thread
                        _sourcePictureBox.Image = _sourceFrame.Bitmap;

                        if (_trackStatus != TrackStatus.None)
                            SetTrackObject(blurImage);


                        _sourcePictureBox.Refresh();
                        ///
                        ////////////////////////////////////////////////////////////////////////////////////
                        ///
                        ReleaseImage(ref _resultImage);
                        // TODO: Process image here

                        if (_trackingObjL != null || _trackingObjR != null)
                        {
                            _resultImage = sourceImage.Clone();
                        }

                        if (_trackingObjL != null)
                        {
                            UpdateTrackView(_leftBar, _trackingObjL.Tracking(blurImage), rectPenL, _resultImage.Bitmap);
                        }
                        if (_trackingObjR != null)
                        {
                            UpdateTrackView(_rightBar, _trackingObjR.Tracking(blurImage), rectPenR, _resultImage.Bitmap);
                        }

                        if (_resultImage == null)
                        {
                            _resultImage = sourceImage.Clone();
                        }

                        _resultPictureBox.Image = _resultImage.Bitmap;
                        _resultPictureBox.Refresh();
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
                _capture.Stop();//摄像头关闭
                _capture.ImageGrabbed -= ProcessFrame;
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

                _captureFrame = new Mat();
                _openCameraButton.Enabled = true;

                if (_capture != null)
                {
                    _capture.Start();
                    _isCapturing = true;
                }
            }
        }

        private void SetResultImage(Image<Bgr, byte> target)
        {
            _resultPictureBox.Image = null;
            ReleaseImage(ref _resultImage);
            _resultPictureBox.Image = _resultImage.Bitmap;
            _resultPictureBox.Refresh();
        }

        private void ProgressCapture(object sender, EventArgs e)
        {
            if (_isDetectorProcessing)
                return;
            if (!IsCapturing())
                return;
            this.Invoke((MethodInvoker)delegate
            {
                using (Mat _sourceFrame = _captureFrame.Clone())
                {
                    _isDetectorProcessing = true;
                    Mat _grayFrame = new Mat();
                    CvInvoke.CvtColor(_sourceFrame, _grayFrame, ColorConversion.Bgr2Gray);
                    _resultPictureBox.Image = DrawMatches.DetectBillInScreen(_grayFrame, ref b, WIDTH, HEIGHT).Bitmap;
                    _isDetectorProcessing = false;
                    Console.WriteLine("Currently Point: " + DrawMatches.GetMoneyInScreen().ToString());
                }
            });

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
