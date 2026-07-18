#!/usr/bin/env bash
# Smoke-drives the running Basic Demo stack: API CRUD via curl, then the real
# UI in a browser via agent-browser. Run from repo root AFTER the 3 processes
# are up (see SKILL.md). Screenshot lands in $PWD/smoke-tasks.png.
#
# ponytail: trusted CDP clicks silently die on this app mid-session (see
# SKILL.md Gotchas), so every click/submit goes through JS dispatch (eval).
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

echo "== UI smoke (agent-browser) =="
agent-browser open "$UI" >/dev/null
agent-browser wait 'header button' >/dev/null   # Angular bootstrapped

# Log in through the drawer. Header button click can be trusted-input-dead
# (see Gotchas) so open + submit via JS dispatch, values via fill.
agent-browser eval "document.querySelector('header button').click(); 'drawer'" >/dev/null
agent-browser wait 'input[name="username"]' >/dev/null   # drawer open
agent-browser fill 'input[name="username"]' demo >/dev/null
agent-browser fill 'input[name="password"]' 'Password123!' >/dev/null
agent-browser eval "(() => { const f=[...document.querySelectorAll('form')].find(f=>f.querySelector('input[name=username]')); f.requestSubmit(); return 'login-submitted'; })()" >/dev/null
agent-browser wait 'form.task-form' >/dev/null   # logged in, tasks page rendered
agent-browser get url | grep -q '/tasks' && echo "login: ok (on /tasks)"

BEFORE=$(agent-browser eval "document.querySelectorAll('li.task').length")
agent-browser fill 'form.task-form input[formcontrolname="title"]' 'ui-smoke task' >/dev/null
agent-browser eval "document.querySelector('form.task-form').requestSubmit(); 'ok'" >/dev/null
agent-browser wait 1200 >/dev/null
AFTER=$(agent-browser eval "document.querySelectorAll('li.task').length")
[ "$AFTER" -gt "$BEFORE" ] && echo "ui create: ok ($BEFORE -> $AFTER tasks)" || { echo "ui create FAILED"; exit 1; }

# Delete it again: confirm() must be overridden (headless auto-dismisses it).
agent-browser eval "window.confirm = () => true; (() => { const li=[...document.querySelectorAll('li.task')].find(l=>l.querySelector('strong')?.textContent.trim()==='ui-smoke task'); li.querySelector('button.danger').click(); return 'deleted'; })()" >/dev/null
agent-browser wait 1000 >/dev/null
FINAL=$(agent-browser eval "document.querySelectorAll('li.task').length")
[ "$FINAL" = "$BEFORE" ] && echo "ui delete: ok (back to $BEFORE tasks)" || { echo "ui delete FAILED"; exit 1; }

agent-browser screenshot "$PWD/smoke-tasks.png" >/dev/null
agent-browser close >/dev/null
echo "screenshot: $PWD/smoke-tasks.png"
echo "SMOKE PASSED"
