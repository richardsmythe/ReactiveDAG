using ReactiveDAG.Core.Models;

namespace ReactiveDAG.Services
{
    public static class DagUtils
    {
        public static bool HasChanged<T>(Cell<T> cell)
        {
            return !EqualityComparer<T>.Default.Equals(cell.Value, cell.PreviousValue);
        }
    }
}
