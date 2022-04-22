namespace hexe
{
    class Blob
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

    class Selection
    {
        public long Offset { get; set; }
        public long Length { get; set; }
        public Selection(){
        }
        public Selection(long Offset, long Length)
        {
            this.Offset = Offset;
            this.Length = Length;
        }
    }

    
}
