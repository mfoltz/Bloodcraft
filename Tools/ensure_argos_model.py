#!/usr/bin/env python3
"""Reconstruct and install Argos translation models.

Ensures the Argos model for a given target language code is installed.
If ``argostranslate`` reports the model missing, split archives under
``Resources/Localization/Models/<code>`` are combined, extracted, and
installed automatically.
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
from pathlib import Path
import zipfile

from argostranslate import translate as argos_translate
import argostranslate.package as argos_package


def _combine_segments(model_dir: Path) -> Path:
    """Combine split ``translate-*.z??`` parts into ``model.zip``.

    Returns the path to the combined zip archive.
    """
    segments = sorted(model_dir.glob("translate-*.z[0-9][0-9]"))
    zip_part = next(model_dir.glob("translate-*.zip"), None)
    if not segments or zip_part is None:
        raise FileNotFoundError(
            f"Missing model segments in {model_dir}. Re-clone or download the model files."
        )
    model_zip = model_dir / "model.zip"
    with model_zip.open("wb") as out:
        for seg in segments:
            out.write(seg.read_bytes())
        out.write(zip_part.read_bytes())
    return model_zip


def _extract_archive(model_zip: Path) -> Path:
    """Extract ``model.zip`` and return the resulting ``.argosmodel`` path."""
    subprocess.run(["unzip", "-o", model_zip.name], cwd=model_zip.parent, check=True)
    argosmodel = next(model_zip.parent.glob("translate-*.argosmodel"), None)
    if argosmodel is None:
        raise FileNotFoundError(
            f"Failed to extract Argos model from {model_zip}."
        )
    return argosmodel


def _verify_metadata(argosmodel: Path, code: str) -> None:
    """Confirm the ``from_code`` and ``to_code`` match ``en`` and ``code``."""
    with zipfile.ZipFile(argosmodel, "r") as zf:
        metadata_name = next(
            (n for n in zf.namelist() if n.endswith("metadata.json")), None
        )
        if metadata_name is None:
            raise RuntimeError("metadata.json not found in Argos model")
        metadata = json.loads(zf.read(metadata_name).decode("utf-8"))
    from_code = metadata.get("from_code")
    to_code = metadata.get("to_code")
    if from_code != "en" or to_code != code:
        raise RuntimeError(
            f"Model metadata mismatch: expected en->{code}, got {from_code}->{to_code}"
        )


def ensure_model(code: str, root: Path) -> bool:
    """Ensure Argos model for ``en`` -> ``code`` is installed.

    Returns ``True`` if installation occurred, ``False`` if already installed.
    """
    argos_translate.load_installed_languages()
    translator = argos_translate.get_translation_from_codes("en", code)
    if translator is not None:
        return False

    model_dir = root / "Resources" / "Localization" / "Models" / code
    if not model_dir.is_dir():
        raise FileNotFoundError(f"Model directory {model_dir} does not exist")

    model_zip = _combine_segments(model_dir)
    argosmodel = _extract_archive(model_zip)
    _verify_metadata(argosmodel, code)
    argos_package.install_from_path(str(argosmodel))
    argos_translate.load_installed_languages()
    if argos_translate.get_translation_from_codes("en", code) is None:
        raise RuntimeError(f"Failed to install Argos model for en->{code}")
    return True


def main() -> None:
    ap = argparse.ArgumentParser(
        description="Reconstruct and install the Argos model for a target language code"
    )
    ap.add_argument("code", help="Target language ISO code")
    ap.add_argument(
        "--root",
        default=Path(__file__).resolve().parents[1],
        help="Repository root (defaults to project root)",
    )
    args = ap.parse_args()
    root = Path(args.root).resolve()
    installed = ensure_model(args.code, root)
    if installed:
        print(f"Installed Argos model for en->{args.code}")
    else:
        print(f"Argos model for en->{args.code} already installed")


if __name__ == "__main__":
    main()
