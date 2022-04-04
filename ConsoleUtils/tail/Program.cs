try
{
    using (var sr = new StreamReader(@"C:\Users\Johannes\Documents\src\ConsoleUtils\ConsoleUtils\tail\TailReader.cs"))
    {
        foreach(String line in TailReader.Tail(sr, 5))
            Console.WriteLine(line);
    }
}
catch (IOException e)
{
    Console.WriteLine("The file could not be read:");
    Console.WriteLine(e.Message);
}