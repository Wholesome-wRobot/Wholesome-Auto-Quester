using System;

namespace SafeDapper
{
    public class DapperObjectMappingException : Exception
    {
        public DapperObjectMappingException()
        {
        }

        public DapperObjectMappingException(string message)
            : base(message)
        {
        }

        public DapperObjectMappingException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
