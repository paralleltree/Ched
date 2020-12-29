using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ched.Plugins
{
    [Serializable]
    public class UserCancelledException : Exception
    {
        public UserCancelledException()
        {
        }

        public UserCancelledException(string message) : base(message)
        {
        }

        public UserCancelledException(string message, Exception inner) : base(message, inner)
        {
        }

        protected UserCancelledException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context)
        {
        }
    }
}
