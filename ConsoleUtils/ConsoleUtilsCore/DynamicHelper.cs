using System;
using System.Collections.Generic;
using System.Dynamic;


public class DynamicHelper
{
    public static bool HasProperty(dynamic dyn, string propertyname)
    {
        if (dyn is ExpandoObject)
            return ((IDictionary<string, object>)dyn).ContainsKey(propertyname);

        return dyn.GetType().GetProperty(propertyname) != null;
    }
}
