using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


public class InetHelper
{
    public static bool IsFqdn(string uri)
    {
        return Regex.IsMatch(uri, @"(?=^.{4,253}\.?$)(^((?!-)[a-zA-Z0-9-]{1,63}(?<!-)\.)+[a-zA-Z]{2,63}\.?$)");
    }
}

