using BitMagic.Common;
using BitMagic.Compiler.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BitMagic.Compiler;

internal class CommandParser
{
    private readonly Regex _firstWord = new Regex("^\\s*(?<result>([.][\\w\\-:]+))(?<line>(.*))$", RegexOptions.Compiled);

    private readonly Dictionary<string, Action<SourceFilePosition, CompileState, string>> _lineProcessor = new ();
    private Action<string, CompileState, SourceFilePosition>? _labelProcessor;

    private CommandParser()
    {
    }

    public static CommandParser Parser()
    {
        return new CommandParser();
    }

    public CommandParser WithParameters(string verb, Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, IList<string>? defaultNames = null)
    {
        _lineProcessor.Add(verb, (p, s, r) => ProcesParameters(r, p, s, action, defaultNames));
        return this;
    }

    public CommandParser WithAssignment(string verb, Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, bool hasType)
    {
        _lineProcessor.Add(verb, (p, s, r) => ProcessAssignment(r, p, s, action, hasType));
        return this;
    }

    public CommandParser WithLine(string verb, Action<SourceFilePosition, CompileState> action)
    {
        _lineProcessor.Add(verb, (p, s, r) => ProcessLine(p, s, action));
        return this;
    }

    public CommandParser WithLabel(Action<string, CompileState, SourceFilePosition> action)
    {
        _labelProcessor = action;
        return this;
    }

    public void Process(SourceFilePosition source, CompileState state)
    {
        if (string.IsNullOrEmpty(source.Source))
            return;

        var result = _firstWord.Match(source.Source);

        if (!result.Success)
        {
            throw new CompilerVerbException(source, $"Cannot find verb on line.");
        }

        var thisVerb = result.Groups["result"].Value;
        var toProcess = result.Groups["line"].Value;

        if (thisVerb.EndsWith(':'))
        {
            if (_labelProcessor == null)
                throw new Exception("Label processor is null");

            _labelProcessor(thisVerb, state, source);
            return;
        }

        if (!_lineProcessor.ContainsKey(thisVerb))
            throw new CompilerVerbException(source, $"Unknown verb '{thisVerb.Substring(1)}'");

        var map = _lineProcessor[thisVerb];

        map(source, state, toProcess);
    }

    /// <summary>
    /// An assignment or definition of a variable, eg
    /// name type [=] value
    /// .const foo $100
    /// .const foo = $100
    /// .const byte foo $100
    /// .const byte foo = CODE + 10
    /// .const byte foo CODE + 10
    /// </summary>
    /// <param name="rawParams"></param>
    /// <param name="source"></param>
    /// <param name="state"></param>
    /// <param name="action"></param>
    private static void ProcessAssignment(string rawParams, SourceFilePosition source, CompileState state, Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, bool hasType)
    {
        var idx = rawParams.IndexOf(";");
        if (idx != -1)
            rawParams = rawParams[..idx];

        idx = rawParams.IndexOf("//");
        if (idx != -1)
            rawParams = rawParams[..idx];

        rawParams = rawParams.Trim();

        idx = rawParams.IndexOf(' ');

        var paramDict = new Dictionary<string, string>();

        if (hasType)
        {
            var toAdd = rawParams[..idx].Trim();
            rawParams = rawParams[idx..].Trim();
            idx = rawParams.IndexOf(' ');

            if (idx != -1 && rawParams[..idx].Trim() == "ptr")
            {
                toAdd += " ptr";

                rawParams = rawParams[idx..].Trim();
                idx = rawParams.IndexOf(' ');
            }

            paramDict.Add("type", toAdd);
        }

        if (idx == -1)
        {
            paramDict.Add("name", rawParams.Trim());
            action(paramDict, state, source);
            return;
        }

        paramDict.Add("name", rawParams[..idx]);
        rawParams = rawParams[idx..].Trim();

        if (rawParams.StartsWith("="))
            rawParams = rawParams.Substring(1).Trim();

        paramDict.Add("value", rawParams);

        action(paramDict, state, source);
    }


    /// <summary>
    /// Processes a verb which has standard parameter values, eg
    /// .segment main $100 $200
    /// .segment main, $100, $200
    /// .segment main $100 $200 filename=main.bin
    /// .segment main, $100, $200, filename = main.bin
    /// .segment main $100 _ main.bin
    /// </summary>
    /// <param name="rawParams"></param>
    /// <param name="source"></param>
    /// <param name="state"></param>
    /// <param name="action"></param>
    /// <param name="defaultNames"></param>
    /// <exception cref="CompilerCannotParseVerbParameters"></exception>
    private static void ProcesParameters(string rawParams, SourceFilePosition source, CompileState state, Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, IList<string>? defaultNames)
    {
        var parameters = new Dictionary<string, string>();

        if (string.IsNullOrEmpty(rawParams))
        {
            action(parameters, state, source);
            return;
        }

        var defaultPos = 0;

        var seperator = rawParams.Contains(',') ? ',' : ' ';

        var thisArgs =
            rawParams.Split('"')
                     .Select((element, index) => index % 2 == 0  // If even index
                                       ? element.Split(seperator, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)  // Split the item
                                       : new string[] { element })  // Keep the entire item
                     .SelectMany(element => element).ToArray();

        for (var argsPos = 0; argsPos < thisArgs.Length; argsPos++)
        {
            if (string.IsNullOrWhiteSpace(thisArgs[argsPos]))
                continue;

            if (thisArgs[argsPos].StartsWith(';'))
                break;

            if (thisArgs[argsPos].StartsWith("//"))
                break;

            if (thisArgs[argsPos].EndsWith(','))
                thisArgs[argsPos] = thisArgs[argsPos][..^1].Trim();

            if (thisArgs[argsPos] == "_")
            {
                defaultPos++;
                continue;
            }

            var idx = thisArgs[argsPos].IndexOf(':');

            if (idx == -1)
                idx = thisArgs[argsPos].IndexOf('=');

            if (idx == -1 || defaultNames == null) // if there are no default names, then we just set as there cant be named parameter
            {
                if (defaultNames == null || defaultPos >= defaultNames.Count)
                    throw new CompilerCannotParseVerbParameters(source, $"Unknown parameter {thisArgs[argsPos]} at {source}");

                parameters.Add(defaultNames[defaultPos++], thisArgs[argsPos]);
                continue;
            }

            var name = thisArgs[argsPos][..idx].Trim();

            if (defaultNames.Contains(name))
            {
                var value = thisArgs[argsPos][(idx + 1)..].Trim();

                parameters.Add(name, value);

                continue;
            }

            parameters.Add(defaultNames[defaultPos++], thisArgs[argsPos]);
        }

        action(parameters, state, source);
    }

    private static void ProcessLine(SourceFilePosition source, CompileState state, Action<SourceFilePosition, CompileState> action) => action(source, state);

    private static void ProcessLabel(SourceFilePosition source, CompileState state, Action<SourceFilePosition, CompileState> action) => action(source, state);
}

