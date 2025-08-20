import collect_skipped_hashes as csh


def test_parse_hashes_with_named_and_numeric(tmp_path):
    log = tmp_path / "translate.log"
    log.write_text(
        "00123: SKIPPED (token mismatch)\n"
        "welcome: SKIPPED (token mismatch)\n"
        "other: TRANSLATED\n",
        encoding="utf-8",
    )
    hashes = csh.parse_hashes(log)
    assert hashes == {"123", "welcome"}
