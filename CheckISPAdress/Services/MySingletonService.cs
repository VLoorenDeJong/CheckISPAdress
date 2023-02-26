using System;

namespace CheckISPAdress.Services
{
    using System;
    using System.Threading;

    public class MySingletonService
    {
        public string LastIPAddress { get; internal set; }

        public void DoWork()
        {
            Console.WriteLine("MySingletonService is doing work.");
        }
    }

}
