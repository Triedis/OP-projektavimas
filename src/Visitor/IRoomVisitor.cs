
// This interface is implemented by all "elements" in the object structure.
public interface IVisitableRoom
{
    void Accept(IRoomVisitor visitor);
}

// The Visitor interface declares a Visit method for each concrete element type.
public interface IRoomVisitor
{
    void Visit(StandardRoom room);
    void Visit(TreasureRoom room);
    void Visit(BossRoom room);
}
