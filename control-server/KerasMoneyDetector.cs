using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;

namespace control_server
{
    class KerasMoneyDetector : IMoneyDetector
    {
        private readonly int WIDTH;
        private readonly int HEIGHT;
        private int _moneyInScreen = 0;
        private const string SERVER_URL = "http://127.0.0.1:4329/det";
        private readonly HttpClient _client = new HttpClient();

        public KerasMoneyDetector(int width, int height)
        {
            WIDTH = width;
            HEIGHT = height;
        }

        public Point[][] DetectBillInScreen(Mat frame)
        {
            byte[] bytes = null;
            using (var ms = new MemoryStream())
            {
                frame.Bitmap.Save(ms, ImageFormat.Jpeg);
                bytes = ms.ToArray();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, SERVER_URL);
                var content = new ByteArrayContent(bytes);
                content.Headers.Add("Content-Type", "image/jpeg");
                content.Headers.Add("Content-Length", bytes.Length.ToString());
                request.Content = content;
                var ret = _client.SendAsync(request).Result;
                var retStr = ret.Content.ReadAsStringAsync().Result;
                _moneyInScreen = int.Parse(retStr);
            }

            return null;
        }

        public void Dispose()
        {
        }

        public int GetMoneyInScreen() => _moneyInScreen;
    }
}
