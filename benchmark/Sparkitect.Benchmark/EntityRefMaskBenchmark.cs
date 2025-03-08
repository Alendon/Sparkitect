using System;
using System.Linq;
using System.Numerics;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Sparkitect.Benchmark;

[DisassemblyDiagnoser(exportDiff: true, exportHtml: true, printSource: true, exportCombinedDisassemblyReport: true)]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, baseline: true)]
[SimpleJob(RuntimeMoniker.Net90)]
public class EntityRefMaskBenchmark
{
    [Params(100000)] 
    public int InputSize { get; set; }

    [Params(100)] 
    public int MaskSize { get; set; }
    
    [Params(0, 10, 100)]
    public int Padding { get; set; }

    private int[] input;
    private int[] mask;
    private int[] paddedInput;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42); // Fixed seed for reproducibility
        input = Enumerable.Range(0, InputSize).Select(_ => random.Next(0, InputSize)).ToArray();
        mask = Enumerable.Range(0, MaskSize).Select(_ => random.Next(0, InputSize)).Distinct().ToArray();
        paddedInput = CreatePaddedInput(input, Padding);
    }

    private int[] CreatePaddedInput(int[] originalInput, int padding)
    {
        if (padding == 0) return originalInput;

        int[] paddedInput = new int[originalInput.Length * (padding + 1)];
        paddedInput.AsSpan().Fill(-1);
        
        for (int i = 0; i < originalInput.Length; i++)
        {
            paddedInput[i * (padding + 1)] = originalInput[i];
        }
        return paddedInput;
    }

    [Benchmark]
    public int[] LinqImplementation() => SpecialVectorComparisonLINQ(paddedInput, mask);

    [Benchmark]
    public int[] FirstSIMDImplementation() => SpecialVectorComparisonSIMD(paddedInput, mask);

    [Benchmark]
    public int[] OptimizedSIMDImplementation() => SpecialVectorComparisonSIMDOptimized(paddedInput, mask);

    // LINQ-based implementation
    private static int[] SpecialVectorComparisonLINQ(int[] input, int[] mask)
    {
        var maskSet = new HashSet<int>(mask);
        var maskIndexDict = mask.Select((value, index) => new { value, index })
            .ToDictionary(x => x.value, x => x.index);
        return input.Select(x => maskSet.Contains(x) ? maskIndexDict[x] : -1).ToArray();
    }

    // First SIMD implementation
    private static int[] SpecialVectorComparisonSIMD(int[] input, int[] mask)
    {
        int maxMaskValue = mask.Max() + 1;
        int[] maskLookup = new int[maxMaskValue];
        Array.Fill(maskLookup, -1);

        Span<int> inputSpan = input;
        Span<int> maskSpan = mask;
        Span<int> maskLookupSpan = maskLookup;

        for (var i = 0; i < maskSpan.Length; i++)
        {
            maskLookupSpan[maskSpan[i]] = i;
        }

        var inputLength = input.Length;


        for (int j = 0; j < inputLength; j++)
        {
            int value = inputSpan[j];
            if (value < maxMaskValue && value >= 0)
            {
                inputSpan[j] = maskLookupSpan[value];
            }
        }
        
        return input;
    }

    // Optimized SIMD implementation
    private static int[] SpecialVectorComparisonSIMDOptimized(int[] input, int[] mask)
    {
        int[] result = new int[input.Length];
        int vectorSize = Vector<int>.Count;

        Vector<int> minusOneVector = new Vector<int>(-1);

        int i = 0;
        for (; i <= input.Length - vectorSize; i += vectorSize)
        {
            Vector<int> inputVector = new Vector<int>(input, i);
            Vector<int> resultVector = minusOneVector;

            for (int j = 0; j < mask.Length; j++)
            {
                Vector<int> maskVector = new Vector<int>(mask[j]);
                Vector<int> comparisonVector = Vector.Equals(inputVector, maskVector);
                Vector<int> indexVector = new Vector<int>(j);
                resultVector = Vector.ConditionalSelect(comparisonVector, indexVector, resultVector);
            }

            resultVector.CopyTo(result, i);
        }

        for (; i < input.Length; i++)
        {
            result[i] = Array.IndexOf(mask, input[i]);
        }

        return result;
    }
}