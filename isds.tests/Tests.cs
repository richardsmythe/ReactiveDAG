using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;
using ReactiveDAG.Services;

namespace ReactiveDAG.tests
{
    public class Tests
    {
        [Fact]
        public async Task Test_AddInput()
        {
            int cellValue = 6;
            var dag = new DagEngine();
            var cell = dag.AddInput(cellValue);
            Assert.Equal(CellType.Input, cell.Type);
            int retrievedValue = await dag.GetResult<int>(cell);
            Assert.Equal(cellValue, retrievedValue);
        }

        [Fact]
        public async Task Test_Summing_Cells()
        {
            var dag = new DagEngine();
            var inputCells = new BaseCell[] { dag.AddInput(6), dag.AddInput(4) };
            var functionCell = dag.AddFunction(inputCells,
                inputs => (int)inputs[0] + (int)inputs[1]
            );
            var result = await dag.GetResult<int>(functionCell);
            Assert.Equal(10, result);
        }

        [Fact]
        public async Task Test_ScheduledFunction_RunOnce_True()
        {
            var startTime = DateTime.UtcNow;
            var interval = 2;
            var builder = Builder.Create()
                .AddInput(6.2, out var cell1)
                .AddInput(4.2, out var cell2)
                .AddScheduledFunction(inputs =>
                {
                    var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
                    return sum;
                }, TimeSpan.FromSeconds(interval), runOnce: true, out var resultCell)
                .Build();
            await Task.Delay(TimeSpan.FromSeconds(interval));
            var resultValue = await builder.GetResult<double>(resultCell);
            var endTime = DateTime.UtcNow;
            var elapsedTime = (endTime - startTime).TotalSeconds;
            Assert.True(elapsedTime >= 1.75 && elapsedTime <= 2.5);
            Assert.Equal(10.4, resultValue, precision: 1);
        }

        [Fact]
        public async Task Test_ScheduledFunction_RunOnce_False()
        {
            var interval = 5;
            var results = new List<double>();
            var builder2 = Builder.Create()
           .AddInput(2, out var cell4)
           .AddInput(2, out var cell5)
           .AddInput(2, out var cell6)
           .AddScheduledFunction(inputs =>
           {
               var sum = inputs.Select(i => Convert.ToDouble(i)).Sum();
               results.Add(sum);
               return sum;
           }, TimeSpan.FromSeconds(interval),
           runOnce: false, out var resultCell)
           .Build();            
            var initialResult = await builder2.GetResult<double>(resultCell);
            Assert.Equal(6, initialResult);
            await Task.Delay(TimeSpan.FromSeconds(interval));
            Assert.True(results.Count >=2);
            Assert.Equal(6, results[0]);            
            builder2.UpdateInput(cell4, 10);
            await Task.Delay(TimeSpan.FromSeconds(interval + 1));          
            Assert.Equal(14, results[results.Count - 1]);
        }


        [Fact]
        public async Task Test_Updating_Cell()
        {
            var dag = new DagEngine();
            var inputCell = dag.AddInput(4);
            var functionCell = dag.AddFunction(new BaseCell[] { inputCell, inputCell },
                inputs => (int)inputs[0] * (int)inputs[1]
            );
            var initialResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(16, initialResult);

            dag.UpdateInput(inputCell, 5);
            var updatedResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(25, updatedResult);
        }

        [Fact]
        public async Task Test_ChainingFunctions()
        {
            var dag = new DagEngine();
            var concatFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput("R"), dag.AddInput("S") },
                inputs => (string)inputs[0] + inputs[1]
            );
            var additionFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput(4.5), dag.AddInput(2) },
                inputs => (double)inputs[0] + (int)inputs[1]
            );
            var concatResult = await dag.GetResult<string>(concatFuncCell);
            var additionResult = await dag.GetResult<double>(additionFuncCell);
            Assert.Equal("RS", concatResult);
            Assert.Equal(6.5, additionResult);
        }

        [Fact]
        public async Task Test_ChainingMultipleFunctions()
        {
            var dag = new DagEngine();
            var concatFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput("R"), dag.AddInput("S") },
                inputs => (string)inputs[0] + inputs[1]
            );
            var sumFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput(10), dag.AddInput(5) },
                inputs => (int)inputs[0] + (int)inputs[1]
            );
            var combinedFuncCell = dag.AddFunction(
                new BaseCell[] { concatFuncCell, sumFuncCell },
                inputs => (string)inputs[0] + " " + (int)inputs[1]
            );
            var combinedResult = await dag.GetResult<string>(combinedFuncCell);
            Assert.Equal("RS 15", combinedResult);
        }

        [Fact]
        public async Task Test_ComplexExpression()
        {
            // (input1 + (input2 * input3)) - input4
            var dag = new DagEngine();
            var input1 = dag.AddInput(4);
            var input2 = dag.AddInput(3);
            var input3 = dag.AddInput(6);
            var input4 = dag.AddInput(2);

            var multFuncCell = dag.AddFunction(
                new BaseCell[] { input2, input3 },
                inputs => (int)inputs[0] * (int)inputs[1]
            );
            var addFuncCell = dag.AddFunction(
                new BaseCell[] { input1, multFuncCell },
                inputs => (int)inputs[0] + (int)inputs[1]
            );
            var finalFuncCell = dag.AddFunction(
                new BaseCell[] { addFuncCell, input4 },
                inputs => (int)inputs[0] - (int)inputs[1]
            );

            var result = await dag.GetResult<int>(finalFuncCell);
            Assert.Equal(20, result);
        }

        [Fact]
        public async Task Test_HasChanged()
        {
            var dag = new DagEngine();
            var inputCell = dag.AddInput(25);
            var functionCell = dag.AddFunction(
                new BaseCell[] { inputCell },
                inputs => (int)inputs[0] * 2
            );
            var initialResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(50, initialResult);
            Assert.False(DagUtils.HasChanged(inputCell));
            dag.UpdateInput(inputCell, 4);
            var updatedResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(8, updatedResult);
            Assert.True(DagUtils.HasChanged(inputCell));
        }

        [Fact]
        public void Test_DagIsAcyclic()
        {
            var dag = new DagEngine();
            var input1 = dag.AddInput(1);
            var input2 = dag.AddInput(2);
            var input3 = dag.AddInput(3);
            var functionCell1 = dag.AddFunction(new BaseCell[] { input1 }, inputs => (int)inputs[0]);
            var functionCell2 = dag.AddFunction(new BaseCell[] { input2, functionCell1 }, inputs => (int)inputs[0] + (int)inputs[1]);
            var functionCell3 = dag.AddFunction(new BaseCell[] { input3, functionCell2 }, inputs => (int)inputs[0] * (int)inputs[1]);
            Assert.True(functionCell1.Index < functionCell2.Index);
            Assert.True(functionCell2.Index < functionCell3.Index);
        }

        [Fact]
        public async void Test_CreateMatrix_Perform_MatrixAdditionAndUpdate()
        {
            var dag = new DagEngine();
            var matrixA = new[]
            {
                dag.AddInput(3.0), dag.AddInput(8.0),
                dag.AddInput(4.0), dag.AddInput(6.0)
            };
            var matrixB = new[]
            {
                dag.AddInput(4.0), dag.AddInput(0.0),
                dag.AddInput(1.0), dag.AddInput(-9.0)
            };

            var matrixAdditionFunctionCell = dag.AddFunction(new BaseCell[]
            {
                matrixA[0], matrixA[1], matrixA[2], matrixA[3],
                matrixB[0], matrixB[1], matrixB[2], matrixB[3]
            }, inputs =>
            {
                double[] A = inputs.Take(4).Select(i => Convert.ToDouble(i)).ToArray();
                double[] B = inputs.Skip(4).Select(i => Convert.ToDouble(i)).ToArray();

                return new double[]
                {
                    A[0] + B[0], // 3.0 + 4.0  = 7.0
                    A[1] + B[1], // 8.0 + 0.0  = 8.0
                    A[2] + B[2], // 4.0 + 1.0  = 5.0
                    A[3] + B[3]  // 6.0 + -9.0 = -3.0
                };
            });
            var result = await dag.GetResult<double[]>(matrixAdditionFunctionCell);
            var expectedResult = new double[] { 7.0, 8.0, 5.0, -3.0 };
            Assert.Equal(expectedResult, result);
            dag.UpdateInput(matrixA[0], 4.0);
            var updatedResult = await dag.GetResult<double[]>(matrixAdditionFunctionCell);
            var expectedUpdate = new double[] { 8.0, 8.0, 5.0, -3.0 };
            Assert.Equal(expectedUpdate, updatedResult);
        }

        [Fact]
        public async void Test_CreateMatrix_GetDeterminant()
        {
            var dag = new DagEngine();
            var matrixA = new[]
            {
                dag.AddInput(3.0), dag.AddInput(8.0),
                dag.AddInput(4.0), dag.AddInput(6.0)
            };
            var matrixDeterminantFunctionCell = dag.AddFunction(new BaseCell[]
            {
                matrixA[0], matrixA[1], matrixA[2], matrixA[3]
            }, inputs =>
            {
                var A = inputs.Take(4).Select(i => Convert.ToDouble(i)).ToArray();
                return new double[]
                {
                    A[0] * A[3] - A[1] * A[2]
                };
            });
            var result = await dag.GetResult<double[]>(matrixDeterminantFunctionCell);
            Assert.Equal(-14.0, result[0]);
        }

        [Fact]
        public async Task Test_RemoveNode()
        {
            int cellValue = 6;
            var dag = new DagEngine();
            var cell = dag.AddInput(cellValue);
            Assert.Equal(CellType.Input, cell.Type);
            int retrievedValue = await dag.GetResult<int>(cell);
            Assert.Equal(cellValue, retrievedValue);
            int initialCount = dag.NodeCount;
            dag.RemoveNode(cell);
            Assert.Equal(initialCount - 1, dag.NodeCount);
        }

        [Fact]
        public async Task Test_LargeDagOperation()
        {
            var dag = new DagEngine();
            int inputCount = 1000;
            var inputCells = new List<Cell<int>>();
            for (int i = 0; i < inputCount; i++)
            {
                inputCells.Add(dag.AddInput(i));
            }

            var intermediateCells = new List<Cell<int>>();
            for (int i = 0; i < inputCount - 1; i += 2)
            {
                var cell = dag.AddFunction(
                    new BaseCell[] { inputCells[i], inputCells[i + 1] },
                    inputs => (int)inputs[0] + (int)inputs[1]
                );
                intermediateCells.Add(cell);
            }

            var finalCells = new List<Cell<int>>();
            for (int i = 0; i < intermediateCells.Count - 1; i += 2)
            {
                var cell = dag.AddFunction(
                    new BaseCell[] { intermediateCells[i], intermediateCells[i + 1] },
                    inputs => (int)inputs[0] * (int)inputs[1]
                );
                finalCells.Add(cell);
            }

            var aggregateCell = dag.AddFunction(
                finalCells.Cast<BaseCell>().ToArray(),
                inputs => inputs.Sum(input => (int)input)
            );
            var finalResult = await dag.GetResult<int>(aggregateCell);
            var expectedSum = Enumerable.Range(0, inputCount).Sum();
            var expectedIntermediateResults = new List<int>();

            for (int i = 0; i < inputCount - 1; i += 2)
            {
                expectedIntermediateResults.Add(i + (i + 1));
            }

            var expectedFinalResults = new List<int>();
            for (int i = 0; i < expectedIntermediateResults.Count - 1; i += 2)
            {
                expectedFinalResults.Add(expectedIntermediateResults[i] * expectedIntermediateResults[i + 1]);
            }

            var expectedFinalSum = expectedFinalResults.Sum();

            Assert.Equal(expectedFinalSum, finalResult);
            dag.UpdateInput(inputCells[0], 1000);
            var updatedResult = await dag.GetResult<int>(aggregateCell);
            expectedIntermediateResults[0] = 1000 + 1;
            expectedFinalResults[0] = expectedIntermediateResults[0] * expectedIntermediateResults[1];
            var updatedExpectedFinalSum = expectedFinalResults.Sum();
            Assert.Equal(updatedExpectedFinalSum, updatedResult);
        }
    }
}
