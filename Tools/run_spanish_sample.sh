#!/usr/bin/env bash
set -euo pipefail

# Verify Argos Spanish model is installed
if ! argospm list | grep -q 'translate-en_es'; then
  echo 'Required model translate-en_es not found. Please install before running.' >&2
  exit 1
fi

# Prepare timestamped run directory
timestamp="$(date +%Y%m%d_%H%M%S)"
run_dir="translations/es/$timestamp"
mkdir -p "$run_dir"

# Run translation with logs and reports in run directory
python Tools/translate_argos.py Resources/Localization/Messages/Spanish_sample.json \
  --to es \
  --run-dir "$run_dir" \
  --log-level INFO \
  --log-file "$run_dir/translate.log" \
  --report-file "$run_dir/report.csv" \
  --metrics-file "$run_dir/metrics.json"
exit_code=$?

echo "run_dir=$run_dir"
echo "exit_code=$exit_code"
exit $exit_code
