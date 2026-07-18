#!/usr/bin/env bash
# Smoke-drives the running Basic Demo stack: API CRUD via curl. Run from repo
# root AFTER the 3 processes are up (see SKILL.md).
#
# ponytail: browser UI smoke (agent-browser) removed 2026-07-18 — it missed
# real UI issues and was more burden than signal; re-add when we trust it.
set -e

STS=http://localhost:5143
API=http://localhost:5216
UI=http://127.0.0.1:58906

echo "== waiting for stack =="
for i in $(seq 1 30); do
  ok=1
  curl -s -o /dev/null --max-time 2 "$UI" || ok=0
  curl -s -o /dev/null --max-time 2 "$API/api/tasks/statuses" || ok=0
  curl -s -o /dev/null --max-time 2 "$STS/api/auth/me" || ok=0
  [ $ok = 1 ] && break
  [ $i = 30 ] && { echo "stack not up after 60s"; exit 1; }
  sleep 2
done
echo "all 3 ports responding"

echo "== API smoke (curl) =="
TOKEN=$(curl -s -X POST "$STS/api/auth/login" -H "Content-Type: application/json" \
  -d '{"username":"demo","password":"Password123!"}' \
  | python -c "import sys,json;print(json.load(sys.stdin)['token'])")
[ -n "$TOKEN" ] || { echo "login failed"; exit 1; }

curl -s "$STS/api/auth/me" -H "Authorization: Bearer $TOKEN" | grep -q '"username":"demo"' && echo "me: ok"

NEW=$(curl -s -X POST "$API/api/tasks" -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"api-smoke","description":"","status":"Pending","dueDate":"2026-12-31"}')
ID=$(echo "$NEW" | python -c "import sys,json;print(json.load(sys.stdin)['id'])")
echo "create: id=$ID"
curl -s -o /dev/null -w "update: %{http_code}\n" -X PUT "$API/api/tasks/$ID" \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"title":"api-smoke","description":"updated","status":"Done","dueDate":"2026-12-31"}'
curl -s -o /dev/null -w "delete: %{http_code}\n" -X DELETE "$API/api/tasks/$ID" -H "Authorization: Bearer $TOKEN"

echo "SMOKE PASSED"
