
string pathline = args[0]; //C:\\Users\\ringej/.ssh/known_hosts:125

;


string line = null;
int line_number = 0;
int line_to_delete = 12;

using (StreamReader reader = new StreamReader("C:\\input"))
{
    using (StreamWriter writer = new StreamWriter("C:\\output"))
    {
        while ((line = reader.ReadLine()) != null)
        {
            line_number++;

            if (line_number == line_to_delete)
                continue;

            writer.WriteLine(line);
        }
    }
}