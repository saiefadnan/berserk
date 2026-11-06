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
    private readonly List<EnemyController> registeredCombatants = new List<EnemyController>();
    private float lastAttackFinishTime = -999f;
    private EnemyCombat lastAttacker;

    private void Awake(){
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update(){
        activeAttackers.RemoveWhere(e=> e == null || e.IsDead);
        registeredCombatants.RemoveAll(e=> e == null || e.IsDead);
    }

    public void RegisterCombatant(EnemyController enemy){
        if (enemy != null && !registeredCombatants.Contains(enemy)){
            registeredCombatants.Add(enemy);
        }
    }
    public float GetAsignedFlankAngle(EnemyController enemy){
        if (enemy == null || enemy.IsDead) return 0f;

        registeredCombatants.RemoveAll(e => e == null || e.IsDead);

        // Auto-register failsafe if Awake/Start missed it
        if (!registeredCombatants.Contains(enemy)){
            registeredCombatants.Add(enemy);
        }

        int index = registeredCombatants.IndexOf(enemy);
        int total = registeredCombatants.Count;
        if (total <= 1) return 0f;

        float angleStep = 360f / total;
        int pairIndex = (index + 1) / 2;
        float sign = (index % 2 == 0) ? -1f : 1f;

        return sign * (pairIndex * angleStep);
    }

    public void UnregisterCombatant(EnemyController enemy){
        if (enemy != null && registeredCombatants.Contains(enemy)){
            registeredCombatants.Remove(enemy);
            var combat = enemy.GetComponent<EnemyCombat>();
            if (combat != null) ReleaseAttackToken(combat);
        }
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

    private void OnDrawGizmos()
    {
        GameObject player = GameObject.Find("player");
        if (player == null) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(player.transform.position, 3.2f);

        int total = registeredCombatants.Count;
        if (total == 0) return;

        Vector3 gutsFacing = player.transform.forward;
        gutsFacing.y = 0f;
        gutsFacing.Normalize();

        for (int i = 0; i < total; i++)
        {
            float angleStep = 360f / total;
            int pairIndex = (i + 1) / 2;
            float sign = (i % 2 == 0) ? -1f : 1f;
            float angle = sign * (pairIndex * angleStep);

            Vector3 flankDir = Quaternion.Euler(0f, angle, 0f) * gutsFacing;
            Vector3 pos = player.transform.position + flankDir * 3.2f;

            Gizmos.color = (i == 0) ? Color.red : Color.green;
            Gizmos.DrawSphere(pos, 0.25f);
            Gizmos.DrawLine(player.transform.position, pos);
        }
    }
}
