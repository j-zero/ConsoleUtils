public class Blob
{

    public long Offset { get; set; }
    public byte[] Data { get; set; }
    public int Length {
        get
        {
            return Data.Length;
        }
    }
    public Blob()
    {

    }
    public Blob(byte[] Data)
    {
        this.Data = Data;
    }
    public Blob(long Offset, byte[] Data)
    {
        this.Offset = Offset;
        this.Data = Data;
    }
}

public class Selection
{
    public int Offset { get; set; }
    public int Length { get; set; }
    public int End { 
        get { 
            return Offset + Length; 
        } 
        set { 
            this.Length = value - Offset; 
        } 
    }

    public Selection(){
    }
    public Selection(int Offset, int Length)
    {
        this.Offset = Offset;
        this.Length = Length;
    }
}

   

