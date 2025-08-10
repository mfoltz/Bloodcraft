import os
import tempfile
import pytest

from translate_argos import ensure_model_installed


def test_raises_when_segments_present_without_model():
    with tempfile.TemporaryDirectory() as root:
        model_dir = os.path.join(
            root, "Resources", "Localization", "Models", "xx"
        )
        os.makedirs(model_dir)
        open(os.path.join(model_dir, "translate-test.z01"), "wb").close()
        with pytest.raises(RuntimeError) as err:
            ensure_model_installed(root, "xx")
        assert "cd Resources/Localization/Models/xx" in str(err.value)
