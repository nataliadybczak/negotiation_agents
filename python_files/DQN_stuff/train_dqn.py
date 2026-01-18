import os
import gym

from mlagents_envs.environment import UnityEnvironment
from mlagents_envs.envs.unity_gym_env import UnityToGymWrapper
from stable_baselines3 import DQN
from stable_baselines3.common.callbacks import CheckpointCallback
from stable_baselines3.common.monitor import Monitor
from stable_baselines3.common.vec_env import DummyVecEnv, VecNormalize
from mlagents_envs.side_channel.engine_configuration_channel import EngineConfigurationChannel

from shimmy.openai_gym_compatibility import GymV21CompatibilityV0


class UnityVectorObservationWrapper(gym.Wrapper):
    """
    Ten wrapper naprawia bląd "NotImplementedError: Tuple(Box...".
    Wyjmuje wektor obserwacji z tupli, która zwraca Unity.
    """
    def __init__(self, env):
        super().__init__(env)
        self.observation_space = env.observation_space[0]

    def reset(self, **kwargs):
        obs = self.env.reset(**kwargs)

        if isinstance(obs, (tuple, list)):
            return obs[0]
        return obs

    def step(self, action):
        obs, reward, done, info = self.env.step(action)

        if isinstance(obs, (tuple, list)):
            obs = obs[0]
        return obs, reward, done, info

def main():
    unity_env_path = r"/Build/ML-Agents-Project.exe"

    models_dir = "models/DQN"
    log_dir = "logs_dqn"

    os.makedirs(models_dir, exist_ok=True)
    os.makedirs(log_dir, exist_ok=True)

    print("1. Uruchamiam środowisko Unity...")

    channel = EngineConfigurationChannel()

    channel.set_configuration_parameters(width=80, height=80, quality_level=1,
                                         time_scale=20.0, target_frame_rate=-1)

    unity_env = UnityEnvironment(file_name=unity_env_path, seed=1, no_graphics=True,
                                 worker_id=1, side_channels=[channel])

    print("2. Konwertuję środowisko do formatu Gym...")
    env = UnityToGymWrapper(
        unity_env,
        uint8_visual=False,
        allow_multiple_obs=True
    )

    env = UnityVectorObservationWrapper(env)
    env = GymV21CompatibilityV0(env=env)

    env = Monitor(env, log_dir)

    env = DummyVecEnv([lambda: env])
    env = VecNormalize(env, norm_obs=True, norm_reward=True, clip_obs=10.)

    print("3. Inicjalizuję Jacka (DQN Optimized)...")
    model = DQN(
        "MlpPolicy",
        env,
        verbose=1,
        tensorboard_log=log_dir,
        learning_rate=0.0001,
        buffer_size=100000,
        learning_starts=1000,
        batch_size=64,
        gamma=0.99,
        exploration_fraction=0.3,
        target_update_interval=1000
    )

    checkpoint_callback = CheckpointCallback(save_freq=10000, save_path=models_dir, name_prefix="dqn_model")

    print("4. ROZPOCZYNAM TRENING!")
    model.learn(total_timesteps=100000, callback=checkpoint_callback)

    print("5. Trening zakończony. Zapisuję finalny model.")
    model.save(f"{models_dir}/dqn_negotiation_final")

    env.close()
    print("Środowisko zamknięte.")


if __name__ == '__main__':
    main()