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
        private IVariables? _variables = null;
        private readonly CompileState _state;
        private static readonly Regex _relativeLabel = new Regex(@"(?<relative>[-+]*)(?<label>[\w\d_]*)", RegexOptions.Compiled);
        private SourceFilePosition _source;

        public List<string> RequiresRevalNames = new();

        public ExpressionEvaluator(CompileState state)
        {
            _state = state;
        }

        public IVariables? Variables => _variables;

        // not thread safe!!!
        public (int Result, bool RequiresRecalc) Evaluate(string expression, SourceFilePosition source, IVariables variables, int address, bool final)
        {
            _source = source;
            // first check its not a relative label
            if (expression[0] is '-' or '+')
            {
                var match = _relativeLabel.Match(expression);

                if (match.Success)
                {
                    if (!final)
                        return (0xabcd, true);  // we always reval ambigous labels

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

                    if (variables.Values.ContainsKey(label))
                    {
                        var l = variables.Values[label];

                        if (l.Value > address && direction == 1)
                            return (l.Value, false);

                        if (l.Value <= address && direction == -1)
                            return (l.Value, false);

                        throw new RelativeLabelException(source, $"Searching for relative label {label}, with a count of {direction}, but no label found");
                    }

                    var labels = direction > 0 ?
                        variables.AmbiguousVariables.Where(i => i.Name == label && i.Value >= address).OrderBy(i => i.Value) :
                        variables.AmbiguousVariables.Where(i => i.Name == label && i.Value <= address).OrderByDescending(i => i.Value);

                    var item = labels.Skip(Math.Abs(direction) - 1).FirstOrDefault();

                    if (item != null)
                        return (item.Value, false);

                    return (0xabcd, true); // error
                }
            }
            _variables = variables;
            _requiresReval = false;
            int result = 0;
            _evaluator.PreEvaluateVariable += _evaluator_PreEvaluateVariable;
            try
            {
                result = (int)_evaluator.Evaluate(expression);
            }
            catch (Exception e)
            {
                throw new ExpressionException(source, e.Message);
            }
            finally
            {
                _evaluator.PreEvaluateVariable -= _evaluator_PreEvaluateVariable;
            }

            return new(result, _requiresReval);
        }

        private void _evaluator_PreEvaluateVariable(object? sender, VariablePreEvaluationEventArg e)
        {
            if (_variables == null)
                throw new NullReferenceException("_procedure is null");

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
