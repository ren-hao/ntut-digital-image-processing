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
        private VideoCapture _capture = null;
        private Image<Bgr, Byte> _resultImage;
        private Mat _captureFrame;
        private int moneyInScreen = 0;
        private const int CAM_ID = 0;
        private String[] b = new String[] { "100_0", "200_0", "500_0", "1000_0", "2000_0" };
        // property

        public Form1()
        {
            InitializeComponent();
            _timer.Interval = 1000 / SEND_PER_SEC;
            _captureTimer.Interval = 5000;
            this.Closing += new System.ComponentModel.CancelEventHandler(Form1_Closing);
            _sourcePictureBox.Size = _resultPictureBox.Size = new Size(WIDTH, HEIGHT);

            //long matchTime;
            //for(int i = 0; i < 5;i++)
            //{
            //    String bill = "resources/" + b[i] + ".jpg";
            //    using (Mat modelImage = CvInvoke.Imread(bill, ImreadModes.Grayscale))
            //    using (Mat observedImage = CvInvoke.Imread("resources/test100.jpg", ImreadModes.Grayscale))
            //    {
            //        CvInvoke.Resize(observedImage, observedImage, new Size(WIDTH, HEIGHT));
            //        CvInvoke.Resize(modelImage, modelImage, new Size(WIDTH, HEIGHT));
            //        Mat result = DrawMatches.Draw(modelImage, observedImage, out matchTime);
            //        ImageViewer.Show(result, String.Format("Matched in {0} milliseconds", matchTime));
            //        Point[] ps = DrawMatches.GetPs();
            //        if (ps != null)
            //        {
            //            //Console.WriteLine(b[i] + ps[0].ToString() + ps[1].ToString() + ps[2].ToString() + ps[3].ToString());
            //            moneyInScreen += Convert.ToInt32(b[i].Split('_')[0]);
            //        }
            //    }
            //}
            //Console.WriteLine(moneyInScreen.ToString());

            // 用圖片當observation
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
                        _resultImage = sourceImage.Clone();


                        if (_resultImage == null)
                        {
                            _resultImage = sourceImage.Clone();
                        }

                        /*
                        _resultPictureBox.Image = _resultImage.Bitmap;
                        _resultPictureBox.Refresh();
                        */
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
                _captureTimer.Enabled = false;
            }
            else
            {
                _openCameraButton.Enabled = false;
                _openCameraButton.Text = "停止攝影機";
                _captureTimer.Enabled = true;
                _capture = new VideoCapture(CAM_ID);

                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.AutoExposure, 0);
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameWidth, WIDTH);//_sourcePictureBox.Width
                _capture.SetCaptureProperty(Emgu.CV.CvEnum.CapProp.FrameHeight, HEIGHT);// _sourcePictureBox.Height
                _capture.ImageGrabbed += ProcessFrame;
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

        private void _captureTimer_Tick(object sender, EventArgs e)
        {
            if (IsCapturing())
            {
                Mat _grayFrame = new Mat();
                Mat _sourceFrame = _captureFrame.Clone();

                CvInvoke.CvtColor(_sourceFrame, _grayFrame, ColorConversion.Bgr2Gray);
                //DrawMatches.DetectBillInScreen(_grayFrame, ref b, WIDTH, HEIGHT);
                //_resultPictureBox.Image = _resultImage.Bitmap;
                _resultPictureBox.Image = DrawMatches.DetectBillInScreen(_grayFrame, ref b, WIDTH, HEIGHT).Bitmap;
                _resultPictureBox.Refresh();

            }
            else
            {

            }
        }
    }
}
