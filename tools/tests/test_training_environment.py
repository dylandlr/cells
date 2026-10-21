from pathlib import Path
import sys
import unittest
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from check_training_environment import check_versions, EXPECTED

class EnvironmentTests(unittest.TestCase):
    def test_pinned_environment(self):
        self.assertEqual(check_versions((3, 10, 12), EXPECTED), [])

    def test_wrong_python(self):
        for version in ((3, 11, 0), (3, 10, 13), (3, 9, 12)):
            with self.subTest(version=version):
                self.assertTrue(check_versions(version, EXPECTED))

    def test_missing_and_mismatched_packages(self):
        self.assertEqual(len(check_versions((3, 10, 12), {})), 2)
        self.assertTrue(check_versions((3, 10, 12), {'mlagents': '1.1.0', 'mlagents-envs': '1.0.0'}))
