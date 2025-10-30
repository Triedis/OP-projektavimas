using System.Text.Json.Serialization;
using Serilog;
using OP_Projektavimas.Utils;
class Slime : Enemy
{
    [JsonConstructor]
    public Slime() : base() 
    { 
        SetStrategy(new DeepSplitStrategy());
    }
    public Slime(Guid identity, Room room, Vector2 positionInRoom, Dagger sword) : base(identity, room, positionInRoom, sword) 
    {
        SetStrategy(new DeepSplitStrategy()); 
    }
    public override string ToString()
    {
        return $"Slime";
    }

}
