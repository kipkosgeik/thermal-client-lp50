using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace thermal_printer_client
{

    /// <summary>
    /// Simple application that sends commands to a DATECS LP-50 Thermal printer to print out a label from a saved form
    /// in the printers memory.
    /// 
    /// http://www.datecs.bg/en/products/53
    /// 
    /// It requires command line arg file_name which should be a CSV with format:
    /// <code>FORM_NAME,Variable_count,V00,V01,..,Vnn</code>
    /// <para></para>
    /// </summary>
    class ThermalPrinterBridge
    {
        private const int PORT_READ_TIMEOUT = 10000;
        private const int PORT_WRITE_TIMEOUT = 10000;
        private string TARGET_FORM = "L5";

        static void Main(string[] args)
        {
            try {
                //temporary
                //args = new string[] { "My file" };
                if (args.Length == 0) {
                    throw new Exception("Usage: Please include file name as an argument to this program.");
                } else {
                    Console.WriteLine(string.Format("Reading file {0}...", args[0]));
                    ThermalPrinterBridge bridge = new ThermalPrinterBridge(); 
                    List<string> variables = bridge.ReadValues(args[0]);
                    const string LP_50_PORT = "COM3"; //fetch port from config
                    List<String> ports = bridge.getAllPorts();
                    Console.WriteLine("Enumerating available ports...");
                    bool targetPortFound = false;
                    foreach (string port in ports) {
                        if (port.ToLower().Equals(LP_50_PORT.ToLower())) { //select the configured port
                            targetPortFound = true;
                            SerialPort serialPort = null;
                            try {
                                serialPort = new SerialPort(port);
                                if (serialPort.IsOpen == false) { //if not open, open the port
                                    serialPort.Open();
                                    serialPort.ReadTimeout = PORT_READ_TIMEOUT;
                                    serialPort.WriteTimeout = PORT_WRITE_TIMEOUT;
                                    Console.WriteLine("Communicating through " + port);
                                    bridge.ConverseWithPrinter(serialPort, variables[0], variables.GetRange(2, variables.Count() - 1));
                                } else {
                                    throw new Exception(string.Format("Serial port {0} is already in use by another program.", LP_50_PORT));
                                }
                            } finally {
                                if (serialPort != null) { //always close port unconditionally
                                    Console.WriteLine("Closing port..");
                                    serialPort.Close();
                                }
                            }

                            //done with the LP-50 port communication, ignore any other ports
                            break;
                        }
                    }

                    if (targetPortFound) {
                        Console.WriteLine("\n\nENTER to exit");
                        Console.ReadLine(); //wait for user to press any button -- Temporary
                    } else {
                        throw new Exception(String.Format("Printer not found on port {0}. Please connect printer on correct USB port.", LP_50_PORT));
                    }
                }
            } catch (Exception e) {
                Console.Error.WriteLine(string.Format("{0}\n\nENTER to exit", e.Message));
                Console.ReadLine(); //wait for user to press any button
            }

        }

        /// <summary>
        /// Reads the CSV file, expected to be a single line of data in format:
        /// FORM_NAME,Variable_count,V00,V01,..,Vnn
        /// </summary>
        /// <param name="filePath">Absolute path of target file which should be ready to be openned for read operation</param>
        /// <returns>Arraylist with order: FORM_NAME,Variable_count,V00,V01,..,Vnn</returns>
        /// <exception cref="Exception">IO errors and other malfunctions will be wrapped under generic exception</exception>
        public List<string> ReadValues(string filePath)
        {
            StreamReader streamReader = null;
            try {
                List<string> values = new List<string>();

                FileStream f = new FileStream(filePath, FileMode.Open);
                streamReader = new StreamReader(f);
                CsvConfiguration config = new CsvConfiguration();
                config.Delimiter = ",";
                CsvReader csvReader = new CsvReader(streamReader, config);
                while (csvReader.Read())
                    values.Add(csvReader.GetRecord<string>());

                return values;
            } catch (Exception e) {
                throw new Exception("Error reading file. " + e.Message);
            } finally {
                if (streamReader != null) {
                    streamReader.Close();
                }
            }
        }

        /// <summary>
        /// Fetch available COM ports.
        /// </summary>
        /// <returns>List of available Serial ports</returns>
        public List<string> getAllPorts()
        {
            List<String> allPorts = new List<String>();
            foreach (String portName in System.IO.Ports.SerialPort.GetPortNames()) {
                allPorts.Add(portName);
            }
            return allPorts;
        }

        /// <summary>
        /// Communicate with Datecs LP-50 Thermal printer using the printer commands detailed in the documentation at
        /// 
        /// http://www.datecs.bg/en/products/53
        /// </summary>
        /// <param name="port">Openned port that is ready for read/write ops</param>
        /// <param name="targetForm">The form to activate, it must exist in the printer or nothing happens</param>
        /// <param name="variables">the values to substitute in the form variables, they SHOULD be exact NUMBER as in form for best results. The variables SHOULD ALSO be IN THE ORDER they are declared in the forms e.g from V00,V01...Vnn</param>
        public void ConverseWithPrinter(SerialPort port, string targetForm, List<String> variables)
        {
            try {

                //Read installed forms in printer memory and confirm if required form is available
                //Us UF command to list forms
                port.WriteLine("UF");
                List<String> forms = ReadPortResponse(port, PORT_READ_TIMEOUT, true);
                //forms are listed starting with the form count as first item e.g
                //002
                //L0
                //L1
                //means there are 2 forms named L0,L1
                bool targetFormAvailable = false;
                foreach (string form in forms) {
                    Console.WriteLine(form);
                    if (form.ToLower().Equals(targetForm.ToLower())) { //Form names are case insensitive
                        targetFormAvailable = true;
                    }
                }

                if (targetFormAvailable) {
                    //Load & activate target form, user FR command
                    port.WriteLine(string.Format("FR\"{0}\"", targetForm));
                    //read all form prompts and respond till no more prompts, then print label
                    //It is very important that the variable be equal number with the prompts, otherwise
                    //printer will remain in prompt mode and won't take other commands with expected behavior
                    port.WriteLine("?"); //prompt printer to start variable & counter prompts.

                    //Write out variables to substitute each prompt.
                    foreach (string variable in variables) {
                        port.WriteLine(variable);
                    }
                    //flush out any bytes
                    port.BaseStream.Flush();

                    //Print one label out of the above variables. Uses athe P command.
                    port.WriteLine("P1,1");
                    Console.WriteLine("Completed");
                } else {
                    Console.Error.WriteLine(string.Format("Form {0} not found in printer memory, please try again. Contact system admin if problem persists.", TARGET_FORM));
                }
            } finally {
                port.BaseStream.Flush();
                port.Close();
            }

        }

        ///Read printer output as unique lines. The CR character is stripped out at the end.
        ///Open serial port to read from
        ///Timeout in milliseconds after which read is abandoned
        ///multiple - whether only a single-line response is expected. multiple lines will take longer
        ///as we have to wait for the printer to write every line and can't establish the exact rows to wait for
        ///This forces a wait, max the timeout period to wait for the next line
        ///
        ///Returns an array list of the individual lines read out
        private List<string> ReadPortResponse(SerialPort port, int timeout, bool multiple)
        {
            Console.WriteLine("Reading printer response...");
            this.FlushPortBuffers(port);
            SerialPortTimer timer = new SerialPortTimer();
            timer.Start(timeout);

            while (port.BytesToRead == 0 && !timer.timedout) ; //wait for inbound buffer until timeout

            List<String> response = new List<String>();

            if (port.BytesToRead > 0) {
                try {
                    int c;
                    StringBuilder b = new StringBuilder();
                    //read line by line
                    while ((c = port.ReadByte()) != '\n' || port.BytesToRead > 0) {
                        if (c == '\r') { //a line of input has been read
                            response.Add(b.ToString());
                            b.Clear();
                            //if multiple lines are expected, we wait for a while for next bytes to be written to buffer if any
                            if (multiple) {
                                timer.Start(PORT_READ_TIMEOUT);
                                while (port.BytesToRead == 0 && !timer.timedout) ;
                            }
                        } else if (c == '\n') {
                            continue; //Continue to new line
                        } else {
                            b.Append((char)c);
                        }
                    }
                } catch (TimeoutException e) {
                    Console.Error.WriteLine(string.Format("Read timed out. {0}", e.Message));
                }
            } else {
                Console.WriteLine("Read timed out");
            }

            return response;
        }

        /// <summary>
        /// Discards anything in the IO buffers of the serial port.
        /// </summary>
        /// <param name="port">Target port to flush buffers. It should be open.</param>
        private void FlushPortBuffers(SerialPort port)
        {
            port.DiscardInBuffer();
            port.DiscardOutBuffer();
        }
    }
}
