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

        private bool _shouldHandleMouseDown = false;
        private Point _clickPoint;
        private CamShiftTracking _trackingObjL = null;
        private CamShiftTracking _trackingObjR = null;
        Rectangle _drawRectL = Rectangle.Empty;
        Rectangle _drawRectR = Rectangle.Empty;
        private bool _isMouseDown = false;

        public Pen rectPenL = new Pen(Brushes.Red, 3);
        public Pen rectPenR = new Pen(Brushes.Blue, 3);




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

        private void ProcessFrame(object sender, EventArgs e)
        {
            if (IsCapturing())
            {
                if (!IsCapturing())
                    return;

                //取得網路攝影機的影像
                _capture.Retrieve(_captureFrame);
                CvInvoke.Resize(_captureFrame, _captureFrame, new Size(WIDTH, HEIGHT), 0, 0, Emgu.CV.CvEnum.Inter.Cubic);
                this.Invoke((MethodInvoker)delegate
                {
                    if (!IsCapturing()) return;
                    //if (!_isDetectorProcessing) _captureObservedFrame = _captureFrame.Clone();
                    using (Mat _sourceFrame = _captureFrame.Clone())
                    using (Image<Bgr, byte> sourceImage = _sourceFrame.ToImage<Bgr, Byte>())
                    {
                        // runs on UI thread
                        _sourcePictureBox.Image = _sourceFrame.Bitmap;

                        if (_drawRectL != Rectangle.Empty)
                        {
                            var ctx = Graphics.FromImage(_sourceFrame.Bitmap);
                            ctx.DrawRectangle(rectPenL, _drawRectL);
                            int pointRectCnt = (_drawRectL.Left + _drawRectL.Width / 2);
                            int pointBoxCnt = _sourcePictureBox.Width / 2 ;
                            _leftBar.Value = (int)Math.Round((double)(pointRectCnt - pointBoxCnt) / (_sourcePictureBox.Width / 2) * 100);

                        }

                        if (_drawRectR != Rectangle.Empty)
                        {
                            var ctx = Graphics.FromImage(_sourceFrame.Bitmap);
                            ctx.DrawRectangle(rectPenR, _drawRectR);
                            int pointRectCnt = (_drawRectR.Left + _drawRectR.Width / 2);
                            int pointBoxCnt = _sourcePictureBox.Width / 2;
                            _rightBar.Value = (int)Math.Round((double)(pointRectCnt - pointBoxCnt) / (_sourcePictureBox.Width / 2) * 100);
                        }

                        _sourcePictureBox.Refresh();
                        ///
                        ////////////////////////////////////////////////////////////////////////////////////
                        ///
                        ReleaseImage(ref _resultImage);
                        // TODO: Process image here
                        //need modify
                        //_resultImage = sourceImage.Clone();

                        _shouldHandleMouseDown = true;

                        if (_trackingObjL != null)
                        {
                            lock (_trackingObjL)
                                _drawRectL = _trackingObjL.Tracking(sourceImage);
                        }
                        if (_trackingObjR != null)
                        {
                            lock (_trackingObjR)
                                _drawRectR = _trackingObjR.Tracking(sourceImage);
                        }

                        if (_resultImage == null)
                        {
                            _resultImage = sourceImage.Clone();
                        }

                        //
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
                _shouldHandleMouseDown = false;
            }
            else
            {
                _openCameraButton.Enabled = false;
                _openCameraButton.Text = "停止攝影機";
                //_capture = new VideoCapture(CAM_ID);
                string _videoPath = LoadVideoFile();
                _capture = new VideoCapture(_videoPath);

                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoExposure, 0);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, WIDTH);//_sourcePictureBox.Width
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, HEIGHT);// _sourcePictureBox.Height
                _capture.ImageGrabbed += ProcessFrame;
                //_capture.ImageGrabbed += ProgressCapture;

                _captureFrame = new Mat();
                _openCameraButton.Enabled = true;
                _shouldHandleMouseDown = true;

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
            if (!_shouldHandleMouseDown) return;
            _clickPoint = _sourcePictureBox.PointToClient(Cursor.Position);
            _isMouseDown = true;

        }
        private void SetTrackObject(bool right)
        {
            if (!_shouldHandleMouseDown) return;

            if (_trackingObjL != null && !right) { _trackingObjL.Dispose(); _trackingObjL = null; }
            if (_trackingObjR != null && right) { _trackingObjR.Dispose(); _trackingObjR = null; }
            using (var tmpMat = _captureFrame.Clone())
            using (var tmpImg = tmpMat.ToImage<Bgr, byte>())
            {
                if (_drawRectL != Rectangle.Empty && _trackingObjL == null)
                {
                    _trackingObjL = new CamShiftTracking(tmpImg, _drawRectL);
                }
                if (_drawRectR != Rectangle.Empty && _trackingObjR == null)
                {
                    _trackingObjR = new CamShiftTracking(tmpImg, _drawRectR);
                }
            }
        }

        private void _sourcePictureBox_MouseUp(object sender, MouseEventArgs e)
        {

            if (!_shouldHandleMouseDown || !_isMouseDown) return;
            bool right = false;
            if (e.Button == MouseButtons.Right) right = true;

            SetTrackObject(right);
            _isMouseDown = false;
            if (e.Button == MouseButtons.Left) _drawRectL = Rectangle.Empty;
            if (e.Button == MouseButtons.Right) _drawRectR = Rectangle.Empty;
        }

        private void _sourcePictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_shouldHandleMouseDown || !_isMouseDown) return;

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
            dialog.Filter = "Video File|*.mkv;*.mp4;*.avi;*.flv|MP4 (*.mp4)|*.mp4|MKV (*.mkv)|*.mkv|FLV (*.flv)|*.flv";

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
