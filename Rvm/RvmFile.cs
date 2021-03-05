namespace rvmsharp.Rvm
{
    public class RvmFile
    {
        public readonly uint Version;
        public readonly string Info;
        public readonly string Note;
        public readonly string Date;
        public readonly string User;
        public readonly string Encoding;

        public RvmModel Model { get; internal set; }

        public RvmFile(uint version, string info, string note, string date, string user, string encoding)
        {
            Version = version;
            Info = info;
            Note = note;
            Date = date;
            User = user;
            Encoding = encoding;
        }
    }
}