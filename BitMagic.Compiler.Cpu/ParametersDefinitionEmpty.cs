namespace BitMagic.Compiler.Cpu
{
    public class ParametersDefinitionEmpty : ParametersDefinitionSingle
    {
        internal override string GetParameter(string parameters) => parameters;

        public override bool Valid(string parameters) => string.IsNullOrWhiteSpace(parameters);
        public override bool HasTemplate => false;
    }
}
