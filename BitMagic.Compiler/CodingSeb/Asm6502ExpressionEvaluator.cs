using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CodingSeb.ExpressionEvaluator;

namespace BitMagic.Compiler.CodingSeb;

/// <summary>
/// Adds 6502 ASM extensions to the expression evaluator.
/// </summary>
public class Asm6502ExpressionEvaluator : BaseExpressionEvaluator
{
    protected new static readonly IList<ExpressionOperator> rightOperandOnlyOperatorsEvaluationDictionary =
        BaseExpressionEvaluator.rightOperandOnlyOperatorsEvaluationDictionary
            .ToList()
            .FluidAdd(Asm6502Operator.LowByte)
            .FluidAdd(Asm6502Operator.HighByte)
            .FluidAdd(Asm6502Operator.TopByte);

    protected new static readonly IList<IDictionary<ExpressionOperator, Func<dynamic, dynamic, object>>> operatorsEvaluations =
        BaseExpressionEvaluator.operatorsEvaluations
            .Copy()
            .AddOperatorEvaluationAtNewLevelAfter(Asm6502Operator.LowByte, (left, right) => right & 0xff, ExpressionOperator.UnaryPlus)
            .AddOperatorEvaluationAtLevelOf(Asm6502Operator.HighByte, (left, right) => (right & 0xff00) >> 8, Asm6502Operator.LowByte)
            .AddOperatorEvaluationAtLevelOf(Asm6502Operator.TopByte, (left, right) => (right & 0xff0000) >> 16, Asm6502Operator.LowByte);

    protected override IList<ExpressionOperator> RightOperandOnlyOperatorsEvaluationDictionary => rightOperandOnlyOperatorsEvaluationDictionary;
 
    protected override IList<IDictionary<ExpressionOperator, Func<dynamic, dynamic, object>>> OperatorsEvaluations => operatorsEvaluations;

    protected override void Init()
    {
        unaryOperatorsDictionary.Add(">", Asm6502Operator.LowByte);
        unaryOperatorsDictionary.Add("<", Asm6502Operator.HighByte);
        unaryOperatorsDictionary.Add("^", Asm6502Operator.TopByte);
    }
}

internal class Asm6502Operator : ExpressionOperator
{
    public static readonly ExpressionOperator LowByte = new Asm6502Operator();
    public static readonly ExpressionOperator HighByte = new Asm6502Operator();
    public static readonly ExpressionOperator TopByte = new Asm6502Operator();
}