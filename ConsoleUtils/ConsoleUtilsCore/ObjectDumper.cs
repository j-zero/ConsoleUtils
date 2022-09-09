using System;
using System.ComponentModel;
public class ObjectDumper
{
    public static void Dump(object obj)
    {
        foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
        {
            string name = descriptor.Name;
            object value = descriptor.GetValue(obj);
            Console.WriteLine("{0} = {1}", name, value);
        }
    }
}

