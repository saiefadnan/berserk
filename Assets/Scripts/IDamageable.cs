using UnityEngine;

public enum HitReactionType{
    SwordLight,
    SwordMedium,
    SwordKnockdown,
    CannonballKnockdown
}

public struct DamageData {
    public float amount;
    public Vector3 hitPoint;
    public Vector3 hitDirection;
    public GameObject attacker;
    public HitReactionType reactionType;
    public float knockbackForce;
}

public enum SurfaceType{
    Flesh,
    Metal,
    Wood,
    Stone
}

public interface IDamageable
{
    SurfaceType surfaceType{get;}
    GameObject victim{get;}
    void TakeDamage(DamageData data);
}
