using System.Collections.Generic;

namespace WSUSMaintenance
{
    public enum ResultMessageType
    {
        Info,
        Warn,
        Error
    }

    public class Result
    {
        public Result(bool success, IDictionary<ResultMessageType, IList<string>> messages)
        {
            Success = success;
            Messages = messages;
        }

        public  bool Success { get; }
        public IDictionary<ResultMessageType, IList<string>> Messages { get; }

        
    }
}