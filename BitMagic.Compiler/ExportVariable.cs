using BitMagic.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BitMagic.Compiler;

/// <summary>
/// An `.export name value`, public unless `.export private`. When value is a single name the export is an alias:
/// it takes its value and type from that name, which can be anything in the export's scope
/// whatever its visibility. Otherwise value is an expression, evaluated like a `.const` written
/// at the same place: one that starts with a name and adds or subtracts (`score + 1`) has that
/// name's type, anything else is a plain constant. Until the value is known (a forward reference,
/// or another export not resolved yet) it needs re-evaluating, so the final passes pick it up like
/// any other constant.
/// </summary>
internal class ExportVariable : AsmVariable
{
    // a name, or a path of names: feed, App:Main:feed, text:print:loop, App::counter, endproc
    private const string NamePattern = @":?[A-Za-z_]\w*(?::{1,2}[A-Za-z_]\w*)*";
    private static readonly Regex _singleName = new($"^{NamePattern}$", RegexOptions.Compiled);
    private static readonly Regex _startsWithName = new($@"^(?<name>{NamePattern})\s*[+-]", RegexOptions.Compiled);
    private static readonly Regex _names = new($@"(?<![\w$%.]){NamePattern}", RegexOptions.Compiled);

    public string Target { get; }

    // the procedure the export is written in, where the lookup starts
    private readonly Variables _from;

    // anything in here can be exported, whatever its visibility
    private readonly Variables _scope;

    private readonly IExpressionEvaluator _evaluator;

    // false for an alias
    private readonly bool _isExpression;

    // for an expression that starts with a name and adds or subtracts, the name it takes its type from
    private readonly string _typedBy;

    public ExportVariable(string name, string target, Variables from, Variables scope, IExpressionEvaluator evaluator, Visibility visibility, SourceFilePosition source)
    {
        Name = name;
        Target = target;
        _from = from;
        _scope = scope;
        _evaluator = evaluator;
        _isExpression = !_singleName.IsMatch(target);

        var startsWithName = _startsWithName.Match(target);
        _typedBy = _isExpression && startsWithName.Success ? startsWithName.Groups["name"].Value : null;

        Visibility = visibility;
        SourceFilePosition = source;
        VariableDataType = VariableDataType.Constant;
        RequiresReval = true;
        // until it's resolved, look like an unknown name does: a two byte value, so an
        // instruction using it is sized for an absolute address rather than zero page
        Value = 0xabcd;
        Evaluate = _isExpression ? ResolveExpression : ResolveAlias;
    }

    private (int Value, bool RequiresReval) ResolveAlias(bool final)
    {
        if (!Find(Target, out var target, out var owner))
            return (0, true);

        if (target is DebuggerVariable)
            throw new VariableException(SourceFilePosition, Target, $"'{Target}' is a debugger alias, debugger aliases can't be exported.");

        if (target.RequiresReval)
            return (0, true);

        VariableDataType = target.VariableDataType;
        Length = target.Length;
        Array = target.Array;
        AliasOf = target is AsmVariable { AliasOf: not null } alias ? alias.AliasOf : $"{owner.FullName}:{target.Name}";

        return (target.Value, false);
    }

    private (int Value, bool RequiresReval) ResolveExpression(bool final)
    {
        foreach (Match match in _names.Matches(Target))
        {
            if (Find(match.Value, out var used, out _) && used is DebuggerVariable)
                throw new VariableException(SourceFilePosition, match.Value, $"'{match.Value}' is a debugger alias, debugger aliases can't be exported.");
        }

        // evaluated like a `.const` written here
        var (value, requiresReval) = _evaluator.Evaluate(Target, SourceFilePosition, _from, -1, final);

        if (requiresReval)
            return (0, true);

        VariableDataType = VariableDataType.Constant;
        Length = 0;
        Array = false;

        // `score + 1` is the same type as score, at a different address. An array keeps its
        // element type, but not its length, as the offset points into it.
        if (_typedBy != null && Find(_typedBy, out var typedBy, out _))
        {
            VariableDataType = typedBy.VariableDataType;
            Length = typedBy.Array ? 0 : typedBy.Length;
        }

        // shown by the debugger after the type, and marks this as a second name for something
        AliasOf = Target;

        return (value, false);
    }

    private bool Find(string target, out IAsmVariable result, out Variables owner) =>
        _from.TryGetValue(target, SourceFilePosition, out result, out owner, _from, _scope);

    /// <summary>
    /// Why the export couldn't be resolved, for the error once the final passes give up.
    /// </summary>
    public string FailureMessage()
    {
        if (_isExpression)
            return ExpressionFailureMessage();

        var visited = new List<ExportVariable> { this };
        var current = this;

        while (true)
        {
            if (current._isExpression)
                return current.ExpressionFailureMessage();

            if (!current.Find(current.Target, out var target, out _))
            {
                var hidden = current._from.HiddenReason(current.Target);
                return hidden != null ?
                    $"Cannot export '{current.Target}' as '{current.Name}': {hidden}." :
                    $"Cannot export '{current.Target}' as '{current.Name}': it can't be found.";
            }

            if (target is not ExportVariable next || !next.RequiresReval)
                return $"Cannot export '{current.Target}' as '{current.Name}': its value can't be evaluated.";

            var loops = visited.Contains(next);
            visited.Add(next);

            if (loops)
                return $"The exports form a loop: {string.Join(" -> ", visited.ConvertAll(i => i.Name))}.";

            current = next;
        }
    }

    // the first name in the expression that can't be found, or can't be evaluated yet
    private string ExpressionFailureMessage()
    {
        foreach (var name in _names.Matches(Target).Select(i => i.Value).Distinct())
        {
            if (!Find(name, out var used, out _))
            {
                var hidden = _from.HiddenReason(name);
                return hidden != null ?
                    $"Cannot export '{Target}' as '{Name}': {hidden}." :
                    $"Cannot export '{Target}' as '{Name}': '{name}' can't be found.";
            }

            if (used.RequiresReval)
                return $"Cannot export '{Target}' as '{Name}': '{name}' can't be evaluated.";
        }

        return $"Cannot export '{Target}' as '{Name}': its value can't be evaluated.";
    }
}
