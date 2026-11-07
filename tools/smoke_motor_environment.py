"""Exercise a real CellsMotorV1 environment before training (requires mlagents-envs).

Run without --build to connect to Unity Editor; press Play when prompted.
This is an interface/lifecycle check with constant actions, not training or evaluation.
"""
import argparse
import json
import math


def validate_spec(spec):
    shapes = [tuple(obs.shape) for obs in spec.observation_specs]
    if shapes != [(20,)]:
        raise ValueError(f'Expected one 20-float stacked observation; got {shapes}')
    if spec.action_spec.continuous_size != 1 or spec.action_spec.discrete_size != 0:
        raise ValueError('Expected one continuous action and no discrete actions')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--build', help='Optional Unity executable path; otherwise use Editor')
    parser.add_argument('--episodes', type=int, default=3)
    parser.add_argument('--max-steps', type=int, default=10000)
    parser.add_argument('--activation', type=float, default=.75)
    parser.add_argument('--seed', type=int, default=631)
    args = parser.parse_args()
    if args.episodes <= 0 or args.max_steps <= 0 or not math.isfinite(args.activation) or not 0 <= args.activation <= 1:
        parser.error('episodes/steps must be positive; activation must be finite and in [0,1]')
    # Lazy import lets --help and contract unit tests work without trainer dependencies.
    try:
        import numpy as np
        from mlagents_envs.environment import UnityEnvironment
        from mlagents_envs.base_env import ActionTuple
    except ImportError:
        parser.exit(2, 'Install the documented ML-Agents Python environment first.\n')
    env = None
    try:
        if not args.build:
            print('Waiting for Unity Editor: open MotorTraining, set Behavior Type Default, then press Play.', flush=True)
        env = UnityEnvironment(file_name=args.build, seed=args.seed, timeout_wait=60)
        env.reset()
        names = list(env.behavior_specs)
        if len(names) != 1 or names[0].split('?')[0] != 'CellsMotorV1':
            raise ValueError(f'Expected only CellsMotorV1; got {names}')
        name = names[0]
        validate_spec(env.behavior_specs[name])
        completed = []
        rewards = {}
        for step in range(args.max_steps):
            decisions, terminals = env.get_steps(name)
            for group in (decisions, terminals):
                if len(group.obs) != 1 or group.obs[0].shape != (len(group), 20):
                    raise ValueError('Runtime observation shape changed')
                if not np.isfinite(group.obs[0]).all() or not np.isfinite(group.reward).all():
                    raise ValueError('Nonfinite observation/reward from Unity')
                for agent_id, reward in zip(group.agent_id, group.reward):
                    key = int(agent_id)
                    rewards[key] = rewards.get(key, 0.) + float(reward)
            for index, agent_id in enumerate(terminals.agent_id):
                completed.append({'return': rewards.pop(int(agent_id), 0.),
                                  'interrupted': bool(terminals.interrupted[index])})
            if len(completed) >= args.episodes:
                print(json.dumps({'schema': 'cells.motor-smoke.v1', 'status': 'passed',
                                  'environmentSteps': step, 'episodes': completed,
                                  'activation': args.activation,
                                  'note': 'Real environment interface check only; no policy was trained.'}, indent=2))
                return
            if len(decisions):
                actions = np.full((len(decisions), 1), 2 * args.activation - 1, dtype=np.float32)
                env.set_actions(name, ActionTuple(continuous=actions))
            env.step()
        raise ValueError(f'Only {len(completed)} completed episodes within {args.max_steps} steps')
    except Exception as error:
        parser.exit(2, f'Smoke test failed: {error}\n')
    finally:
        if env is not None:
            env.close()


if __name__ == '__main__':
    main()
