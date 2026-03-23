using System.Collections.Generic;
using System.Linq;

namespace PowerDocu.Common
{
    public class ProgressTracker
    {
        private readonly List<(string Label, int Current, int Total)> _components = new List<(string Label, int Current, int Total)>();
        private readonly object _lock = new object();

        public void Register(string label, int total)
        {
            if (total > 0) _components.Add((label, 0, total));
        }

        public void Increment(string label)
        {
            lock (_lock)
            {
                int idx = _components.FindIndex(c => c.Label == label);
                if (idx < 0) return;
                var c = _components[idx];
                _components[idx] = (c.Label, c.Current + 1, c.Total);
                NotificationHelper.SendStatusUpdate(BuildString());
            }
        }

        public void Complete(string label)
        {
            lock (_lock)
            {
                int idx = _components.FindIndex(c => c.Label == label);
                if (idx < 0) return;
                var c = _components[idx];
                _components[idx] = (c.Label, c.Total, c.Total);
                NotificationHelper.SendStatusUpdate(BuildString());
            }
        }

        public string BuildString()
        {
            return string.Join(", ", _components.Select(c => $"{c.Current}/{c.Total} {c.Label}"));
        }
    }
}
