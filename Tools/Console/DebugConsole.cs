﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlogAppBackend.Tools.Console
{
    public class DebugConsole : IDebugConsole
    {
        public void Log(string message)
        {
            System.Console.WriteLine(message);
        }
    }
}
