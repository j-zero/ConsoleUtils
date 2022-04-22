namespace hexe
{
    class Blob
    {

        public int Offset { get; set; }
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
        public Blob(int Offset, byte[] Data)
        {
            this.Offset = Offset;
            this.Data = Data;
        }
    }

    class Selection
    {
        public int Offset { get; set; }
        public int Length { get; set; }
        public Selection(){
        }
        public Selection(int Offset, int Length)
        {
            this.Offset = Offset;
            this.Length = Length;
        }
    }

    
}
