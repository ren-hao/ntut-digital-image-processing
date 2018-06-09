using System;
using System.ServiceModel.Dispatcher;

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
