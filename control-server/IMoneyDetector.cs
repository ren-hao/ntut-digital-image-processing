using Emgu.CV;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace control_server
{
    interface IMoneyDetector : IDisposable
    {
        int GetMoneyInScreen();

        Point[][] DetectBillInScreen(Mat frame);
    }
}
