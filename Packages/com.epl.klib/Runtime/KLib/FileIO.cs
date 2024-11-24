using UnityEngine;
using System.Collections.Generic;

using System;
using System.IO;

using System.Reflection;
using System.Xml; 
using System.Xml.Serialization;

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ProtoBuf;

namespace KLib
{ 
    public static class FileIO
    {
        public static Stream _stream = null;

        public static void JSONSerialize<T>(T t, string path)
        {
            WriteTextFile(path, JSONSerializeToString(t));
        }

        public static T JSONDeserialize<T>(string path)
        {
            return JsonConvert.DeserializeObject<T>(ReadTextFile(path));
        }

        public static string JSONSerializeToString<T>(T t, Newtonsoft.Json.Formatting formatting)
        {
            return JsonConvert.SerializeObject(t, formatting, new StringEnumConverter { CamelCaseText = false });
        }

        public static string JSONSerializeToString<T>(T t)
        {
            return JsonConvert.SerializeObject(t, Newtonsoft.Json.Formatting.Indented, new StringEnumConverter { CamelCaseText = false });
        }

        public static T JSONDeserializeFromString<T>(string text)
        {
            return JsonConvert.DeserializeObject<T>(text);
        }

        public static string JSONStringAdd(string json, string name, string val)
        {
            if (json.Length == 0)
            {
                json = "{" + System.Environment.NewLine;
            }
            else
            {
                json = json.Substring(0, json.Length - 1) + ",";
            }

            json += "\"" + name + "\":" + val + System.Environment.NewLine + "}";
            return json;
        }

        public static void WriteTextFile(string path, string text)
        {
#if UNITY_EDITOR
            using (System.IO.Stream s = File.Create(path))
            {
                byte[] b = System.Text.Encoding.ASCII.GetBytes(text);
                s.Write(b, 0, b.Length);
            }
#else
            using (System.IO.Stream s = File.Create(path))
            {
                byte[] b = System.Text.Encoding.UTF8.GetBytes(text);
                s.Write(b, 0, b.Length);
            }
#endif
        }

        public static string ReadTextFile(string path)
        {
#if UNITY_EDITOR
            return File.ReadAllText(path);
#else
            return File.ReadAllText(path);
#endif
        }

        public static void AppendTextFile(string path, string text)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR
            using (System.IO.Stream s = File.Open(path, FileMode.Append))
            {
                byte[] b = System.Text.Encoding.ASCII.GetBytes(text);
                s.Write(b, 0, b.Length);
            }
#else
            using (System.IO.Stream s = File.OpenAppend(path))
            {
                byte[] b = System.Text.Encoding.UTF8.GetBytes(text);
                s.Write(b, 0, b.Length);
            }
#endif
        }

        public static void XmlSerialize<T>(T t, string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            using (System.IO.Stream s = File.Create(path)) {
                serializer.Serialize(s, t); 
            }
        }
        
        public static T XmlDeserialize<T>(string path)
		{
            T t;
			XmlSerializer serializer = new XmlSerializer(typeof(T));
#if !UNITY_METRO || UNITY_EDITOR
            using (System.IO.Stream s = File.OpenRead(path))
#else
            using (System.IO.Stream s = File.Open(path))
#endif
            {
                t = (T)serializer.Deserialize(s);
            }
			
			return t;
		}

        public static T XmlDeserializeFromTextAsset<T>(string assetPath)
        {
            TextAsset textAsset = (TextAsset) Resources.Load(assetPath);
            return XmlDeserializeFromString<T>(textAsset.text);
        }

        public static string XmlSerializeToString<T>(T t)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            XmlSerializer serializer = new XmlSerializer(typeof(T));

            using (var writer = new System.IO.StringWriter(sb))
            {
                serializer.Serialize(writer, t);
            }

            return sb.ToString();
        }

        public static T XmlDeserializeFromString<T>(string data)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            T t = default(T);

            using (System.IO.StringReader reader = new System.IO.StringReader(data))
            {
                t = (T)serializer.Deserialize(reader);
            }
            return t;
        }

        public static void SerializeToBinary<T>(T t, string path)
		{
			using (System.IO.Stream s = File.Create(path)) {
                Serializer.Serialize<T>(s, t);
			}
		}
		
		public static T DeserializeFromBinary<T>(string path)
		{
			T t;
#if !UNITY_METRO || UNITY_EDITOR
			using (System.IO.Stream s = File.OpenRead(path))
#else
            using (System.IO.Stream s = File.Open(path))
#endif
            {
                t = Serializer.Deserialize<T>(s);
			}

			return t;
		}
		
		public static string CombinePaths(params string[] paths)
		{
			string combinedPath = paths[0];
			for (int k=1; k<paths.Length; k++)
				combinedPath = System.IO.Path.Combine(combinedPath, paths[k]);
			
			return (combinedPath);
		}

        public static void CreateBinarySerialization(string path)
        {
            _stream = File.Create(path);
        }

        public static void OpenBinarySerialization(string path)
        {
#if !UNITY_METRO || UNITY_EDITOR
            _stream = File.OpenRead(path);
#else
            _stream = File.Open(path);
#endif
        }

        public static void SerializeToBinary<T>(T t)
        {
            Serializer.SerializeWithLengthPrefix(_stream, t, PrefixStyle.Base128);
            //Serializer.Serialize<T>(_stream, t);
        }

        public static T DeserializeFromBinary<T>()
        {
            return Serializer.DeserializeWithLengthPrefix<T>(_stream, PrefixStyle.Base128);
        }

        public static void CloseBinarySerialization()
        {
            if (_stream != null)
            {
                //_stream.Close();
                _stream.Dispose();
                _stream = null;
            }
        }

        public static void CopyUnique(string from, string to)
        {
            string folder = Path.GetDirectoryName(to);
            string filestem = Path.GetFileNameWithoutExtension(to);
            string ext = Path.GetExtension(to);

            string uniqueTo = to;
            int k = 2;
            while (File.Exists(uniqueTo))
            {
                uniqueTo = Path.Combine(folder, filestem + "_" + k.ToString() + ext);
                k++;
            }

            Copy(from, uniqueTo);
        }

        public static void Copy(string from, string to)
        {
#if !UNITY_METRO || UNITY_EDITOR
            File.Copy(from, to);
#else
            File.Copy(from, to);
#endif
        }

        public static void CreateFolder(string folder)
        {
            if (!Directory.Exists(folder))
            {
#if !UNITY_METRO || UNITY_EDITOR
                Directory.CreateDirectory(folder);
#else
                string localFolder = folder.Contains(DataFileLocations.DataRoot) ? folder.Substring(DataFileLocations.DataRoot.Length+1) : folder;
                if (!Directory.CreateDirectory(localFolder))
                    throw new ApplicationException("Could not create folder: " + localFolder);
#endif
            }
        }


        public static List<string> ParseWebFiles(string xmlString)
        {
            return ParseWebFolderData(xmlString, false);
        }

        public static List<string> ParseWebFolders(string xmlString)
        {
            return ParseWebFolderData(xmlString, true);
        }

        private static List<string> ParseWebFolderData(string xmlString, bool folders)
        {
            List<string> remoteFiles = new List<string>();

            if (!string.IsNullOrEmpty(xmlString))
            {
                using (System.IO.StringReader reader = new System.IO.StringReader(xmlString))
                using (XmlTextReader xml = new XmlTextReader(reader) { Namespaces = false })
                {
                    while (xml.Read())
                    {
                        if (xml.NodeType == XmlNodeType.Element)
                        {
                            if (xml.Name == "D:href")
                            {
                                xml.Read();
                                string url = xml.Value;
                                if (url[url.Length - 1] != '/' && !folders) remoteFiles.Add(url);
                                if (url[url.Length - 1] == '/' && folders) remoteFiles.Add(url);
                            }
                        }
                    }
                }
            }
            return remoteFiles;
        }

        public static DateTime GetDateFromPropfindResponse(string response, string property)
        {
            DateTime dt = new DateTime();

            using (System.IO.StringReader reader = new System.IO.StringReader(response))
            using (XmlTextReader xml = new XmlTextReader(reader) { Namespaces = false })
            {
                while (xml.Read())
                {
                    if (xml.NodeType == XmlNodeType.Element)
                    {
                        if (xml.Name == "D:" + property)
                        {
                            xml.Read();
                            dt = DateTime.Parse(xml.Value);
                        }
                    }
                }

            }

            return dt;
        }

        public static List<string> ParseWeb(string xmlString)
        {
            List<string> remoteFiles = new List<string>();

            if (!string.IsNullOrEmpty(xmlString))
            {
                using (System.IO.StringReader reader = new System.IO.StringReader(xmlString))
                using (XmlTextReader xml = new XmlTextReader(reader) { Namespaces = false })
                {
                    while (xml.Read())
                    {
                        if (xml.NodeType == XmlNodeType.Element)
                        {
                            if (xml.Name == "D:href")
                            {
                                xml.Read();
                                string url = xml.Value;
                                if (url[url.Length - 1] != '/') remoteFiles.Add(url);
                            }
                        }
                    }
                }
            }
            return remoteFiles;
        }

        public static string ReadString(System.IO.Stream s, int len)
        {
            byte[] buffer = new byte[len];
            int nread = s.Read(buffer, 0, len);
            return new string(System.Text.Encoding.UTF8.GetChars(buffer));
        }
        public static uint ReadUInt(System.IO.Stream s)
        {
            byte[] buffer = new byte[4];
            int nread = s.Read(buffer, 0, 4);
            return BitConverter.ToUInt32(buffer, 0);
        }
        public static ushort ReadUShort(System.IO.Stream s)
        {
            byte[] buffer = new byte[2];
            int nread = s.Read(buffer, 0, 2);
            return BitConverter.ToUInt16(buffer, 0);
        }
        public static double ReadDouble(System.IO.Stream s)
        {
            byte[] buffer = new byte[8];
            int nread = s.Read(buffer, 0, 8);
            return BitConverter.ToDouble(buffer, 0);
        }

        public static string ExtractJsonObjectString(string json, string name, string next)
        {
            string objIndicator = ",\"" + name + "\":";
            int objStart = json.IndexOf(objIndicator);
            if (objStart < 0)
                throw new Exception("Json object not found: " + name);

            objStart += objIndicator.Length;

            int objEnd = json.Length - 1;
            if (!string.IsNullOrEmpty(next))
            {
            }

            return json.Substring(objStart, objEnd - objStart);
        }

        public static T ExtractJsonObject<T>(string json, string name, string next)
        {
            string objString = ExtractJsonObjectString(json, name, next);
            return JsonConvert.DeserializeObject<T>(objString);
        }

        public static byte[] ToProtoBuf<T>(T obj)
        {
            byte[] pbuf;
            using (var ms = new System.IO.MemoryStream())
            {
                Serializer.Serialize<T>(ms, obj);
                pbuf = ms.ToArray();
            }
            return pbuf;
        }

        public static T FromProtoBuf<T>(byte[] pbuf)
        {
            T obj = default(T);
            using (var ms = new System.IO.MemoryStream(pbuf))
            {
                obj = Serializer.Deserialize<T>(ms);
            }

            return obj;
        }

#if UNITY_EDITOR
        public static void DumpF32(string fn, float[] data)
        {
            using (Stream s = File.Create(fn))
            using (BinaryWriter bw = new BinaryWriter(s))
            {
                byte[] b = new byte[data.Length * 4];
                Buffer.BlockCopy(data, 0, b, 0, b.Length);
                bw.Write(b);
            }
        }
#endif

    }
}