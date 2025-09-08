import json
import sys
import pytest
import check_fix_tokens_metrics


def test_pass_on_empty(tmp_path, monkeypatch):
    metrics = []
    path = tmp_path / "metrics.json"
    path.write_text(json.dumps(metrics))
    monkeypatch.setattr(sys, "argv", ["check_fix_tokens_metrics.py", "--file", str(path)])
    check_fix_tokens_metrics.main()


def test_fail_on_mismatch(tmp_path, monkeypatch):
    metrics = [{"token_mismatches": 1, "timestamp": "t"}]
    path = tmp_path / "metrics.json"
    path.write_text(json.dumps(metrics))
    monkeypatch.setattr(sys, "argv", ["check_fix_tokens_metrics.py", "--file", str(path)])
    with pytest.raises(SystemExit) as exc:
        check_fix_tokens_metrics.main()
    assert exc.value.code == 1
