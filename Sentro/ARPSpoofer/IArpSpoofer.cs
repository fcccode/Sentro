﻿using System.Collections.Generic;

namespace Sentro.ARPSpoofer
{
    interface IArpSpoofer
    {
        void Spoof(string myIp, HashSet<string> targets);
        void Spoof(string myIp);
        void Include(string target);
        void Include(HashSet<string> targets);
        void Exclude(string target);
        void Exclude(HashSet<string> targets);
        void Stop();
        void Pause();
        void Resume();
        void Start();
        void Usage();        

    }
}
