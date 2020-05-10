namespace FileDedupe.Logging
{
    public interface ILogger
    {
        void Error(string message);
        void Warn(string message);
    }
}
