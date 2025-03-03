﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ReactiveDAG.Core.Models
{
    public class Cell<T> : BaseCell
    {
        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (!EqualityComparer<T>.Default.Equals(_value, value))
                {
                    PreviousValue = _value;
                    _value = value;
                    OnValueChanged?.Invoke(_value);
                }
            }
        }
        public T PreviousValue { get; set; }
        public Action<T> OnValueChanged { get; set; }

        public Cell(int index, CellType type, T value)
        {
            Index = index;
            CellType = type;
            Value = value;
            PreviousValue = value;
        }

        public static Cell<T> CreateInputCell(int index, T value) => new Cell<T>(index, CellType.Input, value);
        public static Cell<T> CreateFunctionCell(int index) => new Cell<T>(index, CellType.Function, default);

        public IDisposable Subscribe(Action<T> onChanged)
        {
            OnValueChanged += onChanged;
            return new Unsubscriber(() => OnValueChanged -= onChanged);
        }

        public override IDisposable Subscribe(Func<object, Task> onChanged)
        {
            async void Handler(T value)
            {
                await onChanged(value);
            }
            return Subscribe(new Action<T>(Handler));
        }
    }

    internal class Unsubscriber : IDisposable
    {
        private readonly Action _unsubscribe;

        public Unsubscriber(Action unsubscribe)
        {
            _unsubscribe = unsubscribe;
        }

        public void Dispose()
        {
            _unsubscribe();
        }
    }

    public enum CellType
    {
        Input,
        Function
    }
}
