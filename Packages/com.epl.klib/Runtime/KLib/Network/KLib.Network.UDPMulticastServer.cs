using UnityEngine;
using System.Collections;
using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KLib.Network
{
    public class UDPMulticastServer : MonoBehaviour
    {
        Thread _readThread;

        UdpClient _udp = null;
        string _name = "";
        int _port = -1;

        void Start()
        {
        }

        public bool IsReceiving { get { return _readThread.IsAlive; } }

        public void StartReceiving(string name, int port)
        {
            _name = name;
            _port = port;

            // create thread for reading UDP messages
            _readThread = new Thread(new ThreadStart(ReceiveData));
            _readThread.IsBackground = true;

            _readThread.Start();
        }

        public void StopReceiving()
        {
            Debug.Log("Stopping multicast thread");
            StopThread();
        }

        // Stop reading UDP messages
        private void StopThread()
        {
            Debug.Log("IsAlive = " + _readThread.IsAlive.ToString());
            _readThread.Abort();
            if (_udp != null)
            {
                _udp.Close();
                Debug.Log("Multicast closed");
            }

            //if (_readThread.IsAlive)
            //{
            //    _readThread.Abort();
            //    _udp.Close();
            //    Debug.Log("Multicast thread aborted");
            //}
        }

        private void OnDestroy()
        {
            //StopThread();
        }

        private void ReceiveData()
        {
            //        var addy = SRITCP.FindServerAddress();
            string addy = "";
            Debug.Log("Multicast listening on: " + addy);

            IPAddress localAddress;
            if (addy.Equals("localhost"))
            {
                localAddress = IPAddress.Loopback;
            }
            else
            {
                localAddress = IPAddress.Parse(addy);
            }

            var ipLocal = new IPEndPoint(localAddress, 10000);

            var address = IPAddress.Parse("234.5.6.7");
            var ipEndPoint = new IPEndPoint(address, 10000);

            _udp = new UdpClient();
            _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udp.Client.Bind(ipLocal);
            _udp.Client.ReceiveTimeout = 500;

            _udp.JoinMulticastGroup(address, localAddress);

            var anyIP = new IPEndPoint(IPAddress.Any, 0);


            while (true)
            {
                try
                {
                    // receive bytes
                    var bytes = _udp.Receive(ref anyIP);
                    var response = Encoding.Default.GetString(bytes);
                    Debug.Log("Multicast received: " + response);

                    if (response.Equals(_name))
                    {
                        bytes = Encoding.UTF8.GetBytes(_port.ToString());
                        _udp.Send(bytes, bytes.Length, anyIP);
                        //Debug.Log("response sent");
                    }
                }
                catch (Exception ex)
                {
                    //Debug.Log(ex.Message);
                }
            }
        }
    }
}