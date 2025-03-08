using System;
using System.Linq;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Sparkitect.Benchmark;

[DisassemblyDiagnoser(exportDiff: true, exportHtml: true, printSource: true, exportCombinedDisassemblyReport: true)]
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class RandomMaskComp
{
    [Params(10000, 100000)]
    public int InputSize { get; set; }

    [Params(100, 1000)]
    public int MaskSize { get; set; }

    [Params(0, 10, 100)]
    public int Padding { get; set; }

    [Params(0.1, 0.5, 0.9)]
    public double HitRate { get; set; }

    private ulong[] input;
    private ulong[] mask;
    private ulong[] paddedInput;

    [GlobalSetup]
    public void Setup()
    {
        var random = new Random(42);
        input = new ulong[InputSize];
        mask = new ulong[MaskSize];
            
        for (int i = 0; i < InputSize; i++)
        {
            input[i] = ((ulong)random.NextInt64() << 32) | (uint)random.NextInt64();
        }

        for (int i = 0; i < MaskSize; i++)
        {
            mask[i] = ((ulong)random.NextInt64() << 32) | (uint)random.NextInt64();
        }

        // Ensure hit rate
        int hitsNeeded = (int)(InputSize * HitRate);
        for (int i = 0; i < hitsNeeded; i++)
        {
            input[i] = mask[i % MaskSize];
        }

        // Shuffle input
        for (int i = input.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (input[i], input[j]) = (input[j], input[i]);
        }

        paddedInput = CreatePaddedInput(input, Padding);
    }

    private ulong[] CreatePaddedInput(ulong[] originalInput, int padding)
    {
        if (padding == 0) return originalInput;

        ulong[] paddedInput = new ulong[originalInput.Length * (padding + 1)];
        for (int i = 0; i < originalInput.Length; i++)
        {
            paddedInput[i * (padding + 1)] = originalInput[i];
        }
        return paddedInput;
    }

    [Benchmark]
    public ulong[] LinearSearch() => VectorComparisonLinearSearch(paddedInput, mask);

    [Benchmark]
    public ulong[] BinarySearch() => VectorComparisonBinarySearch(paddedInput, mask);

    [Benchmark]
    public ulong[] DictionaryLookup() => VectorComparisonDictionary(paddedInput, mask);

    private static ulong[] VectorComparisonLinearSearch(ulong[] input, ulong[] mask)
    {
        ulong[] result = new ulong[input.Length];
        Span<ulong> inputSpan = input;
        Span<ulong> maskSpan = mask;
        Span<ulong> resultSpan = result;

        for (int i = 0; i < inputSpan.Length; i++)
        {
            ulong value = inputSpan[i];
            int index = -1;
            for (int j = 0; j < maskSpan.Length; j++)
            {
                if (maskSpan[j] == value)
                {
                    index = j;
                    break;
                }
            }
            resultSpan[i] = index == -1 ? value : (ulong)index;
        }

        return result;
    }

    private static ulong[] VectorComparisonBinarySearch(ulong[] input, ulong[] mask)
    {
        ulong[] result = new ulong[input.Length];
        Span<ulong> inputSpan = input;
        Span<ulong> resultSpan = result;

        // Sort mask for binary search
        Array.Sort(mask);
        Span<ulong> maskSpan = mask;

        for (int i = 0; i < inputSpan.Length; i++)
        {
            ulong value = inputSpan[i];
            int index = maskSpan.BinarySearch(value);
            resultSpan[i] = index < 0 ? value : (ulong)index;
        }

        return result;
    }

    private static ulong[] VectorComparisonDictionary(ulong[] input, ulong[] mask)
    {
        ulong[] result = new ulong[input.Length];
        Dictionary<ulong, int> maskDict = new Dictionary<ulong, int>(mask.Length);
            
        for (int i = 0; i < mask.Length; i++)
        {
            maskDict[mask[i]] = i;
        }

        Span<ulong> inputSpan = input;
        Span<ulong> resultSpan = result;

        for (int i = 0; i < inputSpan.Length; i++)
        {
            ulong value = inputSpan[i];
            resultSpan[i] = maskDict.TryGetValue(value, out int index) ? (ulong)index : value;
        }

        return result;
    }
}