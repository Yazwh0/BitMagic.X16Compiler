using BitMagic.Common;

namespace BitMagic.Compiler.Exceptions;

internal class DebugLoadArgumentsException(SourceFilePosition source, string arguementName) : CompilerSourceException(source, $".debugload needs filename and address. Missing {arguementName}");
