using System;
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
    SWITCH
}

public class CmdParameter
{

    public CmdParameterTypes Type { get; set; }
    public object Value { get; set; }

    public CmdParameter(CmdParameterTypes Type, object Value)
    {
        this.Type = Type;
        this.Value = Value;
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

}

public class CmdParser : KeyedCollection<string, CmdOption>
{
    private Queue<string> fifo = new Queue<string>();

    public CmdParser(string[] Args)
    {
        foreach (var arg in Args)
            fifo.Enqueue(arg);

    }

    public void Parse()
    {
        KeyedCollection<string, CmdOption> foo = this;

        while (fifo.Count > 0)
        {
            var currentArgument = fifo.Dequeue();
            
            if(this.TryGetValue(currentArgument, out CmdOption arg))     // known command
            {
                string name = arg.Name;
                int parameterCount = arg.Parameters.Count;

                if (arg.CmdType == CmdCommandTypes.SWITCH)
                {
                    ;
                }
                else
                {
                    foreach (var p in this[currentArgument].Parameters)
                    {
                        object f = fifo.Dequeue();
                        if (p.Type == CmdParameterTypes.BOOL)
                        {
                            ;
                        }
                        else if (p.Type == CmdParameterTypes.DECIMAL)
                        {

                        }
                        else if (p.Type == CmdParameterTypes.INT)
                        {

                        }
                        else if (p.Type == CmdParameterTypes.STRING)
                        {

                        }

                    }
                }


                
            } 
            else                                                // unknown
            {
                ;                               
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