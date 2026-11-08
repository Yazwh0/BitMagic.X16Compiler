using BitMagic.Common;
using BitMagic.Compiler.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BitMagic.Compiler;

internal class CommandParser
{
    private readonly Regex _firstWord = new Regex("^\\s*(?<result>([.][\\w\\-:]+))(?<line>(.*))$", RegexOptions.Compiled);

    private readonly Dictionary<string, Action<SourceFilePosition, CompileState, string>> _lineProcessor = new ();
    private readonly Dictionary<string, IList<string>> _parameterNames = new();
    private Action<string, CompileState, SourceFilePosition> _labelProcessor;

    /// <summary>
    /// A verb's parameters in order, eg 'name, address, maxsize, filename, scope' for '.segment'. Empty if it has none.
    /// </summary>
    public IList<string> ParameterNames(string verb) => _parameterNames.TryGetValue(verb, out var names) ? names : [];

    private CommandParser()
    {
    }

    public static CommandParser Parser()
    {
        return new CommandParser();
    }

    /// <param name="allowVisibility">The verb takes `public` or `private` as its first word, passed on as dict["visibility"].</param>
    public CommandParser WithParameters(string verb, Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, IList<string> defaultNames = null, bool allowVisibility = false)
    {
        _parameterNames[verb] = defaultNames ?? [];
        _lineProcessor.Add(verb, (p, s, r) =>
        {
            var withVisibility = WithVisibility(action, r, allowVisibility, out var rest);
            ProcesParameters(verb, rest, p, s, withVisibility, defaultNames ?? []);
        });
        return this;
    }

    /// <param name="allowVisibility">The verb takes `public` or `private` as its first word, passed on as dict["visibility"].</param>
    public CommandParser WithAssignment(string verb, Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, bool hasType, bool allowVisibility = false)
    {
        _lineProcessor.Add(verb, (p, s, r) =>
        {
            var withVisibility = WithVisibility(action, r, allowVisibility, out var rest);
            ProcessAssignment(rest, p, s, withVisibility, hasType);
        });
        return this;
    }

    // Takes a leading `public` / `private` off the parameters, and wraps the action so it is
    // passed on in the dictionary.
    private static Action<IDictionary<string, string>, CompileState, SourceFilePosition> WithVisibility(
        Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, string rawParams, bool allowVisibility, out string rest)
    {
        rest = rawParams;

        if (!allowVisibility || rawParams == null)
            return action;

        var trimmed = rawParams.TrimStart();
        foreach (var keyword in Variables.ReservedNames)
        {
            if (trimmed == keyword || trimmed.StartsWith(keyword + " ") || trimmed.StartsWith(keyword + "\t"))
            {
                rest = trimmed[keyword.Length..];
                return (dict, state, source) =>
                {
                    dict["visibility"] = keyword;
                    action(dict, state, source);
                };
            }
        }

        return action;
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
                throw new GeneralCompilerException(source, $"Cannot process label '{thisVerb}', no label processor is set.");

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
    /// .const foo=$100
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

        // the name ends at a space or an '=', so 'foo=$100' is the same as 'foo = $100'
        idx = rawParams.IndexOfAny([' ', '\t', '=']);

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
    /// .segment main $100 _ "main file.bin"
    /// Values are in the verb's order, '_' skips one, and 'name=value' (or 'name:value') gives one by name.
    /// </summary>
    /// <exception cref="CompilerCannotParseVerbParameters"></exception>
    private static void ProcesParameters(string verb, string rawParams, SourceFilePosition source, CompileState state, Action<IDictionary<string, string>, CompileState, SourceFilePosition> action, IList<string> names)
    {
        var parameters = new Dictionary<string, string>();
        var position = 0;

        foreach (var token in Tokenise(rawParams ?? ""))
        {
            if (token == "_")
            {
                position++;
                continue;
            }

            var (name, value) = SplitNamed(verb, token, names, source);

            if (name == null)
            {
                // an empty "" isn't a value, as before
                if (value.Length == 0)
                    continue;

                if (names.Count == 0)
                    throw new CompilerCannotParseVerbParameters(source, $"'{verb}' doesn't take any parameters, but has '{token}'.");

                if (position >= names.Count)
                    throw new CompilerCannotParseVerbParameters(source, $"'{verb}' takes {string.Join(", ", names)}, so '{token}' is one too many.");

                name = names[position++];

                if (parameters.ContainsKey(name))
                    throw new CompilerCannotParseVerbParameters(source, $"'{verb}' has '{name}' twice: '{token}' is in its place, but it's already given by name.");
            }
            else if (parameters.ContainsKey(name))
            {
                throw new CompilerCannotParseVerbParameters(source, $"'{verb}' has '{name}' twice.");
            }

            parameters[name] = value;
        }

        action(parameters, state, source);
    }

    private static readonly Regex _word = new("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

    /// <summary>
    /// A token as a named parameter, or (null, value) if it's positional. Quotes are taken off the value.
    /// </summary>
    private static (string Name, string Value) SplitNamed(string verb, string token, IList<string> names, SourceFilePosition source)
    {
        // a quoted value is never named, eg "C:\x.bin"
        if (token.StartsWith('"'))
            return (null, Unquote(token));

        var idx = token.IndexOfAny(['=', ':']);

        if (idx > 0)
        {
            var name = token[..idx];

            if (names.Contains(name))
                return (name, Unquote(token[(idx + 1)..]));

            // ':' isn't checked, as it's in values such as 'C:\x.bin' that '.map' is given
            if (token[idx] == '=' && _word.IsMatch(name))
                throw new CompilerCannotParseVerbParameters(source, names.Count == 0 ?
                    $"'{verb}' doesn't take any parameters, but has '{name}'." :
                    $"'{verb}' has no parameter '{name}'. It takes {string.Join(", ", names)}.");
        }

        return (null, Unquote(token));
    }

    private static string Unquote(string value) =>
        value.Length >= 2 && value.StartsWith('"') && value.EndsWith('"') ? value[1..^1] : value;

    /// <summary>
    /// Splits parameters on spaces and commas, up to a ';' or '//' comment. A quoted value is kept whole, with its
    /// quotes, and 'name = value' is joined into 'name=value'.
    /// </summary>
    private static List<string> Tokenise(string rawParams)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        void Flush()
        {
            if (current.Length != 0)
                tokens.Add(current.ToString());

            current.Clear();
        }

        for (var i = 0; i < rawParams.Length; i++)
        {
            var c = rawParams[i];

            if (c == '"')
            {
                inQuotes = !inQuotes;
                current.Append(c);
                continue;
            }

            if (!inQuotes)
            {
                if (c == ';')
                    break;

                // only at the start of a value, so a path such as 'http://' isn't cut short
                if (c == '/' && current.Length == 0 && i + 1 < rawParams.Length && rawParams[i + 1] == '/')
                    break;

                if (c == ',' || char.IsWhiteSpace(c))
                {
                    Flush();
                    continue;
                }
            }

            current.Append(c);
        }

        Flush();

        // 'name = value', 'name= value' and 'name =value'
        var joined = new List<string>();
        for (var i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (_word.IsMatch(token) && i + 2 < tokens.Count && tokens[i + 1] == "=")
            {
                joined.Add($"{token}={tokens[i + 2]}");
                i += 2;
            }
            else if (token.EndsWith('=') && _word.IsMatch(token[..^1]) && i + 1 < tokens.Count)
            {
                joined.Add(token + tokens[i + 1]);
                i++;
            }
            else if (_word.IsMatch(token) && i + 1 < tokens.Count && tokens[i + 1].Length > 1 && tokens[i + 1].StartsWith('='))
            {
                joined.Add(token + tokens[i + 1]);
                i++;
            }
            else
            {
                joined.Add(token);
            }
        }

        return joined;
    }

    private static void ProcessLine(SourceFilePosition source, CompileState state, Action<SourceFilePosition, CompileState> action) => action(source, state);

    private static void ProcessLabel(SourceFilePosition source, CompileState state, Action<SourceFilePosition, CompileState> action) => action(source, state);
}

