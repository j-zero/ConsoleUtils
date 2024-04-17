using Pastel;
using System;
using System.ComponentModel;
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
using static System.Net.Mime.MediaTypeNames;
using static certy.Program;


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
        static string HeaderColor2 = ColorTheme.OffsetColorHighlight2;

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
                { "host", "h", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.STRING, null }
                    }, "Host" },
                { "port", "p", CmdCommandTypes.PARAMETER, new CmdParameters() {
                        { CmdParameterTypes.INT, 443 }
                    }, "Timeout" },
                { "no-proxy", "", CmdCommandTypes.FLAG, new CmdParameters() {
                        { CmdParameterTypes.BOOL, false }
                    }, "Disable proxy" },
                { "protocol", "P", CmdCommandTypes.PARAMETER, new CmdParameters() {
                    { CmdParameterTypes.STRING, "tcp" }
                }, "Protocol. Can be: tcp/ssl, http, smtp" },
                { "extended", "e", CmdCommandTypes.FLAG, new CmdParameters() {
                        { CmdParameterTypes.BOOL, false }
                    }, "Shows extended informations" },
                { "chain", "c", CmdCommandTypes.FLAG, new CmdParameters() {
                        { CmdParameterTypes.BOOL, false }
                    }, "Shows certificate chain" },
                { "pem", "", CmdCommandTypes.FLAG, new CmdParameters() {
                        { CmdParameterTypes.BOOL, false }
                    }, "Show PEM format only" },

            };

            cmd.DefaultParameter = "host";
            cmd.Parse();


            string host = cmd["host"].String;
            int port = (int)cmd["port"].Int;
            int timeout = 2000;

            if (cmd.HasFlag("help") || host == null)
                ShowHelp();

            if (cmd["protocol"].String == "http")
            {

                Task.Run(async () =>
                {
                    await ShowHTTPCertificate(host);
                }).GetAwaiter().GetResult();
                //await task;
            }
            else if (cmd["protocol"].String == "smtp")
            {
                ShowSMTPCertificate(host, port);
            }
            else if (cmd["protocol"].String == "tcp" || cmd["protocol"].String == "ssl")
            {
                ShowTCPCertificate(host, port, timeout);
            }
            else
            {
                ConsoleHelper.WriteError($"Unknown protocol: {cmd["protocol"].String}");
                Exit(1);
            }
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
            Console.WriteLine($"certy, {ConsoleHelper.GetVersionString("9CDCFE", "569CD6")}");
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
            //string parrentProcess = ConsoleUtilsCore.ParentProcessUtilities.GetParentProcess().ProcessName;
            //Console.WriteLine(parrentProcess);

            if (System.Diagnostics.Debugger.IsAttached) // is debugger attached or started by double-click/file-drag
            {
                Console.WriteLine("\nPress any key to exit.");
                Console.ReadKey();
            }

            Environment.Exit(exitCode);
        }

        static void ShowSMTPCertificate(string host, int port, int timeout = 2000)
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);
            using (System.Net.Mail.SmtpClient S = new System.Net.Mail.SmtpClient(host,port))
            {
                S.EnableSsl = true;
                using (System.Net.Mail.MailMessage M = new System.Net.Mail.MailMessage("test@example.com", "test@example.com", "Test", "Test"))
                {
                    try
                    {
                        S.Send(M);
                    }
                    catch (System.Net.Mail.SmtpException sex)
                    {
                        return;
                    }
                    catch (Exception ex)
                    {
                        ConsoleHelper.WriteError(ex.GetType().ToString());
                        ConsoleHelper.WriteError(ex);
                        if (ex.InnerException != null)
                            ConsoleHelper.WriteError(ex.InnerException);
                    }
                }
            }
        }

        static void ShowTCPCertificate(string host, int port, int timeout = 2000)
        {
            TcpClient client = new TcpClient();

            try
            {
                if (client.ConnectAsync(host, port).Wait(timeout)) // 2000ms timeout
                {
                    //Console.WriteLine("Foobar!");
                    using (SslStream sslStream = new SslStream(client.GetStream(), false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null,EncryptionPolicy.RequireEncryption))
                    {
                        
                        try
                        {
                            //sslStream.SslProtocol = System.Security.Authentication.SslProtocols.Tls13;
                            //sslStream.AuthenticateAsClient(host,null,System.Security.Authentication.SslProtocols.Tls11, false);
                            sslStream.AuthenticateAsClient(host);
                            // do nothing with the data
                        }
                        catch (Exception ex)
                        {
                            ConsoleHelper.WriteError(ex);
                            if (ex.InnerException != null)
                                ConsoleHelper.WriteError(ex.InnerException);
                            Console.WriteLine(ConsoleHelper.VarDump(sslStream));
                        }
                    }
                }
                else
                {
                    ConsoleHelper.WriteError($"Timeout connecting to {host}:{port.ToString()}");
                }
            }
            catch (Exception ex)
            {
                ConsoleHelper.WriteError(ex);
                if (ex.InnerException != null)
                    ConsoleHelper.WriteError(ex.InnerException);
            }
            finally
            {
                client.Close();
            }

        }


        static async Task ShowHTTPCertificate(string URL)
        {
            string EndPoint = URL;

            if (!URL.StartsWith("https://"))
                EndPoint = "https://" + URL;

            //HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(EndPoint);
            //var p = WebRequest.DefaultWebProxy;

            //string proxy = GetProxyForUrlStatic(EndPoint);

            //if (proxy != null)
            //    Console.Error.WriteLine($"Using proxy: {proxy}");


            var handler = new HttpClientHandler
            {
                // Use system proxy settings
                UseProxy = !cmd["no-proxy"].Bool,
                // Use default credentials for proxy authentication if required
                UseDefaultCredentials = true
            };

            handler.ServerCertificateCustomValidationCallback = ValidateServerCertificate;
            HttpClient client = new HttpClient(handler);
            try
            {
                HttpResponseMessage response = await client.GetAsync(EndPoint);
            }
            catch (HttpRequestException ex)
            {
                ConsoleHelper.WriteError(ex);
                if (ex.InnerException != null)
                    ConsoleHelper.WriteError(ex.InnerException);
            }
            handler.Dispose();
            client.Dispose();
        }

        public static string GetProxyForUrlStatic(string url)
        {
           // WebProxy proxy = (WebProxy)WebProxy.GetDefaultProxy();
           var proxy = System.Net.WebRequest.GetSystemWebProxy();

            Uri resource = new Uri(url);
            // Display the proxy's properties.
            //DisplayProxyProperties(proxy);

            // See what proxy is used for the resource.
            Uri resourceProxy = proxy.GetProxy(resource);

            // Test to see whether a proxy was selected.
            if (resourceProxy == null || resourceProxy == resource)
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



            if(cmd.HasFlag("chain") && !cmd.HasFlag("pem"))
                Console.WriteLine($"Certificate:\n".Pastel(HeaderColor));

            X509Certificate2 X5092 = new X509Certificate2(certificate);

            // Certificate
            PrintCertificateInfo(X5092, cmd.HasFlag("extended"));

            if(cmd.HasFlag("chain"))
            // Chain
            if (chain.ChainElements.Count > 0)
            {
                int chainCount = 2;
                if (!cmd.HasFlag("pem"))
                    Console.WriteLine($"\nChain:".Pastel(HeaderColor));

                foreach (var c in chain.ChainElements.Skip(1))
                {
                    if (!cmd.HasFlag("pem")) Console.WriteLine($"\n#{chainCount++}".Pastel(HeaderColor));
                    PrintCertificateInfo(c.Certificate, cmd.HasFlag("extended"));
                }
            }

            ;
            return true;
        }

        static void PrintCertificateInfo(X509Certificate2 X5092, bool extendedInformations = false) {

            if (cmd.HasFlag("pem"))
            {
                string PEM = X5092.ExportCertificatePem();
                Console.WriteLine(PEM);
                return;
            }
            
            int maxDescLength = Console.WindowWidth - 2;
            int padding = 21;
            var ecdsa = X5092.GetECDsaPublicKey();
            var rsa = X5092.GetRSAPublicKey();
            var dsa = X5092.GetDSAPublicKey();

            string keyAlgo = "";
            int bits = 0;
            string keyAlgoString = "";
            string bitsString = "";

            DateTime notBefore = X5092.NotBefore;
            DateTime notAfter = X5092.NotAfter;

            string cn = X5092.GetNameInfo(X509NameType.SimpleName, false);
            string keystring = "";

            //string notBeforeString = "";
            //string notAfterString = "";


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




            //SAN
            var SAN = X5092.Extensions["2.5.29.17"];

            

            ConsoleHelper.WriteSplittedKeyValue("Common names: ".Pastel(HeaderColor2), cn, maxDescLength, padding);
            Console.WriteLine();

            if (SAN != null)
            {
                //File.WriteAllBytes("2.5.29.17.dmp", SAN.RawData);
                //Console.WriteLine($"{SAN.Oid.FriendlyName} ({SAN.Oid.Value}):".Pastel(HeaderColor2));

                List<string> str_sans = new List<string>();
                foreach (var san in ParseSubjectAltName(SAN.RawData))
                {

                    if (san.SanType == SANType.dNSName)
                    {
                        if (InetHelper.IsFqdn(san.Value))
                            str_sans.Add($"{san.Value.Pastel(GoodColor)}{(extendedInformations ? " (DNS)" : "")}");
                        else
                            str_sans.Add($"{san.Value.Pastel(WarnColor2)}{(extendedInformations ? " (DNS)" : "")}");
                    }
                    else if (san.SanType == SANType.IPAddress)
                    {
                        str_sans.Add($"{san.Value.Pastel(BadColor)}{(extendedInformations ? " (IP)" : "")}");
                    }
                    else
                    {
                        str_sans.Add($"{san.Value.Pastel(BadColor)}{(extendedInformations ? $" ({san.SanType.ToString()})" : "")}");
                    }
                }
                ConsoleHelper.WriteSplittedKeyValue("Alternate names: ".Pastel(HeaderColor2), str_sans.ToArray(), ", ", maxDescLength, padding);
                Console.WriteLine();
            }

            Console.Write($"Issuer: ".PadRight(padding).Pastel(HeaderColor2));
            ConsoleHelper.WriteSplittedText(X5092.GetNameInfo(X509NameType.SimpleName, true), maxDescLength, "", 0, ColorTheme.Text);
            Console.WriteLine();


            Console.Write($"Public key:".PadRight(padding).Pastel(HeaderColor2));



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

            Console.WriteLine($"{keyAlgoString}, {bitsString} bits");


            Console.Write($"Serial number:".PadRight(padding).Pastel(HeaderColor2));
            Console.WriteLine($"{X5092.SerialNumber.ToLower()}");

            Console.Write($"Thumbprint:".PadRight(padding).Pastel(HeaderColor2));
            Console.WriteLine($"{X5092.Thumbprint.ToLower()}");
            Console.Write($"Signature Algorhitm:".PadRight(padding).Pastel(HeaderColor2));
            Console.WriteLine($"{X5092.SignatureAlgorithm.FriendlyName}");

            Console.Write($"Not before:".PadRight(padding).Pastel(HeaderColor2));


            if (notBefore > DateTime.Now)
                Console.WriteLine($"{notBefore.ToShortDateString().Pastel(BadColor)} {notBefore.ToShortTimeString().Pastel(BadColor2)}");
            else
                Console.WriteLine($"{notBefore.ToShortDateString().Pastel(ColorTheme.Default1)} {notBefore.ToShortTimeString().Pastel(ColorTheme.Default2)}");

            Console.Write($"Not after:".PadRight(padding).Pastel(HeaderColor2));
            if (notAfter < DateTime.Now)
                Console.WriteLine($"{notAfter.ToShortDateString().Pastel(BadColor)} {notAfter.ToShortTimeString().Pastel(BadColor2)}");
            else if (notAfter < DateTime.Now.AddDays(30))
                Console.WriteLine($"{notAfter.ToShortDateString().Pastel(WarnColor)} {notAfter.ToShortTimeString().Pastel(WarnColor2)}");
            else
                Console.WriteLine($"{notAfter.ToShortDateString().Pastel(ColorTheme.Default1)} {notAfter.ToShortTimeString().Pastel(ColorTheme.Default2)}");

            /*
            Console.WriteLine($"Not Before:                 {X5092.NotBefore.ToLongDateString()} {X5092.NotBefore.ToLongTimeString()}");
            Console.WriteLine($"Not After:                  {X5092.NotAfter.ToLongDateString()} {X5092.NotAfter.ToLongTimeString()}");
            */



            if (extendedInformations)
            {
                Console.Write($"Subject:".Pastel(HeaderColor2));
                ConsoleHelper.WriteSplittedText(X5092.SubjectName.Name, maxDescLength, "", padding - 8, ColorTheme.Text);
                Console.WriteLine();

                Console.Write($"Issuer (CN):".Pastel(HeaderColor2));
                ConsoleHelper.WriteSplittedText(X5092.Issuer, maxDescLength, "", padding - 12, ColorTheme.Text);
                Console.WriteLine();

                Console.WriteLine($"\nPublic key (binary):".Pastel(HeaderColor2));
                SimpleHexDump(X5092.GetPublicKey());

                foreach (var ext in X5092.Extensions)
                {
                    if (ext.Oid.Value != "2.5.29.17")
                    {
                        Console.WriteLine($"\n{ext.Oid.FriendlyName} ({ext.Oid.Value}):".Pastel(HeaderColor2));
                        SimpleHexDump(ext.RawData, "", 16);
                    }
                    //Console.WriteLine($"   {ext.}:");
                }
                string PEM = X5092.ExportCertificatePem();
                Console.WriteLine($"\nPEM:".Pastel(HeaderColor2));
                Console.WriteLine(PEM);

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

        public static void SimpleHexDump(byte[] bytes, string prefix = "", int bytesPerLine = 16)
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
            //StringBuilder result = new StringBuilder(expectedLines * lineLength);

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
                //result.Append(line);
                Console.Write(prefix + new string(line));
            }
            //Console.WriteLine(result.ToString());
        }
    }
}
