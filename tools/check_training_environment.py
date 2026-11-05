"""Check the Python environment for the pinned ML-Agents release before launch."""
import argparse
from importlib import metadata
import json
import sys

EXPECTED = {'mlagents': '1.1.0', 'mlagents-envs': '1.1.0'}


def check_versions(python_version, installed):
    errors = []
    if not (python_version[:2] == (3, 10) and 1 <= python_version[2] <= 12):
        errors.append('Use Python 3.10.1–3.10.12 for this pinned workflow (recommended: 3.10.12).')
    for package, expected in EXPECTED.items():
        actual = installed.get(package)
        if actual != expected:
            errors.append(f'{package}: expected {expected}, found {actual or "not installed"}')
    return errors


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.parse_args()
    installed = {}
    for name in (*EXPECTED, 'torch', 'numpy', 'protobuf'):
        try:
            installed[name] = metadata.version(name)
        except metadata.PackageNotFoundError:
            installed[name] = None
    errors = check_versions(sys.version_info[:3], installed)
    # Import the real entry points to expose incompatible transitive dependencies.
    # No environment is launched and no network connection is attempted here.
    if not errors:
        try:
            from mlagents_envs.environment import UnityEnvironment
            from mlagents.trainers.learn import main as trainer_main
            import torch
            import numpy
        except Exception as error:
            errors.append(f'Training dependency import failed: {type(error).__name__}: {error}')
    print(json.dumps({'pythonExecutable': sys.executable,
                      'pythonVersion': '.'.join(map(str, sys.version_info[:3])),
                      'packages': installed, 'ready': not errors, 'errors': errors}, indent=2))
    return 0 if not errors else 2


if __name__ == '__main__':
    sys.exit(main())
