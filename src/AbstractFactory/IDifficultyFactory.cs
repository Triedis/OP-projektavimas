
interface IDifficultyFactory
{
    Weapon CreateWeapon();
    Enemy CreateEnemy(Room room, Vector2 pos);
}
