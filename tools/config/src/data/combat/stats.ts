import { StatId, type StatLabelsFile } from "../../schemas/ui.schema";

export { StatId };

export const StatLabels: StatLabelsFile = {
  [StatId.Might]: "STAT_MIGHT",
  [StatId.Area]: "STAT_AREA",
  [StatId.CooldownReduction]: "STAT_COOLDOWN_REDUCTION",
  [StatId.ProjectileSpeed]: "STAT_PROJECTILE_SPEED",
  [StatId.Duration]: "STAT_DURATION",
  [StatId.Amount]: "STAT_AMOUNT",
  [StatId.Pierce]: "STAT_PIERCE",
  [StatId.Knockback]: "STAT_KNOCKBACK",
  [StatId.CritChance]: "STAT_CRIT_CHANCE",
  [StatId.CritDamage]: "STAT_CRIT_DAMAGE",
  [StatId.AilmentDamage]: "STAT_AILMENT_DAMAGE",
  [StatId.MaxHealth]: "STAT_MAX_HEALTH",
  [StatId.HealthRegen]: "STAT_HEALTH_REGEN",
  [StatId.Armor]: "STAT_ARMOR",
  [StatId.MoveSpeed]: "STAT_MOVE_SPEED",
  [StatId.Evasion]: "STAT_EVASION",
  [StatId.Block]: "STAT_BLOCK",
  [StatId.LifeSteal]: "STAT_LIFE_STEAL",
  [StatId.Magnet]: "STAT_MAGNET",
};
