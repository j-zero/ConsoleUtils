﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum CmdParameterTypes
{
    STRING,
    INT,
    BOOL,
    DECIMAL
}

public enum CmdCommandTypes
{
    VERB,
    PARAMETER,
    FLAG,
    UNNAMED
}

public class CmdParameter
{

    public CmdParameterTypes Type { get; set; }
    public object Value { get; set; }
    public long IntValue { get; set; }
    public bool BoolValue { get; set; }
    public decimal DecimalValue { get; set; }
    public string String { get { return Value != null ? Value.ToString() : null; } }

    public CmdParameter(CmdParameterTypes Type, object Value)
    {
        this.Type = Type;
        this.Value = Value;
        try { IntValue = Convert.ToInt64(Value); } catch { }
        try { this.BoolValue = Convert.ToBoolean(Value); } catch { }
        try { this.DecimalValue = Convert.ToDecimal(Value); } catch { }
    }
}

public class CmdParameters : List<CmdParameter>
{
    public new CmdParameters Add(CmdParameter item)
    {
        base.Add(item);
        return this;
    }
    public new CmdParameters Add(CmdParameterTypes type, object value)
    {
        base.Add(new CmdParameter(type, value));
        return this;
    }
}

public class CmdOption
{
    public string Name { get; set; }
    public string ShortName { get; set; }
    public CmdCommandTypes CmdType { get; set; }
    public CmdParameters Parameters { get; set; }
    public string Description { get; set; }

    public bool WasUserSet { get; set; }

    public CmdOption(string Name)
    {
        this.Name = Name;
    }
    public CmdOption(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameters CmdParams, string Description)
    {
        this.Name = Name;
        this.ShortName = Shortname;
        this.CmdType = CmdType;
        this.Parameters = CmdParams;
        this.Description = Description;
    }

    public long[] Longs
    {
        get { return this.Parameters.Select(x => x.IntValue).ToArray(); }
    }
    public string[] Strings
    {
        get { return this.Parameters.Where(x=> x.String != null).Select(x => x.String).ToArray(); }
    }

    public bool[] Bools
    {
        get { return this.Parameters.Select(x => x.BoolValue).ToArray(); }
    }

    public decimal[] Decimals
    {
        get { return this.Parameters.Select(x => x.DecimalValue).ToArray(); }
    }

    public long GetLong(int index)
    {
        if (index < this.Parameters.Count)
            return this.Parameters[index].IntValue;
        else
            return 0;
    }

    public bool GetBool(int index)
    {
        if (index < this.Parameters.Count)
            return this.Parameters[index].BoolValue;
        else
            return false;
    }

    public string GetString(int index)
    {
        if (index < this.Parameters.Count)
            return this.Parameters[index].String;
        else
            return null;
    }

    public decimal GetDecimal(int index)
    {
        if (index < this.Parameters.Count)
            return this.Parameters[index].DecimalValue;
        else
            return 0;
    }

}

public class CmdParser : KeyedCollection<string, CmdOption>
{
    private Queue<string> fifo = new Queue<string>();

    public string DefaultParameter { get; set; }
    public string DefaultVerb { get; set; }

    public bool HasFlag(string flag)
    {
        return this[flag].GetBool(0);
    }

    public string[] Verbs
    {
        get
        {   
            string[] verbs = this.Where(c => c.CmdType == CmdCommandTypes.VERB && c.WasUserSet).Select(x => x.Name).ToArray();
            return verbs.Length > 0 ? verbs : DefaultVerb != null ? new string[] { DefaultVerb } : new string[0];
        }
    }

    public CmdParser(string[] Args)
    {
        foreach (var arg in Args)
            fifo.Enqueue(arg);

    }

    private bool TryGetValue(string key, out CmdOption item)
    {

        if (this.Contains(key))
        {
            item = this[key];
            return true;
        }
        else
        {
            item = null;
            return false;
        }
    }

    public void Parse()
    {
        while (fifo.Count > 0)
        {
            var currentArgument = fifo.Dequeue();
            
            var parseKey = this.Where(
                x => x.ShortName == currentArgument || x.Name == currentArgument
                ).Select(x => x.Name).FirstOrDefault();

            if(parseKey != null)
                currentArgument = parseKey;

            if (this.TryGetValue(currentArgument, out CmdOption arg))     // known command
            {
                string name = arg.Name;
                int parameterCount = arg.Parameters.Count;
                string expectedParamsString = string.Join(", ", arg.Parameters.Select(x => x.Type.ToString()).ToArray());

                arg.WasUserSet = true;

                if (arg.CmdType == CmdCommandTypes.FLAG)
                {
                    CmdParameter cmdParam = new CmdParameter(CmdParameterTypes.BOOL, true);
                    this[currentArgument].Parameters.Add(cmdParam);
                }
                else
                {
                    foreach (var p in this[currentArgument].Parameters)
                    {
                        string f = fifo.Dequeue();


                        if (p.Type == CmdParameterTypes.BOOL)
                        {
                            string low = f.ToLower().Trim();
                            if (low == "0" || low == "false" || low == "off" || low == "disabled" || low == "disable" || low == "no")
                                p.Value = false;
                            else if (low == "1" || low == "true" || low == "on" || low == "enabled" || low == "enable" || low == "yes")
                                p.Value = true;
                            else
                                throw new Exception($"Can't parse \"{f}\" as {p.Type.ToString()}, {name} expects: {expectedParamsString}.");
                        }
                        else if (p.Type == CmdParameterTypes.INT)
                        {
                            int v = 0;
                            bool success = false;

                            if (f.StartsWith("0x"))
                                success = int.TryParse(f.Substring(2), System.Globalization.NumberStyles.HexNumber, null, out v);
                            else
                                success = int.TryParse(f, out v);

                            if (!success)
                                throw new Exception($"Can't parse \"{f}\" as {p.Type.ToString()}, {name} expects: {expectedParamsString}.");

                            p.Value = v;
                            p.IntValue = v;
                            p.DecimalValue = v;
                            p.BoolValue = Convert.ToBoolean(v);

                        }
                        else if (p.Type == CmdParameterTypes.DECIMAL)
                        {
                            decimal v = 0;

                            if (!decimal.TryParse(f, out v))
                                throw new Exception($"Can't parse \"{f}\" as {p.Type.ToString()}, {name} expects: {expectedParamsString}.");

                            p.Value = v;
                            p.IntValue = (int)v;
                            p.DecimalValue = v;
                            p.BoolValue = Convert.ToBoolean(v);
                        }
                        else if (p.Type == CmdParameterTypes.STRING)
                        {
                            p.Value = f;
                        }
                        
                    }
                }


                
            } 
            else                                                // unnamed
            {
                if(this.DefaultParameter != null)
                {
                    this[this.DefaultParameter].Parameters.Add(CmdParameterTypes.STRING, currentArgument);
                }                    
            }
 

        }
    }


    protected override string GetKeyForItem(CmdOption item)
    {
        if (item == null)
            throw new ArgumentNullException("option");
        if (item.Name != null && item.Name.Length > 0)
            return item.Name;
        throw new InvalidOperationException("Option has no names!");
    }

    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters(), Description));
        return this;
    }

    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameters CmdParams, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, CmdParams, Description));
        return this;
    }

    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, CmdParameterTypes Type, object DefaultValue, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { Type, DefaultValue } }, Description));
        return this;
    }
    public new CmdParser Add(string Name, string Shortname, CmdCommandTypes CmdType, bool DefaultValue, string Description)
    {
        base.Add(new CmdOption(Name, Shortname, CmdType, new CmdParameters() { { CmdParameterTypes.BOOL, DefaultValue } }, Description));
        return this;
    }
}