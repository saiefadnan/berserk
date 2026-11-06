using UnityEngine;
using System.Collections.Generic;

public class CombatDirector : MonoBehaviour{
    public static CombatDirector Instance {get; private set;}

    [Header("Token Settings")]
    [Tooltip("Maximum enemies that can attack at the exact same time")]
    [SerializeField] private int maxSimultaneousAttackers = 1;

    [Tooltip("Delay (seconds) after an attack finishes before the next enemy can swing")]
    [SerializeField] private float postAttackCooldown = 0.5f;

    private readonly HashSet<EnemyCombat> activeAttackers = new HashSet<EnemyCombat>();
    private float lastAttackFinishTime = -999f;
    private EnemyCombat lastAttacker;

    private void Awake(){
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update(){
        activeAttackers.RemoveWhere(e=> e == null || e.IsDead);
    }

    public bool RequestAttackToken(EnemyCombat requester){
        if (requester == null || requester.IsDead) return false;
        if (activeAttackers.Contains(requester)) return true;
        if (requester == lastAttacker) return false;
        if(activeAttackers.Count < maxSimultaneousAttackers && Time.time >= lastAttackFinishTime + postAttackCooldown){
            activeAttackers.Add(requester);
            return true;
        }

        return false;
    }

    public void ReleaseAttackToken(EnemyCombat requester){
        if(requester != null && activeAttackers.Remove(requester)){
            lastAttackFinishTime = Time.time;
            lastAttacker = requester;
        }
    }
}
