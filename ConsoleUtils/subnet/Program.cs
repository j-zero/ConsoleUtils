using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;
using System.Net;

namespace subnet
{
    internal class Program
    {
        static string defaultIP = "0.0.0.0";

        static void Main(string[] args)
        {
            uint ip = 0, mask = 0, net = 0, cidr = 0, bc = 0, start = 0, end = 0;
            uint[] ip_net = new uint[2];

            try
            {
                if (args.Length == 0 || (args.Length == 1 && args[0] == "--help"))
                {
                    ConsoleHelper.WriteError("Usage: subnet [/cidr|host-count|ip/cidr|ip/mask|ip host-count]");
                    Environment.Exit(1);
                }
                else if (args.Length == 1)
                {
                    if (args[0].Contains("/")) // Network
                    {
                        ip_net = getIpCidrFromNetString(args[0]);
                        ip = ip_net[0];
                        cidr = ip_net[1];
                        if (ip == 0)
                        {
                            if (cidr >= 0 && cidr < 8)
                                ip = addrToInt("0.0.0.0");
                            else if (cidr >= 8 && cidr < 12)
                                ip = addrToInt("10.0.0.0");
                            else if (cidr >= 12 && cidr < 16)
                                ip = addrToInt("172.16.0.0");
                            else if (cidr >= 16)
                                ip = addrToInt("192.168.0.0");
                            else
                                ip = addrToInt(defaultIP);
                        }

                    }
                    else if (args[0].All(char.IsDigit)) // Hostcount
                    {
                        
                        uint ips = uint.Parse(args[0]);
                        cidr = getCidrFromHostCount(uint.Parse(args[0]), true);
                        if (cidr >= 0 && cidr < 8)
                            ip = addrToInt("0.0.0.0");
                        else if (cidr >= 8 && cidr < 12)
                            ip = addrToInt("10.0.0.0");
                        else if (cidr >= 12 && cidr < 16)
                            ip = addrToInt("172.16.0.0");
                        else if (cidr >= 16)
                            ip = addrToInt("192.168.0.0");
                        else
                            ip = addrToInt(defaultIP);
                    }
                    else
                    {
                        ip_net = getIpCidrFromNetString(args[0]);
                        ip = ip_net[0];
                        cidr = ip_net[1];
                    }
                }
                else if (args.Length == 2)
                {
                    ip = addrToInt(args[0]);

                    if (args[1].All(char.IsDigit))
                    {
                        uint ips = uint.Parse(args[1]);
                        cidr = getCidrFromHostCount(ips, true);
                    }
                    else
                    {
                        cidr = getCidrFromSubnetMask(args[1]);
                    }
                }
                else
                {
                    ConsoleHelper.WriteError("wat?");
                    Environment.Exit(255);
                }


                //Color highlight = Color.LightSkyBlue;


                uint count = getInfosFromIp(ip, cidr, out mask, out net, out bc, out start, out end);

                var ipObj = System.Net.IPAddress.Parse(ip.ToString());
                string ipinfo = getNetworkInfoFromIP(ip);
                string mappedv6 = ipObj.MapToIPv6().ToString();
                var nextNet = bc + 1;
                var prevNet = net - 1;

                //Console.WriteLine($"{"CIDR:".Pastel(Color.White)}      {intToAddr(net)}{cidr.ToString().Pastel(highlight)}");
                //Console.WriteLine($"{"Mask:".Pastel(Color.White)}      {"/".Pastel(Color.White)}{cidr.ToString().Pastel(highlight)}, {intToAddr(mask)}");

                if (net != 0) {
                    Console.WriteLine($"{"IP:".Pastel(Color.White)}               {intToAddr(ip).Pastel(ColorTheme.Default1)} {("(" + ipinfo + ")").Pastel(ColorTheme.DarkText)}");
                    Console.WriteLine($"{"Other rep.:".Pastel(Color.White)}       {ip.ToString()}, 0x{ip.ToString("X").ToLower()}, { StringHelper.AddSeperator(Convert.ToString(ip, 2).PadLeft(32, '0'), ".", 8)}, {getArpaStringFromIP(ip)}"); //, from 0x{start.ToString("X").ToLower()} - 0x{end.ToString("X").ToLower()}");
                    Console.WriteLine($"{"IPv6 conv.:".Pastel(Color.White)}       ::ffff:{StringHelper.AddSeperator(ip.ToString("X").ToLower(), ":", 4)}, {mappedv6}");
                }

                if (net != 0 && cidr != 32)
                    Console.WriteLine();

                if (cidr != 32) {
                    
                    Console.WriteLine($"{"Network:".Pastel(Color.White)}          {intToAddr(net).Pastel(ColorTheme.Default1)}{"/".Pastel(ColorTheme.Default2)}{cidr.ToString().Pastel(ColorTheme.Default1)} {"|".Pastel(ColorTheme.DarkText)} {intToAddr(mask)}");
                    Console.WriteLine($"{"Hosts:".Pastel(Color.White)}            {(count - 2).ToString().Pastel(ColorTheme.Default1)} ({count.ToString().Pastel(ColorTheme.Default2)}{" - 2".Pastel(ColorTheme.DarkText)}) {"|".Pastel(ColorTheme.DarkText)} {intToAddr(start)} {"-".Pastel(ColorTheme.Default2)} {intToAddr(end)}");
                    Console.WriteLine($"{"Broadcast:".Pastel(Color.White)}        {intToAddr(bc)}");
                    Console.WriteLine();

                    if (!(net < prevNet))
                    {
                        count = getInfosFromIp(prevNet, cidr, out mask, out net, out bc, out start, out end);
                        Console.WriteLine($"{"Previous network:".Pastel(Color.White)} {intToAddr(prevNet)}{"/".Pastel(ColorTheme.Default2)}{cidr} {"|".Pastel(ColorTheme.DarkText)} {intToAddr(start)} {"-".Pastel(ColorTheme.Default2)} {intToAddr(end)}");
                    }

                    if (nextNet > net)
                    {
                        count = getInfosFromIp(nextNet, cidr, out mask, out net, out bc, out start, out end);
                        Console.WriteLine($"{"Next network:".Pastel(Color.White)}     {intToAddr(nextNet)}{"/".Pastel(ColorTheme.Default2)}{cidr} {"|".Pastel(ColorTheme.DarkText)} {intToAddr(start)} {"-".Pastel(ColorTheme.Default2)} {intToAddr(end)}");
                    }
                }
                else { 
                    //Console.WriteLine($"{"Hosts:".Pastel(Color.White)}            {"1".Pastel(ColorTheme.Default1)}");
                    
                }

            }
            catch(Exception ex)
            {
                ConsoleHelper.WriteErrorTRex(ex.Message);
                Environment.Exit(1);
            }

        }

        static string getNetworkInfoFromIP(uint ip)
        {
            if (ip >= 0 && ip <= 0xfffffe)
                return "current network, 0.0.0.0/8, only valid as source, RFC 3232";

            else if (ip >= 0xa000000 && ip <= 0xafffffe)
                return "private, 10.0.0.0/8, private addresses, RFC 1918";

            else if (ip >= 0xac100000 && ip <= 0xac1ffffe)
                return "private, 172.16.0.0/12, private addresses, RFC 1918";

            else if (ip >= 0xc0a80000 && ip <= 0xc0a8fffe)
                return "private, 192.168.0.0/16, private addresses, RFC 1918";

            else if (ip >= 0x64400000 && ip <= 0x647ffffe)
                return "private, 100.64.0.0/10, carrier-grade NAT, RFC 1918";

            else if (ip >= 0x7f000000 && ip <= 0x7ffffffe)
                return "localnet, 127.0.0.0/8, loopback addresses, RFC 3330";

            else if (ip >= 0x7f000000 && ip <= 0x7ffffffe)
                return "zeroconf, 169.254.0.0/16, link-local addresses, RFC 3927";

            else if (ip >= 0xc0000000 && ip <= 0xc0000006)
                return "reserved, 192.0.0.0/29, Dual-Stack Lite, RFC 6333";

            else if (ip >= 0xc0000000 && ip <= 0xc00000fe)
                return "reserved, 192.0.0.0/24, IETF Protocol Assignments, RFC 6890";

            else if (ip >= 0xc0000200 && ip <= 0xc00002fe)
                return "documentation, 192.0.2.0/24, documentation and examples (TEST-NET-1), RFC 5737";

            else if (ip >= 0xc6336400 && ip <= 0xc63364fe)
                return "documentation, 198.51.100.0/24, documentation and examples (TEST-NET-2), RFC 5737";

            else if (ip >= 0xcb007100 && ip <= 0xcb0071fe)
                return "documentation, 203.0.113.0/24, documentation and examples (TEST-NET-3), RFC 5737";

            else if (ip >= 0xc0586300 && ip <= 0xc05863fe)
                return "reserved, 192.88.99.0/24, 6to4-Anycast, RFC 3068";

            else if (ip >= 0xc6120000 && ip <= 0xc613fffe)
                return "private, 198.18.0.0/15, network benchmark testing, RFC 2544";

            else if (ip >= 0xe0000000 && ip <= 0xeffffffe)
                return "private, 224.0.0.0/4, multicast, RFC 3171";

            else if (ip >= 0xe9fc0000 && ip <= 0xe9fc00fe)
                return "documentation, 233.252.0.0/24, documentation and examples (MCAST-TEST-NET), RFC 5771";

            else if (ip >= 0xf0000000 && ip <= 0xfffffffe)
                return "reserved, 240.0.0.0/4, Reserved for future use, RFC 3232";

            else if (ip == 0xffffffff)
                return "broadcast, 255.255.255.255/32, broadcast";

            return "public";
        }

        static string getArpaStringFromIP(uint address)
        {
            //4.3.2.1.in-addr.arpa
            uint[] o = new uint[4] { (address & 0xFF000000) >> 24, (address & 0xFF0000) >> 16, (address & 0xFF00) >> 8, (address & 0xFF) };
            return string.Format("{3}.{2}.{1}.{0}.in-addr.arpa", o[0], o[1], o[2], o[3]);
        }

        static uint addrToInt(string ip)
        {
            string[] s = ip.Split('.');
            if (s.Length != 4)
                throw new Exception($"Can't parse IPv4 \"{ip}\".");
            return Convert.ToUInt32((UInt32.Parse(s[0]) * 0x1000000) + (UInt32.Parse(s[1]) * 0x10000) + (UInt32.Parse(s[2]) * 0x100) + UInt32.Parse(s[3]));
        }

        static string intToAddr(uint address)
        {
            uint[] o = new uint[4] { (address & 0xFF000000) >> 24, (address & 0xFF0000) >> 16, (address & 0xFF00) >> 8, (address & 0xFF) };
            return string.Format("{0}.{1}.{2}.{3}", o[0], o[1], o[2], o[3]);
        }

        static uint getCidrFromSubnetMask(string mask)
        {
            uint n = addrToInt(mask);
            uint c = 0;
            while (n != 0)
            {
                c += n & 1;
                n = n >> 1;
            }
            return c;
        }

        static uint getCidrFromHostCount(uint count, bool mustFit = false)
        {
            uint cidr = 32 - (uint)Math.Ceiling(Math.Log(count, 2));
            if (mustFit && (Math.Pow(2, 32 - cidr) - 2) < count)
                cidr -= 1;
            return cidr;
        }

        static uint getInfosFromIp(uint ip, uint cidr, out uint mask, out uint net, out uint bc, out uint start, out uint end)
        {
            mask = 0xFFFFFFFF << 32 - (int)cidr;
            net = mask & ip;
            start = 0;
            end = 0;
            bc = net + (0xFFFFFFFF - mask);
            uint hostcount = (uint)Math.Pow(2, 32 - cidr);

            if (cidr == 31)
            {
                start = net;
                end = bc;
                
            }
            else if (cidr == 32)
            {
                start = ip;
                end = ip;
                hostcount = 1;
            }
            else if (cidr > 32)
            {
                    throw new Exception("Do you even understand subnets?");
            }
            else if(cidr > 32 || cidr < 1)
            {
                    throw new Exception("Invalid network!");
            }
            else
            {
                start = net + 1;
                end = bc - 1;
                //return hostcount;//getIpRangeFromString(start, end);
            }


            return hostcount;

        }

        static uint[] getIpCidrFromNetString(string net)
        {
            uint ip = 0;
            uint cidr = 0;
            string strCidr = "";
            string[] ipnet = net.Split('/');
            if (ipnet.Length < 2)
            {
                if(IPAddress.TryParse(ipnet[0], out IPAddress ipAddr))
                {
                    ip = addrToInt(ipnet[0]);
                    cidr = 32;
                }
                else
                    throw new Exception("No network given!");
                /*var isNumeric = uint.TryParse(ipnet[0], out cidr);
                if (isNumeric && cidr > 0 && cidr <= UInt32.MaxValue)
                {
                    ip = addrToInt("10.0.0.0");
                    strCidr = ipnet[0];
                }
                else*/

            }
            else {
                if (ipnet[0] != String.Empty)
                    ip = addrToInt(ipnet[0]);
                else
                    ip = addrToInt(defaultIP);

                strCidr = ipnet[1];
            }

            if (cidr == 0)
            {
                if (strCidr.All(char.IsDigit))
                {
                    cidr = uint.Parse(strCidr);
                }
                else
                {
                    cidr = getCidrFromSubnetMask(strCidr);
                }
            }
            //
            
            return new uint[] { ip, cidr };
        }

        static string[] getIpRangeFromString(uint ipStart, uint ipEnd)
        {
            List<string> result = new List<string>();

            uint start = ipStart;
            uint end = ipEnd;

            uint current = start;
            while (current <= end)
            {
                string a = intToAddr(current);
                result.Add(a);
                current++;
            }

            return result.ToArray();
        }

    }
}
