import subprocess
import sys
import textwrap
from pathlib import Path

import check_localization_reply


def run_tool(tmp_path, source):
    cs = tmp_path / "Test.cs"
    cs.write_text(textwrap.dedent(source))
    cwd = tmp_path
    # Execute main() with cwd set to tmp_path
    return subprocess.run(
        [sys.executable, check_localization_reply.__file__],
        cwd=cwd,
        capture_output=True,
        text=True,
    )


def test_reports_string_literal(tmp_path):
    result = run_tool(
        tmp_path,
        """
        using System;
        class C {
            void M() {
                LocalizationService.Reply("Hello");
            }
        }
        """,
    )
    assert result.returncode == 1
    assert "LocalizationService.Reply(" in result.stdout
    assert "Found LocalizationService.Reply calls with string literals" in result.stdout


def test_reports_sentinel_string(tmp_path):
    result = run_tool(
        tmp_path,
        """
        using System;
        class C {
            void M() {
                LocalizationService.Reply("[[TOKEN_SENTINEL]]");
            }
        }
        """,
    )
    assert result.returncode == 1
    assert "[[TOKEN_SENTINEL]]" in result.stdout
    assert "Found LocalizationService.Reply calls with string literals" in result.stdout
