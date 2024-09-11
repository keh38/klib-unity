using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KLib.Network
{
    public static class NetworkUtils
    {
        public static string ServerAddress;

        public static string FindServerAddress()
        {
            System.Diagnostics.Process p = null;
            string output = string.Empty;
            string address = string.Empty;

            try
            {
                p = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("arp", "-a")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                });

                output = p.StandardOutput.ReadToEnd();
                p.Close();

                foreach (var line in output.Split(new char[] { '\n', '\r' }))
                {
                    // Parse out all the MAC / IP Address combinations
                    if (!string.IsNullOrEmpty(line))
                    {
                        var pieces = (from piece in line.Split(new char[] { ' ', '\t' })
                                      where !string.IsNullOrEmpty(piece)
                                      select piece).ToArray();

                        if (line.StartsWith("Interface:") && pieces[1].StartsWith("169.254"))
                        {
                            address = pieces[1];
                            ServerAddress = address;
                            return address;
                        }
                        if (line.StartsWith("Interface:") && pieces[1].StartsWith("11.12"))
                        {
                            address = pieces[1];
                            ServerAddress = address;
                            return address;
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                throw new Exception("IPInfo: Error Retrieving 'arp -a' Results", ex);
            }
            finally
            {
                if (p != null)
                {
                    p.Close();
                }
            }
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(address))
        {
            address = "localhost";
            ServerAddress = address;
        }
#endif

            return address;
        }
    }
}