# Enemy System

A basic enemy system for combat encounters in the game.

## Components

### Enemy.cs
Core enemy component that handles health, damage, and death.

**Features:**
- Health management (max health, current health)
- TakeDamage() method for receiving damage
- Death handling with optional effects
- Events for death, damage, and health changes
- Optional health bar visualization in editor

**Usage:**
1. Add `Enemy` component to a GameObject
2. Set `maxHealth` in inspector
3. Assign a `Collider2D` component (required)
4. Optionally assign a death effect prefab
5. Set the GameObject's layer to match `enemyLayerMask` in PlayerCombat

**Example:**
```csharp
Enemy enemy = GetComponent<Enemy>();
enemy.TakeDamage(10f); // Deal 10 damage
```

### EnemyAI.cs
Basic AI controller for enemies with multiple behavior modes.

**Features:**
- **Idle**: Enemy stands still
- **Patrol**: Enemy moves between patrol points
- **ChasePlayer**: Enemy chases player when in detection range
- **AttackPlayer**: Enemy attacks player when in attack range

**Usage:**
1. Add `EnemyAI` component to an enemy GameObject (requires `Enemy` and `Rigidbody2D`)
2. Set behavior type (Idle, Patrol, ChasePlayer, AttackPlayer)
3. Configure detection range and attack range
4. For patrol behavior, assign patrol points in the `patrolPoints` array

**Configuration:**
- `moveSpeed`: How fast the enemy moves
- `detectionRange`: Distance at which enemy detects player
- `attackRange`: Distance at which enemy attacks player
- `patrolPoints`: Array of transforms for patrol path
- `attackDamage`: Damage dealt to player
- `attackCooldown`: Time between attacks

## Integration with Combat System

The enemy system integrates with `PlayerCombat`:

1. **Enemy Layer**: Set enemies to a specific layer and configure `enemyLayerMask` in PlayerCombat
2. **Damage**: Enemies use `TakeDamage()` which is called by PlayerCombat's hitbox system
3. **Death**: Enemies are destroyed or disabled when health reaches 0

## Setup Instructions

1. **Create Enemy Prefab:**
   - Create a GameObject
   - Add `Collider2D` component (CircleCollider2D or BoxCollider2D)
   - Add `Rigidbody2D` component
   - Add `Enemy` component
   - Add `EnemyAI` component (optional, for AI behavior)
   - Set the GameObject's layer to "Enemy" (or create a new layer)

2. **Configure PlayerCombat:**
   - In PlayerCombat component, set `enemyLayerMask` to include the enemy layer
   - Set `attackRadius` for hitbox size

3. **Configure Enemy:**
   - Set `maxHealth` based on desired difficulty
   - Optionally assign `deathEffectPrefab` for visual feedback
   - Configure AI behavior if using EnemyAI

4. **Test:**
   - Equip a weapon (press 1-9 hotkey)
   - Left-click to attack enemies
   - Enemies should take damage and die when health reaches 0

## Events

Enemy component provides events for integration:
- `OnDeath`: Called when enemy dies
- `OnDamageTaken`: Called when enemy takes damage
- `OnHealthChanged`: Called when health changes

## Example Usage

```csharp
// Subscribe to enemy death
enemy.OnDeath += (deadEnemy) => {
    Debug.Log($"{deadEnemy.name} died!");
    // Award experience, spawn loot, etc.
};

// Subscribe to damage events
enemy.OnDamageTaken += (damagedEnemy, damage) => {
    Debug.Log($"{damagedEnemy.name} took {damage} damage");
};
```

