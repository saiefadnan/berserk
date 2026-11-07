using UnityEngine;
using System.Collections.Generic;

public class CombatDirector : MonoBehaviour{
    public static CombatDirector Instance {get; private set;}

    [Header("Token Settings")]
    [Tooltip("Maximum enemies that can attack at the exact same time")]
    [SerializeField] private int maxSimultaneousAttackers = 1;

    [Tooltip("Delay (seconds) after an attack finishes before the next enemy can swing")]
    [SerializeField] private float postAttackCooldown = 0.5f;

    [Header("Whiff Punish Settings")]
    [Tooltip("Chance (0..1) that an orbiting enemy seizes a player whiff to launch a gap-closing counter")]
    [SerializeField] private float whiffPunishChance = 0.75f;

    private readonly HashSet<EnemyCombat> activeAttackers = new HashSet<EnemyCombat>();
    private readonly List<EnemyController> registeredCombatants = new List<EnemyController>();
    private readonly Dictionary<EnemyCombat, float> lastAttackTimes = new Dictionary<EnemyCombat, float>();
    private float lastAttackFinishTime = 0f;

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
        float myTime = lastAttackTimes.ContainsKey(requester) ? lastAttackTimes[requester] : 0f;
        if(activeAttackers.Count >=  maxSimultaneousAttackers || Time.time < lastAttackFinishTime + postAttackCooldown){
            return false;
        }
        foreach (var enemyCtrl in registeredCombatants) {
            if (enemyCtrl == null || enemyCtrl.IsDead) continue;
            EnemyCombat other = enemyCtrl.GetComponent<EnemyCombat>();
            if (other == null || other == requester || other.IsDead || !other.CanAttack) continue;
            if(myTime > GetLastAttackTime(other)) return false;
        }
        activeAttackers.Add(requester);
        return true;
    }

    private float GetLastAttackTime(EnemyCombat enemy) {
        if (lastAttackTimes.TryGetValue(enemy, out float time)) return time;
        return 0f;
    }

    public void ReleaseAttackToken(EnemyCombat requester){
        if(requester != null && activeAttackers.Remove(requester)){
            lastAttackTimes[requester] = Time.time;
            lastAttackFinishTime = Time.time;
        }
    }

    public void NotifyPlayerWhiff(Vector3 playerPos){
        if (activeAttackers.Count >= maxSimultaneousAttackers) return;
        if (Random.value > whiffPunishChance) return;

        EnemyController bestCandidate = null;
        float closestDist = float.MaxValue;

        foreach (var enemy in registeredCombatants){
            if (enemy == null || enemy.IsDead) continue;
            EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
            if (combat == null || !combat.CanAttack || combat.IsBlocking)continue;

            float dist = Vector3.Distance(enemy.transform.position, playerPos);
            if (dist <= 4.2f && dist < closestDist){
                closestDist = dist;
                bestCandidate = enemy;
            }
        }

        if (bestCandidate != null){
            EnemyCombat combat = bestCandidate.GetComponent<EnemyCombat>();
            activeAttackers.Add(combat);
            Debug.Log($"<color=red><b>[WHIFF PUNISH] {bestCandidate.name} lunging in to punish player whiff!</b></color>");
        }
    }

    private void OnDrawGizmos()
    {
        GameObject player = GameObject.Find("player");
        if (player == null) return;

        // 1. Orbit Circle (3.2m spacing)
        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireSphere(player.transform.position, 3.2f);

        // 2. Assigned Flank Slots
        int total = registeredCombatants.Count;
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

            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Gizmos.DrawSphere(pos, 0.18f);
        }

        // 3. Attack Token Visualizer (Who has the token vs waiting)
        foreach (var enemy in registeredCombatants)
        {
            if (enemy == null || enemy.IsDead) continue;
            EnemyCombat combat = enemy.GetComponent<EnemyCombat>();
            if (combat == null) continue;

            Vector3 headPos = enemy.transform.position + Vector3.up * 2.3f;
            bool hasToken = activeAttackers.Contains(combat);

            if (hasToken)
            {
                // RED BEACON: Enemy currently holds the Attack Token
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(headPos, 0.32f);
                Gizmos.DrawWireSphere(headPos, 0.5f);
                Gizmos.DrawLine(enemy.transform.position + Vector3.up * 1.2f, player.transform.position + Vector3.up * 1.2f);

#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.red;
                UnityEditor.Handles.Label(headPos + Vector3.up * 0.35f, "⚔️ [ATTACKING]");
#endif
            }
            else
            {
                // GREEN INDICATOR: Enemy waiting in orbit queue
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
                Gizmos.DrawWireSphere(headPos, 0.2f);

#if UNITY_EDITOR
                UnityEditor.Handles.color = Color.green;
                UnityEditor.Handles.Label(headPos + Vector3.up * 0.25f, "🛡️ [ORBITING]");
#endif
            }
        }
    }
}
