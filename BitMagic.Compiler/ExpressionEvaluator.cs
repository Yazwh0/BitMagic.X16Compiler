using BitMagic.Common;
using BitMagic.Compiler.CodingSeb;
using BitMagic.Compiler.Exceptions;
using CodingSeb.ExpressionEvaluator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BitMagic.Compiler
{
    internal class ExpressionEvaluator : IExpressionEvaluator
    {
        private readonly Asm6502ExpressionEvaluator _evaluator = new();
        private bool _requiresReval;
        private IVariables _variables = null;
        private readonly CompileState _state;
        private static readonly Regex _relativeLabel = new Regex(@"(?<relative>[-+]*)(?<label>[\w\d_]*)", RegexOptions.Compiled);
        private SourceFilePosition _source;

        public List<string> RequiresRevalNames = new();

        // names in the last expression that were found but are private, with why
        public List<string> HiddenReasons = new();

        // whether the last expression had a value that isn't known yet, so its result is only a placeholder
        public bool LastRequiresReval { get; private set; }

        public ExpressionEvaluator(CompileState state)
        {
            _state = state;
        }

        public IVariables Variables => _variables;

        // not thread safe!!!
        public (int Result, bool RequiresRecalc) Evaluate(string expression, SourceFilePosition source, IVariables variables, int address, bool final)
        {
            _source = source;

            // if the experssion is only relative moves, then append the default value
            if (expression.All(i => i == '+' || i == '-'))
                expression += Compiler.AnonymousLabel;

            // first check its not a relative label
            if (expression[0] is '-' or '+')
            {
                var match = _relativeLabel.Match(expression);

                if (match.Success)
                {
                    if (!final)
                    {
                        LastRequiresReval = true;
                        return (0xabcd, true);  // we always reval ambigous labels
                    }

                    var relative = match.Groups["relative"].Value;
                    var label = match.Groups["label"].Value;

                    var direction = 0;
                    for(var i = 0; i < relative.Length; i++)
                    {
                        if (relative[i] == '-')
                            direction--;
                        else
                            direction++;
                    }

                    if (direction == 0)
                        throw new RelativeLabelException(source, $"Parsing relative label in expression {expression} rendered no direction");

                    // a label name used once has been made explicit, otherwise all the labels with the name are ambiguous.
                    var addresses = variables.Values.TryGetValue(label, out var explicitLabel) ?
                        new List<int> { explicitLabel.Value } :
                        variables.AmbiguousVariables.Where(i => i.Name == label).Select(i => i.Value).ToList();

                    // A label on the same opcode as this reference (eg `.: jmp -`) sits at the reference's own address.
                    // Backwards that is the loop the reference is in, so it matches: `.: jmp -` jumps to itself.
                    // Forwards it never matches, `.: bne +` goes to the next label rather than itself.
                    var inDirection = direction > 0 ?
                        addresses.Where(i => i > address).Order().ToList() :
                        addresses.Where(i => i <= address).OrderDescending().ToList();

                    var count = Math.Abs(direction);

                    if (inDirection.Count >= count)
                        return (inDirection[count - 1], false);

                    throw new RelativeLabelException(source, RelativeLabelError(relative, label, direction, addresses.Count, inDirection.Count));
                }
            }
            _variables = variables;
            _requiresReval = false;
            LastRequiresReval = false;
            HiddenReasons.Clear();
            int result = 0;
            _evaluator.PreEvaluateVariable += _evaluator_PreEvaluateVariable;
            try
            {
                var returnObj = _evaluator.Evaluate(expression);

                if (returnObj is char chr)
                {
                    result = (byte)chr;
                }
                else if (returnObj is bool bol)
                {
                    result = bol ? 1 : 0;
                }
                else
                {
                    result = (int)returnObj;
                }
            }
            catch (Exception e)
            {
                throw new ExpressionException(source, e.Message);
            }
            finally
            {
                _evaluator.PreEvaluateVariable -= _evaluator_PreEvaluateVariable;
            }

            LastRequiresReval = _requiresReval;
            return new(result, _requiresReval);
        }

        // Why a relative label can't be found: no label of that name, they are all in the other direction, or there
        // aren't enough of them for the count (eg '--loop' with only one loop before it).
        private static string RelativeLabelError(string relative, string label, int direction, int total, int inDirection)
        {
            var anonymous = label == Compiler.AnonymousLabel;
            var written = anonymous ? relative : relative + label;
            var name = anonymous ? "anonymous label" : $"'{label}' label";
            var article = anonymous ? "an" : "a";
            var where = direction < 0 ? "before" : "after";
            var count = Math.Abs(direction);

            if (total == 0)
                return $"'{written}' needs {article} {name}, but there isn't one.";

            if (inDirection == 0)
            {
                var opposite = direction < 0 ? "after" : "before";
                var suggestion = (direction < 0 ? "+" : "-") + (anonymous ? "" : label);
                return $"'{written}' needs {article} {name} {where} it, but the only {(total == 1 ? "one is" : "ones are")} {opposite} it. Did you mean '{suggestion}'?";
            }

            return $"'{written}' needs {count} {name}s {where} it, but there {(inDirection == 1 ? "is" : "are")} only {inDirection}.";
        }

        private void _evaluator_PreEvaluateVariable(object sender, VariablePreEvaluationEventArg e)
        {
            if (_variables == null)
                throw new CompilerGeneralException($"Cannot evaluate '{e.Name}', no variables are in scope.");

            if (_variables.TryGetValue(e.Name, _source, out var result))
            {
                e.Value = result.Value;
                _requiresReval |= result.RequiresReval;
                //_requiresReval |= false;
            }
            else
            {
                RequiresRevalNames.Add(e.Name);
                _requiresReval |= true;

                if (_variables is Variables tree && tree.HiddenReason(e.Name) is string reason)
                    HiddenReasons.Add(reason);

                // activate when we have preprocess constant collection
                //e.Value = _size switch
                //{
                //    ParameterSize.Bit8 => 0xab,
                //    ParameterSize.Bit16 => 0xabcd,
                //    ParameterSize.Bit32 => 0xabcdabcd,
                //    _ => throw new InvalidOperationException($"Unknown size {_size}")
                //};
                e.Value = 0xabcd; // random two byte number
            }
        }

        public void Reset()
        {
            RequiresRevalNames.Clear();
        }
    }
}
