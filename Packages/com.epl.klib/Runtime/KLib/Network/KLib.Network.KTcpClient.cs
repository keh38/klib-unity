using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace KLib.Network
{
    public class KTcpClient
    {
        private TcpClient _socket = null;

        public int SendTimeout { get; set; }
        public int ReceiveTimeout { get; set; }
        public string LastError { get { return _lastError; } }

        private string _lastError;

        private NetworkStream _theStream;
        private BinaryReader _theReader;
        private BinaryWriter _theWriter;

        public void Connect(string hostName, int port)
        {
            _socket = new TcpClient(hostName, port);
        }

        public void ConnectAsync(string hostName, int port)
        {
            _socket = new TcpClient();
            var result = _socket.BeginConnect(hostName, port, null, null);

            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1));

            if (!success)
            {
                throw new Exception("failed to connect");
            }

            _socket.EndConnect(result);
        }

        public void Close()
        {
            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
        }

        public void ConnectTCPServer(string hostName, int port)
        {
            _socket = new TcpClient(hostName, port);
            _theStream = _socket.GetStream();
            _theReader = new BinaryReader(_theStream);
            _theWriter = new BinaryWriter(_theStream);
        }

        public void CloseTCPServer()
        {
            _theStream.Dispose();

            if (_socket != null)
            {
                _socket.Close();
                _socket = null;
            }
        }

        public int WriteBinary(string s)
        {
            var byteArray = System.Text.Encoding.UTF8.GetBytes(s);
            int nbytes = byteArray.Length;

            _theWriter.Write(nbytes);
            _theWriter.Write(byteArray);
            _theWriter.Flush();

            return _theReader.ReadInt32();
        }

        public int WriteBinary(byte[] data)
        {
            int nbytes = data.Length;

            _theWriter.Write(nbytes);
            _theWriter.Write(data);
            _theWriter.Flush();

            return _theReader.ReadInt32();
        }

        public string WriteStringAndBytes(string s, string data)
        {
            string result = null;

            using (NetworkStream theStream = _socket.GetStream())
            using (BinaryReader theReader = new BinaryReader(theStream))
            using (BinaryWriter theWriter = new BinaryWriter(theStream))
            using (StreamWriter textWriter = new StreamWriter(theStream))
            {
                textWriter.WriteLine(s);
                textWriter.Flush();

                var byteArray = System.Text.Encoding.UTF8.GetBytes(data);
                int nbytes = byteArray.Length;

                var bytes = BitConverter.GetBytes(nbytes);
                Array.Reverse(bytes);
                nbytes = BitConverter.ToInt32(bytes, 0);

                theWriter.Write(nbytes);
                theWriter.Write(byteArray);
                theWriter.Flush();

                result = theReader.ReadString();
            }

            return result;
        }

        public string WriteStringAndBytes(string s, byte[] data)
        {
            string result = null;

            using (NetworkStream theStream = _socket.GetStream())
            using (BinaryReader theReader = new BinaryReader(theStream))
            using (BinaryWriter theWriter = new BinaryWriter(theStream))
            using (StreamWriter textWriter = new StreamWriter(theStream))
            {
                textWriter.WriteLine(s);
                textWriter.Flush();

                int nbytes = data.Length;

                var bytes = BitConverter.GetBytes(nbytes);
                Array.Reverse(bytes);
                nbytes = BitConverter.ToInt32(bytes, 0);

                theWriter.Write(nbytes);
                theWriter.Write(data);
                theWriter.Flush();

                result = theReader.ReadString();
            }

            return result;
        }

        public string WriteStringToOutputStream(string s)
        {
            string result = "";

            using (NetworkStream theStream = _socket.GetStream())
            using (StreamWriter theWriter = new StreamWriter(theStream))
            using (BinaryReader theReader = new BinaryReader(theStream))
            {
                theWriter.WriteLine(s);
                theWriter.Flush();
                result = theReader.ReadString();
            }

            return result;
        }

        public string ReadStringFromInputStream()
        {
            string result = null;

            using (NetworkStream theStream = _socket.GetStream())
            using (BinaryReader theReader = new BinaryReader(theStream))
            {
                result = theReader.ReadString();
            }

            return result;
        }


#if NETFX_CORE || WINDOWS_PHONE
        private StreamSocket _socket = null;
        DataWriter _writer;

        private async Task EnsureSocket(string hostName, int port)
        {
            try
            {
                var host = new HostName(hostName);
                _socket = new StreamSocket();
                await _socket.ConnectAsync(host, port.ToString(), SocketProtectionLevel.SslAllowNullEncryption);
            }
            catch (Exception ex)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(ex.HResult) == SocketErrorStatus.Unknown)
                {
                    // TODO abort any retry attempts on Unity side
                    throw;
                }
            }
        }

        private async Task WriteToOutputStreamAsync(byte[] bytes)
        {

            if (_socket == null) return;
            _writer = new DataWriter(_socket.OutputStream);
            _writer.WriteBytes(bytes);

            var debugString = UTF8Encoding.UTF8.GetString(bytes, 0, bytes.Length);

            try
            {
                await _writer.StoreAsync();
                await _socket.OutputStream.FlushAsync();

                _writer.DetachStream();
                _writer.Dispose();
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    // TODO abort any retry attempts on Unity side
                    throw;
                }
            }
        }
#endif

        public Stream GetStream()
        {
#if NETFX_CORE || WINDOWS_PHONE
            if (_socket == null) return null;
            return _socket.InputStream.AsStreamForRead();
#else
            throw new NotImplementedException();
#endif
        }

        public Stream GetOutputStream()
        {
#if NETFX_CORE || WINDOWS_PHONE
            if (_socket == null) return null;
            return _socket.OutputStream.AsStreamForWrite();
#else
            throw new NotImplementedException();
#endif
        }

        public void WriteToOutputStream(byte[] bytes)
        {
#if NETFX_CORE || WINDOWS_PHONE
            var thread = WriteToOutputStreamAsync(bytes);
            thread.Wait();
#else
            throw new NotImplementedException();
#endif
        }

        public byte ReadByteFromInputStream()
        {
            throw new NotImplementedException();
        }

        public int ReadIntFromInputStream()
        {
            int result = -1;

            using (NetworkStream theStream = _socket.GetStream())
            using (BinaryReader theReader = new BinaryReader(theStream))
            {
                result = theReader.ReadInt32();
            }

            return result;
        }


    }
}