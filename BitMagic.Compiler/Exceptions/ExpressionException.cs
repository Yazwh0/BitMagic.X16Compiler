
using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

public class ExpressionException(SourceFilePosition source, string message) : CompilerSourceException(source, message);
