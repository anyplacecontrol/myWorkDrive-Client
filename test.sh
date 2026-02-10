#!/bin/bash
# Regression test runner — wraps trace-tools for this app
#
# Usage:
#   ./test.sh list                          List available baselines
#   ./test.sh save <trace.json> <journey>   Save an exported trace as baseline
#   ./test.sh run <journey>                 Generate test, run it, compare
#   ./test.sh run-all                       Run all baselines
#   ./test.sh update <journey>              Promote latest capture to baseline
#   ./test.sh compare <journey>             Compare latest capture vs baseline
#   ./test.sh summary <journey>             Show journey summary

TRACE_TOOLS="$(dirname "$0")/trace-tools"
BASELINES="$(dirname "$0")/traces/baselines"
CAPTURES="$(dirname "$0")/traces/captures"

if [ ! -d "$TRACE_TOOLS" ]; then
  echo "trace-tools not found. Run:"
  echo "  git clone https://github.com/xmlui-org/trace-tools.git"
  echo "  cd trace-tools && npm install && npx playwright install chromium"
  exit 1
fi

case "${1:-help}" in
  list)
    echo "Available baselines:"
    for f in "$BASELINES"/*.json; do
      [ -f "$f" ] || continue
      name=$(basename "$f" .json)
      events=$(node -e "console.log(JSON.parse(require('fs').readFileSync('$f','utf8')).length)")
      echo "  $name ($events events)"
    done
    ;;

  save)
    if [ -z "$2" ] || [ -z "$3" ]; then
      echo "Usage: ./test.sh save <trace.json> <journey-name>"
      exit 1
    fi
    cp "$2" "$BASELINES/$3.json"
    echo "Saved baseline: $3"
    node "$TRACE_TOOLS/summarize.js" --show-journey "$BASELINES/$3.json"
    ;;

  run)
    if [ -z "$2" ]; then
      echo "Usage: ./test.sh run <journey-name>"
      echo "Available baselines:"
      ls "$BASELINES"/*.json 2>/dev/null | while read f; do echo "  $(basename "$f" .json)"; done
      exit 1
    fi
    BASELINE="$BASELINES/$2.json"
    if [ ! -f "$BASELINE" ]; then
      echo "Baseline not found: $2"
      echo "Save one first: ./test.sh save <trace.json> $2"
      exit 1
    fi

    # Resolve absolute paths before cd
    ABS_BASELINE="$(cd "$(dirname "$BASELINE")" && pwd)/$(basename "$BASELINE")"
    ABS_CAPTURES="$(cd "$(dirname "$CAPTURES")" && pwd)/$(basename "$CAPTURES")"

    # Generate test
    TEST_FILE="$TRACE_TOOLS/generated-$2.spec.ts"
    node "$TRACE_TOOLS/generate-playwright.js" "$ABS_BASELINE" "$2" > "$TEST_FILE"
    echo "Generated: $TEST_FILE"

    # Run test
    cd "$TRACE_TOOLS"
    TEST_OUTPUT=$(mktemp)
    npx playwright test "generated-$2.spec.ts" > "$TEST_OUTPUT" 2>&1
    TEST_EXIT=$?

    echo ""
    echo "═══════════════════════════════════════════════════════════════"
    echo "                    REGRESSION TEST: $2"
    echo "═══════════════════════════════════════════════════════════════"
    echo ""

    if [ $TEST_EXIT -eq 0 ]; then
      echo "PASS — Journey completed successfully"
    else
      echo "FAIL — Selector error (see below)"
      echo ""
      grep -A 10 "Error:" "$TEST_OUTPUT" | head -15
    fi
    echo ""

    # Compare traces semantically (APIs, forms, navigation)
    CAPTURED="captured-trace.json"
    if [ -f "$CAPTURED" ]; then
      cp "$CAPTURED" "$ABS_CAPTURES/$2.json"
      SEMANTIC_OUTPUT=$(node compare-traces.js --semantic "$ABS_BASELINE" "$CAPTURED" 2>&1)
      echo "$SEMANTIC_OUTPUT"
      echo ""
      if echo "$SEMANTIC_OUTPUT" | grep -q "Traces match semantically"; then
        echo "SEMANTIC: PASS — Same APIs, forms, and navigation"
      else
        echo "SEMANTIC: FAIL — Behavioral regression detected"
      fi
    else
      echo "No trace captured (test may have failed before any actions)"
    fi

    echo ""
    echo "═══════════════════════════════════════════════════════════════"

    rm -f "$TEST_OUTPUT"

    # Exit 0 if semantics match even if a selector failed
    if [ $TEST_EXIT -ne 0 ] && echo "$SEMANTIC_OUTPUT" | grep -q "Traces match semantically"; then
      exit 0
    fi
    exit $TEST_EXIT
    ;;

  run-all)
    PASS=0
    FAIL=0
    FAILED=()
    for f in "$BASELINES"/*.json; do
      [ -f "$f" ] || continue
      name=$(basename "$f" .json)
      echo "--- Running: $name ---"
      "$0" run "$name"
      if [ $? -eq 0 ]; then
        PASS=$((PASS + 1))
      else
        FAIL=$((FAIL + 1))
        FAILED+=("$name")
      fi
      echo ""
    done
    echo "═══════════════════════════════════════════════════════════════"
    echo "  Results: $PASS passed, $FAIL failed"
    if [ ${#FAILED[@]} -gt 0 ]; then
      echo "  Failed: ${FAILED[*]}"
    fi
    echo "═══════════════════════════════════════════════════════════════"
    [ $FAIL -eq 0 ]
    ;;

  update)
    if [ -z "$2" ]; then
      echo "Usage: ./test.sh update <journey-name>"
      exit 1
    fi
    CAPTURED="$CAPTURES/$2.json"
    if [ ! -f "$CAPTURED" ]; then
      echo "No capture found for $2. Run the test first: ./test.sh run $2"
      exit 1
    fi
    cp "$CAPTURED" "$BASELINES/$2.json"
    echo "Updated baseline: $2"
    ;;

  compare)
    if [ -z "$2" ]; then
      echo "Usage: ./test.sh compare <journey-name>"
      exit 1
    fi
    BASELINE="$BASELINES/$2.json"
    CAPTURED="$CAPTURES/$2.json"
    if [ ! -f "$BASELINE" ]; then echo "No baseline: $2"; exit 1; fi
    if [ ! -f "$CAPTURED" ]; then echo "No capture: $2 (run the test first)"; exit 1; fi
    node "$TRACE_TOOLS/compare-traces.js" --semantic "$BASELINE" "$CAPTURED"
    ;;

  summary)
    if [ -z "$2" ]; then
      echo "Usage: ./test.sh summary <journey-name>"
      exit 1
    fi
    BASELINE="$BASELINES/$2.json"
    if [ ! -f "$BASELINE" ]; then echo "No baseline: $2"; exit 1; fi
    node "$TRACE_TOOLS/summarize.js" --show-journey "$BASELINE"
    ;;

  help|*)
    echo "Usage: ./test.sh <command> [args]"
    echo ""
    echo "Commands:"
    echo "  list                          List available baselines"
    echo "  save <trace.json> <journey>   Save an exported trace as baseline"
    echo "  run <journey>                 Generate test, run it, compare"
    echo "  run-all                       Run all baselines"
    echo "  update <journey>              Promote latest capture to baseline"
    echo "  compare <journey>             Compare latest capture vs baseline"
    echo "  summary <journey>             Show journey summary"
    ;;
esac
