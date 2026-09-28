namespace Game.Combat;

/// <summary>
/// Projectile source faction. Selects the target set (enemy registry vs player
/// cell), never the species: minions fire as <see cref="Enemy"/> regardless of
/// summoner, hero-summoned allies would fire as <see cref="Player"/>.
/// </summary>
public enum Team
{
    Player = 0,
    Enemy = 1,
}
