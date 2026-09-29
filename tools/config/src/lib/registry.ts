/**
 * Helper types and utilities for primary key registries, foreign keys, and zero-boilerplate authoring.
 */

/** Extract key union from a record */
export type IdOf<T extends Record<string, unknown>> = keyof T;

/** Extract 'id' property union from an array of objects with an 'id' string property */
export type ArrayIdOf<T extends readonly { readonly id: string }[]> = T[number]["id"];

/**
 * Table container type holding both named entity access and an array conversion method.
 */
export type DataTable<R extends Record<string, any>> = {
  readonly [K in keyof R]: R[K] & { readonly id: K };
} & {
  /** Returns all records as an array with injected `id` properties */
  readonly asArray: () => readonly (R[keyof R] & { readonly id: keyof R })[];
};

/**
 * Type helper for authoring keyed tables without ID boilerplate.
 * Dynamically binds `id: key` to every item and provides an `.asArray()` accessor.
 *
 * Example:
 * ```ts
 * export const Enemies = defineTable<EnemyDef>()({
 *   imp: { size: 44, life: 40, damage: 2 },
 *   bat: { size: 40, life: 30, damage: 1 },
 * });
 *
 * // Enemies.imp has type { readonly id: "imp", size: number, ... }
 * // Enemies.asArray() returns [{ id: "imp", ... }, { id: "bat", ... }]
 * // export type EnemyId = keyof typeof Enemies;
 * ```
 */
export function defineTable<TItem = any>() {
  return <const R extends Record<string, Omit<TItem, "id">>>(table: R): DataTable<R> => {
    const entries: Record<string, any> = {};
    const arrayList: any[] = [];
    for (const [key, val] of Object.entries(table)) {
      const withId = { id: key, ...(val as any) };
      entries[key] = withId;
      arrayList.push(withId);
    }
    Object.defineProperty(entries, "asArray", {
      value: () => arrayList,
      enumerable: false,
      writable: false,
    });
    return entries as any;
  };
}

/** Type helper for defining a typed dictionary registry (raw map output) */
export function defineMapRegistry<T>() {
  return <const R extends Record<string, T>>(registry: R) => registry;
}

/** Type helper for defining a typed array registry (raw array output) */
export function defineArrayRegistry<T>() {
  return <const R extends readonly T[]>(registry: R) => registry;
}
