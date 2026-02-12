using System.Collections.Generic;

namespace BitMagic.Compiler;

public enum DebugActionType
{
    DebugLoad,
    Exception
}

public interface IDebugAction
{
    public IDebugAction? NextAction { get; }
    public void PushAction(IDebugAction action);
    public DebugActionType DebugActionType { get; }
}

public class DebugActionManager
{
    public Dictionary<uint, IDebugAction> DebugActions { get; } = new();
    private uint Id { get; set; } = 1;
    public uint CreateDebugLoadAction(uint? actionId, string filename, int address) =>
        Process(new DebugLoadAction() { Filename = filename, Address = address }, actionId);
    public uint CreateExceptionAction(uint? actionId, string exceptionMessage = "") =>
        Process(new ExceptionAction() { ExceptionMessage = exceptionMessage }, actionId);

    private uint Process(IDebugAction action, uint? actionId)
    {
        if (actionId != null)
        {
            var currentAction = DebugActions[actionId.Value];
            currentAction.PushAction(action);
            return actionId.Value;
        }

        var id = Id++;
        DebugActions.Add(id, action);
        return id;
    }

    public IDebugAction? GetAction(uint id)
    {
        if (DebugActions.TryGetValue(id, out var value))
            return value;

        return null;
    }
}

public abstract class DebugActionBase : IDebugAction
{
    public IDebugAction? NextAction { get; private set; }
    public abstract DebugActionType DebugActionType { get; }

    public void PushAction(IDebugAction action)
    {
        if (NextAction != null)
            NextAction.PushAction(action);
        else
            NextAction = action;
    }
}

public class DebugLoadAction : DebugActionBase
{
    public string Filename { get; internal set; }
    public int Address { get; internal set; }
    public override DebugActionType DebugActionType => DebugActionType.DebugLoad;
}

public class ExceptionAction : DebugActionBase
{
    public string ExceptionMessage {get; internal set; }
    public override DebugActionType DebugActionType => DebugActionType.Exception;
}