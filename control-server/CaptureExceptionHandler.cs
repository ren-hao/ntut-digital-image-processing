using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace control_server
{
    class CaptureExceptionHandler : ExceptionHandler
    {
        public override bool HandleException(Exception exception)
        {
            Console.Error.WriteLine(exception.StackTrace);
            return true;
        }
    }
}
