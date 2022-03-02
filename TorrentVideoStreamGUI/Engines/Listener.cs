using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace TorrentVideoStreamGUI.Engines
{
    public class Listener : TraceListener
    {
        private readonly int capacity;
        private readonly LinkedList<string> traces;

        public Listener(int capacity)
        {
            this.capacity = capacity;
            this.traces = new LinkedList<string>();
        }

        public override void Write(string message)
        {
            lock (traces)
                traces.Last.Value += message;
        }

        public override void WriteLine(string message)
        {
            lock (traces)
            {
                if (traces.Count >= capacity)
                    traces.RemoveFirst();

                traces.AddLast(message);
            }
        }

        public void ExportTo(TextWriter output)
        {
            lock (traces)
                foreach (string s in this.traces)
                    output.WriteLine(s);
        }
    }
}
