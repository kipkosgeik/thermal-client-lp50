using System.Timers;
public class SerialTimer {
    public const int DEFAULT_TIMEOUT = 1000;

     System.Timers.Timer timer = new System.Timers.Timer();
     public bool timedout = false;


    public SerialTimer(){
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
        Console.WriteLine("Timed out");
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