using System;
using FeverstoneWilds.Flight.Behavior;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace FeverstoneWilds.Flight.AiTask;

/// <summary>Performs a timed melee strike against a nearby target while airborne.</summary>
public class AiTaskFlightMeleeAttack : AiTaskBaseTargetable
{
    private readonly BehaviorFlight flight;
    private readonly float attackRange;
    private readonly float damage;
    private readonly int damageTier;
    private readonly EnumDamageType damageType;
    private readonly float minCooldownMs;
    private readonly float maxCooldownMs;
    private readonly float attackDurationMs;
    private readonly float damageAtMs;
    private readonly string animation;
    private readonly float animationSpeed;

    private float elapsedMs;
    private long nextAttackAllowedMs;
    private bool damageApplied;

    public AiTaskFlightMeleeAttack(EntityAgent entity, JsonObject taskConfig, JsonObject aiConfig) : base(entity, taskConfig, aiConfig)
    {
        flight = entity.GetBehavior<BehaviorFlight>();
        attackRange = taskConfig["attackRange"].AsFloat(2.5f);
        damage = taskConfig["damage"].AsFloat(1f);
        damageTier = taskConfig["damageTier"].AsInt(0);
        damageType = ParseDamageType(taskConfig["damageType"].AsString("SlashingAttack"));
        minCooldownMs = taskConfig["mincooldown"].AsFloat(750f);
        maxCooldownMs = taskConfig["maxcooldown"].AsFloat(1500f);
        attackDurationMs = taskConfig["attackDurationMs"].AsFloat(800f);
        damageAtMs = taskConfig["damageAtMs"].AsFloat(taskConfig["damagePlayerAtMs"].AsFloat(500f));
        animation = taskConfig["animation"].AsString(null);
        animationSpeed = taskConfig["animationSpeed"].AsFloat(1f);
    }

    public override bool ShouldExecute()
    {
        if (!PreconditionsSatisfied()) return false;
        if (flight == null || !entity.Alive || !flight.IsFlying || flight.IsLanding) return false;
        if (entity.World.ElapsedMilliseconds < nextAttackAllowedMs) return false;

        targetEntity = entity.World.GetNearestEntity(entity.Pos.XYZ, attackRange, attackRange, candidate =>
            IsTargetableEntity(candidate, attackRange)
        );

        return targetEntity != null;
    }

    public override void StartExecute()
    {
        base.StartExecute();

        elapsedMs = 0;
        damageApplied = false;
        flight.BeginFlightAttack();
        flight.SetFlightAnimation("flightmeleeattack", animation, animationSpeed, 30f, true);
    }

    public override bool ContinueExecute(float dt)
    {
        if (!entity.Alive || !flight.IsFlying || flight.IsLanding || targetEntity == null || !targetEntity.Alive) return false;
        if (entity.Pos.XYZ.SquareDistanceTo(targetEntity.Pos.XYZ) > attackRange * attackRange) return false;

        elapsedMs += dt * 1000;
        if (!damageApplied && elapsedMs >= damageAtMs)
        {
            targetEntity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Entity,
                SourceEntity = entity,
                CauseEntity = entity,
                Type = damageType,
                DamageTier = damageTier
            }, damage);
            damageApplied = true;
        }

        return elapsedMs < attackDurationMs;
    }

    public override void FinishExecute(bool cancelled)
    {
        flight?.ClearFlightAnimation("flightmeleeattack");
        flight?.EndFlightAttack();
        float minimum = Math.Max(0, minCooldownMs);
        float maximum = Math.Max(minimum, maxCooldownMs);
        nextAttackAllowedMs = entity.World.ElapsedMilliseconds + (long)(minimum + (maximum - minimum) * entity.World.Rand.NextDouble());
        base.FinishExecute(cancelled);
    }

    private static EnumDamageType ParseDamageType(string value)
    {
        return Enum.TryParse(value, true, out EnumDamageType parsed) ? parsed : EnumDamageType.SlashingAttack;
    }
}
