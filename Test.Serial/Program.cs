/*
  This file is part of SerialPortLib (https://github.com/genielabs/serialport-lib-dotnet)
 
  Copyright (2012-2018) G-Labs (https://github.com/genielabs)

  Licensed under the Apache License, Version 2.0 (the "License");
  you may not use this file except in compliance with the License.
  You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

  Unless required by applicable law or agreed to in writing, software
  distributed under the License is distributed on an "AS IS" BASIS,
  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
  See the License for the specific language governing permissions and
  limitations under the License.
*/

using System;
using System.Threading;

using SerialPortLib;
using NLog;


namespace Test.Serial
{
    class MainClass
    {
        //rivate static string defaultPort = "/dev/ttyUSB0";
        //private static string defaultPort = "COM3 USB-SERIAL CH340";
        private static string defaultPort = "COM3";
        private static SerialPortInput serialPort;

        public static void Main(string[] args)
        {
            // NOTE: To disable debug output uncomment the following two lines
            //LogManager.Configuration.LoggingRules.RemoveAt(0);
            //LogManager.Configuration.Reload();

            serialPort = new SerialPortInput();
            serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
            serialPort.MessageReceived += SerialPort_MessageReceived;

            string [] portNames = System.IO.Ports.SerialPort.GetPortNames();
            foreach (string name in portNames)
            {
                Console.WriteLine("-->>>>>>  {0}", name);
            }
            Console.WriteLine("------------------------------");


            while (true)
            {
                //N101=1,呼叫0,1000
                var t1 = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x03, 0x04, 0x00, 0x01, 0x1E, 0x0A, 0xDF, 0xF4 };
                //N102=3,呼叫1,1000
                var t2 = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x03, 0x04, 0x01, 0x02, 0x1E, 0x20, 0xF7, 0x11 };
                //N103=2,呼叫2,1000
                var t3 = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x03, 0x04, 0x02, 0x03, 0x1E, 0x30, 0x09, 0x28 };
                //N104=0,呼叫3,1000
                var t4 = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x03, 0x04, 0x03, 0x04, 0x1E, 0x38, 0x13, 0x37 };
                //N107=0,同步时钟,1000
                var msgSyncTime = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x08, 0xE6, 0x07, 0x02, 0x07, 0x07, 0x0C, 0x08, 0x10, 0xD9, 0x83 };
                //N108=0,8同步时间,1000
                var msgNo = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x08, 0xE6, 0x07, 0x07, 0x12, 0x02, 0x15, 0x2E, 0x33, 0x36, 0xFE };
                //N109=0,返回设备列表,1000
                var msgReturnDevices = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05, 0x00, 0x06, 0x00, 0x07, 0x00, 0x08, 0x00, 0x09, 0x00, 0x0A, 0x00, 0x0B, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x0E, 0x00, 0x0F, 0x49, 0x96 };
                //N110=0,同步时间,1000
                var msgSync = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x81, 0x00, 0x1e, 0x00, 0x00, 0x42, 0x00, 0x01, 0x41, 0x00, 0x02, 0x43, 0x00, 0x03, 0x45, 0x00, 0x04, 0x3f, 0x00, 0x05, 0x3e, 0x00, 0x06, 0x41, 0x00, 0x07, 0x41, 0x00, 0x08, 0x3d, 0x00, 0x09, 0x40, 0x02, 0x0c };

                var result = ComAssist.checkData(msgSync);
                Console.WriteLine("------> check result <-------   :  {0}", result);
                result = ComAssist.checkData(msgReturnDevices);
                Console.WriteLine("------> check result <-------   :  {0}", result);
                result = ComAssist.checkData(msgNo);
                Console.WriteLine("------> check result <-------   :  {0}", result);

                Console.WriteLine("\nPlease enter serial to open (eg. \"COM7\" or \"/dev/ttyUSB0\" without double quotes),");
                Console.WriteLine("or enter \"QUIT\" to exit.\n");
                Console.Write("Port [{0}]: ", defaultPort);
                string port = Console.ReadLine();
                if (String.IsNullOrWhiteSpace(port))
                    port = defaultPort;
                else
                    defaultPort = port;

                // exit if the user enters "quit"
                if (port.Trim().ToLower().Equals("quit"))
                    break;
            
                //serialPort.SetPort(port, 115200);
                serialPort.SetPort(port, 9600);
                serialPort.Connect();

                Console.WriteLine("Waiting for serial port connection on {0}.", port);
                while (!serialPort.IsConnected)
                {
                    Console.Write(". ");
                    Thread.Sleep(100);
                }
                // This is a test message (ZWave protocol message for getting the nodes stored in the Controller)
                //var testMessage = new byte[] { 0x01, 0x03, 0x00, 0x02, 0xFE };
                var testMessage = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05, 0x00, 0x06, 0x00, 0x07, 0x00, 0x08, 0x00, 0x09, 0x00, 0x0A, 0x00, 0x0B, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x0E, 0x00, 0x0F, 0x49, 0x96 };
                // Try sending some data if connected
                if (serialPort.IsConnected)
                {
                    Console.WriteLine("\nConnected! Sending test message 5 times.");
                    for (int s = 0; s < 10; s++)
                    {
                        Thread.Sleep(1000);
                        Console.WriteLine("\nSEND [{0}]", (s + 1));
                        serialPort.SendMessage(testMessage);
                    }
                }
                Console.WriteLine("\nTest sequence completed, now disconnecting.");

                Thread.Sleep(2000);
                serialPort.Disconnect();
            }
        }

        static void SerialPort_MessageReceived(object sender, MessageReceivedEventArgs args)
        {
            Console.WriteLine("Received message: {0}", BitConverter.ToString(args.Data));
            // On every message received we send an ACK message back to the device
            serialPort.SendMessage(new byte[] { 0x06 });
        }

        static void SerialPort_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            Console.WriteLine("Serial port connection status = {0}", args.Connected);
        }
    }
}
