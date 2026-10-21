from pathlib import Path
import sys
from types import SimpleNamespace as NS
import unittest
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from smoke_motor_environment import validate_spec

class ContractTests(unittest.TestCase):
    def spec(self, shapes=((20,),), continuous=1, discrete=0):
        return NS(observation_specs=[NS(shape=s) for s in shapes],
                  action_spec=NS(continuous_size=continuous, discrete_size=discrete))

    def test_correct_contract(self):
        validate_spec(self.spec())

    def test_wrong_stacking_or_extra_sensor_rejected(self):
        for shapes in (((5,),), ((20,), (1,)), ((4, 5),)):
            with self.subTest(shapes=shapes), self.assertRaises(ValueError):
                validate_spec(self.spec(shapes=shapes))

    def test_wrong_actions_rejected(self):
        for continuous, discrete in ((0, 1), (2, 0), (1, 1)):
            with self.subTest(continuous=continuous, discrete=discrete), self.assertRaises(ValueError):
                validate_spec(self.spec(continuous=continuous, discrete=discrete))
