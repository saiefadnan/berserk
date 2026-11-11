using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TargetLockSystem : MonoBehaviour {
    [Header("Lock-On Settings")]
    [SerializeField] private KeyCode lockKey = KeyCode.Mouse2;
    [SerializeField] private float maxLockDistance = 18f;
    [SerializeField] private float breakDistance = 22f;
    [SerializeField] private float maxViewAngle = 75f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Visual Reticle")]
    [SerializeField] private Image reticleImage;
    [SerializeField] private float reticleSpinSpeed = 45f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color deathblowColor = new Color(1f, 0.12f, 0.15f, 1f);
    [SerializeField] private float deathblowPulseSpeed = 10f;

     private ExecutionTarget targetExecution;

    public bool IsLockedOn => currentTarget != null;
    public Transform CurrentTarget => currentTarget;

    private Transform currentTarget;
    private Camera playerCam;
    private Health targetHealth;

    private void Awake(){
        playerCam = Camera.main;
    }

    private void Update(){
        HandleInput();
        CheckBreakConditions();
        UpdateReticle();
    }

    private void HandleInput(){
        if (Input.GetKeyDown(lockKey)){
            if (IsLockedOn){
                Unlock();
            }else{
                FindAndLockTarget();
            }
        }

        if (IsLockedOn){
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.05f){
                SwitchTarget(scroll > 0 ? 1 : -1);
            }
        }
    }

    private void FindAndLockTarget(){
        EnemyController[] allEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController bestTarget = null;
        float bestScore = float.MaxValue;

        Vector3 camPos = playerCam.transform.position;
        Vector3 camFwd = playerCam.transform.forward;

        foreach(var enemy in allEnemies){
            if (enemy == null || enemy.IsDead) continue;
            Vector3 dirToEnemy = enemy.transform.position - camPos;
            float dist = dirToEnemy.magnitude;
            if (dist > maxLockDistance) continue;

            float angle = Vector3.Angle(camFwd, dirToEnemy);
            if (angle > maxViewAngle) continue;

            if (!Physics.Raycast(camPos, dirToEnemy.normalized, dist, obstacleLayers)){
                float score = angle * 1.5f + dist;
                if (score < bestScore){
                    bestScore = score;
                    bestTarget = enemy;
                }
            }
        }

        if (bestTarget != null){
            LockOnto(bestTarget.transform);
        }
    }

    private void SwitchTarget(int direction)
    {
        // Finds the closest enemy to the left or right of current target
        EnemyController[] allEnemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
        EnemyController bestNext = null;
        float bestAngle = float.MaxValue;

        Vector3 camRight = playerCam.transform.right;
        Vector3 currentDir = (currentTarget.position -
        transform.position).normalized;

        foreach (var enemy in allEnemies)
        {
            if (enemy == null || enemy.IsDead || enemy.transform ==
            currentTarget) continue;
            float dist = Vector3.Distance(transform.position,
            enemy.transform.position);
            if (dist > maxLockDistance) continue;

            Vector3 toOther = (enemy.transform.position -
            transform.position).normalized;
            float side = Vector3.Dot(camRight, toOther - currentDir);

            // Filter by direction (+1 = right, -1 = left)
            if ((direction > 0 && side > 0.1f) || (direction < 0 && side <
            -0.1f))
            {
                float angle = Vector3.Angle(currentDir, toOther);
                if (angle < bestAngle)
                {
                    bestAngle = angle;
                    bestNext = enemy;
                }
            }
        }

        if (bestNext != null)
        {
            LockOnto(bestNext.transform);
        }
    }

    private void LockOnto(Transform target){
        currentTarget = target;
        targetHealth = target.GetComponent<Health>() ?? target.GetComponentInParent<Health>();
        targetExecution = target.GetComponent<ExecutionTarget>() ?? target.GetComponentInParent<ExecutionTarget>();
        if (reticleImage != null) reticleImage.gameObject.SetActive(true);
    }

    public void Unlock(){
        currentTarget = null;
        targetHealth = null;
        if (reticleImage != null) reticleImage.gameObject.SetActive(false);
    }

    private void CheckBreakConditions(){
        if (!IsLockedOn) return;
        if (targetHealth != null && targetHealth.IsDead){
            Unlock();
            FindAndLockTarget();
            return;
        }
        float dist = Vector3.Distance(transform.position, currentTarget.position);
        if (dist > breakDistance){
            Unlock();
        }
    }
    private void UpdateReticle(){
        if (!IsLockedOn || reticleImage == null) return;
        Vector3 lockPoint = currentTarget.position + Vector3.up * 1f;
        Vector3 screenPos = playerCam.WorldToScreenPoint(lockPoint);
        if (screenPos.z < 0){
            reticleImage.gameObject.SetActive(false);
            return;
        }
        reticleImage.gameObject.SetActive(true);
        reticleImage.transform.position = screenPos;
        bool isDeathblowReady = targetExecution != null && targetExecution.IsStaggered;
        reticleImage.color = isDeathblowReady ? deathblowColor : normalColor;
        if (isDeathblowReady){
            float pulse = Mathf.PingPong(Time.time * deathblowPulseSpeed, 0.3f) + 0.9f;
            reticleImage.transform.localScale = Vector3.one * pulse;
        }else{
            reticleImage.transform.localScale = Vector3.one;
        }
        reticleImage.transform.Rotate(0, 0, reticleSpinSpeed * Time.deltaTime);
    }
}
