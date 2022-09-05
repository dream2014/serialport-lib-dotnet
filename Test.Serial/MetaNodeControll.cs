
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Serialization;
using System.Text;
using SerialPortLib;
using NLog;

namespace MetaLib
{
    class MetaNodeController
    {
        #region Private fields

        private static SerialPortInput serialPort = null;
        private static int defaultBaudRate = 9600;
        private static int tryMaxTime = 10;

        private string portName = "COM3";
        private const int commandDelayMin = 100;
        private int commandDelay = commandDelayMin;

        //private bool busyReceiving = false;

        private object readLock = new object();

        private ControllerStatus controllerStatus = ControllerStatus.Disconnected;

        private List<MetaNode> nodeList = new List<MetaNode>();

        private string configFolder;

        #endregion
        
        #region Lifecycle

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaLib.MetaNodeControll"/> class.
        /// </summary>
        public MetaNodeController()
        {
            //string codeBase = GetType().Assembly.CodeBase;
            //UriBuilder uri = new UriBuilder(codeBase);
            //string path = Uri.UnescapeDataString(uri.Path);
            //configFolder = Path.GetDirectoryName(path);
            //serialPort = new SerialPortInput();
            //serialPort.MessageReceived += SerialPort_MessageReceived;
            //serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
            //// Setup Queue Manager Task
            //queuedMessages = new List<MetaMessage>();
            //queueManager = new Thread(QueueManagerTask);
            //queueManager.Start();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MetaLib.MetaNodeControll"/> class.
        /// </summary>https://stackoverflow.com/questions/52921966/unable-to-resolve-ilogger-from-microsoft-extensions-logging
        /// <param name="portName">The serial port name.</param>
        public MetaNodeController(string portName) : this()
        {
            PortName = portName;
        }

        public void Dispose()
        {
            // Disconnect the serial port
            Disconnect();
            // Update the nodes configuration file
            SaveNodesConfig();
        }

        #endregion

        #region Public members

        /// <summary>
        /// Connect this instance.
        /// </summary>
        public void Connect()
        {
            LoadNodesConfig();
            new Thread(() => { serialPort.Connect(); }).Start();
        }

        /// <summary>
        /// Disconnect this instance.
        /// </summary>
        public void Disconnect()
        {
            serialPort.Disconnect();
        }

        /// <summary>
        /// Gets or sets the name of the serial port.
        /// </summary>
        /// <value>The name of the port.</value>
        public string PortName
        {
            get { return portName; }
            set
            {
                portName = value;
                serialPort.SetPort(value);
            }
        }

        /// <summary>
        /// Gets or sets the amount of command delay.
        /// </summary>
        /// <value>The length of the delay in ms.</value>
        public int CommandDelay
        {
            get { return commandDelay; }
            set
            {
                commandDelay = (value > commandDelayMin ? value : commandDelayMin);
            }
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>The status.</value>
        public ControllerStatus Status
        {
            get { return controllerStatus; }
        }

        #region

        /// <summary>
        /// Processes a ZWave message.
        /// </summary>
        /// <param name="zm">Zm.</param>
        private void ProcessMessage(MetaMessage zm)
        {
            if (zm.Header == FrameHeader.SOF)
            {
                if (MetaMessage.VerifyChecksum(zm.RawData))
                {
                    var msgNo = (MetaCommand)zm.RawData[MetaMessage.msgNoPos];
                    Console.WriteLine("processMessage {0}", msgNo);
                    switch(msgNo)
                    {
                        case MetaCommand.RefreshNodesInfo:
                            RefreshNodesInfoACK(zm.RawData);
                            break;
                        case MetaCommand.SetDateTime:
                            SetDateTimeACK(zm.RawData);
                            break;
                        case MetaCommand.GetNodesInfo:
                            GetNodesInfoACK(zm.RawData);
                            break;
                        case MetaCommand.GetNodesState:
                            GetNodesStateACK(zm.RawData);
                            break;
                        case MetaCommand.GetGatewayState:
                            GetGatewayStateACK(zm.RawData);
                            break;
                        case MetaCommand.RemoveNodes:
                            GetGatewayStateACK(zm.RawData);
                            break;
                        case MetaCommand.CallGatewayFromNode:
                            CallGatewayFromNodeACK(zm.RawData);
                            break;
                        case MetaCommand.ReportNodesBatteryLevel:
                            ReportNodesBatteryLevelACK(zm.RawData);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Utility.logger.Warn("Bad message checksum");
                }
            }
            else
            {
                Utility.logger.Warn("Unhandled message type: {0}", BitConverter.ToString(zm.RawData));
            }
        }

        #endregion

        #region Controller commands

        /// <summary>
        /// Sends the message without waiting other pending requests to complete.
        /// </summary>
        /// <returns>True if sending succesfull, False otherwise.</returns>
        /// <param name="message">Message.</param>
        public void SendMessage(MetaMessage message)
        {
            #region Debug
            Utility.logger.Trace("[[[ BEGIN REQUEST ]]]");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            #endregion
            //Utility.logger.Trace("Sending Message (Node={0}, CallbackId={1}, Function={2}, CommandClass={3})", pendingRequest.NodeId, pendingRequest.CallbackId.ToString("X2"), pendingRequest.Function, pendingRequest.CommandClass);
            serialPort.SendMessage(message.RawData);

            #region Debug
            stopWatch.Stop();
            Utility.logger.Trace("[[[ END REQUEST ]]] took {0} ms", stopWatch.ElapsedMilliseconds);
            #endregion
        }

        #endregion
        #region ZWave Discovery / Node Querying

        /// <summary>
        /// Gets the node.
        /// </summary>
        /// <returns>The node.</returns>
        /// <param name="nodeId">Node identifier.</param>
        public MetaNode GetNode(byte nodeId)
        {
            return nodeList.Find(zn => zn.Id == nodeId);
        }

        /// <summary>
        /// Gets the nodes.
        /// </summary>
        /// <value>The nodes.</value>
        public List<MetaNode> Nodes
        {
            get { return nodeList; }
        }

        #endregion
        
        #region MetaNode event handlers
        #endregion
        
        #region Serial Port events and data parsing

        /// <summary>
        /// Parses the data buffer coming from the serial port.
        /// </summary>
        /// <param name="message">raw bytes data.</param>
        private void ParseSerialData(byte[] message)
        {
            try
            {
                if(message.Length >= MetaMessage.msgMinLength)
                {
                    ProcessMessage(new MetaMessage(message));
                }
                else
                {
                    Utility.logger.Error("message length less than minLength({0})", MetaMessage.msgMinLength);
                }
            }
            catch (Exception e)
            {
                Utility.logger.Error(e);
            }
        }

        private void SerialPort_MessageReceived(object sender, SerialPortLib.MessageReceivedEventArgs args)
        {
            lock (readLock)
            {
                //busyReceiving = true;
                ParseSerialData(args.Data);
                //busyReceiving = false;
            }
        }

        #endregion

        #region Node management and configuration persiste
    
        private void RemoveNode(byte nodeId)
        {
            var node = GetNode(nodeId);
            nodeList.RemoveAll(zn => zn.Id == nodeId);
        }

        private void LoadNodesConfig()
        {
            string configPath = Path.Combine(configFolder, "MetaNodes.xml");
            if (File.Exists(configPath))
            {
                try
                {
                    var serializer = new XmlSerializer(nodeList.GetType());
                    var reader = new StreamReader(configPath);
                    nodeList = (List<MetaNode>)serializer.Deserialize(reader);
                    reader.Close();
                    foreach (var node in nodeList)
                    {
                        //node.NodeUpdated += ZWave_NodeUpdated;
                        node.SetController(this);
                    }
                }
                catch (Exception e)
                {
                    Utility.logger.Error(e);
                }
            }
        }

        private void SaveNodesConfig()
        {
            string configPath = Path.Combine(configFolder, "MetaNodes.xml");
            try
            {
                var settings = new System.Xml.XmlWriterSettings();
                settings.Indent = true;
                var serializer = new System.Xml.Serialization.XmlSerializer(nodeList.GetType());
                var writer = System.Xml.XmlWriter.Create(configPath, settings);
                serializer.Serialize(writer, nodeList);
                writer.Close();
            }
            catch (Exception e)
            {
                Utility.logger.Error(e);
            }
        }

        #endregion

        #region 普通公共函数

        //获取到我们的网关设备
        public string GetMyDevice()
        {
            string strDevice = "COM3";
            string[] portNames = System.IO.Ports.SerialPort.GetPortNames();
            foreach (string name in portNames)
            {
                Console.WriteLine("-->>>>>>  {0}", name);
            }
            Console.WriteLine("------------------------------");
            return strDevice;
        }

        public string[] GetPortNames()
        {
            string[] portNames = System.IO.Ports.SerialPort.GetPortNames();
            return portNames;
        }

        public bool Connect(string portName)
        {
            string codeBase = GetType().Assembly.CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            configFolder = Path.GetDirectoryName(path);

            bool connectResult = true;
            serialPort = new SerialPortInput();
            serialPort.ConnectionStatusChanged += SerialPort_ConnectionStatusChanged;
            serialPort.MessageReceived += SerialPort_MessageReceived;

            serialPort.SetPort(portName, defaultBaudRate);
            serialPort.Connect();

            Console.WriteLine("Waiting for serial port connection on {0}.", portName);
            //尝试次数
            int testTime = 0;
            while (!serialPort.IsConnected)
            {
                Console.Write("test connect {0}, time{1}", portName, testTime);
                Thread.Sleep(10);
                testTime++;
                if (testTime > tryMaxTime)
                {
                    connectResult = false;
                }
            }

            return connectResult;
        }

        public bool SendMessage(byte[] message)
        {
            if (serialPort != null)
            {
                return serialPort.SendMessage(message);
            }
            else
            {
                Console.WriteLine("SendMessage fail. serial port can't find");
            }

            return false;
        }
        private void SerialPort_ConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            Console.WriteLine("Serial port connection status = {0}", args.Connected);
        }
        #endregion


        #region pc->网关，pc主动发送到网关的指令

        //允许组网	0x00	允许当前网关设备添加节点		
        //返回添加完成	0x00	当添加完成时返回命令
        public void RefreshNodesInfo()
        {
            var data = new byte[1];
            var msg = MetaMessage.BuildSendDataRequest((byte)MetaCommand.RefreshNodesInfo, data);

            SendMessage(msg);
        }

        //设置时间	0x01	配置当前设备的时间		
        //配置时间状态	0x01	返回从机设置时间的状态
        public void SetDateTime(int year, int month, int day, int hour, int minute, int second, int week)
        {
            var data = new byte[1];
            var msg = MetaMessage.BuildSendDataRequest((byte)MetaCommand.SetDateTime, data);

            SendMessage(msg);
        }

        //获取节点信息	0x02	获取当前已配对节点信息		
        //返回节点信息	0x02	返回当前网关内已配对的节点信息
        public void GetNodesInfo(List<Int16> deviceIDs)
        {
            ////N109=0,返回设备列表,1000
            //var msgGetDevicesInfo = new byte[] { 0xAF, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x20, 0x00, 0x00, 0x00, 0x01, 0x00, 0x02, 0x00, 0x03, 0x00, 0x04, 0x00, 0x05, 0x00, 0x06, 0x00, 0x07, 0x00, 0x08, 0x00, 0x09, 0x00, 0x0A, 0x00, 0x0B, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x0E, 0x00, 0x0F, 0x49, 0x96 };
            //SendMessage(msgGetDevicesInfo);

            var data = new byte[deviceIDs.Count * 2];
            var tmpByte = new byte[2];
            int index = 0;
            foreach (Int16 deviceID in deviceIDs)
            {
                tmpByte = BitConverter.GetBytes(deviceID);
                data[index * 2 + 0] = tmpByte[0];
                data[index * 2 + 1] = tmpByte[1];
                index++;
            }

            var msg = MetaMessage.BuildSendDataRequest((byte)MetaCommand.GetNodesInfo, data);

            SendMessage(msg);
        }

        //呼叫节点	0x03	呼叫当前已注册的节点		
        //返回状态	0x03	返回呼叫节点的状态
        public void GetNodesState(List<Int16> deviceIDs)
        {
            var data = new byte[deviceIDs.Count * 2];
            var tmpByte = new byte[2];
            int index = 0;
            foreach (Int16 deviceID in deviceIDs)
            {
                tmpByte = BitConverter.GetBytes(deviceID);
                data[index * 2 + 0] = tmpByte[0];
                data[index * 2 + 1] = tmpByte[1];
                index++;
            }

            var msg = MetaMessage.BuildSendDataRequest((byte)MetaCommand.GetNodesState, data);

            SendMessage(msg);
        }

        //获取当前网关状态	0x04	获取当前网关的状态		
        //返回状态	0x04	返回当前节点状态
        public void GetGatewayState()
        {
            var data = new byte[1];
            var msg = MetaMessage.BuildSendDataRequest((byte)MetaCommand.GetGatewayState, data);

            SendMessage(msg);
        }

        //删除手表设备	0x05	删除当前已配对的节点		返回状态	0x05	返回当前删除节点状态
        public void RemoveNode(List<Int16> deviceIDs)
        {
            var data = new byte[deviceIDs.Count * 2];
            var tmpByte = new byte[2];
            int index = 0;
            foreach (Int16 deviceID in deviceIDs)
            {
                tmpByte = BitConverter.GetBytes(deviceID);
                data[index * 2 + 0] = tmpByte[0];
                data[index * 2 + 1] = tmpByte[1];
                index++;
            }

            var msg = MetaMessage.BuildSendDataRequest((byte)MetaCommand.RemoveNodes, data);

            SendMessage(msg);
        }

        #endregion


        #region 网关->PC，PC被动接收到的消息（包括网关上报，PC指令的回复）

        //允许组网	0x00	允许当前网关设备添加节点		
        //返回添加完成	0x00	当添加完成时返回命令
        public void RefreshNodesInfoACK(byte[] message)
        {
            Console.WriteLine("RefreshNodesInfoACK");
        }

        //设置时间	0x01	配置当前设备的时间		
        //配置时间状态	0x01	返回从机设置时间的状态
        public void SetDateTimeACK(byte[] message)
        {
            Console.WriteLine("SetDateTimeACK");
        }

        //获取节点信息	0x02	获取当前已配对节点信息		
        //返回节点信息	0x02	返回当前网关内已配对的节点信息
        public void GetNodesInfoACK(byte[] message)
        {
            var dataLength = BitConverter.ToInt16(message, MetaMessage.dataLengthHigh);
            //check
            if(dataLength%2 != 0)
            {
                Console.WriteLine("GetNodesInfoACK find data length %3 != 0");
                return;
            }

            Console.WriteLine("GetNodesInfoACK find node num: {0}", dataLength/3);
            List<Int16> deviceIDs = new List<Int16>();
            for(int i=0;i<dataLength; i+=2)
            {
                var nodeId = BitConverter.ToInt16(message, MetaMessage.dataStartPos + i*2);
                Console.WriteLine("GetNodesInfoACK find node : {0}", nodeId);
                deviceIDs.Add(nodeId);
            }

            //todo:
        }

        //呼叫节点	0x03	呼叫当前已注册的节点		
        //返回状态	0x03	返回呼叫节点的状态
        public void GetNodesStateACK(byte[] message)
        {
            byte state = message[MetaMessage.dataStartPos];

            //todo:
        }

        //获取当前网关状态	0x04	获取当前网关的状态		
        //返回状态	0x04	返回当前节点状态
        public void GetGatewayStateACK(byte[] message)
        {
            Console.WriteLine("GetGatewayStateACK");
        }

        //删除手表设备	0x05	删除当前已配对的节点		返回状态	0x05	返回当前删除节点状态
        public void RemoveNodesACK(byte[] message)
        {
            byte state = message[MetaMessage.dataStartPos];

            //todo:
        }

        //节点呼叫网关指令	0x80	手表节点主动呼叫PC中心
        public void CallGatewayFromNodeACK(byte[] message)
        {
            Console.WriteLine("GetGatewayStateACK");
        }

        //返回节点电量	0x81	网关主动上报当前最新获取到的手表节点电量
        public void ReportNodesBatteryLevelACK(byte[] message)
        {
            var dataLength = BitConverter.ToInt16(message, MetaMessage.dataLengthHigh);
            //check
            if (dataLength % 3 != 0)
            {
                Console.WriteLine("ReportNodesBatteryLevelACK find data length %3 != 0");
                return;
            } 

            Console.WriteLine("ReportNodesBatteryLevelACK find node num: {0}", dataLength / 2);
            List<Int16> deviceIDs = new List<Int16>();
            for (int i = 0; i < dataLength; i += 3)
            {
                var nodeId = BitConverter.ToInt16(message, MetaMessage.dataStartPos + i);
                deviceIDs.Add(nodeId);
                byte batteryLevel = message[MetaMessage.dataStartPos + i * 3 + 2];
                Console.WriteLine("ReportNodesBatteryLevelACK find node : {0}, ReportNodesBatteryLevelACK:{1}", nodeId, batteryLevel);
            }

            //todo:
        }
        #endregion

        #endregion
    }

    //private static void OnSerialPort_MessageReceived(object sender, SerialPortLib.MessageReceivedEventArgs args)
    //{
    //    Console.WriteLine("Received message: {0}", BitConverter.ToString(args.Data));
    //    // On every message received we send an ACK message back to the device
    //    serialPort.SendMessage(new byte[] { 0x06 });
    //}
}
