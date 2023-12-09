using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CockpitHardwareHUB_v2.Classes
{
    internal class ProcessProperties
    {
        private readonly ProcessAction _ProcessAction;
        private COMDevice _device;

        public ProcessAction ProcessAction { get { return _ProcessAction; } }
        public COMDevice Device { get { return _device; } }

        internal ProcessProperties(ProcessAction processAction, COMDevice device)
        {
            _ProcessAction = processAction;
            _device = device;
        }
    }

    internal enum ProcessAction
    {
        Add,
        Remove
    }
}
