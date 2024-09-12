using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace KLib.Network
{
    /// <summary>
    /// Wrapper for System.Net.Sockets.TcpListener that simplifies read/write functionality.
    /// </summary>
    public class KTcpListener
    {
        private TcpListener _listener = null;
        private TcpClient _client = null;

        private NetworkStream _network = null;
        private BinaryReader _theReader;
        private BinaryWriter _theWriter;

        private bool _bigEndian = true;

        public bool StartListener(string address, int port)
        {
            return StartListener(address, port, true);
        }

        /// <summary>
        /// Start TCP server
        /// </summary>
        /// <param name="address">TCP server IP address</param>
        /// <param name="port">TCP server port</param>
        /// <param name="bigEndian">set byte order to big endian</param>
        /// <returns>Returns true if successful</returns>
        public bool StartListener(string address, int port, bool bigEndian)
        {
            _bigEndian = bigEndian;


            IPAddress ipAddress = (address.Equals("localhost")) ? IPAddress.Loopback : IPAddress.Parse(address);

            _listener = new TcpListener(ipAddress, port);
            _listener.Start();
            

            return true;
        }

        public void CloseListener()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;
            }
        }

        public bool Pending()
        {
            return _listener.Pending();
        }

        /// <summary>
        /// Accepts incoming connection, initializes stream
        /// </summary>
        public void AcceptTcpClient()
        {
            _client = _listener.AcceptTcpClient();
            _network = _client.GetStream();
            _theReader = new BinaryReader(_network);
            _theWriter = new BinaryWriter(_network);
        }

        public void CloseTcpClient()
        {
            _network.Dispose();
        }

        public void WriteStringToOutputStream(string s)
        {
            using (NetworkStream theStream = _client.GetStream())
            using (StreamWriter theWriter = new StreamWriter(theStream))
            {
                theWriter.WriteLine(s);
                theWriter.Flush();
            }
        }

        public void WriteStringAsByteArray(string s)
        {
            var byteArray = System.Text.Encoding.UTF8.GetBytes(s);
            int nbytes = byteArray.Length;

            if (_bigEndian)
            {
                var bytes = BitConverter.GetBytes(nbytes);
                Array.Reverse(bytes);
                nbytes = BitConverter.ToInt32(bytes, 0);
            }

            _theWriter.Write(nbytes);
            _theWriter.Write(byteArray);
            _theWriter.Flush();
        }

        public void WriteByteArray(byte[] byteArray)
        {
            int nbytes = byteArray.Length;

            if (_bigEndian)
            {
                var bytes = BitConverter.GetBytes(nbytes);
                Array.Reverse(bytes);
                nbytes = BitConverter.ToInt32(bytes, 0);
            }

            _theWriter.Write(nbytes);
            _theWriter.Write(byteArray);
            _theWriter.Flush();
        }

        public void WriteInt32ToOutputStream(int value)
        {
            _theWriter.Write(value);
            _theWriter.Flush();
        }

        public void WriteIntToOutputStream(int value)
        {
            using (NetworkStream theStream = _client.GetStream())
            using (StreamWriter theWriter = new StreamWriter(theStream))
            {
                theWriter.Write(value);
                theWriter.Flush();
            }
        }

        public string ReadStringFromInputStream()
        {
            string result = null;

            int nbytes = _theReader.ReadInt32();
            if (_bigEndian)
            {
                var bytes = BitConverter.GetBytes(nbytes);
                Array.Reverse(bytes);
                nbytes = BitConverter.ToInt32(bytes, 0);
            }

            var byteArray = _theReader.ReadBytes(nbytes);
            result = System.Text.Encoding.UTF8.GetString(byteArray, 0, byteArray.Length);

            return result;
        }

        public byte[] ReadByteArrayFromInputStream()
        {
            byte[] result = null;

            int nbytes = _theReader.ReadInt32();
            if (_bigEndian)
            {
                var bytes = BitConverter.GetBytes(nbytes);
                Array.Reverse(bytes);
                nbytes = BitConverter.ToInt32(bytes, 0);
            }

            result = _theReader.ReadBytes(nbytes);

            return result;
        }

        public int ReadInt32FromInputStream()
        {
            int result = _theReader.ReadInt32();
            return result;
        }

        public void SendAcknowledgement()
        {
            _theWriter.Write((int)1);
            _theWriter.Flush();
        }
        public void SendAcknowledgement(bool success)
        {
            _theWriter.Write(success ? (int)1 : (int)-1);
            _theWriter.Flush();
        }

    }
}