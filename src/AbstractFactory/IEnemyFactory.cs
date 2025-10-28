
interface IEnemyFactory
{
    Weapon CreateWeapon();
    Enemy CreateEnemy(Room room, Vector2 pos);
}
