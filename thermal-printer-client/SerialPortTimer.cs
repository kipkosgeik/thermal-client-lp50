using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;


namespace thermal_printer_client
{
    /// <summary>
    /// Assists to time read/writes while communicating with serial port.
    /// 
    /// Adopted from http://codesamplez.com/programming/serial-port-communication-c-sharp
    /// </summary>
    class SerialPortTimer
    {
        public const int DEFAULT_TIMEOUT = 1000;
        public Timer timer = new Timer();
        public bool timedout = false;

        /// <summary>
        /// Creates a new instance of SerialPortTimer with default timeout.
        /// </summary>
        public SerialPortTimer()
        {
            timedout = false;
            timer.AutoReset = false;
            timer.Enabled = false;
            timer.Interval = DEFAULT_TIMEOUT; 
            timer.Elapsed += new ElapsedEventHandler(OnTimeout);
        }

        /// <summary>
        /// Called by ElapsedEventHandler when counter runs down.
        /// </summary>
        /// <param name="source">Even source</param>
        /// <param name="e">Event args</param>
        private void OnTimeout(object source, ElapsedEventArgs e)
        {
            timedout = true;
            timer.Stop();
        }

        /// <summary>
        /// Resets timer and starts count-down to specified timeout
        /// </summary>
        /// <param name="timeout"></param>
        public void Start(double timeout)
        {
            timer.Interval = timeout;             //time to time out in milliseconds
            timer.Stop();
            timedout = false;
            timer.Start();
        }
    }
}
