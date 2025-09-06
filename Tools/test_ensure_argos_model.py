import sys
import types

argos_translate_stub = types.SimpleNamespace()
argos_package_stub = types.SimpleNamespace()
sys.modules.setdefault("argostranslate", types.SimpleNamespace(
    translate=argos_translate_stub, package=argos_package_stub
))
sys.modules.setdefault("argostranslate.translate", argos_translate_stub)
sys.modules.setdefault("argostranslate.package", argos_package_stub)

import json
import zipfile
from pathlib import Path

import pytest

import ensure_argos_model


def test_missing_segments_or_zip_file_raises(tmp_path):
    model_dir = tmp_path / "model"
    model_dir.mkdir()

    # No segments and no zip part
    with pytest.raises(FileNotFoundError):
        ensure_argos_model._combine_segments(model_dir)

    # Segments exist but zip part missing
    (model_dir / "translate-test.z00").write_bytes(b"data")
    with pytest.raises(FileNotFoundError):
        ensure_argos_model._combine_segments(model_dir)

    # Zip part exists but segments missing
    for child in model_dir.iterdir():
        child.unlink()
    (model_dir / "translate-test.zip").write_bytes(b"data")
    with pytest.raises(FileNotFoundError):
        ensure_argos_model._combine_segments(model_dir)


def create_segmented_model(tmp_path: Path) -> Path:
    model_dir = tmp_path / "xx"
    model_dir.mkdir()

    # Create minimal argosmodel with valid metadata
    argosmodel_path = model_dir / "translate-en_xx.argosmodel"
    with zipfile.ZipFile(argosmodel_path, "w") as zf:
        zf.writestr("metadata.json", json.dumps({"from_code": "en", "to_code": "xx"}))

    # Package the argosmodel into a zip archive
    orig_zip = model_dir / "orig.zip"
    with zipfile.ZipFile(orig_zip, "w") as zf:
        zf.write(argosmodel_path, argosmodel_path.name)

    data = orig_zip.read_bytes()
    split_at = len(data) // 2 or 1
    (model_dir / "translate-en_xx.z00").write_bytes(data[:split_at])
    (model_dir / "translate-en_xx.zip").write_bytes(data[split_at:])

    argosmodel_path.unlink()
    orig_zip.unlink()
    return model_dir


def test_combine_and_extract_segments(tmp_path):
    model_dir = create_segmented_model(tmp_path)

    model_zip = ensure_argos_model._combine_segments(model_dir)
    assert model_zip.is_file()

    argosmodel = ensure_argos_model._extract_archive(model_zip)
    assert argosmodel.is_file()
    assert not model_zip.exists()

    with zipfile.ZipFile(argosmodel, "r") as zf:
        metadata = json.loads(zf.read("metadata.json").decode("utf-8"))
    assert metadata == {"from_code": "en", "to_code": "xx"}


def test_metadata_mismatch_raises(tmp_path):
    argosmodel = tmp_path / "translate-en_xx.argosmodel"
    with zipfile.ZipFile(argosmodel, "w") as zf:
        zf.writestr("metadata.json", json.dumps({"from_code": "en", "to_code": "yy"}))

    with pytest.raises(RuntimeError):
        ensure_argos_model._verify_metadata(argosmodel, "xx")
