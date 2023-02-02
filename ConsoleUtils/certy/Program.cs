using Pastel;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace certy
{
    internal class Program
    {
        static System.Drawing.Color GoodColor = System.Drawing.Color.LimeGreen;
        static System.Drawing.Color WarnColor = System.Drawing.Color.LightGoldenrodYellow;
        static System.Drawing.Color BadColor = System.Drawing.Color.OrangeRed;
        static System.Drawing.Color WarnColor2 = System.Drawing.Color.Yellow;
        static System.Drawing.Color BadColor2 = System.Drawing.Color.Firebrick;
        static string HeaderColor = ColorTheme.OffsetColorHighlight;
        public enum SANType
        {
            otherName, rfc822Name, dNSName, x400Address, directoryName, ediPartyName, uniformResourceIdentifier, IPAddress, registeredID
        }

        internal class SAN
        {
            public SANType SanType { get; set; }
            public string Value { get; set; }

            public SAN()
            {

            }
            public SAN(SANType SanType, string Value)
            {
                this.SanType = SanType;
                this.Value = Value;
            }
        }

        static CmdParser cmd;

        static void Main(string[] args)
        {
            cmd = new CmdParser(args)
            {
                { "help", "", CmdCommandTypes.FLAG, "Show this help." },
                { "host", "u", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Host" },
                { "port", "p", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.INT, 443 }
                    }, "Timeout" },
                { "no-proxy", "P", CmdCommandTypes.FLAG, new CmdParameters() {
                        { CmdParameterTypes.BOOL, false }
                    }, "Disable proxy" },
                { "http-mode", "H", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Overwrite Proxy" },
            };

            cmd.DefaultParameter = "host";
            cmd.Parse();


            string host = cmd["host"].String;
            int port = (int)cmd["port"].Int;
            int timeout = 2000;

            if (cmd.HasFlag("help") || host == null)
                ShowHelp();

            ShowTCPCertificate(host, port, timeout);
            /*
            TcpClient client = new TcpClient(host, port);

            using (SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
            {
                sslStream.AuthenticateAsClient(host);
                // This is where you read and send data
            }
            client.Close();
            */
            if (System.Diagnostics.Debugger.IsAttached)
                Console.ReadLine();
        }

        static void ShowHelp()
        {
            Console.WriteLine($"certy, {ConsoleHelper.GetVersionString()}");
            Console.WriteLine($"Usage: {AppDomain.CurrentDomain.FriendlyName} [Options] {{[--host|-h] [--port|-p]}}");
            Console.WriteLine($"Options:");
            foreach (CmdOption c in cmd.OrderBy(x => x.Name))
            {
                string l = $"  --{c.Name}".Pastel("9CDCFE") + (!string.IsNullOrEmpty(c.ShortName) ? $", {("-" + c.ShortName).Pastel("9CDCFE")}" : "") + (c.Parameters.Count > 0 && c.CmdType != CmdCommandTypes.FLAG ? " <" + string.Join(", ", c.Parameters.Select(x => x.Type.ToString().ToLower().Pastel("569CD6")).ToArray()) + ">" : "") + ": " + c.Description;
                Console.WriteLine(l);
            }
            //WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
            Exit(0);
        }

        static void Exit(int exitCode)
        {
            string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached || parrentProcess.ToLower().Contains("explorer")) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }
        static void ShowTCPCertificate(string host, int port, int timeout = 2000)
        {
            TcpClient client = new TcpClient();
            try
            {
                if (client.ConnectAsync(host, port).Wait(timeout)) // 2000ms timeout
                {
                    using (SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null))
                    {
                        sslStream.AuthenticateAsClient(host);
                        // do nothing with the data
                    }
                    client.Close();
                }
                else
                {
                    ConsoleHelper.WriteError($"Timeout connecting to {host}:{port.ToString()}");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex);
            }
            finally
            {
                client.Close();
            }
        }


        static async Task ShowHTTPCertificate(string EndPoint)
        {
            string proxy = GetProxyForUrlStatic(EndPoint);

            if (proxy != null)
                Console.Error.WriteLine($"Using proxy: {proxy}");


            HttpClientHandler handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback = ValidateServerCertificate;
            HttpClient client = new HttpClient(handler);
            try
            {
                HttpResponseMessage response = await client.GetAsync(EndPoint);
            }
            catch (HttpRequestException ex)
            {
                ConsoleHelper.WriteError(ex);
            }
            handler.Dispose();
            client.Dispose();
        }

        public static string GetProxyForUrlStatic(string url)
        {
            WebProxy proxy = (WebProxy)WebProxy.GetDefaultProxy();
            Uri resource = new Uri(url);
            // Display the proxy's properties.
            //DisplayProxyProperties(proxy);

            // See what proxy is used for the resource.
            Uri resourceProxy = proxy.GetProxy(resource);

            // Test to see whether a proxy was selected.
            if (resourceProxy == resource)
            {
                return null;
            }
            else
            {
                return resourceProxy.ToString();
            }
        }

        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null || chain == null)
                return false;

            X509Certificate2 X5092 = new X509Certificate2(certificate);


            var ecdsa = X5092.GetECDsaPublicKey();
            var rsa = X5092.GetRSAPublicKey();
            var dsa = X5092.GetDSAPublicKey();

            string keyAlgo = "";
            int bits = 0;
            DateTime notBefore = X5092.NotBefore;
            DateTime notAfter = X5092.NotAfter;

            string notBeforeString = "";
            string notAfterString = "";


            if (ecdsa != null)
            {
                keyAlgo = "ECDSA";
                bits = ecdsa.KeySize;
            }
            else if (rsa != null)
            {
                keyAlgo = rsa.KeyExchangeAlgorithm;
                bits = rsa.KeySize;
            }
            else if (dsa != null)
            {
                keyAlgo = dsa.KeyExchangeAlgorithm;
                bits = dsa.KeySize;
            }

            int pos = 3;

            int maxDescLength = Console.WindowWidth - 8;


            Console.WriteLine($"Subject:".Pastel(HeaderColor));
            ConsoleHelper.WriteSplittedText(X5092.SubjectName.Name, maxDescLength, "", 3, ColorTheme.OffsetColor);
            Console.WriteLine();
            Console.WriteLine($"Issuer:".Pastel(HeaderColor));
            ConsoleHelper.WriteSplittedText(X5092.Issuer, maxDescLength, "", 3, ColorTheme.OffsetColor);
            Console.WriteLine();
            Console.WriteLine($"Public key:".Pastel(HeaderColor));

            string keyAlgoString = "";
            string bitsString = "";

            if (keyAlgo == "RSA")
            {
                keyAlgoString = keyAlgo.Pastel(WarnColor);
                if (bits < 2048)
                    bitsString = bits.ToString().Pastel(BadColor);
                else if (bits < 3072)
                    bitsString = bits.ToString().Pastel(WarnColor2);
                else
                    bitsString = bits.ToString().Pastel(GoodColor);
            }
            else if (keyAlgo == "ECDSA")
            {
                bitsString = bits.ToString().Pastel(GoodColor);
                keyAlgoString = keyAlgo.Pastel(GoodColor);
            }
            else
            {
                bitsString = bits.ToString().Pastel(BadColor2);
                keyAlgoString = keyAlgo.Pastel(BadColor);
            }

            Console.WriteLine($"   {keyAlgoString}, {bitsString} bits");


            Console.WriteLine($"Serial number:".Pastel(HeaderColor));
            Console.WriteLine($"   {X5092.SerialNumber}");

            Console.WriteLine($"Thumbprint:".Pastel(HeaderColor));
            Console.WriteLine($"   {X5092.Thumbprint}");
            Console.WriteLine($"Signature Algorhitm:".Pastel(HeaderColor));
            Console.WriteLine($"   {X5092.SignatureAlgorithm.FriendlyName}");

            Console.WriteLine($"Not Before:".Pastel(HeaderColor));


            if (notBefore > DateTime.Now)
                Console.WriteLine($"   {notBefore.ToShortDateString().Pastel(BadColor)} {notBefore.ToShortTimeString().Pastel(BadColor2)}");
            else
                Console.WriteLine($"   {notBefore.ToShortDateString().Pastel(ColorTheme.Default1)} {notBefore.ToShortTimeString().Pastel(ColorTheme.Default2)}");

            Console.WriteLine($"Not after:".Pastel(HeaderColor));
            if (notAfter < DateTime.Now)
                Console.WriteLine($"   {notAfter.ToShortDateString().Pastel(BadColor)} {notAfter.ToShortTimeString().Pastel(BadColor2)}");
            else if (notAfter < DateTime.Now.AddDays(30))
                Console.WriteLine($"   {notAfter.ToShortDateString().Pastel(WarnColor)} {notAfter.ToShortTimeString().Pastel(WarnColor2)}");
            else
                Console.WriteLine($"   {notAfter.ToShortDateString().Pastel(ColorTheme.Default1)} {notAfter.ToShortTimeString().Pastel(ColorTheme.Default2)}");

            /*
            Console.WriteLine($"Not Before:                 {X5092.NotBefore.ToLongDateString()} {X5092.NotBefore.ToLongTimeString()}");
            Console.WriteLine($"Not After:                  {X5092.NotAfter.ToLongDateString()} {X5092.NotAfter.ToLongTimeString()}");
            */
            var SAN = X5092.Extensions["2.5.29.17"];        //SAN

            //File.WriteAllBytes("2.5.29.17.dmp", SAN.RawData);

            Console.WriteLine($"{SAN.Oid.FriendlyName}:".Pastel(HeaderColor));


            foreach (var san in ParseSubjectAltName(SAN.RawData))
            {
                if (san.SanType == SANType.dNSName )
                {
                    if(InetHelper.IsFqdn(san.Value))
                        Console.WriteLine($"   DNS: {san.Value.Pastel(GoodColor)}");
                    else
                        Console.WriteLine($"   DNS: {san.Value.Pastel(WarnColor)}");
                }
                else if (san.SanType == SANType.IPAddress)
                {
                    Console.WriteLine($"   IP:  {san.Value.Pastel(BadColor)}");
                }
            }

            // extended information
            //Console.WriteLine($"Version:                    {X5092.Version}");
            /*
                foreach (var ext in X5092.Extensions)
            {
                // System.Security.Cryptography.X509Certificates.X509KeyUsageExtension
                // System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension

                Console.WriteLine($"{ext.Oid.FriendlyName} ({ext.Oid.Value})");

                if (ext.GetType() == typeof(System.Security.Cryptography.X509Certificates.X509KeyUsageExtension))
                {
                    Console.WriteLine($"{GetExtensionContentString(ext)}");
                }
                else if (ext.GetType() == typeof(System.Security.Cryptography.X509Certificates.X509EnhancedKeyUsageExtension))
                {
                    Console.WriteLine($"{GetExtensionContentString(ext)}");
                }
                else if (ext.GetType() == typeof(System.Security.Cryptography.X509Certificates.X509BasicConstraintsExtension))
                {
                    Console.WriteLine($"{GetExtensionContentString(ext)}");
                }
                else if (ext.GetType() == typeof(System.Security.Cryptography.X509Certificates.X509SubjectKeyIdentifierExtension))
                {
                    Console.WriteLine($"{GetExtensionContentString(ext)}");
                }
                else if (ext.Oid.Value == "2.5.29.17") // Alternativer Antragstellername
                {
                    Console.WriteLine($"{GetExtensionContentString(ext)}");
                    
                    ;
                    SimpleHexDump(ext.RawData);
                    File.WriteAllBytes("2.5.29.17.dmp", ext.RawData);
                    
                }
                else
                {
                    Console.WriteLine($"{GetExtensionContentString(ext)}"); ;
                }

                *

                //DumpProperties(ext, "   ");
                ;
                //Console.WriteLine(ext.)
            }

            /*
            Console.WriteLine("--- DUMP ---");
            DumpProperties(X5092);
            Console.WriteLine("--- DUMP ---");
            */

            ;
            return true;
        }

        static void GetExtensionContent(X509Extension ext)
        {

            ;

        }

        static string GetExtensionContentString(X509Extension ext, bool MultiLine = true)
        {
           
            var data = new AsnEncodedData(ext.Oid, ext.RawData).Format(true);
            return data;
        }




        static void DumpProperties(object obj, string prefix = "")
        {
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                Console.WriteLine(prefix + "{0} = \"{1}\"", name, value);
            }
        }

        // https://oidref.com/2.5.29.17
        static List<SAN> ParseSubjectAltName(byte[] rawData)
        {
            List<SAN> result = new List<SAN>();
            SANType sanType = SANType.otherName;
            string sanValue = null;
            byte[] arr = null;
            int i = 2;
            
            while (i < rawData.Length)
            {
                byte b = rawData[i];
                if (((b >> 4) & 0xF) == 8) // first value
                {
                    sanType = (SANType)(b & 0xF);
                    i++;
                    int lenth = rawData[i];
                    arr = new byte[lenth];
                    for (int j = 0; j < lenth; j++)
                    {
                        i++;
                        arr[j] = rawData[i];
                    }

                    if (sanType == SANType.dNSName || sanType == SANType.rfc822Name || sanType == SANType.uniformResourceIdentifier)
                    {
                        sanValue = Encoding.ASCII.GetString(arr);
                    }
                    else if (sanType == SANType.IPAddress)
                    {
                        sanValue = arr[0].ToString() + "." + arr[1].ToString() + "." + arr[2].ToString() + "." + arr[3].ToString();
                    }
                    else
                    {
                        sanValue = Encoding.ASCII.GetString(arr); // ?? 
                    }
                    result.Add(new SAN(sanType, sanValue));
                }
                else
                {
                    i++;    // next byte
                }

            }
            return result;
        }

        public static void SimpleHexDump(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null)
            {
                Console.WriteLine("<null>");
                return;
            }
            int bytesLength = bytes.Length;

            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn =
                  8                   // 8 characters for the address
                + 3;                  // 3 spaces

            int firstCharColumn = firstHexColumn
                + bytesPerLine * 3       // - 2 digit for the hexadecimal value and 1 space
                + (bytesPerLine - 1) / 8 // - 1 extra space every 8 characters from the 9th
                + 2;                  // 2 spaces 

            int lineLength = firstCharColumn
                + bytesPerLine           // - characters to show the ascii value
                + Environment.NewLine.Length; // Carriage return and line feed (should normally be 2)

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }
            Console.WriteLine(result.ToString());
        }
    }
}
