using Sake;
using System;
using System.Linq;

var inputCount = 300;
var userCount = 100;
var remixRatio = 0.3;

var runs = 25;
var results = new List<(decimal inputAmount, decimal outputAmount, int outputCount, decimal fee, long size, decimal avgAnonSetGain, decimal avgInputsAnonSetGain, decimal avgOutputAnonSetGain, decimal blockEfficiency, decimal privacyEfficiency, int nonStandardOutputs)>(runs);

for (var i = 0; i < runs; i++)
{
    Console.Write(".");

    var preRandomAmounts = Sample.Amounts.RandomElements(inputCount);
    var preGroups = preRandomAmounts.RandomGroups(userCount);

    IMixer preMixer = new DecomposeMixer();
    var preMix = preMixer.CompleteMix(preGroups);

    var remixCount = (int)(inputCount * remixRatio);
    var randomAmounts = Sample.Amounts.RandomElements(inputCount - remixCount).Concat(preMix.SelectMany(x => x).RandomElements(remixCount));
    var inputGroups = randomAmounts.RandomGroups(userCount).ToArray();
    IMixer mixer = new DecomposeMixer();
    var outputGroups = mixer.CompleteMix(inputGroups).Select(x => x.ToArray()).ToArray();

    if (inputGroups.SelectMany(x => x).Sum() <= outputGroups.SelectMany(x => x).Sum())
    {
        throw new InvalidOperationException("Bug. Transaction doesn't pay fees.");
    }

    var outputCount = outputGroups.Sum(x => x.Length);
    var inputAmount = inputGroups.SelectMany(x => x).Sum();
    var outputAmount = outputGroups.SelectMany(x => x).Sum();
    var fee = inputAmount - outputAmount;
    var size = inputCount * mixer.InputSize + outputCount * mixer.OutputSize;
    var feeRate = (fee / size).ToSats();
    results.Add((inputAmount, outputAmount, outputCount, fee, size,
        Analyzer.AverageAnonsetGain(inputGroups, outputGroups),
        Analyzer.AverageAnonsetGain(inputGroups),
        Analyzer.AverageAnonsetGain(outputGroups),
        Analyzer.BlockspaceEfficiency(inputGroups, outputGroups, size),
        Analyzer.PrivacyEfficiency(inputGroups, outputGroups, fee),
        Analyzer.NonStandardOutputs(outputGroups, mixer.Denominations)));
}

Console.WriteLine();
Console.WriteLine($"Number of users:\t{userCount}");
Console.WriteLine($"Number of inputs:\t{inputCount}");
Console.WriteLine();
Console.WriteLine("----------------------------------------------------------------------------------------------------------------------------------------------------");
Console.WriteLine($"{"Input Amount",14} {"Output Amount",14} {"Outputs",10} {"Fee Paid",10} {"Tx.",10} {"Avg.",14} {"Avg. Inputs",14} {"Avg. Outputs",14} {"Block",14} {"Privacy",14} {"Non-Std",10}");
Console.WriteLine($"{"",14} {"",14} {"",10} {"",10} {"Size",10} {"Anonset Gain",14} {"Anonset Gain",14} {"Anonset Gain",14} {"Efficiency",14} {"Efficiency",14} {"Outputs",10}");
Console.WriteLine("-------------- -------------- ---------- ---------- ---------- -------------- -------------- -------------- -------------- -------------- ----------");

foreach (var r in results)
{
    Console.WriteLine($"{r.inputAmount,14} {r.outputAmount,14} {r.outputCount,10} {r.fee,10:0.########} {r.size,10} {r.avgAnonSetGain,14:0.##} {r.avgInputsAnonSetGain,14:0.##} {r.avgOutputAnonSetGain,14:0.##} {r.blockEfficiency,14:0.##} {r.privacyEfficiency,14:0.##} {r.nonStandardOutputs,10}");
}
Console.WriteLine("-------------- -------------- ---------- ---------- ---------- -------------- -------------- -------------- -------------- -------------- ----------");
Console.WriteLine($"{"",14} {"Min:",14} {results.Min(x=>x.outputCount),10} {results.Min(x=>x.fee),10:0.########} {results.Min(x=>x.size),10} {results.Min(x=>x.avgAnonSetGain),14:0.##} {results.Min(x=>x.avgInputsAnonSetGain),14:0.##} {results.Min(x=>x.avgOutputAnonSetGain),14:0.##} {results.Min(x=>x.blockEfficiency),14:0.##} {results.Min(x=>x.privacyEfficiency),14:0.##} {results.Min(x=>x.nonStandardOutputs),10:0.##}");
Console.WriteLine($"{"",14} {"Max:",14} {results.Max(x=>x.outputCount),10} {results.Max(x=>x.fee),10:0.########} {results.Max(x=>x.size),10} {results.Max(x=>x.avgAnonSetGain),14:0.##} {results.Max(x=>x.avgInputsAnonSetGain),14:0.##} {results.Max(x=>x.avgOutputAnonSetGain),14:0.##} {results.Max(x=>x.blockEfficiency),14:0.##} {results.Max(x=>x.privacyEfficiency),14:0.##} {results.Max(x=>x.nonStandardOutputs),10:0.##}");
Console.WriteLine($"{"",14} {"Average:",14} {results.Average(x=>x.outputCount),10} {results.Average(x=>x.fee),10:0.########} {results.Average(x=>x.size),10} {results.Average(x=>x.avgAnonSetGain),14:0.##} {results.Average(x=>x.avgInputsAnonSetGain),14:0.##} {results.Average(x=>x.avgOutputAnonSetGain),14:0.##} {results.Average(x=>x.blockEfficiency),14:0.##} {results.Average(x=>x.privacyEfficiency),14:0.##} {results.Average(x=>x.nonStandardOutputs),10:0.##}");