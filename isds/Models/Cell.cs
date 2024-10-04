namespace ReactiveDAG.Core.Models
{
    public abstract class BaseCell
    {
        public int Index { get; set; }
        public CellType Type { get; set; }

    }

    public class Cell<T> : BaseCell
    {
        public T Value { get; set; }
        public T PreviousValue { get; set; }
        public Action<T> OnValueChanged { get; set; }

        public Cell(int index, CellType type, T value)
        {
            Index = index;
            Type = type;
            Value = value;
            PreviousValue = value;
        }

        public static Cell<T> CreateInputCell(int index, T value) => new Cell<T>(index, CellType.Input, value);
        public static Cell<T> CreateFunctionCell(int index) => new Cell<T>(index, CellType.Function, default);

    }
    public enum CellType
    {
        Input,
        Function
    }
}