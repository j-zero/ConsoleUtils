using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;



public static class AssemblyHelper
{
    public static string GetNETVersionFromAssembly(string path)
    {
            
        string result = null;
        try
        {
            
            var asm = System.Reflection.Assembly.LoadFrom(path);
            object[] list = asm.GetCustomAttributes(true);
            var attribute = list.OfType<TargetFrameworkAttribute>().First();

            //Console.WriteLine(attribute.FrameworkName);
            return attribute.FrameworkDisplayName;
                
        }
        catch
        {

        }
        return result;
    }
}

