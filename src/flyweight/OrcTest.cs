
using System;
using OP_Projektavimas.Utils;
class OrcTest : Enemy
{
    
    public OrcTest() { SetStrategy(new MeleeStrategy()); }
    public OrcTest(Guid identity, Room room, Vector2 positionInRoom, Axe weapon, string imagePath, bool useFlyweight = false) : base(identity, room, positionInRoom, weapon, imagePath, useFlyweight)
    {
        SetStrategy(new MeleeStrategy());
    }
    public override string ToString()
    {
        return $"Orc";
    }
}
