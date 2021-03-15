using System;

namespace Panthea.Asset
{
    public class RemoteFileNotFound : Exception
    {
        public RemoteFileNotFound(string message) : base(message)
        {
        }
    }
}
