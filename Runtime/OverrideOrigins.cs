namespace SerializableSettings
{
    internal interface IOverrideOrigin
    {
        string ToString();
    }

    internal struct FileOverrideOrgin : IOverrideOrigin
    {
        public FileOverrideOrgin( string filePath ) : this()
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
        public override string ToString() => $"File: {FilePath}";
    }

    internal struct FileWatcherOverrideOrgin : IOverrideOrigin
    {
        public FileWatcherOverrideOrgin( string filePath ) : this()
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
        public override string ToString() => $"FileWatcher: {FilePath}";
    }

    internal struct CommandlineOverrideOrgin : IOverrideOrigin
    {
        public CommandlineOverrideOrgin( string argument ) : this()
        {
            Argument = argument;
        }

        public string Argument { get; }
        public override string ToString() => $"Commandline: {Argument}";
    }
}
