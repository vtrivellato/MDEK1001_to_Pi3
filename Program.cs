using Microsoft.Data.Sqlite;
using System;
using System.IO.Ports;

namespace consoleApplication1
{
    public class RTLS_Logger
    {
        static void Main()
        {
            try
            {
                Console.WriteLine("[{0}] Starting interface...", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                var serialPort1 = new SerialPort();

                if (serialPort1.IsOpen)
                {
                    serialPort1.Close();
                }
                
                var portaCOM = "/dev/ttyACM0";
                
                Console.WriteLine("[{0}] Configuring serial port " + portaCOM + ".", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                serialPort1.PortName = portaCOM;
                serialPort1.BaudRate = 115200;
                serialPort1.DataBits = 8;

                serialPort1.Parity = Parity.None;

                serialPort1.StopBits = StopBits.One;
                serialPort1.ReadTimeout = 1000;
                serialPort1.WriteTimeout = 1000;

                serialPort1.Handshake = Handshake.None;
                serialPort1.RtsEnable = true;
                serialPort1.DtrEnable = true;

                serialPort1.Open();

                if (serialPort1.IsOpen)
                {
                    Console.WriteLine("[{0}] Serial port configured.", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                    serialPort1.DataReceived += new SerialDataReceivedEventHandler(serialPort1_DataReceived);

                    // Send \r twice to enter UART shell mode
                    serialPort1.Write("\r");
                    serialPort1.Write("\r");

                    Thread.Sleep(1000);

                    serialPort1.Write("lec\r");
                    
                    Console.WriteLine("[{0}] Listener in UART shell mode.", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                    Console.WriteLine("[{0}] Logging data...", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                    Console.Read();
                    
                    serialPort1.Write("quit");

                    serialPort1.Close();

                    Console.WriteLine("[{0}] Ending interface execution...", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
                }
                else
                {
                    Console.WriteLine("[{0}] Could not establish connection to serial port. Restart interface.", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + (ex.InnerException != null ? Environment.NewLine + ex.InnerException : string.Empty));
            }

            Console.WriteLine("[{0}] Interface execution ended", DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"));
        }

        static SerialPort serialPortSender = new SerialPort();
        static SqliteConnection connection = new SqliteConnection("Data Source=test.db");

        static string inData = string.Empty;
        static DateTime dataAtual = new DateTime();

        static string sqlCommand = 
        @"
            INSERT INTO pos_register (tag, posX, posY, posZ, data) 
            VALUES (@tagName, @posX, @posY, @posZ, @dataAtual)
        ";

        static void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            serialPortSender = (SerialPort)sender;
			
            inData = serialPortSender.ReadExisting();
            dataAtual = DateTime.Now;

            var inDataLines = inData.Split("\r\n");                       

            foreach (var data in inDataLines)
            {
                if (!string.IsNullOrWhiteSpace(data))
                {
                    var parts = data.Split(",");

                    if (parts.Length >= 8)
                    {
                        connection.Open();

                        var command = connection.CreateCommand();

                        command.CommandText = sqlCommand;

                        command.Parameters.AddWithValue("@tagName", parts[2]);
                        command.Parameters.AddWithValue("@posX", parts[3]);
                        command.Parameters.AddWithValue("@posY", parts[4]);
                        command.Parameters.AddWithValue("@posZ", parts[5]);
                        command.Parameters.AddWithValue("@dataAtual", dataAtual);

                        command.ExecuteNonQuery();
                    
                        connection.Close();
                    }
                }
            }
        }            
    }
}
