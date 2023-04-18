using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pastel;
using System.Drawing;

namespace subnet
{
    internal class Program
    {
        static void Main(string[] args)
        {
            uint ip = 0, mask = 0, net = 0, cidr = 0, bc = 0, start = 0, end = 0;
            uint[] ip_net = new uint[2];

            try
            {
                if (args.Length == 0)
                {
                    Console.Write("net: ");
                    getIpCidrFromNetString(Console.ReadLine());
                }
                else if (args.Length == 1 && args[0] == "--help")
                {
                    ConsoleHelper.WriteError("Usage: subnet [ip/cidr|ip/mask|ip number_of_hosts]");
                    Environment.Exit(1);
                }
                else if (args.Length == 1)
                {
                    ip_net = getIpCidrFromNetString(args[0]);
                    ip = ip_net[0];
                    cidr = ip_net[1];
                }
                else if (args.Length == 2)
                {
                    ip = addrToInt(args[0]);

                    if (args[1].All(char.IsDigit))
                    {
                        uint ips = uint.Parse(args[1]);
                        cidr = getCidrFromHostCount(ips);
                        var test = Math.Pow(2, 32 - cidr) - 2;

                        if (test < ips)
                            cidr -= 1;

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

                Color highlight = Color.LightSkyBlue;


                uint count = getInfosFromIp(ip, cidr, out mask, out net, out bc, out start, out end);


                //Console.WriteLine($"{"CIDR:".Pastel(Color.White)}      {intToAddr(net)}{cidr.ToString().Pastel(highlight)}");
                //Console.WriteLine($"{"Mask:".Pastel(Color.White)}      {"/".Pastel(Color.White)}{cidr.ToString().Pastel(highlight)}, {intToAddr(mask)}");
                Console.WriteLine($"{"IP:".Pastel(Color.White)}        {intToAddr(ip)}");
                Console.WriteLine($"{"Network:".Pastel(Color.White)}   {intToAddr(net).Pastel(ColorTheme.Default1)}{"/".Pastel(ColorTheme.Default2)}{cidr.ToString().Pastel(ColorTheme.Default1)} {"|".Pastel(ColorTheme.DarkText)} {intToAddr(mask)}");
                Console.WriteLine($"{"Broadcast:".Pastel(Color.White)} {intToAddr(bc)}");
                Console.WriteLine($"{"Hosts:".Pastel(Color.White)}     {count.ToString().Pastel(highlight)} {"|".Pastel(ColorTheme.DarkText)} {intToAddr(start)}{"-".Pastel(ColorTheme.Default2)}{intToAddr(end)}");

            }
            catch(Exception ex)
            {
                ConsoleHelper.WriteErrorTRex(ex.Message);
                Environment.Exit(1);
            }



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

        static uint getCidrFromHostCount(uint count)
        {
            return 32 - (uint)Math.Ceiling(Math.Log(count, 2));
        }

        static uint getInfosFromIp(uint ip, uint cidr, out uint mask, out uint net, out uint bc, out uint start, out uint end)
        {
            mask = 0xFFFFFFFF << 32 - (int)cidr;
            net = mask & ip;
            start = 0;
            end = 0;
            bc = net + (0xFFFFFFFF - mask);
            uint hostcount = (uint)Math.Pow(2, 32 - cidr) - 2;

            if (cidr == 31)
            {
                start = net;
                end = bc;
                
            }
            else if (cidr == 32)
            {
                    throw new Exception("Do you even understand subnets?");
            }
            else if(cidr > 31 || cidr < 1)
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
            string[] ipnet = net.Split('/');
            if(ipnet.Length < 2)
                throw new Exception("No network given!");

            uint ip = addrToInt(ipnet[0]);
            uint cidr = 0;
            if (ipnet[1].All(char.IsDigit))
            {
                cidr = uint.Parse(ipnet[1]);
            }
            else
            {
                cidr = getCidrFromSubnetMask(ipnet[1]);
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
