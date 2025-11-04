
# DR Drill Runbook
Objectives: RPO ≤ 5m, RTO ≤ 15m.
1) Fail primary region DB to replica.
2) Shift traffic (Argo Rollouts / DNS).
3) Validate health + data consistency.
4) Postmortem & rollback.
