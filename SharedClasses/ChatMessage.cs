using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SharedClasses
{
    public class ChatMessage
    {
        public string FromUsername { get; set; }
        public string ToUsername { get; set; }
        public string MessageText { get; set; }
    }
}
