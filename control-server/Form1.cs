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
        private const int CAM_ID = 0;
        private DrawMatches _momeyMatches = new DrawMatches(WIDTH, HEIGHT);
        private Queue<int> _moneyQueue = new Queue<int>();
        private const int MAX_QUEUE_SIZE = 10;
        private int _realMoney = 0;
        // property

        public Form1()
        {
            InitializeComponent();
            _timer.Interval = 1000 / SEND_PER_SEC;
            this.Closing += new System.ComponentModel.CancelEventHandler(Form1_Closing);
            _sourcePictureBox.Size = _resultPictureBox.Size = new Size(WIDTH, HEIGHT);

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

                        _sourcePictureBox.Refresh();
                        ///
                        ////////////////////////////////////////////////////////////////////////////////////
                        ///
                        ReleaseImage(ref _resultImage);
                        // TODO: Process image here
                        //need modify
                        //_resultImage = sourceImage.Clone();


                        if (_resultImage == null)
                        {
                            _resultImage = sourceImage.Clone();
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
                _capture = new VideoCapture(CAM_ID);

                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoExposure, 0);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, WIDTH);//_sourcePictureBox.Width
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, HEIGHT);// _sourcePictureBox.Height
                _capture.ImageGrabbed += ProcessFrame;
                _capture.ImageGrabbed += ProgressCapture;

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
            this.Invoke((MethodInvoker)delegate
            {
                using (Mat _sourceFrame = _captureFrame.Clone())
                {
                    _isDetectorProcessing = true;
                    using (Mat _grayFrame = new Mat())
                    {
                        CvInvoke.CvtColor(_sourceFrame, _grayFrame, ColorConversion.Bgr2Gray);
                        var tmpImage = _momeyMatches.DetectBillInScreen(_grayFrame);
                        _resultPictureBox.Image = tmpImage.Bitmap;
                        _resultPictureBox.Refresh();
                        ReleaseImage(ref tmpImage);
                        _isDetectorProcessing = false;
                    }

                    //Console.WriteLine("Currently Point: " + _momeyMatches.GetMoneyInScreen().ToString());
                    if (_moneyQueue.Count == MAX_QUEUE_SIZE)
                        _moneyQueue.Dequeue();
                    _moneyQueue.Enqueue(_momeyMatches.GetMoneyInScreen());

                    int change = GetMostItem() - _realMoney;
                    //Console.WriteLine(GetMostItem());     
                    if (change != 0) SendDonateMoney(change);
                    _realMoney += change;
                }
            });

        }
    }
}
