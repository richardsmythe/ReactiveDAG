﻿using ReactiveDAG.Core.Engine;
using ReactiveDAG.Core.Models;
using ReactiveDAG.tests.TestHelpers;

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
            Assert.Equal(CellType.Input, cell.CellType);
            int retrievedValue = await dag.GetResult<int>(cell);
            Assert.Equal(cellValue, retrievedValue);
        }

        [Fact]
        public async Task Test_Summing_Cells()
        {
            var builder = DagPipelineBuilder.Create();
            builder.AddInput(6, out var cell1)
                   .AddInput(4, out var cell2)
                   .AddFunction(
                       async inputs => (int)inputs[0] + (int)inputs[1],
                       out Cell<int> functionCell,
                       cell1, cell2
                   )
                   .Build();

            var result = await builder.GetResult<int>(functionCell);
            Assert.Equal(10, result);
        }

        [Fact]
        public async Task Test_Updating_Cell()
        {
            var dag = new DagEngine();
            var inputCell1 = dag.AddInput(4);
            var inputCell2 = dag.AddInput(4);
            var functionCell = dag.AddFunction(new BaseCell[] { inputCell1, inputCell2 },
                async inputs => (int)inputs[0] * (int)inputs[1]);
            var initialResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(16, initialResult);
            await dag.UpdateInput(inputCell1, 5);
            var updatedResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(20, updatedResult);
        }

        [Fact]
        public void Test_Cyclic_Dependency()
        {
            var dag = new DagEngine();
            var inputCell1 = dag.AddInput(4);
            var inputCell2 = dag.AddInput(4);
            var functionCell1 = dag.AddFunction(new BaseCell[] { inputCell1 },
                async inputs => (int)inputs[0] * 2
            );
            var functionCell2 = dag.AddFunction(new BaseCell[] { functionCell1 },
                async inputs => (int)inputs[0] + 10
            );
            Action createCycle = () =>
            {
                dag.AddFunction(new BaseCell[] { functionCell2 },
                    async inputs => (int)inputs[0] * 2
                );
            };
            bool isCyclic = dag.IsCyclic(functionCell2.Index, inputCell1.Index);
            Assert.True(isCyclic);
        }

        [Fact]
        public async Task Test_Chaining_Functions()
        {
            var dag = new DagEngine();
            var concatFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput("R"), dag.AddInput("S") },
                async inputs => (string)inputs[0] + inputs[1]
            );
            var additionFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput(4.5), dag.AddInput(2) },
                async inputs => (double)inputs[0] + (int)inputs[1]
            );
            var concatResult = await dag.GetResult<string>(concatFuncCell);
            var additionResult = await dag.GetResult<double>(additionFuncCell);
            Assert.Equal("RS", concatResult);
            Assert.Equal(6.5, additionResult);
        }


        [Fact]
        public async Task Test_Chaining_Multiple_Functions()
        {
            var dag = new DagEngine();
            var concatFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput("R"), dag.AddInput("S") },
                async inputs => (string)inputs[0] + inputs[1]
            );
            var sumFuncCell = dag.AddFunction(
                new BaseCell[] { dag.AddInput(10), dag.AddInput(5) },
                async inputs => (int)inputs[0] + (int)inputs[1]
            );
            var combinedFuncCell = dag.AddFunction(
                new BaseCell[] { concatFuncCell, sumFuncCell },
                async inputs => (string)inputs[0] + " " + (int)inputs[1]
            );
            var combinedResult = await dag.GetResult<string>(combinedFuncCell);
            Assert.Equal("RS 15", combinedResult);
        }

        [Fact]
        public async Task Test_Complex_Expression()
        {
            // (input1 + (input2 * input3)) - input4
            var dag = new DagEngine();
            var input1 = dag.AddInput(4);
            var input2 = dag.AddInput(3);
            var input3 = dag.AddInput(6);
            var input4 = dag.AddInput(2);

            var multFuncCell = dag.AddFunction(
                new BaseCell[] { input2, input3 },
                async inputs => (int)inputs[0] * (int)inputs[1]
            );
            var addFuncCell = dag.AddFunction(
                new BaseCell[] { input1, multFuncCell },
                async inputs => (int)inputs[0] + (int)inputs[1]
            );
            var finalFuncCell = dag.AddFunction(
                new BaseCell[] { addFuncCell, input4 },
                async inputs => (int)inputs[0] - (int)inputs[1]
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
                async inputs => (int)inputs[0] * 2
            );
            var initialResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(50, initialResult);
            Assert.False(dag.HasChanged(inputCell));
            await dag.UpdateInput(inputCell, 4);
            var updatedResult = await dag.GetResult<int>(functionCell);
            Assert.Equal(8, updatedResult);
            Assert.True(dag.HasChanged(inputCell));
        }

        [Fact]
        public void Test_Dag_Is_Acyclic()
        {
            var dag = new DagEngine();
            var input1 = dag.AddInput(1);
            var input2 = dag.AddInput(2);
            var input3 = dag.AddInput(3);
            var functionCell1 = dag.AddFunction(new BaseCell[] { input1 }, async inputs => (int)inputs[0]);
            var functionCell2 = dag.AddFunction(new BaseCell[] { input2, functionCell1 }, async inputs => (int)inputs[0] + (int)inputs[1]);
            var functionCell3 = dag.AddFunction(new BaseCell[] { input3, functionCell2 }, async inputs => (int)inputs[0] * (int)inputs[1]);
            Assert.True(functionCell1.Index < functionCell2.Index);
            Assert.True(functionCell2.Index < functionCell3.Index);
        }

        [Fact]
        public async void Test_CreateMatrix_Perform_Matrix_Addition_And_Update()
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
            }, async inputs =>
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
            await dag.UpdateInput(matrixA[0], 4.0);
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
            }, async inputs =>
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
            Assert.Equal(CellType.Input, cell.CellType);
            int retrievedValue = await dag.GetResult<int>(cell);
            Assert.Equal(cellValue, retrievedValue);
            int initialCount = dag.NodeCount;
            dag.RemoveNode(cell);
            Assert.Equal(initialCount - 1, dag.NodeCount);
        }

        [Fact]
        public async Task Test_Large_Dag_Operation()
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
                    async inputs => (int)inputs[0] + (int)inputs[1]
                );
                intermediateCells.Add(cell);
            }

            var finalCells = new List<Cell<int>>();
            for (int i = 0; i < intermediateCells.Count - 1; i += 2)
            {
                var cell = dag.AddFunction(
                    new BaseCell[] { intermediateCells[i], intermediateCells[i + 1] },
                    async inputs => (int)inputs[0] * (int)inputs[1]
                );
                finalCells.Add(cell);
            }

            var aggregateCell = dag.AddFunction(
                finalCells.Cast<BaseCell>().ToArray(),
                async inputs => inputs.Sum(input => (int)input)
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
            await dag.UpdateInput(inputCells[0], 1000);
            var updatedResult = await dag.GetResult<int>(aggregateCell);
            expectedIntermediateResults[0] = 1000 + 1;
            expectedFinalResults[0] = expectedIntermediateResults[0] * expectedIntermediateResults[1];
            var updatedExpectedFinalSum = expectedFinalResults.Sum();
            Assert.Equal(updatedExpectedFinalSum, updatedResult);
        }

        [Fact]
        public async Task Test_Creation_Of_Updatable_Covariance_Matrix()
        {
            var dag = new DagEngine();
            var heightCell = dag.AddInput(new List<double> { 60, 62, 65, 70, 72 });
            var weightCell = dag.AddInput(new List<double> { 120, 130, 150, 168, 170 });
            var ageCell = dag.AddInput(new List<double> { 25, 30, 35, 40, 45 });
            var covHeightWeightCell = dag.AddFunction(new BaseCell[] { heightCell, weightCell }, async inputs =>
            {
                var heights = (List<double>)inputs[0];
                var weights = (List<double>)inputs[1];
                return Helpers.CalculateCovariance(heights, weights);
            });
            var covHeightAgeCell = dag.AddFunction(new BaseCell[] { heightCell, ageCell }, async inputs =>
            {
                var heights = (List<double>)inputs[0];
                var ages = (List<double>)inputs[1];
                return Helpers.CalculateCovariance(heights, ages);
            });
            var covWeightAgeCell = dag.AddFunction(new BaseCell[] { weightCell, ageCell }, async inputs =>
            {
                var weights = (List<double>)inputs[0];
                var ages = (List<double>)inputs[1];
                return Helpers.CalculateCovariance(weights, ages);
            });

            var initialCovHeightWeight = await dag.GetResult<double>(covHeightWeightCell);
            var initialCovHeightAge = await dag.GetResult<double>(covHeightAgeCell);
            var initialCovWeightAge = await dag.GetResult<double>(covWeightAgeCell);
            Assert.NotEqual(0, initialCovHeightWeight);
            Assert.NotEqual(0, initialCovHeightAge);
            Assert.NotEqual(0, initialCovWeightAge);

            var updatedHeights = new List<double> { 65, 66, 68, 72, 75 };
            await dag.UpdateInput(heightCell, updatedHeights);
            var updatedCovHeightWeight = await dag.GetResult<double>(covHeightWeightCell);
            var updatedCovHeightAge = await dag.GetResult<double>(covHeightAgeCell);
            var updatedCovWeightAge = await dag.GetResult<double>(covWeightAgeCell);
            Assert.NotEqual(initialCovHeightWeight, updatedCovHeightWeight);
            Assert.NotEqual(initialCovHeightAge, updatedCovHeightAge);
            Assert.Equal(initialCovWeightAge, updatedCovWeightAge);
        }

        [Fact]
        public async Task Test_Model_VaR_Calculation_Chain_Executed_In_Parallel()
        {
            var dag = new DagEngine();
            var portfolioScenarios = new List<(List<double> historicalReturns, double portfolioValue)>
            {
                (new List<double> { 0.02, 0.03, -0.01, 0.01, 0.02, -0.02, 0.01 }, 1000000.0),
                (new List<double> { -0.01, 0.05, -0.02, 0.03, -0.01, 0.04, 0.01 }, 1500000.0),
                (new List<double> { 0.01, -0.03, 0.02, 0.04, -0.01, 0.02, 0.01 }, 2000000.0)
            };
            var confidenceLevel = 0.95;
            var tasks = new List<Task>();
            foreach (var scenario in portfolioScenarios)
            {
                var historicalReturnsCell = dag.AddInput(scenario.historicalReturns);
                var portfolioValueCell = dag.AddInput(scenario.portfolioValue);
                var confidenceLevelCell = dag.AddInput(confidenceLevel);
                var volatilityCell = dag.AddFunction(new BaseCell[] { historicalReturnsCell },
                    async inputs => Helpers.CalculateVolatility((List<double>)inputs[0]));
                var zScoreCell = dag.AddFunction(new BaseCell[] { confidenceLevelCell, historicalReturnsCell },
                    async inputs => Helpers.GetZScoreForConfidenceLevel((double)inputs[0], (List<double>)inputs[1]));
                var valueAtRiskCell = dag.AddFunction(new BaseCell[] { portfolioValueCell, volatilityCell, zScoreCell },
                    async inputs => (double)inputs[0] * (double)inputs[1] * (double)inputs[2]);
                tasks.Add(Task.Run(async () =>
                {
                    var result = await dag.GetResult<double>(valueAtRiskCell);

                    var volatility = Helpers.CalculateVolatility(scenario.historicalReturns);
                    var zScore = Helpers.GetZScoreForConfidenceLevel(confidenceLevel, scenario.historicalReturns);
                    var expectedValueAtRisk = scenario.portfolioValue * volatility * zScore;

                    Assert.Equal(expectedValueAtRisk, result, 2);
                }));
            }
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task Test_StreamResults_Yield_Values()
        {
            var dag = DagPipelineBuilder.Create()
                                 .AddInput(0, out var input)
                                 .AddFunction(async inputs => (int)inputs[0] * 2, out var result)
                                 .Build();

            using var cts = new CancellationTokenSource();
            var streamedResults = new List<int>();
            var resultStream = dag.StreamResults(result, cts.Token);
            int? lastValue = null;
            var streamingTask = Task.Run(async () =>
            {
                try
                {
                    await foreach (var r in resultStream.WithCancellation(cts.Token))
                    {
                        if (lastValue is null || r != lastValue)
                        {
                            streamedResults.Add(r);
                        }
                        lastValue = r;
                    }
                }
                catch (OperationCanceledException) { }
            });

            await Task.Delay(10);
            for (int i = 1; i <= 5; i++)
            {
                await dag.UpdateInput(input, i);
                await Task.Delay(10);
            }

            cts.Cancel();
            await streamingTask;
            var expectedResults = new List<int> { 2, 4, 6, 8, 10 };
            Assert.Equal(expectedResults, streamedResults);
        }

        [Fact]
        public async Task Test_API_Orchestration_Parallelized_WithMonitoring()
        {
            var fakeHandler = new FakeHttpMessageHandler();
            var httpClient = new HttpClient(fakeHandler);
            var apiService = new ApiService(httpClient);
            var userId = "user123";

            var dag = DagPipelineBuilder.Create()
                .AddInput(userId, out var userIdInput)
                .AddFunction(
                    async inputs =>
                    {
                        if (inputs.Length == 0) throw new ArgumentException("FetchDataAsync - No inputs received.");
                        var userIdString = inputs[0] as string;
                        if (userIdString == null) throw new InvalidCastException("FetchDataAsync - Expected string input.");
                        var userDetails = await apiService.FetchDataAsync(userIdString);
                        return userDetails;
                    },
                    out var userDetailsCell)
                .AddFunction(
                    async inputs =>
                    {
                        if (inputs.Length == 0) throw new ArgumentException("GetUserPostsAsync - No inputs received.");
                        if (!(inputs[0] is UserDetails userDetails)) throw new InvalidCastException("GetUserPostsAsync - Expected UserDetails input.");
                        return await apiService.GetUserPostsAsync(userDetails.UserId!);
                    },
                    out var userPostsCell)
                .AddFunction(
                    async inputs =>
                    {
                        if (inputs.Length == 0) throw new ArgumentException("ProcessUserPostsConcurrently - No inputs received.");
                        if (!(inputs[0] is UserPosts userPosts)) throw new InvalidCastException("ProcessUserPostsConcurrently - Expected UserPosts input.");
                        var processedPosts = await Helpers.ProcessUserPosts(userPosts);
                        Console.WriteLine("Processed Posts (Uppercased Titles):");
                        foreach (var post in processedPosts)
                        {
                            Console.WriteLine($"- {post.Title}");
                        }
                        return processedPosts;
                    },
                    out var processedPostsCell)
                .AddFunction(
                    async inputs =>
                    {
                        if (inputs.Length == 0) throw new ArgumentException("FetchAdditionalDataAsync - No inputs received.");
                        if (!(inputs[0] is List<Post> processedPosts)) throw new InvalidCastException("FetchAdditionalDataAsync - Expected List<Post> input.");
                        return await Helpers.FetchAdditionalDataAsync(processedPosts);
                    },
                    out var additionalDataCell)
                .AddFunction(new BaseCell[] { userDetailsCell, userPostsCell, processedPostsCell, additionalDataCell },
                    async inputs =>
                    {
                        if (!(inputs[0] is UserDetails userDetails) ||
                            !(inputs[1] is UserPosts userPosts) ||
                            !(inputs[2] is List<Post> processedPosts) ||
                            !(inputs[3] is AdditionalData additionalData))
                        {
                            throw new InvalidCastException("AggregateResults - One or more inputs have incorrect types.");
                        }
                        var result = Helpers.AggregateResults(userDetails, userPosts, processedPosts, additionalData);
                        return result;
                    },
                    out var finalResultCell)
                .Build();

            var tasks = new List<Task>
            {
                dag.GetResult<UserDetails>(userDetailsCell),
                dag.GetResult<UserPosts>(userPostsCell),
                dag.GetResult<List<Post>>(processedPostsCell),
                dag.GetResult<AdditionalData>(additionalDataCell)
            };
            await Task.WhenAll(tasks);

            var userDetailsNode = dag.GetNode(userDetailsCell);
            var userPostsNode = dag.GetNode(userPostsCell);
            var processedPostsNode = dag.GetNode(processedPostsCell);
            var additionalDataNode = dag.GetNode(additionalDataCell);
            var finalResultNode = dag.GetNode(finalResultCell);

            if (userDetailsNode.Status == NodeStatus.Completed &&
                userPostsNode.Status == NodeStatus.Completed &&
                processedPostsNode.Status == NodeStatus.Completed &&
                additionalDataNode.Status == NodeStatus.Completed)
            {
                var finalResult = await dag.GetResult<FinalResult>(finalResultCell);
                Console.WriteLine($"FinalResult: UserId={finalResult.UserId}, PostCount={finalResult.PostCount}, AdditionalDataProcessed={finalResult.AdditionalDataProcessed}");

                Assert.NotNull(finalResult);
                Assert.Equal("user123", finalResult.UserId);
                Assert.True(finalResult.PostCount > 0);
                Assert.True(finalResult.AdditionalDataProcessed);
            }
            else
            {
                Assert.Fail("Not all nodes have completed yet.");
            }
        }

        [Fact]
        public void Test_DagEngine_ToJson_ExportsGraphStructure()
        {
            var dag = new DagEngine();
            var a = dag.AddInput(1);
            var b = dag.AddInput(2);
            var sum = dag.AddFunction(new BaseCell[] { a, b }, async inputs => await Task.Run(() => (int)inputs[0] + (int)inputs[1]));
            string json = dag.ToJson();
            Assert.Contains("\"Index\":", json);
            Assert.Contains("\"Type\":", json);
            Assert.Contains("\"Dependencies\":", json);
            Assert.Contains("\"Value\":", json);
            Assert.Contains("1", json);
            Assert.Contains("2", json);
        }
              
        [Fact]
        public async Task Test_AddFunction_With_Mixed_Cell_Types()
        {
            var builder = DagPipelineBuilder.Create();
            builder.AddInput(42, out var intCell);
            builder.AddInput(3.14, out var doubleCell);
            builder.AddInput("hello", out var stringCell);
            builder.AddFunction(
                async inputs =>
                {
                    int i = (int)inputs[0];
                    double d = (double)inputs[1];
                    string s = (string)inputs[2];
                    return $"{i}-{d}-{s}";
                },
                out Cell<string> resultCell,
                intCell, doubleCell, stringCell
            ).Build();
            var result = await builder.GetResult<string>(resultCell);
            Assert.Equal("42-3.14-hello", result);
        }
    }
}
