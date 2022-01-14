using System;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (args.Length > 0)
            System.Diagnostics.Process.Start(args[0]);
        else
            System.Diagnostics.Process.Start(Environment.CurrentDirectory);
    }
}
